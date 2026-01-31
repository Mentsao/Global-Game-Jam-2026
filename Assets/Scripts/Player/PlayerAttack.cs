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

        [Header("Combat Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip swingSFX;
        [SerializeField] private AudioClip hitSFX;

        [Header("Combat Settings")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float attackRange = 1.5f;

        private bool _isAttacking = false;

        private void Start()
        {
            if (playerPickup == null)
            {
                playerPickup = GetComponent<PlayerPickup>();
            }
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Update()
        {
            // Simple Left Click Check (Supports New Input System via Mouse.current or fallback)
            bool attackInput = false;
            
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
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
            // Allow attack if holding typical weapon items or balisong
            if (heldName.Contains("balisong") || heldName.Contains("knife") || heldName.Contains("weapon") || heldName.Contains("item"))
            {
                StartCoroutine(SlashCoroutine());
            }
        }

        private IEnumerator SlashCoroutine()
        {
            _isAttacking = true;
            
            if (swingSFX != null) audioSource.PlayOneShot(swingSFX);

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
                item.localRotation = Quaternion.Lerp(startRot, targetRot, t);
                item.localPosition = Vector3.Lerp(startPos, targetPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            item.localRotation = targetRot;
            item.localPosition = targetPos;

            // HIT DETECTION at apex of swing
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

            // 2. Return control
            playerPickup.preventRotationUpdate = false;
            _isAttacking = false;
        }
        
        private void CheckForHit()
        {
            if (playerPickup == null || playerPickup.HeldItem == null) return;
            
            // Allow attack if holding typical weapon items or balisong
             string heldName = playerPickup.HeldItem.name.ToLower();
            if (!heldName.Contains("balisong") && !heldName.Contains("knife") && !heldName.Contains("weapon") && !heldName.Contains("item"))
            {
                return;
            }

            // Simple OverlapSphere in front of player
            Transform cam = Camera.main.transform;
            Vector3 attackPos = cam.position + cam.forward * 1.0f; // 1 meter in front
            
            Collider[] hits = Physics.OverlapSphere(attackPos, attackRange, enemyLayer);
            bool hitEnemy = false;
            
            foreach (var hit in hits)
            {
                // Check if it's actually an enemy (tag check fallback if layer is loose)
                // You can add component checks here too, e.g. hit.GetComponent<EnemyHealth>()
                
                // For now, just assume layer is correct per requirements
                Debug.Log($"Hit object: {hit.name}");
                hitEnemy = true;
            }
            
            if (hitEnemy && hitSFX != null)
            {
                audioSource.PlayOneShot(hitSFX);
            }
        }
    }
}
