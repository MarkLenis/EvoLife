using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Stylized herbivore / predator meshes as visual children. Simulation scripts stay on the root.
    /// Silhouette (not only color) distinguishes roles; head/snout mark +Z forward, tail marks rear.
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

            PresentationPrimitives.ClearChildren(visualRoot);
            if (!TryAttachCreatureModel(visualRoot, role))
            {
                BuildPrimitiveMeshes(visualRoot, role);
            }

            AttachFacingLocators(visualRoot, role);
            BodyColor = PresentationPalette.ForRole(role);
            HasVisuals = true;
        }

        static bool TryAttachCreatureModel(Transform visualRoot, CreatureRole role)
        {
            var path = role == CreatureRole.Predator
                ? PresentationModelLibrary.Predator
                : PresentationModelLibrary.Herbivore;
            var scale = role == CreatureRole.Predator ? 0.82f : 0.72f;
            var yaw = role == CreatureRole.Predator ? -90f : 0f;
            return PresentationModelLibrary.TrySpawn(
                visualRoot, "Model", path, Vector3.zero, scale, new Vector3(0f, yaw, 0f), true, out _);
        }

        static void AttachFacingLocators(Transform visualRoot, CreatureRole role)
        {
            EnsureLocator(visualRoot, "Body", Vector3.zero);
            if (role == CreatureRole.Predator)
            {
                EnsureLocator(visualRoot, "Head", new Vector3(0f, 0.62f, 0.95f));
                EnsureLocator(visualRoot, "Snout", new Vector3(0f, 0.48f, 1.45f));
                EnsureLocator(visualRoot, "Tail", new Vector3(0f, 0.55f, -1.15f));
                EnsureLocator(visualRoot, "EarL", new Vector3(-0.18f, 0.85f, 0.72f));
            }
            else
            {
                EnsureLocator(visualRoot, "Head", new Vector3(0f, 1.15f, 0.55f));
                EnsureLocator(visualRoot, "Snout", new Vector3(0f, 0.95f, 0.95f));
                EnsureLocator(visualRoot, "Tail", new Vector3(0f, 0.85f, -0.85f));
                EnsureLocator(visualRoot, "EarL", new Vector3(-0.16f, 1.45f, 0.42f));
            }
        }

        static void EnsureLocator(Transform parent, string name, Vector3 localPosition)
        {
            if (parent.Find(name) != null)
            {
                return;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
        }

        public static void BuildMeshes(Transform visualRoot, CreatureRole role)
        {
            if (visualRoot == null)
            {
                return;
            }

            if (TryAttachCreatureModel(visualRoot, role))
            {
                AttachFacingLocators(visualRoot, role);
                return;
            }

            BuildPrimitiveMeshes(visualRoot, role);
        }

        static void BuildPrimitiveMeshes(Transform visualRoot, CreatureRole role)
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
                        new Vector3(0f, 0.38f, 0.12f), new Vector3(0.58f, 1.05f, 0.58f), body,
                        new Vector3(90f, 0f, 0f), true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Head", PrimitiveType.Cube,
                        new Vector3(0f, 0.50f, 1.12f), new Vector3(0.48f, 0.38f, 0.52f), body,
                        new Vector3(8f, 0f, 0f), true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Snout", PrimitiveType.Cube,
                        new Vector3(0f, 0.40f, 1.52f), new Vector3(0.22f, 0.16f, 0.48f), accent,
                        default, true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "EarL", PrimitiveType.Cube,
                        new Vector3(-0.16f, 0.72f, 0.98f), new Vector3(0.08f, 0.18f, 0.10f), body,
                        new Vector3(0f, 0f, -12f), true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "EarR", PrimitiveType.Cube,
                        new Vector3(0.16f, 0.72f, 0.98f), new Vector3(0.08f, 0.18f, 0.10f), body,
                        new Vector3(0f, 0f, 12f), true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Tail", PrimitiveType.Capsule,
                        new Vector3(0f, 0.46f, -1.05f), new Vector3(0.14f, 0.72f, 0.14f), accent,
                        new Vector3(75f, 0f, 0f), true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "LegFL", PrimitiveType.Cylinder,
                        new Vector3(-0.18f, 0.16f, 0.48f), new Vector3(0.09f, 0.16f, 0.09f), body,
                        default, true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "LegFR", PrimitiveType.Cylinder,
                        new Vector3(0.18f, 0.16f, 0.48f), new Vector3(0.09f, 0.16f, 0.09f), body,
                        default, true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "LegBL", PrimitiveType.Cylinder,
                        new Vector3(-0.18f, 0.16f, -0.32f), new Vector3(0.10f, 0.16f, 0.10f), body,
                        default, true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "LegBR", PrimitiveType.Cylinder,
                        new Vector3(0.18f, 0.16f, -0.32f), new Vector3(0.10f, 0.16f, 0.10f), body,
                        default, true);
                    break;
                case CreatureRole.Herbivore:
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Body", PrimitiveType.Sphere,
                        new Vector3(0f, 0.62f, 0.02f), new Vector3(0.70f, 0.82f, 0.78f), body,
                        default, true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Head", PrimitiveType.Sphere,
                        new Vector3(0f, 1.08f, 0.42f), new Vector3(0.42f, 0.40f, 0.44f), body,
                        default, true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Snout", PrimitiveType.Cube,
                        new Vector3(0f, 0.96f, 0.68f), new Vector3(0.16f, 0.12f, 0.22f), accent,
                        default, true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "EarL", PrimitiveType.Capsule,
                        new Vector3(-0.16f, 1.42f, 0.34f), new Vector3(0.12f, 0.28f, 0.10f), body,
                        new Vector3(12f, 0f, -22f), true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "EarR", PrimitiveType.Capsule,
                        new Vector3(0.16f, 1.42f, 0.34f), new Vector3(0.12f, 0.28f, 0.10f), body,
                        new Vector3(12f, 0f, 22f), true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "Tail", PrimitiveType.Sphere,
                        new Vector3(0f, 0.70f, -0.46f), new Vector3(0.16f, 0.18f, 0.22f), accent,
                        default, true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "LegFL", PrimitiveType.Cylinder,
                        new Vector3(-0.18f, 0.22f, 0.20f), new Vector3(0.08f, 0.22f, 0.08f), body,
                        default, true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "LegFR", PrimitiveType.Cylinder,
                        new Vector3(0.18f, 0.22f, 0.20f), new Vector3(0.08f, 0.22f, 0.08f), body,
                        default, true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "LegBL", PrimitiveType.Cylinder,
                        new Vector3(-0.18f, 0.22f, -0.20f), new Vector3(0.08f, 0.22f, 0.08f), body,
                        default, true);
                    PresentationPrimitives.CreateChild(
                        visualRoot, "LegBR", PrimitiveType.Cylinder,
                        new Vector3(0.18f, 0.22f, -0.20f), new Vector3(0.08f, 0.22f, 0.08f), body,
                        default, true);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }
    }
}
