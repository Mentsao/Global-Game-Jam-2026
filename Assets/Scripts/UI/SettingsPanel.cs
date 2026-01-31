using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SettingsPanel : MonoBehaviour
    {
        public static SettingsPanel Instance { get; private set; }

        [Header("Audio Settings")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        private GameObject _previousPanel;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Keep it in the scene, usually attached to Canvas
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Start hidden
            gameObject.SetActive(false);
        }

        private void Start()
        {
            InitializeSliders();
        }

        private void InitializeSliders()
        {
            // Set initial values from AudioManager (which loads from PlayerPrefs in its Awake)
            if (AudioManager.Instance != null)
            {
                if (masterSlider)
                {
                    masterSlider.value = AudioManager.Instance.masterVolume;
                    masterSlider.onValueChanged.AddListener(SetMasterVolume);
                }
                if (musicSlider)
                {
                    musicSlider.value = AudioManager.Instance.musicVolume;
                    musicSlider.onValueChanged.AddListener(SetMusicVolume);
                }
                if (sfxSlider)
                {
                    sfxSlider.value = AudioManager.Instance.sfxVolume;
                    sfxSlider.onValueChanged.AddListener(SetSFXVolume);
                }
            }
        }

        public void OpenFrom(GameObject callingPanel)
        {
            _previousPanel = callingPanel;
            gameObject.SetActive(true);
            
            // Re-sync sliders in case values changed elsewhere
            InitializeSliders();
        }

        public void CloseSettings()
        {
            gameObject.SetActive(false);
            
            // Return to previous panel if it exists
            if (_previousPanel != null)
            {
                _previousPanel.SetActive(true);
                _previousPanel = null;
            }
        }

        public void SetMasterVolume(float value)
        {
            if (AudioManager.Instance)
            {
                AudioManager.Instance.masterVolume = value;
                PlayerPrefs.SetFloat("Vol_Master", value);
            }
        }

        public void SetMusicVolume(float value)
        {
            if (AudioManager.Instance)
            {
                AudioManager.Instance.musicVolume = value;
                PlayerPrefs.SetFloat("Vol_Music", value);
            }
        }

        public void SetSFXVolume(float value)
        {
            if (AudioManager.Instance)
            {
                AudioManager.Instance.sfxVolume = value;
                PlayerPrefs.SetFloat("Vol_SFX", value);
            }
        }


        
        private void OnDisable()
        {
            PlayerPrefs.Save();
        }
    }
}
