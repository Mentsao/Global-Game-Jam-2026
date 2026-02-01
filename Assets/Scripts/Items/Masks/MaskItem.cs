using UnityEngine;

namespace Items.Masks
{
    public enum MaskType
    {
        None,
        Police,
        Nurse,
        Zombie,
        Government
    }

    public class MaskItem : MonoBehaviour
    {
        [Header("Mask Settings")]
        [SerializeField] private MaskType maskType = MaskType.None;

        [Header("Durability Settings")]
        [SerializeField] private float maxDurability = 100f;
        public float currentDurability;

        public MaskType Type => maskType;
        public float MaxDurability => maxDurability;

        private void Awake()
        {
            currentDurability = maxDurability;
        }
    }
}
