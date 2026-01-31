using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class StealthManager : MonoBehaviour
    {
        public static StealthManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private Slider stealthSlider;

        [Header("Settings")]
        [SerializeField] private float maxStealth = 100f;
        [SerializeField] private float defaultDecay = 2f;

        private float _currentStealth = 0f;

        public float CurrentStealth => _currentStealth;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Since it's usually part of UICanvas, it might already be set to DontDestroy 
                // but let's ensure it's accessible.
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (stealthSlider != null)
            {
                stealthSlider.minValue = 0f;
                stealthSlider.maxValue = maxStealth;
                stealthSlider.value = _currentStealth;
            }
        }

        private void Update()
        {
            // Constant Decay
            if (_currentStealth > 0)
            {
                _currentStealth -= defaultDecay * Time.deltaTime;
                _currentStealth = Mathf.Max(0, _currentStealth);
                UpdateUI();
            }
        }

        public void AddStealth(float amount)
        {
            _currentStealth += amount;
            _currentStealth = Mathf.Clamp(_currentStealth, 0, maxStealth);
            UpdateUI();

            // Check for Scorched Earth Trigger
            if (_currentStealth >= maxStealth)
            {
                if (ScorchedEarthManager.Instance != null)
                {
                    ScorchedEarthManager.Instance.TriggerScorchedEarth();
                }
            }
        }

        private void UpdateUI()
        {
            if (stealthSlider != null)
            {
                stealthSlider.value = _currentStealth;
            }
        }
    }
}
