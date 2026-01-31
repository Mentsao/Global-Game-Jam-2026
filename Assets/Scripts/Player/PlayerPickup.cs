using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerPickup : MonoBehaviour
    {
        [Header("Pickup Settings")]
        [SerializeField] private Transform holdPosition;
        [SerializeField] private Transform weaponHoldSlot;
        [SerializeField] private Transform maskHoldSlot;
        [SerializeField] private Vector3 holdRotation = Vector3.zero;
        [SerializeField] private float pickupRange = 3f;
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        // SFX now handled by AudioManager
        [SerializeField] private LayerMask pickupLayer = ~0; // Default to Everything
        [SerializeField] private System.Collections.Generic.List<string> itemTags = new System.Collections.Generic.List<string> { "Item", "Document", "Mask" };

        private InputSystem_Actions _inputActions;
        private Transform _heldItem; // Currently active item
        
        // Inventory Slots
        private Transform _weaponSlot;   // Key 1
        private Transform _documentSlot; // Key 2
        private Transform _maskSlot1;    // Key 3
        private Transform _maskSlot2;    // Key 4
        
        private Rigidbody _heldRigidbody;
        private Collider _heldCollider;
        private Transform _cameraTransform;

        public Transform HeldItem => _heldItem;

        [Header("Tutorial Checks")]
        public bool isWeapon = false;
        PlayerDeath playerDeath;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
            _cameraTransform = Camera.main.transform;
            playerDeath = GetComponent<PlayerDeath>();
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
            _inputActions.Player.Interact.performed += OnInteract;
        }

        private void OnDisable()
        {
            _inputActions.Player.Interact.performed -= OnInteract;
            _inputActions.Player.Disable();
            
            // Safety: Drop item if script is disabled
            // if (_heldItem != null) DropItem(); 
        }



        private void HandleInput()
        {
            // Slot 1: Weapon
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame || Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSlot(1);
            
            // Slot 2: Document
            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame || Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSlot(2);

            // Slot 3: Mask 1
            if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame || Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSlot(3);

            // Slot 4: Mask 2
            if (Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame || Input.GetKeyDown(KeyCode.Alpha4)) SwitchToSlot(4);
        }

        private void OnValidate()
        {
            UpdateHeldItemRotation();
        }

        public bool preventRotationUpdate = false;
        private bool _isMaskEquipped = false;
        private bool _isAnimatingMask = false;
        
        // Unified Mask System
        public Items.Masks.MaskType CurrentMaskType { get; private set; } = Items.Masks.MaskType.None;

        private void UpdateHeldItemRotation()
        {
            if (preventRotationUpdate || _isAnimatingMask || _isMaskEquipped) return;

            if (_heldItem != null)
            {
                // Check if the item has specific settings
                Interaction.PickupableItem itemSettings = _heldItem.GetComponent<Interaction.PickupableItem>();
                
                if (itemSettings != null)
                {
                    _heldItem.localRotation = Quaternion.Euler(itemSettings.holdRotation);
                    _heldItem.localPosition = itemSettings.holdPositionOffset;
                }
                else
                {
                    // Fallback to global setting
                    _heldItem.localRotation = Quaternion.Euler(holdRotation);
                    _heldItem.localPosition = Vector3.zero;
                }
            }
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            // Debug.Log("Interact Pressed");

            // 1. Try to find an item to Interact/Collect first
            if (TryPickupItem()) 
            {
                return;
            }

            // 2. If nothing collected/picked up, try to Drop
            if (_heldItem != null && !_isMaskEquipped && !_isAnimatingMask)
            {
                // Prevent dropping if it's the Balisong
                // string heldName = _heldItem.name.ToLower();
                // if (heldName.Contains("balisong"))
                // {
                //    Debug.Log("Cannot drop Balisong!");
                //    return;
                // }
                
                DropItem();
            }
         }


        private bool TryPickupItem()
        {
            // Use OverlapSphere to find items near the center of the screen
            Vector3 sphereCenter = _cameraTransform.position + _cameraTransform.forward * (pickupRange * 0.5f);
            Collider[] colliders = Physics.OverlapSphere(sphereCenter, pickupRange * 0.5f, pickupLayer);

            Transform bestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (Collider collider in colliders)
            {
                // Debug.Log($"Found object in range: {collider.name} (Tag: {collider.tag})"); // DEBUG
                if (itemTags.Contains(collider.tag))
                {
                    float distance = Vector3.Distance(_cameraTransform.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestTarget = collider.transform;
                    }
                }
            }

            if (bestTarget != null)
            {
                string lowerName = bestTarget.name.ToLower();
                
                // 1. Consumables (Paper but NOT Document)
                if (lowerName.Contains("paper") && !lowerName.Contains("document"))
                {
                    if (UI.InventoryUI.Instance != null)
                    {
                        UI.InventoryUI.Instance.UpdateItemStatus(bestTarget.name, true);
                    }
                    Debug.Log($"Consumed Item: {bestTarget.name}");
                    Destroy(bestTarget.gameObject);
                    return true;
                }

                // 2. Determine Slot Type
                bool isDocument = lowerName.Contains("document");
                bool isMask = lowerName.Contains("mask");
                isWeapon = lowerName.Contains("balisong") || lowerName.Contains("knife") || lowerName.Contains("weapon");

                if (isDocument)
                {
                    PickupToSlot(bestTarget, 2);
                    playerDeath.DocumentFound();
                    return true;
                }
                else if (isMask)
                {
                    // Find first empty mask slot or replace current if full
                    int targetSlot = 3; // Default to first mask slot
                    if (_maskSlot1 == null) targetSlot = 3;
                    else if (_maskSlot2 == null) targetSlot = 4;
                    else
                    {
                         // All full: Replace currently held mask key, or default to 3
                         // For simplicity, just overwrite Mask 1 if full for now
                         targetSlot = 3;
                    }
                    
                    PickupToSlot(bestTarget, targetSlot);
                    return true;
                }
                else
                {
                    // Default to Weapon Slot (1)
                    PickupToSlot(bestTarget, 1);
                    return true;
                }
            }
            
            return false;
        }

        private void PickupToSlot(Transform item, int slotIndex)
        {
            // 1. Determine which slot variable we are targeting
            Transform previousItem = GetItemInSlot(slotIndex);
            
            if (previousItem != null)
            {
                // If we are currently holding the item we are about to replace, drop it
                if (_heldItem == previousItem)
                {
                   DropItem(); 
                }
                else
                {
                    // Eject from pocket
                    previousItem.SetParent(null);
                    previousItem.gameObject.SetActive(true);
                    
                    // Enable Physics
                    Rigidbody rb = previousItem.GetComponent<Rigidbody>();
                    if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }
                    Collider col = previousItem.GetComponent<Collider>();
                    if (col != null) col.enabled = true;
                }
            }

            // 2. Assign logic
            SetItemInSlot(slotIndex, item);

            // 3. Equip logic 
            SwitchToSlot(slotIndex);

            // Play Pickup Audio
            if (AudioManager.Instance != null && item != null)
            {
                string lowerName = item.name.ToLower();
                if (lowerName.Contains("balisong") || lowerName.Contains("knife")) AudioManager.Instance.PlayBalisongPickup();
                else if (lowerName.Contains("document")) AudioManager.Instance.PlayDocumentPickup();
                // else Mask pickup sound?
            }
        }

        private void SwitchToSlot(int slotIndex)
        {
             // Disable current held item visuals
            if (_heldItem != null)
            {
                _heldItem.gameObject.SetActive(false);
            }

            // Reset Mask State
            _isMaskEquipped = false;
            _isAnimatingMask = false;
            CurrentMaskType = Items.Masks.MaskType.None; // Reset Mask Type on switch

            // Determine new item
            Transform newItem = GetItemInSlot(slotIndex);

            if (newItem != null)
            {
                _heldItem = newItem;
                _heldItem.gameObject.SetActive(true);
                InitializeHeldItem(_heldItem); 
                Debug.Log($"Switched to Slot {slotIndex}: {_heldItem.name}");
            }
            else
            {
                _heldItem = null;
                Debug.Log($"Switched to Slot {slotIndex}: [Empty]");
            }
        }

        private void Update()
        {
            UpdateHeldItemRotation();
            HandleInput();
            HandleMaskInput();
        }

        private void HandleMaskInput()
        {
            // Only check if holding a mask and not currently animating
            if (_heldItem == null || _isAnimatingMask) return;
            if (!_heldItem.name.ToLower().Contains("mask")) return;

            // Check Left Mouse Button
            bool input = false;
             if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) input = true;
             else if (Input.GetMouseButtonDown(0)) input = true; // Fallback

            if (input)
            {
                StartCoroutine(EquipMaskRoutine(!_isMaskEquipped));
            }
        }

        private System.Collections.IEnumerator EquipMaskRoutine(bool equip)
        {
            _isAnimatingMask = true;
            
            // Get Animator from the Mask Hold Slot
            Animator maskSlotAnim = null;
            if (maskHoldSlot != null) maskSlotAnim = maskHoldSlot.GetComponent<Animator>();

            if (maskSlotAnim != null)
            {
                if (equip)
                {
                    // "MaskOn" must be the exact name of the Animation STATE in the Animator
                    maskSlotAnim.Play("MaskOn", 0, 0f); 
                }
                else
                {
                    // If Unequipping, show the mask first so it can animate off
                     if (_heldItem != null) _heldItem.gameObject.SetActive(true);
                     // "MaskOff" must be the exact name of the Animation STATE
                    maskSlotAnim.Play("MaskOff", 0, 0f);
                }
            }
            else
            {
                Debug.LogWarning("PlayerPickup: No Animator found on Mask Hold Slot!");
            }

            //// Wait for animation duration (adjust as needed or check clip length)
            yield return new WaitForSeconds(0.25f);

            _isMaskEquipped = equip;
            _isAnimatingMask = false;
            
            // If Equipped (and animation finished), hide the mask object (assuming off-screen or on face model)
            if (equip && _heldItem != null)
            {
                _heldItem.gameObject.SetActive(false);
                
                // Update Current Mask Type
                Items.Masks.MaskItem maskItem = _heldItem.GetComponent<Items.Masks.MaskItem>();
                if (maskItem != null)
                {
                    CurrentMaskType = maskItem.Type;
                    Debug.Log($"[Mask] Equipped: {CurrentMaskType}");
                }
            }
            else if (!equip)
            {
                // Unequipped
                CurrentMaskType = Items.Masks.MaskType.None;
                Debug.Log("[Mask] Unequipped");
            }
        }


        private Transform GetItemInSlot(int slotIndex)
        {
            switch(slotIndex)
            {
                case 1: return _weaponSlot;
                case 2: return _documentSlot;
                case 3: return _maskSlot1;
                case 4: return _maskSlot2;
                default: return null;
            }
        }

        private void SetItemInSlot(int slotIndex, Transform item)
        {
            switch(slotIndex)
            {
                case 1: _weaponSlot = item; break;
                case 2: _documentSlot = item; break;
                case 3: _maskSlot1 = item; break;
                case 4: _maskSlot2 = item; break;
            }
        }

        private void InitializeHeldItem(Transform item)
        {
            _heldRigidbody = item.GetComponent<Rigidbody>();
            _heldCollider = item.GetComponent<Collider>();

            // Disable Physics
            if (_heldRigidbody != null)
            {
                _heldRigidbody.isKinematic = true;
                _heldRigidbody.useGravity = false;
            }

            // Disable Collision
            if (_heldCollider != null)
            {
                _heldCollider.enabled = false;
            }

            // Update UI
            if (UI.InventoryUI.Instance != null)
            {
                UI.InventoryUI.Instance.UpdateItemStatus(item.name, true);
            }

            // Parent to Hold Position
            Transform itemParent = holdPosition; // Default

            if (item == _weaponSlot && weaponHoldSlot != null)
            {
                itemParent = weaponHoldSlot;
            }
            else if ((item == _maskSlot1 || item == _maskSlot2) && maskHoldSlot != null)
            {
                itemParent = maskHoldSlot;
            }

            item.SetParent(itemParent);
            item.localPosition = Vector3.zero;
            UpdateHeldItemRotation();
        }

        private void OnDrawGizmosSelected()
        {
            if (Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                Vector3 sphereCenter = cam.position + cam.forward * (pickupRange * 0.5f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(sphereCenter, pickupRange * 0.5f);
            }
        }

        private void PickupItem(Transform item) 
        {
            PickupToSlot(item, 1);
        }

        public void ConsumeHeldItem()
        {
            if (_heldItem == null) return;

            // Update UI
            if (UI.InventoryUI.Instance != null)
            {
                UI.InventoryUI.Instance.UpdateItemStatus(_heldItem.name, false);
            }

            // Remove from Slot Reference
            if (_heldItem == _weaponSlot) _weaponSlot = null;
            if (_heldItem == _documentSlot) _documentSlot = null;
            if (_heldItem == _maskSlot1) _maskSlot1 = null;
            if (_heldItem == _maskSlot2) _maskSlot2 = null;

            Destroy(_heldItem.gameObject);

            // Clear ref
            _heldItem = null;
            _heldRigidbody = null;
            _heldCollider = null;
        }

        private void DropItem()
        {
            if (_heldItem == null) return;

            // Enable Physics
            if (_heldRigidbody != null)
            {
                _heldRigidbody.isKinematic = false;
                _heldRigidbody.useGravity = true;
            }

            // Enable Collision
            if (_heldCollider != null)
            {
                _heldCollider.enabled = true;
            }

            // Update UI
            if (UI.InventoryUI.Instance != null)
            {
                UI.InventoryUI.Instance.UpdateItemStatus(_heldItem.name, false);
            }

            // Remove from Slot Reference
            if (_heldItem == _weaponSlot) _weaponSlot = null;
            if (_heldItem == _documentSlot) _documentSlot = null;
            if (_heldItem == _maskSlot1) _maskSlot1 = null;
            if (_heldItem == _maskSlot2) _maskSlot2 = null;

            // Unparent
            _heldItem.SetParent(null);

            // Clear ref
            _heldItem = null;
            _heldRigidbody = null;
            _heldCollider = null;
        }
    }
}
