using UnityEngine;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Stylized water visual. Drinking still uses <see cref="WaterSource"/> / ResourceRegistry.
    /// Geometry is a shallow irregular pond; the collider is a trigger so it does not block locomotion.
    /// </summary>
    public sealed class WaterPresentation : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        bool visualsReady;

        void Awake()
        {
            EnsureVisuals();
        }

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
            var seed = Mathf.Abs(transform.position.GetHashCode());
            var shore = PresentationGroundMesh.CreateIrregularDisc(2.8f, 20, 0.22f, seed + 3, 0.04f);
            PresentationPrimitives.CreateMeshChild(
                visualRoot, "Shore", shore, new Vector3(0f, 0.01f, 0f), PresentationMaterials.WetMud, true);
            var surface = PresentationGroundMesh.CreateIrregularDisc(2.25f, 20, 0.20f, seed + 9, 0.03f);
            PresentationPrimitives.CreateMeshChild(
                visualRoot, "Surface", surface, new Vector3(0f, 0.04f, 0f), PresentationMaterials.Water, true);
            if (!PresentationModelLibrary.TrySpawn(
                visualRoot, "LilyA", PresentationModelLibrary.LilyLarge,
                new Vector3(-0.7f, 0.05f, 0.4f), 1.6f, new Vector3(0f, 25f, 0f), false, out _))
            {
                PresentationPrimitives.CreateChild(
                    visualRoot, "LilyA", PrimitiveType.Cylinder,
                    new Vector3(-0.55f, 0.07f, 0.35f), new Vector3(0.55f, 0.02f, 0.55f),
                    PresentationMaterials.Canopy);
            }

            if (!PresentationModelLibrary.TrySpawn(
                visualRoot, "LilyB", PresentationModelLibrary.LilySmall,
                new Vector3(0.75f, 0.05f, -0.35f), 1.4f, new Vector3(0f, 70f, 0f), false, out _))
            {
                PresentationPrimitives.CreateChild(
                    visualRoot, "LilyB", PrimitiveType.Cylinder,
                    new Vector3(0.62f, 0.07f, -0.28f), new Vector3(0.42f, 0.02f, 0.42f),
                    PresentationMaterials.Canopy);
            }

            EnsureTriggerWithoutChangingRadius();
            visualsReady = true;
        }

        void EnsureTriggerWithoutChangingRadius()
        {
            var trigger = GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
                trigger.center = Vector3.zero;
                trigger.radius = 1.8f;
            }

            trigger.isTrigger = true;
        }
    }
}
