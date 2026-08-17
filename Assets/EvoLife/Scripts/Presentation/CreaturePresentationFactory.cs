using System;
using System.Collections.Generic;
using UnityEngine;
using EvoLife.AI;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Genetics;
using EvoLife.Simulation;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Builds a complete demo creature template. Visuals live under a child named Visual;
    /// simulation and AI scripts stay on the stable root.
    /// </summary>
    public static class CreaturePresentationFactory
    {
        public static readonly Type[] RequiredRootComponents =
        {
            typeof(CreatureIdentity),
            typeof(CreatureVitals),
            typeof(CreatureGenome),
            typeof(CreatureCapabilityMotor),
            typeof(CreatureBrain),
            typeof(PlanarMoveActionExecutor),
            typeof(EvoLifeCreatureAgent),
            typeof(CreatureReproductionBridge),
            typeof(Collider),
            typeof(CreaturePresentation),
            typeof(PhenotypeVisual)
        };

        public static GameObject CreateTemplate(CreatureRole role, Transform parent = null)
        {
            var name = role == CreatureRole.Predator ? "Predator" : "Herbivore";
            var root = new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            var identity = root.AddComponent<CreatureIdentity>();
            identity.Assign(new CreatureId(0), role == CreatureRole.Predator ? "predator" : "herbivore", role);
            root.AddComponent<CreatureVitals>();
            root.AddComponent<CreatureGenome>();
            root.AddComponent<CreatureCapabilityMotor>();
            root.AddComponent<PlanarMoveActionExecutor>();
            root.AddComponent<CreatureBrain>();
            root.AddComponent<EvoLifeCreatureAgent>();
            root.AddComponent<CreatureReproductionBridge>();

            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.45f, 0f);
            collider.radius = role == CreatureRole.Predator ? 0.36f : 0.34f;
            collider.height = role == CreatureRole.Predator ? 1.05f : 1.0f;
            collider.direction = 1;

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;

            var presentation = root.AddComponent<CreaturePresentation>();
            presentation.EnsureVisuals();
            root.AddComponent<PhenotypeVisual>();
            return root;
        }

        public static IReadOnlyList<string> MissingRequirements(GameObject instance)
        {
            var missing = new List<string>();
            if (instance == null)
            {
                missing.Add("GameObject");
                return missing;
            }

            for (var i = 0; i < RequiredRootComponents.Length; i++)
            {
                var type = RequiredRootComponents[i];
                if (instance.GetComponent(type) == null)
                {
                    missing.Add(type.Name);
                }
            }

            var identity = instance.GetComponent<CreatureIdentity>();
            if (identity == null)
            {
                return missing;
            }

            var presentation = instance.GetComponent<CreaturePresentation>();
            if (presentation != null && presentation.Role != identity.Role && presentation.HasVisuals)
            {
                missing.Add("CreaturePresentation.Role mismatch");
            }

            var meshCollider = instance.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                missing.Add("unexpected MeshCollider on root");
            }

            return missing;
        }

        public static bool HasRequiredComponents(GameObject instance) =>
            MissingRequirements(instance).Count == 0;
    }
}
