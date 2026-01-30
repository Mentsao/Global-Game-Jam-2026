using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [Header("Inventory Images")]
        [SerializeField] private Image mask1Image;
        [SerializeField] private Image mask2Image;
        [SerializeField] private Image paperImage;
        [SerializeField] private Image knifeImage;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Ensure images are hidden at start, or keep them as per scene setup?
            // Usually hidden until collected.
            SetImageState(mask1Image, false);
            SetImageState(mask2Image, false);
            SetImageState(paperImage, false);
            SetImageState(knifeImage, false);
        }

        public void UpdateItemStatus(string itemName, bool isHeld)
        {
            // Simple string matching based on item names (or tags if preferred)
            // Assuming naming convention: "Mask1", "Mask2", "Paper", "Knife", "Weapon"
            
            // Normalize inputs slightly to avoid easy errors
            string lowerName = itemName.ToLower();

            if (lowerName.Contains("mask1"))
            {
                SetImageState(mask1Image, isHeld);
            }
            else if (lowerName.Contains("mask2"))
            {
                SetImageState(mask2Image, isHeld);
            }
            else if (lowerName.Contains("paper") || lowerName.Contains("document"))
            {
                SetImageState(paperImage, isHeld);
            }
            else if (lowerName.Contains("knife") || lowerName.Contains("weapon") || lowerName.Contains("balisong"))
            {
                SetImageState(knifeImage, isHeld);
            }
        }

        private void SetImageState(Image img, bool active)
        {
            if (img != null)
            {
                img.gameObject.SetActive(active);
            }
        }
    }
}
