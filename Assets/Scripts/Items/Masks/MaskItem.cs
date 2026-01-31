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

        public MaskType Type => maskType;
    }
}
