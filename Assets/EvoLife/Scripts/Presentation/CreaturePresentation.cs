using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Stylized herbivore / predator meshes as visual children. Simulation scripts stay on the root.
    /// </summary>
    public sealed class CreaturePresentation : MonoBehaviour
    {
        [SerializeField] CreatureRole role = CreatureRole.Herbivore;
        [SerializeField] Transform visualRoot;

        public CreatureRole Role => role;
        public Color BodyColor { get; private set; }
        public bool HasVisuals { get; private set; }

        void Awake()
        {
            EnsureVisuals();
        }

        public void EnsureVisuals()
        {
            var identity = GetComponent<CreatureIdentity>();
            if (identity != null)
            {
                role = identity.Role;
            }

            if (HasVisuals && visualRoot != null)
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
                BuildMeshes(visualRoot, role);
            }

            BodyColor = PresentationPalette.ForRole(role);
            HasVisuals = true;
        }

        public static void BuildMeshes(Transform visualRoot, CreatureRole role)
        {
            if (visualRoot == null)
            {
                return;
            }

            var body = PresentationMaterials.ForRole(role);
            var accent = PresentationMaterials.AccentForRole(role);
            switch (role)
            {
                case CreatureRole.Predator:
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Body", PrimitiveType.Capsule,
                        new Vector3(0f, 0.45f, 0f), new Vector3(0.7f, 0.55f, 1.15f), body);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Head", PrimitiveType.Sphere,
                        new Vector3(0f, 0.62f, 0.55f), new Vector3(0.42f, 0.38f, 0.42f), body);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Snout", PrimitiveType.Cube,
                        new Vector3(0f, 0.52f, 0.88f), new Vector3(0.18f, 0.14f, 0.42f), accent);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "EarL", PrimitiveType.Cube,
                        new Vector3(-0.16f, 0.82f, 0.48f), new Vector3(0.08f, 0.18f, 0.08f), body);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "EarR", PrimitiveType.Cube,
                        new Vector3(0.16f, 0.82f, 0.48f), new Vector3(0.08f, 0.18f, 0.08f), body);
                    break;
                case CreatureRole.Herbivore:
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Body", PrimitiveType.Sphere,
                        new Vector3(0f, 0.4f, 0f), new Vector3(0.85f, 0.7f, 1f), body);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Head", PrimitiveType.Sphere,
                        new Vector3(0f, 0.55f, 0.48f), new Vector3(0.45f, 0.42f, 0.45f), body);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Snout", PrimitiveType.Cube,
                        new Vector3(0f, 0.48f, 0.72f), new Vector3(0.16f, 0.12f, 0.22f), accent);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "EarL", PrimitiveType.Sphere,
                        new Vector3(-0.18f, 0.78f, 0.4f), new Vector3(0.16f, 0.22f, 0.12f), body);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "EarR", PrimitiveType.Sphere,
                        new Vector3(0.18f, 0.78f, 0.4f), new Vector3(0.16f, 0.22f, 0.12f), body);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }
    }
}
