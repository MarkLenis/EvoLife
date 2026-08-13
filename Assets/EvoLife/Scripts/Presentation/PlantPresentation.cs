using UnityEngine;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Optional plant visual. Does not own <see cref="PlantResource"/> lifecycle.
    /// Depletion only scales / tints the Visual child.
    /// </summary>
    public sealed class PlantPresentation : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] Renderer foliageRenderer;

        PlantResource plant;
        readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        static readonly int ColorId = Shader.PropertyToID("_Color");
        bool visualsReady;

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

            if (visualRoot.childCount == 0)
            {
                PresentationPrimitives.CreateChild(
                    visualRoot, "Stem", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.28f, 0f), new Vector3(0.08f, 0.28f, 0.08f),
                    PresentationMaterials.PlantDepleted);
                var foliage = PresentationPrimitives.CreateChild(
                    visualRoot, "Foliage", PrimitiveType.Sphere,
                    new Vector3(0f, 0.62f, 0f), new Vector3(0.45f, 0.4f, 0.45f),
                    PresentationMaterials.PlantHealthy);
                foliageRenderer = foliage.GetComponent<Renderer>();
            }
            else if (foliageRenderer == null)
            {
                var foliage = visualRoot.Find("Foliage");
                foliageRenderer = foliage != null ? foliage.GetComponent<Renderer>() : visualRoot.GetComponentInChildren<Renderer>();
            }

            var trigger = GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.35f, 0f);
            trigger.radius = 0.4f;
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

            var scale = Mathf.Lerp(0.35f, 1f, fill);
            visualRoot.localScale = new Vector3(scale, Mathf.Lerp(0.45f, 1f, fill), scale);

            if (foliageRenderer == null)
            {
                return;
            }

            var color = Color.Lerp(PresentationPalette.PlantDepleted, PresentationPalette.PlantHealthy, fill);
            foliageRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorId, color);
            foliageRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
