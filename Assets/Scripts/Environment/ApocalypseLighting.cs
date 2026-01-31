using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Environment
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Light))]
    public class ApocalypseLighting : MonoBehaviour
    {
        [Header("Atmosphere")]
        [SerializeField] private Color lightColor = new Color(0.2f, 0.4f, 0.45f); // Pale Toxic Cyan
        [SerializeField] private float lightIntensity = 1.2f;
        [SerializeField] private Color fogColor = new Color(0.15f, 0.25f, 0.25f); // Dark Teal Fog
        [SerializeField] private float fogDensity = 0.035f;

        [Header("Breathing Effect")]
        [SerializeField] private bool enableBreathing = true;
        [SerializeField] private float breatheSpeed = 0.5f;
        [SerializeField] private float breatheAmount = 0.1f;

        [Header("Post Processing")]
        [SerializeField] private float chromaticIntensity = 0.5f; // Stronger for broken camera feel
        [SerializeField] private float grainIntensity = 0.6f;     // Gritty
        [SerializeField] private float bloomIntensity = 1.5f;     // Glowy sky
        [SerializeField] private Color colorFilter = new Color(0.7f, 1f, 0.9f); // Green Tint

        [Header("VHS Glitch")]
        [SerializeField] private bool enableGlitch = true;
        [SerializeField] private float glitchChance = 0.05f; // Chance per frame to twitch
        [SerializeField] private float glitchStrength = 0.5f; // How much it warps
        
        [Header("Skybox Material (Auto-Generated)")]
        [SerializeField] private Shader skyShader;
        private Material _skyMaterial;
        private Light _light;
        private Volume _volume; 

        // Cached Overrides
        private ChromaticAberration _chromatic;
        private LensDistortion _distortion;
        private ColorAdjustments _colorAdj;

        private void OnEnable()
        {
            SetupLighting();
        }

        private void Update()
        {
            if (Application.isPlaying && enableBreathing && _light != null)
            {
                float noise = Mathf.PerlinNoise(Time.time * breatheSpeed, 0f);
                _light.intensity = lightIntensity + (noise * breatheAmount);
            }
            else if (!Application.isPlaying)
            {
                 SetupLighting();
            }

            if (Application.isPlaying && enableGlitch && _volume != null)
            {
                 HandleGlitch();
            }
        }

        private void HandleGlitch()
        {
            if (_chromatic == null || _distortion == null) return;

            // Random twitch
            if (Random.value < glitchChance)
            {
                // Glitch State
                float twitch = Random.Range(-1f, 1f) * glitchStrength;
                
                // Spike Chromatic
                _chromatic.intensity.value = chromaticIntensity + Mathf.Abs(twitch) * 2f;
                
                // Warp Lens
                _distortion.intensity.value = twitch * 0.5f;
                _distortion.scale.value = 1f - (Mathf.Abs(twitch) * 0.1f); // Zoom in/out slightly
                
                // Vertical Shift (using Y multiplier if available? No, basic LensDistortion uses Center)
                _distortion.center.value = new Vector2(0.5f + twitch * 0.1f, 0.5f);
            }
            else
            {
                // Return to normal (Smoothly)
                _chromatic.intensity.value = Mathf.Lerp(_chromatic.intensity.value, chromaticIntensity, Time.deltaTime * 10f);
                _distortion.intensity.value = Mathf.Lerp(_distortion.intensity.value, 0f, Time.deltaTime * 10f);
                _distortion.scale.value = Mathf.Lerp(_distortion.scale.value, 1f, Time.deltaTime * 10f);
                _distortion.center.value = Vector2.Lerp(_distortion.center.value, new Vector2(0.5f, 0.5f), Time.deltaTime * 10f);
            }
        }

        public void SetupLighting()
        {
            _light = GetComponent<Light>();
            if (_light == null) return;

            // ... (Light Setups same as before) ...
            _light.type = LightType.Directional;
            _light.color = lightColor;
            _light.intensity = lightIntensity;
            _light.shadows = LightShadows.Soft;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = lightColor;
            RenderSettings.ambientEquatorColor = fogColor;
            RenderSettings.ambientGroundColor = Color.black;

            if (skyShader == null) skyShader = Shader.Find("Custom/ApocalypseSky");
            if (skyShader != null)
            {
                if (_skyMaterial == null || _skyMaterial.shader != skyShader)
                {
                    _skyMaterial = new Material(skyShader);
                    _skyMaterial.name = "ApocalypseSkybox";
                }
                _skyMaterial.SetColor("_TopColor", new Color(0.1f, 0.15f, 0.15f)); 
                _skyMaterial.SetColor("_HorizonColor", new Color(0.1f, 0.3f, 0.25f)); 
                _skyMaterial.SetColor("_BottomColor", new Color(0.05f, 0.1f, 0.1f)); 
                _skyMaterial.SetColor("_SmogColor", new Color(0.1f, 0.4f, 0.3f, 0.5f)); 
                RenderSettings.skybox = _skyMaterial;
            }

            SetupPostProcessing();
        }

        private void SetupPostProcessing()
        {
            _volume = GetComponent<Volume>();
            if (_volume == null) _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            
            if (_volume.profile == null || !_volume.profile.name.StartsWith("ApocalypseProfile"))
            {
                _volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _volume.profile.name = "ApocalypseProfile_Generated";
            }
            VolumeProfile p = _volume.profile;

            // 1. Chromatic Aberration
            if (!p.TryGet(out _chromatic)) _chromatic = p.Add<ChromaticAberration>(true);
            _chromatic.intensity.overrideState = true;
            _chromatic.intensity.value = chromaticIntensity;

            // 2. Film Grain
            if (!p.TryGet(out FilmGrain grain)) grain = p.Add<FilmGrain>(true);
            grain.intensity.overrideState = true;
            grain.intensity.value = grainIntensity;
            grain.type.overrideState = true;
            grain.type.value = FilmGrainLookup.Medium2;

            // 3. Bloom
            if (!p.TryGet(out Bloom bloom)) bloom = p.Add<Bloom>(true);
            bloom.intensity.overrideState = true;
            bloom.intensity.value = bloomIntensity;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.9f;

            // 4. Color Adjustments
            if (!p.TryGet(out _colorAdj)) _colorAdj = p.Add<ColorAdjustments>(true);
            _colorAdj.colorFilter.overrideState = true;
            _colorAdj.colorFilter.value = colorFilter;
            _colorAdj.postExposure.overrideState = true;
            _colorAdj.postExposure.value = 0.2f;

            // 5. Vignette
            if (!p.TryGet(out Vignette vig)) vig = p.Add<Vignette>(true);
            vig.intensity.overrideState = true;
            vig.intensity.value = 0.3f;
            vig.smoothness.overrideState = true;
            vig.smoothness.value = 0.4f;

            // 6. Lens Distortion (New for VHS)
            if (!p.TryGet(out _distortion)) _distortion = p.Add<LensDistortion>(true);
            _distortion.intensity.overrideState = true;
            _distortion.intensity.value = 0f;
            _distortion.scale.overrideState = true;
            _distortion.scale.value = 1f;
        }
    }
}
