using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Optional day/night lighting sink. Simulation time remains on <c>DayNightManager</c>;
    /// Unity lights never drive biology. Night stays readable for a research demo.
    /// </summary>
    public sealed class DayNightLightingPresenter : MonoBehaviour, IDayNightLightingHook
    {
        [SerializeField] Light sun;
        [SerializeField] bool affectAmbient = true;
        [SerializeField] float dayIntensity = 1.15f;
        [SerializeField] float nightIntensity = 0.55f;

        static Material skybox;

        public Light Sun => sun;
        public float LastIntensity { get; private set; }

        public void BindSun(Light light)
        {
            sun = light;
            EnsureSkybox();
            ApplyReadableDefault();
        }

        void ApplyReadableDefault()
        {
            LastIntensity = dayIntensity;
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
                sun.intensity = dayIntensity;
                sun.color = new Color(1f, 0.96f, 0.88f);
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.32f;
            }

            if (affectAmbient)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = PresentationPalette.DayAmbient;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0032f;
            RenderSettings.fogColor = PresentationPalette.FogDay;
        }

        public void OnDayNightUpdated(IReadOnlyDayNightState state)
        {
            if (state == null)
            {
                return;
            }

            EnsureSkybox();
            var t = state.NormalizedTimeOfDay;
            var elevation = Mathf.Sin(t * Mathf.PI * 2f);
            var factor = Mathf.Clamp01(elevation * 0.5f + 0.5f);
            if (state.IsDay)
            {
                factor = Mathf.Max(factor, 0.58f);
            }
            else
            {
                factor = Mathf.Max(factor, 0.32f);
            }

            LastIntensity = Mathf.Lerp(nightIntensity, dayIntensity, factor);

            if (sun != null)
            {
                var pitch = Mathf.Lerp(18f, 58f, factor);
                sun.transform.rotation = Quaternion.Euler(pitch, -28f, 0f);
                sun.intensity = LastIntensity;
                sun.color = Color.Lerp(new Color(0.62f, 0.70f, 0.88f), new Color(1f, 0.96f, 0.88f), factor);
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = Mathf.Lerp(0.12f, 0.38f, factor);
                sun.shadowBias = 0.05f;
            }

            if (affectAmbient)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Color.Lerp(
                    PresentationPalette.NightAmbient,
                    PresentationPalette.DayAmbient,
                    factor);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0032f;
            RenderSettings.fogColor = Color.Lerp(PresentationPalette.FogNight, PresentationPalette.FogDay, factor);

            if (skybox != null && skybox.HasProperty("_Exposure"))
            {
                skybox.SetFloat("_Exposure", Mathf.Lerp(0.95f, 1.22f, factor));
            }
        }

        static void EnsureSkybox()
        {
            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
                return;
            }

            var shader = Shader.Find("Skybox/Procedural") ?? Shader.Find("Skybox/Cubemap");
            if (shader == null)
            {
                return;
            }

            skybox = new Material(shader)
            {
                name = "EvoLifeSkybox",
                hideFlags = HideFlags.HideAndDontSave
            };
            if (skybox.HasProperty("_AtmosphereThickness"))
            {
                skybox.SetFloat("_AtmosphereThickness", 1.05f);
            }

            if (skybox.HasProperty("_Exposure"))
            {
                skybox.SetFloat("_Exposure", 1.15f);
            }

            if (skybox.HasProperty("_GroundColor"))
            {
                skybox.SetColor("_GroundColor", new Color(0.36f, 0.40f, 0.32f));
            }

            RenderSettings.skybox = skybox;
        }
    }
}
