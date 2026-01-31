using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [Range(0f, 1f)] public float musicVolume = 1.0f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    [Range(0f, 1f)] public float ambienceVolume = 1.0f;

    [Header("BGM")]
    [SerializeField] private AudioClip mainTheme;
    [SerializeField] private AudioSource bgmSource;

    [Header("Player SFX")]
    [SerializeField] private AudioClip stepWalk;
    [SerializeField] private AudioClip stepRun;
    [SerializeField] private AudioClip stepCrouch;

    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip attackSwing;
    [SerializeField] private AudioClip balisongPickupSFX;
    [SerializeField] private AudioClip documentPickupSFX;
    
    [Header("Zombie SFX")]
    [SerializeField] private AudioClip zombieGrowl;

    [Header("Police SFX")]
    [SerializeField] private AudioClip policePass;
    [SerializeField] private AudioClip policeFail;
    [SerializeField] private AudioClip policeDetect;
    [SerializeField] private AudioClip policeWaitLoop;
    [SerializeField] private float policeLoopMinDist = 2f;
    [SerializeField] private float policeLoopMaxDist = 15f;

    [Header("Atmosphere")]
    [SerializeField] private AudioClip tensionLoop;

    [SerializeField] private float tensionFadeSpeed = 1.0f;
    private AudioSource tensionSource;
    private AudioSource sfxSource;
    private float _tensionIntensity = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Load saved volume settings
            masterVolume = PlayerPrefs.GetFloat("Vol_Master", 1.0f);
            musicVolume = PlayerPrefs.GetFloat("Vol_Music", 1.0f);
            sfxVolume = PlayerPrefs.GetFloat("Vol_SFX", 1.0f);
            
            InitializeSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSources()
    {
        // BGM Source
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        // SFX Source (General 2D)
        sfxSource = gameObject.AddComponent<AudioSource>();

        // Tension Source
        tensionSource = gameObject.AddComponent<AudioSource>();
        tensionSource.loop = true;
        tensionSource.volume = 0f;
        if (tensionLoop != null)
        {
            tensionSource.clip = tensionLoop;
            tensionSource.Play(); // Play silent, fade vol later
        }

        // Play Main Theme if set
        if (mainTheme != null)
        {
            bgmSource.clip = mainTheme;
            bgmSource.volume = 0.5f;
            bgmSource.Play();
        }
    }

    private void Update()
    {
        // Real-time Volume Updates
        if (bgmSource != null)
        {
            bgmSource.volume = musicVolume * masterVolume * 0.5f; // Initial 0.5 padding kept from original
        }

        if (tensionSource != null)
        {
            tensionSource.volume = _tensionIntensity * ambienceVolume * masterVolume;
        }
    }

    // --- PLAYER ---
    public void PlayFootstep(bool isRunning, bool isCrouching)
    {
        AudioClip clip = stepWalk;
        float vol = 0.5f;

        if (isCrouching)
        {
            clip = stepCrouch;
            vol = 0.3f;
        }
        else if (isRunning)
        {
            clip = stepRun;
            vol = 0.7f;
        }



        if (clip != null) sfxSource.PlayOneShot(clip, vol * sfxVolume * masterVolume);
    }

    public void PlayJump()
    {
        if (jumpSFX != null) sfxSource.PlayOneShot(jumpSFX, 0.8f * sfxVolume * masterVolume);
    }

    public void PlayBalisongPickup()
    {
        if (balisongPickupSFX != null) sfxSource.PlayOneShot(balisongPickupSFX, 1.0f * sfxVolume * masterVolume);
    }

    public void PlayDocumentPickup()
    {
        if (documentPickupSFX != null) sfxSource.PlayOneShot(documentPickupSFX, 1.0f * sfxVolume * masterVolume);
    }

    public void PlayPlayerAttack()
    {
        if (attackSwing != null) sfxSource.PlayOneShot(attackSwing, 0.8f * sfxVolume * masterVolume);
    }

    // --- ZOMBIE ---
    public void PlayZombieGrowl()
    {
        if (zombieGrowl != null) sfxSource.PlayOneShot(zombieGrowl, 1.0f * sfxVolume * masterVolume);
    }

    // --- POLICE ---
    public void PlayPoliceDecision(bool approved)
    {
        AudioClip clip = approved ? policePass : policeFail;
        if (clip != null) sfxSource.PlayOneShot(clip, 1.0f * sfxVolume * masterVolume);
    }

    public void PlayPoliceDetect()
    {
        if (policeDetect != null) sfxSource.PlayOneShot(policeDetect, 1.0f * sfxVolume * masterVolume);
    }

    /// <summary>
    /// Configures a target AudioSource for 3D spatial police waiting loop
    /// </summary>
    public void PlaySpatialPoliceLoop(AudioSource targetSource)
    {
        if (targetSource == null || policeWaitLoop == null) return;

        targetSource.clip = policeWaitLoop;
        targetSource.loop = true;
        targetSource.spatialBlend = 1.0f; // 3D
        targetSource.rolloffMode = AudioRolloffMode.Logarithmic;
        targetSource.minDistance = policeLoopMinDist;
        targetSource.maxDistance = policeLoopMaxDist;
        targetSource.volume = sfxVolume * masterVolume;
        
        targetSource.Play();
    }



    // --- ATMOSPHERE ---
    private Coroutine tensionCoroutine;

    public void SetTensionState(bool active)
    {
        if (tensionLoop == null) return;
        
        float targetIntensity = active ? 0.8f : 0.0f;
        if (tensionCoroutine != null) StopCoroutine(tensionCoroutine);
        tensionCoroutine = StartCoroutine(FadeTension(targetIntensity));
    }

    private IEnumerator FadeTension(float targetIntensity)
    {
        while (Mathf.Abs(_tensionIntensity - targetIntensity) > 0.01f)
        {
            _tensionIntensity = Mathf.MoveTowards(_tensionIntensity, targetIntensity, Time.deltaTime * tensionFadeSpeed);
            yield return null;
        }
        _tensionIntensity = targetIntensity;
    }
}
