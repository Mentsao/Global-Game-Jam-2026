using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Enemy;

namespace UI
{
    public class VignetteEffect : MonoBehaviour
    {
        [Header("Darkness Settings")]
        [Tooltip("How fast the screen darkens when near an enemy (0-1 per second)")]
        [SerializeField] private float darkenSpeed = 0.5f;
        
        [Tooltip("How fast the screen clears when safe (0-1 per second)")]
        [SerializeField] private float recoverySpeed = 1.0f;
        
        [SerializeField] private Color vignetteColor = Color.black;

        [Header("Intense Effects")]
        [Tooltip("Alpha threshold to start showing red pulse and wobble (0-1)")]
        [SerializeField] private float intensityThreshold = 0.5f;

        [Tooltip("Speed of the red heartbeat pulse")]
        [SerializeField] private float redPulseSpeed = 5f;

        [Tooltip("Strength of the screen shake/wobble")]
        [SerializeField] private float wobbleStrength = 10f;

        [Tooltip("Strength of the physical camera shake (Heartbeat)")]
        [SerializeField] private float cameraShakeStrength = 0.05f;

        [SerializeField] private Color pulseColor = new Color(1f, 0f, 0f, 0.5f);

        [Header("Vision Blur (URP)")]
        [SerializeField] private bool enableBlur = true;
        [SerializeField] private float maxChromaticAberration = 1.0f;
        [SerializeField] private float maxMotionBlur = 1.0f;
        
        [Header("Distortion")]
        [Tooltip("Strength of lens distortion (-1 to 1). Negative is Pinch, Positive is FishEye.")]
        [SerializeField] private float maxLensDistortion = -0.5f;
        [Tooltip("Scale multiplier to prevent black edges during distortion (usually 0.8 - 1.2)")]
        [SerializeField] private float lensDistortionScale = 1.0f;


        private Transform _playerTransform;
        private float _currentAlpha = 0f;
        private Texture2D _vignetteTexture;
        private Vector3 _originalLocalPos;
        private Transform _shakeTransform;

        // Post Processing
        private Volume _volume;
        private ChromaticAberration _chromaticAberration;
        private MotionBlur _motionBlur;
        private LensDistortion _lensDistortion;

        // [Header("Audio Settings")]
        // Tension audio is now handled by AudioManager
        // [Tooltip("Looping sound that gets louder as vignette darkens (Heartbeat/Drone)")]
        // [SerializeField] private AudioClip tensionClip; // Deprecated
        // [SerializeField] private float maxVolume = 1.0f; // Deprecated
        // private AudioSource _audioSource; // Deprecated

        private void Start()
        {
            // Setup Camera/Player references
            if (GetComponent<Camera>() != null)
            {
                _shakeTransform = transform;
                _playerTransform = transform.root;
            }
            else if (Camera.main != null)
            {
                _shakeTransform = Camera.main.transform;
                _playerTransform = Camera.main.transform.root;
            }

            if (_shakeTransform != null)
            {
                _originalLocalPos = _shakeTransform.localPosition;
            }

            _vignetteTexture = CreateVignetteTexture();

            // Setup Post Processing Volume
            if (enableBlur)
            {
                SetupVolume();
            }

            // Setup Audio - AudioManager handles it globally now
        }

        private void SetupAudio()
        {
             // Deprecated local setup
        }

        private void SetupVolume()
        {
            // Check if Volume exists, if not add it
            _volume = gameObject.GetComponent<Volume>();
            if (_volume == null)
            {
                _volume = gameObject.AddComponent<Volume>();
            }
            _volume.isGlobal = true;

            // Create a temporary profile so we don't mess up assets
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.profile = profile;
            
            // Add Overrides
            profile.TryGet(out _chromaticAberration);
            if (_chromaticAberration == null)
            {
                _chromaticAberration = profile.Add<ChromaticAberration>(true);
            }
            _chromaticAberration.intensity.overrideState = true;
            _chromaticAberration.intensity.value = 0f;

            profile.TryGet(out _motionBlur);
            if (_motionBlur == null)
            {
                _motionBlur = profile.Add<MotionBlur>(true);
            }
            _motionBlur.intensity.overrideState = true;
            _motionBlur.intensity.value = 0f;

            profile.TryGet(out _lensDistortion);
            if (_lensDistortion == null)
            {
                _lensDistortion = profile.Add<LensDistortion>(true);
            }
            _lensDistortion.intensity.overrideState = true;
            _lensDistortion.intensity.value = 0f;
            _lensDistortion.scale.overrideState = true;
            _lensDistortion.scale.value = 1f;

            // Important: Set priority or weight to ensure it shows
            _volume.weight = 1.0f;
            _volume.priority = 100; // High priority to override global volumes
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                 if (Camera.main != null) 
                 {
                    _shakeTransform = Camera.main.transform;
                    _playerTransform = Camera.main.transform.root;
                    _originalLocalPos = _shakeTransform.localPosition;
                 }
                 if (_playerTransform == null) return;
            }

