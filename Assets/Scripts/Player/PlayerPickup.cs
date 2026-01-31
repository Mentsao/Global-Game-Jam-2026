using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerPickup : MonoBehaviour
    {
        [Header("Pickup Settings")]
        [SerializeField] private Transform holdPosition;
        [SerializeField] private Vector3 holdRotation = Vector3.zero;
        [SerializeField] private float pickupRange = 3f;
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        // SFX now handled by AudioManager
        [SerializeField] private LayerMask pickupLayer = ~0; // Default to Everything
        [SerializeField] private System.Collections.Generic.List<string> itemTags = new System.Collections.Generic.List<string> { "Item", "Document" };

        private InputSystem_Actions _inputActions;
        private Transform _heldItem; // Currently active item
        
        // Inventory Slots
        private Transform _weaponSlot;
        private Transform _documentSlot;
        
        private Rigidbody _heldRigidbody;
        private Collider _heldCollider;
        private Transform _cameraTransform;

        public Transform HeldItem => _heldItem;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
            _cameraTransform = Camera.main.transform;
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

        private void Update()
        {
            UpdateHeldItemRotation();
            HandleInput();
        }

        private void HandleInput()
        {
            // Switch to Weapon (Slot 1)
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame || Input.GetKeyDown(KeyCode.Alpha1))
            {
                SwitchToSlot(1);
            }
            
            // Switch to Document (Slot 2)
            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame || Input.GetKeyDown(KeyCode.Alpha2))
            {
                SwitchToSlot(2);
            }
        }

        private void OnValidate()
        {
            UpdateHeldItemRotation();
        }

        public bool preventRotationUpdate = false;

        private void UpdateHeldItemRotation()
        {
            if (preventRotationUpdate) return;

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
            Debug.Log("Interact Pressed");

            // 1. Try to find an item to Interact/Collect first
            if (TryPickupItem()) 
            {
                return;
            }

            // 2. If nothing collected/picked up, try to Drop
            if (_heldItem != null)
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
                
                // 1. Consumables (Mask, Paper but NOT Document)
                if ((lowerName.Contains("mask") || lowerName.Contains("paper")) && !lowerName.Contains("document"))
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
                bool isWeapon = lowerName.Contains("balisong") || lowerName.Contains("knife") || lowerName.Contains("weapon");

                if (isDocument)
                {
                    // Pickup Document
                    PickupToSlot(bestTarget, 2);
                    return true;
                }
                else
                {
                    // Default to Weapon Slot (1) if it's an Item/Weapon/Everything else
                    PickupToSlot(bestTarget, 1);
                    return true;
                }
            }
            
            return false;
        }

        private void PickupToSlot(Transform item, int slotIndex)
        {
            // 1. If slot is occupied, drop current item in that slot
            Transform previousItem = (slotIndex == 1) ? _weaponSlot : _documentSlot;
            
            if (previousItem != null)
            {
                // If we are currently holding the item we are about to replace, drop it properly
                if (_heldItem == previousItem)
                {
                   DropItem(); // Drop physically
                }
                else
                {
                    // If it's in the pocket (inactive), just unparent and enable it at current position
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
            if (slotIndex == 1) _weaponSlot = item;
            else _documentSlot = item;

            // 3. Equip logic (Switch to this slot immediately)
            SwitchToSlot(slotIndex);

            // Play Pickup Audio
            if (AudioManager.Instance != null)
            {
                string lowerName = item.name.ToLower();
                if (lowerName.Contains("balisong") || lowerName.Contains("knife"))
                {
                    AudioManager.Instance.PlayBalisongPickup();
                }
                else if (lowerName.Contains("document"))
                {
                    AudioManager.Instance.PlayDocumentPickup();
                }
            }
        }

        private void SwitchToSlot(int slotIndex)
        {
             // Disable current held item visuals (put in pocket)
            if (_heldItem != null)
            {
                _heldItem.gameObject.SetActive(false);
            }

            // Determine new item
            Transform newItem = (slotIndex == 1) ? _weaponSlot : _documentSlot;

            if (newItem != null)
            {
                _heldItem = newItem;
                _heldItem.gameObject.SetActive(true);
                InitializeHeldItem(_heldItem); // Setup physics/parenting
                Debug.Log($"Switched to Slot {slotIndex}: {_heldItem.name}");
            }
            else
            {
                _heldItem = null;
                Debug.Log($"Switched to Slot {slotIndex}: [Empty]");
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
            item.SetParent(holdPosition);
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
            // Legacy wrapper - redirects to slot 1 for now if called internally
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

            // Unparent
            _heldItem.SetParent(null);

            // Clear ref
            _heldItem = null;
            _heldRigidbody = null;
            _heldCollider = null;
        }
    }
}
