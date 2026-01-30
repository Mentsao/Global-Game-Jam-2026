using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerPickup : MonoBehaviour
    {
        [Header("Pickup Settings")]
        [SerializeField] private Transform holdPosition;
        [SerializeField] private float pickupRange = 3f;
        [SerializeField] private LayerMask pickupLayer = ~0; // Default to Everything
        [SerializeField] private string itemTag = "Item";

        private InputSystem_Actions _inputActions;
        private Transform _heldItem;
        private Rigidbody _heldRigidbody;
        private Collider _heldCollider;
        private Transform _cameraTransform;

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

        private void OnInteract(InputAction.CallbackContext context)
        {
            Debug.Log("Interact Pressed");
            if (_heldItem != null)
            {
                DropItem();
            }
            else
            {
                TryPickupItem();
            }
        }

        private void TryPickupItem()
        {
            // Use OverlapSphere to find items near the center of the screen
            Vector3 sphereCenter = _cameraTransform.position + _cameraTransform.forward * (pickupRange * 0.5f);
            Collider[] colliders = Physics.OverlapSphere(sphereCenter, pickupRange * 0.5f, pickupLayer);

            Transform bestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (Collider collider in colliders)
            {
                if (collider.CompareTag(itemTag))
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
                PickupItem(bestTarget);
            }
            else
            {
                Debug.Log("No item found in range.");
            }
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

            // Parent to Hold Position
            _heldItem.SetParent(holdPosition);
            _heldItem.localPosition = Vector3.zero;
            _heldItem.localRotation = Quaternion.identity;
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

            // Unparent
            _heldItem.SetParent(null);

            // Clear ref
            _heldItem = null;
            _heldRigidbody = null;
            _heldCollider = null;
        }
    }
}
