using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    public static SettingsPanel Instance { get; private set; }

    [Header("UI refs")]
    [SerializeField] private GameObject panelContent; // The actual UI container (Active/Inactive)
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider masterSlider, musicSlider, sfxSlider;
    [SerializeField] private Button backButton;

    [Header("Audio")]
    [SerializeField] private AudioMixer mixer;

    private GameObject previousPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Force hide at start if not already
        if (panelContent != null)
            panelContent.SetActive(false);
    }

    private void Start()
    {
        SetupFullscreenToggle();
        SetupVolumeSliders();

        if (backButton != null)
            backButton.onClick.AddListener(CloseSettings);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (panelContent != null && panelContent.activeSelf)
                CloseSettings();
        }
    }

    public void OpenFrom(GameObject caller)
    {
        previousPanel = caller;
        if (panelContent != null)
            panelContent.SetActive(true);

        Time.timeScale = 0f; // Pause game while in settings

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseSettings()
    {
        PlayerPrefs.Save();
        if (panelContent != null)
            panelContent.SetActive(false);

        if (previousPanel != null)
        {
            previousPanel.SetActive(true);
            // Don't change timeScale or cursor yet, assume previous panel handles its own state
        }
        else
        {
            Time.timeScale = 1f; // Resume game
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        previousPanel = null;
    }

    #region Fullscreen
    private void SetupFullscreenToggle()
    {
        bool fs = PlayerPrefs.GetInt("FS", Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = fs;
        fullscreenToggle.isOn = fs;

        fullscreenToggle.onValueChanged.AddListener(b =>
        {
            Screen.fullScreen = b;
            PlayerPrefs.SetInt("FS", b ? 1 : 0);
        });
    }
    #endregion

    #region Audio
    private void SetupVolumeSliders()
    {
        masterSlider.value = PlayerPrefs.GetFloat("Vol_Master", 0.8f);
        musicSlider.value = PlayerPrefs.GetFloat("Vol_Music", 0.8f);
        sfxSlider.value = PlayerPrefs.GetFloat("Vol_SFX", 0.8f);

        masterSlider.onValueChanged.AddListener(v => SetVol("MasterVolume", v, "Vol_Master"));
        musicSlider.onValueChanged.AddListener(v => SetVol("MusicVolume", v, "Vol_Music"));
        sfxSlider.onValueChanged.AddListener(v => SetVol("SFXVolume", v, "Vol_SFX"));

        SetVol("MasterVolume", masterSlider.value, "Vol_Master");
        SetVol("MusicVolume", musicSlider.value, "Vol_Music");
        SetVol("SFXVolume", sfxSlider.value, "Vol_SFX");
    }

    private void SetVol(string mixerParam, float sliderVal, string prefKey)
    {
        if (mixer != null)
        {
            float dB = Mathf.Log10(Mathf.Clamp(sliderVal, 0.001f, 1f)) * 20f;
            mixer.SetFloat(mixerParam, dB);
        }
        
        PlayerPrefs.SetFloat(prefKey, sliderVal);

        // Sync with AudioManager
        if (AudioManager.Instance != null)
        {
            if (prefKey == "Vol_Master") AudioManager.Instance.masterVolume = sliderVal;
            else if (prefKey == "Vol_Music") AudioManager.Instance.musicVolume = sliderVal;
            else if (prefKey == "Vol_SFX") AudioManager.Instance.sfxVolume = sliderVal;
        }
    }

    public void InitializeSlidersFromPrefs()
    {
        masterSlider.value = PlayerPrefs.GetFloat("Vol_Master", 1.0f);
        musicSlider.value = PlayerPrefs.GetFloat("Vol_Music", 1.0f);
        sfxSlider.value = PlayerPrefs.GetFloat("Vol_SFX", 1.0f);
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;

        InitializeSlidersFromPrefs();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    #endregion
}
