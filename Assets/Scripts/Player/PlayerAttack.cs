using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerAttack : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerPickup playerPickup;

        [Header("Settings")]
        [SerializeField] private Vector3 slashRotation = new Vector3(80f, 0f, 0f); // Tweak angle for good feel
        [SerializeField] private Vector3 slashMoveOffset = new Vector3(0.5f, -0.2f, 0.5f); // Move forward and slightly down/side
        [SerializeField] private float slashDuration = 0.25f;

        [Header("Combat Settings")]
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private LayerMask attackLayer = ~0; // Default to Everything

        private Transform _camTransform;
        private bool _isAttacking = false;

        private void Awake()
        {
            if (Camera.main != null) _camTransform = Camera.main.transform;
        }

        private void Start()
        {
            if (playerPickup == null)
            {
                playerPickup = GetComponent<PlayerPickup>();
            }
        }

        private void Update()
        {
            // Simple Left Click Check (Supports New Input System via Mouse.current or fallback)
            bool attackInput = false;
            
            if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame))
            {
                attackInput = true;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                attackInput = true;
            }

            if (attackInput && !_isAttacking)
            {
                TryAttack();
            }
        }

        private void TryAttack()
        {
            if (playerPickup == null || playerPickup.HeldItem == null) return;

            string heldName = playerPickup.HeldItem.name.ToLower();
            
            // Prevent attack if holding a mask
            if (heldName.Contains("mask")) return;

            // Allow attack if holding typical weapon items or balisong
            if (heldName.Contains("balisong") || heldName.Contains("knife") || heldName.Contains("weapon") || heldName.Contains("item"))
            {
                StartCoroutine(SlashCoroutine());
            }
        }

        private IEnumerator SlashCoroutine()
        {
            _isAttacking = true;

            // Updated to use AudioManager
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPlayerAttack();
            }
            
            // 1. Take control
            playerPickup.preventRotationUpdate = true;

            Transform item = playerPickup.HeldItem;
            
            // Rotation
            Quaternion startRot = item.localRotation;
            Quaternion targetRot = startRot * Quaternion.Euler(slashRotation); 
            
            // Position
            Vector3 startPos = item.localPosition; // Should be Vector3.zero usually
            Vector3 targetPos = startPos + slashMoveOffset;

            float elapsed = 0f;
            float halfDuration = slashDuration * 0.5f;

            // Forward Swing (Fast)
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                item.localRotation = Quaternion.Lerp(startRot, targetRot, t);
                item.localPosition = Vector3.Lerp(startPos, targetPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            item.localRotation = targetRot;
            item.localPosition = targetPos;

            // HIT CHECK AT APEX
            CheckForHit();

            // Return Swing (Fast)
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                item.localRotation = Quaternion.Lerp(targetRot, startRot, t);
                item.localPosition = Vector3.Lerp(targetPos, startPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            item.localRotation = startRot;
            item.localPosition = startPos;

            playerPickup.preventRotationUpdate = false;
            _isAttacking = false;
        }

        private void CheckForHit()
        {
            if (_camTransform == null)
            {
                 if (Camera.main != null) _camTransform = Camera.main.transform;
                 if (_camTransform == null) return;
            }

            RaycastHit hit;
            // Raycast slightly forward from camera
            if (Physics.Raycast(_camTransform.position, _camTransform.forward, out hit, attackRange, attackLayer))
            {
                // Debug.Log($"[Attack] Hit: {hit.collider.name}");

                // 1. Check for Government Official (Specific directional logic)
                GovernmentOfficial gov = hit.transform.GetComponent<GovernmentOfficial>();
                if (gov == null) gov = hit.transform.GetComponentInParent<GovernmentOfficial>();
                
                if (gov != null)
                {
                    // Scale damage: NPCHealth is usually small int (3), Gov is float (100). 
                    // Assuming attackDamage 1 = 1/3 of a normal enemy. 
                    // So 1 damage -> ~34 damage to Gov (3 hits to kill).
                    float dmg = (float)attackDamage * 34f; 
                    gov.TakeDamage(dmg, transform); 
                    
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayImpact();
                    return;
                }

                // 2. Check for Generic NPC Health
                NPCHealth npc = hit.transform.GetComponent<NPCHealth>();
                if (npc == null) npc = hit.transform.GetComponentInParent<NPCHealth>();

                if (npc != null)
                {
                    npc.TakeDamage(attackDamage);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayImpact();
                }
            }
        }
    }
}
