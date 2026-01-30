using UnityEngine;

namespace Interaction
{
    public class PickupableItem : MonoBehaviour
    {
        [Header("Pickup Settings")]
        [Tooltip("Custom rotation when held by the player.")]
        public Vector3 holdRotation = Vector3.zero;

        [Tooltip("Custom position offset when held by the player.")]
        public Vector3 holdPositionOffset = Vector3.zero;
        
        // Potential future expansion: specific offsets, scale, two-handed, etc.
    }
}