            bool isAnyEnemyInRange = false;

            foreach (var enemy in EnemyPresence.AllEnemies)
            {
                if (enemy == null) continue;
                if (Vector3.Distance(_playerTransform.position, enemy.transform.position) <= enemy.detectionRange)
                {
                    isAnyEnemyInRange = true;
                    break; 
                }
            }

            // Also check for Zombies
            if (!isAnyEnemyInRange)
            {
                foreach (var zombie in ZombieNPCDetect.AllZombies)
                {
                    if (zombie == null) continue;
                    if (Vector3.Distance(_playerTransform.position, zombie.transform.position) <= zombie.rangeOfView)
                    {
                        isAnyEnemyInRange = true;
                        break;
                    }
                }
            }

            if (isAnyEnemyInRange)
            {
                _currentAlpha += darkenSpeed * Time.deltaTime;
                 AudioManager.Instance.SetTensionState(true);
            }
            else
            {
                _currentAlpha -= recoverySpeed * Time.deltaTime;
                 AudioManager.Instance.SetTensionState(false);
            }
            _currentAlpha = Mathf.Clamp01(_currentAlpha);

            // Update Audio Volume - Deprecated local handling
            // if (_audioSource != null)
            // {
            //    _audioSource.volume = _currentAlpha * maxVolume;
            // }

            // --- Effects Update ---
            
            // 1. Camera Shake
            if (_shakeTransform != null)
            {
                if (_currentAlpha > intensityThreshold)
                {
                    float intensityRatio = (_currentAlpha - intensityThreshold) / (1f - intensityThreshold);
                    float pulse = (Mathf.Sin(Time.time * redPulseSpeed) + 1f) * 0.5f; 
                    float beat = Mathf.Pow(pulse, 3f);
                    
                    Vector3 shakeOffset = Random.insideUnitSphere * beat * cameraShakeStrength * intensityRatio;
                    _shakeTransform.localPosition = _originalLocalPos + shakeOffset;
                }
                else
                {
                    _shakeTransform.localPosition = Vector3.Lerp(_shakeTransform.localPosition, _originalLocalPos, Time.deltaTime * 5f);
                }
            }

            // 2. Post Processing (Vision Blur & Distortion)
            if (enableBlur && _volume != null)
            {
                // Effect starts early (at 0.2 alpha)
                float effectRatio = Mathf.Clamp01((_currentAlpha - 0.2f) / 0.8f);
                
                if (effectRatio > 0 && _chromaticAberration != null)
                {
                     // Debug.Log($"Applying Effects! Ratio: {effectRatio}");
                }

                if (_chromaticAberration != null)
                {
                    _chromaticAberration.active = true;
                    _chromaticAberration.intensity.value = effectRatio * maxChromaticAberration;
                }

                if (_motionBlur != null)
                {
                    _motionBlur.active = true;
                    _motionBlur.intensity.value = effectRatio * maxMotionBlur;
                }
                
                if (_lensDistortion != null)
                {
                    _lensDistortion.active = true;
                    _lensDistortion.intensity.value = effectRatio * maxLensDistortion;
                    // Interpolate scale to prevent black borders if using pinch
                    _lensDistortion.scale.value = Mathf.Lerp(1f, lensDistortionScale, effectRatio);
                }
            }
        }

        private void OnGUI()
        {
            if (_currentAlpha <= 0.01f || _vignetteTexture == null) return;

            DrawVignette(_currentAlpha, vignetteColor, false);

            if (_currentAlpha > intensityThreshold)
            {
                float intensityRatio = (_currentAlpha - intensityThreshold) / (1f - intensityThreshold);
                float pulse = (Mathf.Sin(Time.time * redPulseSpeed) + 1f) * 0.5f; 
                float redAlpha = pulse * intensityRatio * 0.8f; 
                
                DrawVignette(redAlpha, pulseColor, true);
            }
        }

        private void DrawVignette(float alpha, Color color, bool wobble)
        {
            Color c = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, alpha);

            Rect rect = new Rect(0, 0, Screen.width, Screen.height);

            if (wobble)
            {
                float shakeX = Random.Range(-1f, 1f) * wobbleStrength * alpha;
                float shakeY = Random.Range(-1f, 1f) * wobbleStrength * alpha;
                rect.x += shakeX;
                rect.y += shakeY;
            }

            GUI.DrawTexture(rect, _vignetteTexture);
            GUI.color = c;
        }

        private Texture2D CreateVignetteTexture()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = Vector2.Distance(Vector2.zero, center);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float t = Mathf.Clamp01(dist / maxDist);
                    float alpha = Mathf.Pow(t, 3f); 
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
