using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Optional day/night lighting sink. Simulation time remains on <c>DayNightManager</c>;
    /// Unity lights never drive biology.
    /// </summary>
    public sealed class DayNightLightingPresenter : MonoBehaviour, IDayNightLightingHook
    {
        [SerializeField] Light sun;
        [SerializeField] bool affectAmbient = true;
        [SerializeField] float dayIntensity = 1.05f;
        [SerializeField] float nightIntensity = 0.08f;

        public Light Sun => sun;
        public float LastIntensity { get; private set; }

        public void BindSun(Light light) => sun = light;

        public void OnDayNightUpdated(IReadOnlyDayNightState state)
        {
            if (state == null)
            {
                return;
            }

            var t = state.NormalizedTimeOfDay;
            var elevation = Mathf.Sin(t * Mathf.PI * 2f);
            var factor = Mathf.Clamp01(elevation);
            LastIntensity = Mathf.Lerp(nightIntensity, dayIntensity, factor);

            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler((t * 360f) - 90f, -35f, 0f);
                sun.intensity = LastIntensity;
                sun.color = Color.Lerp(new Color(0.45f, 0.55f, 0.85f), new Color(1f, 0.96f, 0.88f), factor);
            }

            if (affectAmbient)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Color.Lerp(
                    PresentationPalette.NightAmbient,
                    PresentationPalette.DayAmbient,
                    factor);
            }
        }
    }
}
