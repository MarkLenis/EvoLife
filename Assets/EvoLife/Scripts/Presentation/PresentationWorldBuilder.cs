using UnityEngine;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Builds the demo world visuals after resources are placed. Safe when optional
    /// references are missing.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class PresentationWorldBuilder : MonoBehaviour
    {
        [SerializeField] ResourceManager resourceManager;
        [SerializeField] Transform worldRoot;
        [SerializeField] BiomeGroundPresenter ground;
        [SerializeField] EnvironmentalEventVisualAdapter eventVisuals;
        [SerializeField] bool attachResourceVisuals = true;

        public BiomeGroundPresenter Ground => ground;
        public bool Built { get; private set; }

        public void Bind(
            ResourceManager manager,
            Transform root,
            BiomeGroundPresenter groundPresenter,
            EnvironmentalEventVisualAdapter events)
        {
            resourceManager = manager;
            worldRoot = root;
            ground = groundPresenter;
            eventVisuals = events;
        }

        void Start() => Build();

        public void Build()
        {
            if (resourceManager != null)
            {
                resourceManager.EnsurePlaced();
            }

            if (worldRoot == null)
            {
                var found = transform.Find("WorldPresentation");
                worldRoot = found != null ? found : transform;
            }

            if (ground == null)
            {
                ground = GetComponent<BiomeGroundPresenter>() ?? gameObject.AddComponent<BiomeGroundPresenter>();
            }

            ground.Bind(resourceManager, worldRoot);
            if (resourceManager != null)
            {
                ground.Build();
            }

            if (attachResourceVisuals && resourceManager != null)
            {
                AttachPlantVisuals();
                AttachWaterVisuals();
            }

            eventVisuals?.RefreshVisuals();
            Built = true;
        }

        void AttachPlantVisuals()
        {
            var plants = resourceManager.Plants;
            for (var i = 0; i < plants.Count; i++)
            {
                var plant = plants[i];
                if (plant == null)
                {
                    continue;
                }

                var presentation = plant.GetComponent<PlantPresentation>();
                if (presentation == null)
                {
                    presentation = plant.gameObject.AddComponent<PlantPresentation>();
                }

                presentation.EnsureVisuals();
            }
        }

        void AttachWaterVisuals()
        {
            var waters = resourceManager.Waters;
            for (var i = 0; i < waters.Count; i++)
            {
                var water = waters[i];
                if (water == null)
                {
                    continue;
                }

                var presentation = water.GetComponent<WaterPresentation>();
                if (presentation == null)
                {
                    presentation = water.gameObject.AddComponent<WaterPresentation>();
                }

                presentation.EnsureVisuals();
            }
        }
    }
}
