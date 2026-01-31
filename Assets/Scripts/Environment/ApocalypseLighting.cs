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

        [Header("Skybox Material (Auto-Generated)")]
        [SerializeField] private Shader skyShader;
        private Material _skyMaterial;
        private Light _light;
        private Volume _volume; // Post Processing Volume

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
                // Live preview in editor
                 SetupLighting();
            }
        }

        public void SetupLighting()
        {
            _light = GetComponent<Light>();
            if (_light == null) return;

            // 1. Light Settings
            _light.type = LightType.Directional;
            _light.color = lightColor;
            _light.intensity = lightIntensity;
            _light.shadows = LightShadows.Soft;

            // 2. Fog Settings
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;

            // 3. Ambient Settings
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = lightColor;
            RenderSettings.ambientEquatorColor = fogColor;
            RenderSettings.ambientGroundColor = Color.black;

            // 4. Skybox Setup
            if (skyShader == null) skyShader = Shader.Find("Custom/ApocalypseSky");

            if (skyShader != null)
            {
                if (_skyMaterial == null || _skyMaterial.shader != skyShader)
                {
                    _skyMaterial = new Material(skyShader);
                    _skyMaterial.name = "ApocalypseSkybox";
                }

                // Update Properties
                _skyMaterial.SetColor("_TopColor", new Color(0.1f, 0.15f, 0.15f)); // Dark Green/Grey
                _skyMaterial.SetColor("_HorizonColor", new Color(0.1f, 0.3f, 0.25f)); // Toxic Teal
                _skyMaterial.SetColor("_BottomColor", new Color(0.05f, 0.1f, 0.1f)); // Dark Swamp
                _skyMaterial.SetColor("_SmogColor", new Color(0.1f, 0.4f, 0.3f, 0.5f)); // Green Smog

                RenderSettings.skybox = _skyMaterial;
            }

            // 5. Post Processing Setup
            SetupPostProcessing();
        }

        private void SetupPostProcessing()
        {
            _volume = GetComponent<Volume>();
            if (_volume == null) _volume = gameObject.AddComponent<Volume>();
            
            _volume.isGlobal = true;
            
            // Manage Profile
            if (_volume.profile == null || !_volume.profile.name.StartsWith("ApocalypseProfile"))
            {
                _volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _volume.profile.name = "ApocalypseProfile_Generated";
            }
            VolumeProfile p = _volume.profile;

            // Chromatic Aberration
            if (!p.TryGet(out ChromaticAberration chrome)) chrome = p.Add<ChromaticAberration>(true);
            chrome.intensity.overrideState = true;
            chrome.intensity.value = chromaticIntensity;

            // Film Grain
            if (!p.TryGet(out FilmGrain grain)) grain = p.Add<FilmGrain>(true);
            grain.intensity.overrideState = true;
            grain.intensity.value = grainIntensity;
            grain.type.overrideState = true;
            grain.type.value = FilmGrainLookup.Medium2; // Good generic grit

            // Bloom
            if (!p.TryGet(out Bloom bloom)) bloom = p.Add<Bloom>(true);
            bloom.intensity.overrideState = true;
            bloom.intensity.value = bloomIntensity;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.9f;

            // Color Adjustments (Tint)
            if (!p.TryGet(out ColorAdjustments colorAdj)) colorAdj = p.Add<ColorAdjustments>(true);
            colorAdj.colorFilter.overrideState = true;
            colorAdj.colorFilter.value = colorFilter;
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.2f; // Slight bump to make glow pop

            // Vignette
            if (!p.TryGet(out Vignette vig)) vig = p.Add<Vignette>(true);
            vig.intensity.overrideState = true;
            vig.intensity.value = 0.3f;
            vig.smoothness.overrideState = true;
            vig.smoothness.value = 0.4f;
        }
    }
}
