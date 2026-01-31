using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM")]
    [SerializeField] private AudioClip mainTheme;
    [SerializeField] private AudioSource bgmSource;

    [Header("Player SFX")]
    [SerializeField] private AudioClip stepWalk;
    [SerializeField] private AudioClip stepRun;
    [SerializeField] private AudioClip stepCrouch;
    [SerializeField] private AudioClip attackSwing;
    
    [Header("Zombie SFX")]
    [SerializeField] private AudioClip zombieGrowl;

    [Header("Police SFX")]
    [SerializeField] private AudioClip policePass;
    [SerializeField] private AudioClip policeFail;

    [Header("Atmosphere")]
    [SerializeField] private AudioClip tensionLoop;
    [SerializeField] private float tensionFadeSpeed = 1.0f;
    private AudioSource tensionSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

        if (clip != null) sfxSource.PlayOneShot(clip, vol);
    }

    public void PlayPlayerAttack()
    {
        if (attackSwing != null) sfxSource.PlayOneShot(attackSwing, 0.8f);
    }

    // --- ZOMBIE ---
    public void PlayZombieGrowl()
    {
        if (zombieGrowl != null) sfxSource.PlayOneShot(zombieGrowl, 1.0f);
    }

    // --- POLICE ---
    public void PlayPoliceDecision(bool approved)
    {
        AudioClip clip = approved ? policePass : policeFail;
        if (clip != null) sfxSource.PlayOneShot(clip, 1.0f);
    }

    // --- ATMOSPHERE ---
    private Coroutine tensionCoroutine;

    public void SetTensionState(bool active)
    {
        if (tensionLoop == null) return;
        
        float targetVol = active ? 0.8f : 0.0f;
        if (tensionCoroutine != null) StopCoroutine(tensionCoroutine);
        tensionCoroutine = StartCoroutine(FadeTension(targetVol));
    }

    private IEnumerator FadeTension(float targetVol)
    {
        while (Mathf.Abs(tensionSource.volume - targetVol) > 0.01f)
        {
            tensionSource.volume = Mathf.MoveTowards(tensionSource.volume, targetVol, Time.deltaTime * tensionFadeSpeed);
            yield return null;
        }
        tensionSource.volume = targetVol;
    }
}
