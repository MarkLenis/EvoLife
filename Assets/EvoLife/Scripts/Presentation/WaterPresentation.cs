using UnityEngine;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Stylized water visual. Drinking still uses <see cref="WaterSource"/> / ResourceRegistry.
    /// Geometry is a flat disc; the collider is a trigger so it does not block locomotion.
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

            if (visualRoot.childCount == 0)
            {
                var disc = PresentationPrimitives.CreateChild(
                    visualRoot, "Surface", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.04f, 0f), new Vector3(2.4f, 0.03f, 2.4f),
                    PresentationMaterials.Water);
                disc.transform.localRotation = Quaternion.identity;
            }

            var trigger = GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.radius = 1.3f;
            visualsReady = true;
        }
    }
}
