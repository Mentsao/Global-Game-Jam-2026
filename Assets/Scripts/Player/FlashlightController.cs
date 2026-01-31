using UnityEngine;
using System.Collections;

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the Spotlight here. If empty, tries to find one in children.")]
    [SerializeField] private Light flashlight;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip toggleSound;
    [SerializeField] private AudioClip flickerSound;

    [Header("Settings")]
    [SerializeField] private bool isOn = true;
    [SerializeField] private float baseIntensity = 200f; // Hard 200
    
    [Header("Flicker Effect")]
    [SerializeField] private bool enableFlicker = true;
    [Tooltip("0 to 1 value. 0.05 means 5% chance per frame.")]
    [SerializeField] private float flickerChance = 0.02f; 
    // [SerializeField] private float flickerDims = 0.5f; // Unused in Binary Mode
    [Tooltip("If true, light can completely turn off for a split second.")]
    [SerializeField] private bool canBlackout = true;

    private float _targetIntensity;

    private void Start()
    {
        if (flashlight == null) flashlight = GetComponentInChildren<Light>();
        
        if (flashlight != null) 
        {
            // Auto-fix: If value is the old default (1.8), boost it to 200.
            if (baseIntensity < 2.0f) 
            {
                baseIntensity = 200f;
            }

            _targetIntensity = baseIntensity;
            
            // Ensure strict 0 or 200
            flashlight.intensity = isOn ? baseIntensity : 0f;
        }
        else
        {
            Debug.LogWarning("[FlashlightController] No Light Setup found!");
            enabled = false;
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    [Header("Instability")]
    [Tooltip("How fast the flicker chance increases per second while on.")]
    [SerializeField] private float instabilityRate = 0.005f;
    private float _currentFlickerChance;
    private float _activeTimer = 0f;

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.gKey.wasPressedThisFrame)
        {
            ToggleLight();
        }

        if (isOn && flashlight != null)
        {
            // Increase timer and chance
            _activeTimer += Time.deltaTime;
            _currentFlickerChance = flickerChance + (_activeTimer * instabilityRate);
            
            if (enableFlicker)
            {
                HandleFlicker();
            }
        }
    }

    private void ToggleLight()
    {
        isOn = !isOn;
        flashlight.enabled = isOn;
        
        if (!isOn)
        {
            // Reset instability when turned off
            _activeTimer = 0f;
            _currentFlickerChance = flickerChance;
        }
        
        if (toggleSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(toggleSound);
        }
    }

    private void HandleFlicker()
    {
        if (Random.value < _currentFlickerChance)
        {
            // Trigger a Flicker Event
            StartCoroutine(FlickerRoutine());
        }
    }

    private IEnumerator FlickerRoutine()
    {
        // Binary Flicker: Only 0 or 200. No in-betweens.
        
        // Instant OFF
        flashlight.intensity = 0f; 
        
        if (flickerSound != null) audioSource.PlayOneShot(flickerSound, 0.5f);
        
        // Random wait (Blackout duration)
        // If it's a "major" blackout (simulated by random chance), wait longer
        float waitTime = (Random.value < 0.3f) ? Random.Range(0.1f, 0.3f) : Random.Range(0.05f, 0.1f);
        yield return new WaitForSeconds(waitTime);

        // Instant ON
        flashlight.intensity = baseIntensity;
    }
}
