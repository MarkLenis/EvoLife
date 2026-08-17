using UnityEngine;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Optional plant visual. Does not own <see cref="PlantResource"/> lifecycle.
    /// Depletion only scales / tints the Visual child. Does not change interaction radius.
    /// </summary>
    public sealed class PlantPresentation : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] Renderer foliageRenderer;

        PlantResource plant;
        MaterialPropertyBlock propertyBlock;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        bool visualsReady;

        MaterialPropertyBlock PropertyBlock => propertyBlock ??= new MaterialPropertyBlock();

        void Awake()
        {
            plant = GetComponent<PlantResource>();
            EnsureVisuals();
        }

        void LateUpdate() => ApplyDepletion();

        public void EnsureVisuals()
        {
            if (visualsReady && visualRoot != null)
            {
                return;
            }

            visualRoot = PresentationPrimitives.EnsureVisualRoot(transform);
            if (visualRoot == null)
            {
                return;
            }

            PresentationPrimitives.ClearChildren(visualRoot);
            if (!PresentationModelLibrary.TrySpawn(
                visualRoot, "Foliage", PresentationModelLibrary.EdiblePlant,
                Vector3.zero, 4.2f, Vector3.zero, false, out var foliage))
            {
                PresentationPrimitives.CreateChild(
                    visualRoot, "Stem", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.32f, 0f), new Vector3(0.10f, 0.32f, 0.10f),
                    PresentationMaterials.PlantDepleted);
                PresentationPrimitives.CreateChild(
                    visualRoot, "LeafA", PrimitiveType.Sphere,
                    new Vector3(-0.28f, 0.62f, 0.08f), new Vector3(0.52f, 0.16f, 0.38f),
                    PresentationMaterials.PlantHealthy);
                PresentationPrimitives.CreateChild(
                    visualRoot, "LeafB", PrimitiveType.Sphere,
                    new Vector3(0.26f, 0.66f, -0.10f), new Vector3(0.48f, 0.14f, 0.40f),
                    PresentationMaterials.PlantHealthy);
                foliage = PresentationPrimitives.CreateChild(
                    visualRoot, "Foliage", PrimitiveType.Sphere,
                    new Vector3(0f, 0.88f, 0.02f), new Vector3(0.72f, 0.48f, 0.72f),
                    PresentationMaterials.PlantHealthy);
            }

            foliageRenderer = foliage != null
                ? foliage.GetComponentInChildren<Renderer>()
                : visualRoot.GetComponentInChildren<Renderer>();

            EnsureTriggerWithoutChangingRadius();
            visualsReady = true;
        }

        public void ApplyDepletion()
        {
            if (visualRoot == null)
            {
                return;
            }

            var fill = 1f;
            if (plant != null && plant.Capacity > 0f)
            {
                fill = Mathf.Clamp01(plant.AvailableAmount / plant.Capacity);
            }

            var scale = Mathf.Lerp(0.42f, 1f, fill);
            visualRoot.localScale = new Vector3(scale, Mathf.Lerp(0.50f, 1f, fill), scale);

            if (foliageRenderer == null)
            {
                return;
            }

            var color = Color.Lerp(PresentationPalette.PlantDepleted, PresentationPalette.PlantHealthy, fill);
            var block = PropertyBlock;
            foliageRenderer.GetPropertyBlock(block);
            block.SetColor(ColorId, color);
            foliageRenderer.SetPropertyBlock(block);
        }

        void EnsureTriggerWithoutChangingRadius()
        {
            var trigger = GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
                trigger.center = new Vector3(0f, 0.35f, 0f);
                trigger.radius = 0.4f;
            }

            trigger.isTrigger = true;
        }
    }
}
