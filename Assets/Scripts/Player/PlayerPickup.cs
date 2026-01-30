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
        [SerializeField] private LayerMask pickupLayer = ~0; // Default to Everything
        [SerializeField] private System.Collections.Generic.List<string> itemTags = new System.Collections.Generic.List<string> { "Item", "Document" };

        private InputSystem_Actions _inputActions;
        private Transform _heldItem;
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
            if (_heldItem != null) DropItem();
        }

        private void Update()
        {
            UpdateHeldItemRotation();
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
                string heldName = _heldItem.name.ToLower();
                if (heldName.Contains("balisong"))
                {
                    Debug.Log("Cannot drop Balisong!");
                    return;
                }
                
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
                Debug.Log($"Found object in range: {collider.name} (Tag: {collider.tag})"); // DEBUG
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
                // 1. Check for Document SPECIFICALLY
                // (We want to HOLD the document, not consume it)
                if (lowerName.Contains("document"))
                {
                     if (_heldItem == null)
                    {
                        Debug.Log($"Picking up Document: {bestTarget.name}");
                        PickupItem(bestTarget);
                        return true;
                    }
                    else
                    {
                        Debug.Log("Cannot pick up Document: Hands Full!");
                        return false;
                    }
                }

                // 2. Check for "consumable" inventory item (Masks, Paper)
                // Note: If the document was named "Paper Document", the above check catches it first.
                if (lowerName.Contains("mask") || lowerName.Contains("paper"))
                {
                    if (UI.InventoryUI.Instance != null)
                    {
                        UI.InventoryUI.Instance.UpdateItemStatus(bestTarget.name, true);
                    }
                    Debug.Log($"Consumed Item: {bestTarget.name}");
                    Destroy(bestTarget.gameObject);
                    return true;
                }
                else
                {
                    // 3. Physical item (e.g. Balisong/Knife/Weapon)
                    // If hands are empty, pickup. If full, return false (so we can drop or do nothing)
                    if (_heldItem == null)
                    {
                        PickupItem(bestTarget);
                        return true;
                    }
                    
                    return false;
                }
            }
            
            return false;
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
            _heldItem = item;
            _heldRigidbody = item.GetComponent<Rigidbody>();
            _heldCollider = item.GetComponent<Collider>();

            // Disable Physics
            if (_heldRigidbody != null)
            {
                _heldRigidbody.isKinematic = true;
                _heldRigidbody.useGravity = false;
            }

            // Disable Collision (optional, prevents pushing player)
            if (_heldCollider != null)
            {
                _heldCollider.enabled = false;
            }

            // Update UI
            if (UI.InventoryUI.Instance != null)
            {
                UI.InventoryUI.Instance.UpdateItemStatus(_heldItem.name, true);
            }

            // Parent to Hold Position
            _heldItem.SetParent(holdPosition);
            _heldItem.localPosition = Vector3.zero;
            UpdateHeldItemRotation();
        }

        public void ConsumeHeldItem()
        {
            if (_heldItem == null) return;

            // Update UI (optional: remove item from UI)
            if (UI.InventoryUI.Instance != null)
            {
                // We pass false to indicate it's no longer held/available
                UI.InventoryUI.Instance.UpdateItemStatus(_heldItem.name, false);
            }

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
                // Add diverse throw force? For now just drop.
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

            // Unparent
            _heldItem.SetParent(null);

            // Clear ref
            _heldItem = null;
            _heldRigidbody = null;
            _heldCollider = null;
        }
    }
}
