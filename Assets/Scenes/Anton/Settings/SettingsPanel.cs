using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    public static SettingsPanel Instance { get; private set; }

    [Header("UI refs")]
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] Slider masterSlider, musicSlider, sfxSlider;
    [SerializeField] Button backButton;

    [Header("Audio")]
    [SerializeField] AudioMixer mixer;

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
    }

    private void Start()
    {
        SetupFullscreenToggle();
        SetupVolumeSliders();

        if (backButton != null)
            backButton.onClick.AddListener(CloseSettings);

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseSettings();
    }

    public void OpenFrom(GameObject caller)
    {
        previousPanel = caller;
        gameObject.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseSettings()
    {
        PlayerPrefs.Save();
        gameObject.SetActive(false);

        if (previousPanel != null)
            previousPanel.SetActive(true);

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
        float dB = Mathf.Log10(Mathf.Clamp(sliderVal, 0.001f, 1f)) * 20f;
        mixer.SetFloat(mixerParam, dB);
        PlayerPrefs.SetFloat(prefKey, sliderVal);
    }

    public void InitializeSlidersFromPrefs()
    {
        masterSlider.value = PlayerPrefs.GetFloat("Vol_Master", 0.8f);
        musicSlider.value = PlayerPrefs.GetFloat("Vol_Music", 0.8f);
        sfxSlider.value = PlayerPrefs.GetFloat("Vol_SFX", 0.8f);
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
