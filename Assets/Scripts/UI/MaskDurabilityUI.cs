using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MaskDurabilityUI : MonoBehaviour
    {
        public static MaskDurabilityUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private Slider durabilitySlider;
        [SerializeField] private GameObject visualContainer; // The parent object to hide/show the meter

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (visualContainer != null)
            {
                visualContainer.SetActive(visible);
            }
            else if (durabilitySlider != null)
            {
                durabilitySlider.gameObject.SetActive(visible);
            }
        }

        public void UpdateDurability(float current, float max)
        {
            if (durabilitySlider != null)
            {
                durabilitySlider.minValue = 0f;
                durabilitySlider.maxValue = max;
                durabilitySlider.value = current;
            }
        }
    }
}
