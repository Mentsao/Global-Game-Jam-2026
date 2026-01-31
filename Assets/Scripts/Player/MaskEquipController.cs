using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace Player
{
    public class MaskEquipController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerPickup playerPickup;
        [SerializeField] private Animator playerAnimator;

        [Header("Settings")]
        [SerializeField] private float animationDuration = 2.0f;
        [SerializeField] private string maskOnState = "MaskOn";
        [SerializeField] private string maskOffState = "MaskOff";

        public bool IsMaskEquipped { get; private set; }
        public bool IsAnimating { get; private set; }

        private void Start()
        {
            if (playerPickup == null)
            {
                playerPickup = GetComponentInParent<PlayerPickup>();
            }
            if (playerAnimator == null)
            {
                // Try to find animator in parent (Player) or children
                 playerAnimator = GetComponentInParent<Animator>(); 
            }

            if (playerAnimator == null) Debug.LogWarning("MaskEquipController: Player Animator not found!");
        }

        private void Update()
        {
            HandleMaskInput();
        }

        public void ResetState()
        {
            IsMaskEquipped = false;
            IsAnimating = false;
        }

        public void ToggleMask()
        {
             if (playerPickup == null) return;
             Transform heldItem = playerPickup.HeldItem;
             if (heldItem == null || IsAnimating) return;
             
             // Check if it's a mask
             if (!heldItem.name.ToLower().Contains("mask")) 
             {
                 Debug.Log("MaskEquipController: Held item is not a mask.");
                 return;
             }

             StartCoroutine(EquipMaskRoutine(heldItem, !IsMaskEquipped));
        }

        private void HandleMaskInput()
        {
            // dependencies check
            if (playerPickup == null) return;
            Transform heldItem = playerPickup.HeldItem;

            // Only check if holding a mask and not currently animating
            if (heldItem == null || IsAnimating) return;
            if (!heldItem.name.ToLower().Contains("mask")) return;

            // Check Left Mouse Button
            bool input = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) input = true;
            else if (Input.GetMouseButtonDown(0)) input = true; // Fallback

            if (input)
            {
                StartCoroutine(EquipMaskRoutine(heldItem, !IsMaskEquipped));
            }
        }

        private IEnumerator EquipMaskRoutine(Transform maskItem, bool equip)
        {
            Animator maskAnim = maskItem.gameObject.GetComponent<Animator>();
            
            IsAnimating = true;

            string stateName = equip ? maskOnState : maskOffState;
            Debug.Log($"MaskEquipController: Playing State {stateName}");

            // If unequipping, make sure the mask is visible first
            if (!equip)
            {
                maskItem.gameObject.SetActive(true);
            }

            // Play animations directly
            if (maskAnim != null) 
            {
                maskAnim.Play(stateName);
            }
            else Debug.LogWarning($"MaskEquipController: No Animator on Mask Item {maskItem.name}");

            if (playerAnimator != null) 
            {
                playerAnimator.Play(stateName);
            }
            else Debug.LogWarning("MaskEquipController: Player Animator is null");

            // Wait for animation to finish
            yield return new WaitForSeconds(animationDuration);

            IsMaskEquipped = equip;
            IsAnimating = false;

            // If equipped, hide the held mask object (assuming it's now on face or hidden)
            if (equip)
            {
                maskItem.gameObject.SetActive(false);
            }
            Debug.Log($"MaskEquipController: Animation Complete. Equipped: {IsMaskEquipped}");
        }
    }
}
