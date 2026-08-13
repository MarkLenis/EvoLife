using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Reads ecological event state and applies presentation-only cues.
    /// Never calls Environment mutation APIs or Simulation creature ports.
    /// </summary>
    public sealed class EnvironmentalEventVisualAdapter : MonoBehaviour
    {
        [SerializeField] EnvironmentalEventManager events;
        [SerializeField] BiomeGroundPresenter ground;
        [SerializeField] Transform effectsRoot;
        [SerializeField] bool enableEffects = true;

        GameObject wildfireGlow;
        GameObject smoke;
        GameObject heatHaze;
        bool subscribed;

        public bool EnableEffects
        {
            get => enableEffects;
            set => enableEffects = value;
        }

        public float LastLushness { get; private set; } = 1f;
        public bool WildfireVisible { get; private set; }

        public void Bind(
            EnvironmentalEventManager eventManager,
            BiomeGroundPresenter groundPresenter = null)
        {
            Unsubscribe();
            events = eventManager;
            if (groundPresenter != null)
            {
                ground = groundPresenter;
            }

            Subscribe();
            RefreshVisuals();
        }

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (subscribed || events == null)
            {
                return;
            }

            events.EventStarted += OnEventChanged;
            events.EventEnded += OnEventChanged;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || events == null)
            {
                return;
            }

            events.EventStarted -= OnEventChanged;
            events.EventEnded -= OnEventChanged;
            subscribed = false;
        }

        void OnEventChanged(IReadOnlyEnvironmentalEvent _) => RefreshVisuals();

        public void RefreshVisuals()
        {
            var drought = events != null && events.HasActiveEvent(EnvironmentalEventKind.Drought);
            var wildfire = events != null && events.HasActiveEvent(EnvironmentalEventKind.Wildfire);
            var heat = events != null && events.HasActiveEvent(EnvironmentalEventKind.HeatWave);
            var boom = events != null && events.HasActiveEvent(EnvironmentalEventKind.FoodBoom);

            LastLushness = 1f;
            if (drought)
            {
                LastLushness = Mathf.Min(LastLushness, 0.4f);
            }

            if (boom)
            {
                LastLushness = Mathf.Max(LastLushness, 1.12f);
            }

            if (ground != null)
            {
                ground.SetLushness(LastLushness);
            }

            WildfireVisible = enableEffects && wildfire;
            EnsureEffects();
            if (wildfireGlow != null)
            {
                wildfireGlow.SetActive(WildfireVisible);
            }

            if (smoke != null)
            {
                smoke.SetActive(WildfireVisible);
            }

            if (heatHaze != null)
            {
                heatHaze.SetActive(enableEffects && heat);
            }
        }

        void EnsureEffects()
        {
            if (!enableEffects)
            {
                return;
            }

            if (effectsRoot == null)
            {
                var found = transform.Find("EventEffects");
                if (found != null)
                {
                    effectsRoot = found;
                }
                else
                {
                    var go = new GameObject("EventEffects");
                    go.transform.SetParent(transform, false);
                    effectsRoot = go.transform;
                }
            }

            if (wildfireGlow == null)
            {
                wildfireGlow = PresentationPrimitives.CreateChild(
                    effectsRoot, "WildfireGlow", PrimitiveType.Sphere,
                    new Vector3(0f, 1.2f, 0f), new Vector3(6f, 0.4f, 6f),
                    PresentationMaterials.Wildfire);
                wildfireGlow.SetActive(false);
            }

            if (smoke == null)
            {
                smoke = PresentationPrimitives.CreateChild(
                    effectsRoot, "Smoke", PrimitiveType.Sphere,
                    new Vector3(0f, 2.4f, 0f), new Vector3(3.5f, 2.2f, 3.5f),
                    PresentationMaterials.Smoke);
                smoke.SetActive(false);
            }

            if (heatHaze == null)
            {
                heatHaze = PresentationPrimitives.CreateChild(
                    effectsRoot, "HeatHaze", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.2f, 0f), new Vector3(18f, 0.02f, 18f),
                    PresentationMaterials.Wildfire);
                heatHaze.SetActive(false);
            }
        }
    }
}
