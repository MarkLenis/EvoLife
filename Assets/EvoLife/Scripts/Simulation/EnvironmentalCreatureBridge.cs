using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Simulation adapter so Environment events never own creature biology or spawn objects.
    /// Damage goes through <see cref="CreatureVitals.ApplyDamage"/>; spawn/remove use lifecycle APIs.
    /// </summary>
    public sealed class EnvironmentalCreatureBridge : MonoBehaviour, IEnvironmentalVitalEffects, IEnvironmentalPopulationCommands
    {
        [SerializeField] CreatureSpawner spawner;
        [SerializeField] CreatureLifecycleHub lifecycleHub;
        [SerializeField] PopulationTracker populationTracker;
        [SerializeField] SimulationConfig config;
        [SerializeField] GameObject herbivorePrefab;
        [SerializeField] GameObject predatorPrefab;
        [SerializeField] Transform spawnOrigin;
        [SerializeField] int seed = 42;

        readonly List<GameObject> liveBuffer = new List<GameObject>(64);
        System.Random random = new System.Random(42);

        public void Configure(
            CreatureSpawner creatureSpawner,
            CreatureLifecycleHub hub,
            PopulationTracker tracker,
            SimulationConfig simulationConfig,
            GameObject herbivore,
            GameObject predator,
            Transform origin = null,
            int randomSeed = 42)
        {
            spawner = creatureSpawner;
            lifecycleHub = hub;
            populationTracker = tracker;
            config = simulationConfig;
            herbivorePrefab = herbivore;
            predatorPrefab = predator;
            spawnOrigin = origin;
            seed = randomSeed;
            random = new System.Random(seed);
        }

        public int ApplyEnvironmentalDamage(float amount, DeathCause cause)
        {
            if (amount <= 0f || lifecycleHub == null)
            {
                return 0;
            }

            lifecycleHub.CopyLiveInstances(liveBuffer);
            var hit = 0;
            for (var i = 0; i < liveBuffer.Count; i++)
            {
                var instance = liveBuffer[i];
                if (instance == null)
                {
                    continue;
                }

                var vitals = instance.GetComponent<CreatureVitals>();
                if (vitals == null || !vitals.IsAlive)
                {
                    continue;
                }

                vitals.ApplyDamage(amount, cause);
                hit++;
            }

            return hit;
        }

        public int SpawnRole(CreatureRole role, int count)
        {
            if (spawner == null || count <= 0)
            {
                return 0;
            }

            var prefab = PrefabFor(role);
            if (prefab == null)
            {
                return 0;
            }

            var ecosystem = config != null ? config.Ecosystem : new EcosystemSettings();
            var origin = spawnOrigin != null ? spawnOrigin.position : Vector3.zero;
            var radius = ecosystem.FounderSpawnRadius;
            var species = ecosystem.SpeciesIdFor(role);
            var policy = config != null
                ? (role == CreatureRole.Predator ? config.PredatorPolicy : config.HerbivorePolicy)
                : AgentPolicyKind.ScriptedBaseline;

            var spawned = 0;
            for (var i = 0; i < count; i++)
            {
                if (ecosystem.CapFor(role) > 0 && populationTracker != null
                    && populationTracker.CountFor(role) >= ecosystem.CapFor(role))
                {
                    break;
                }

                var angle = (float)(random.NextDouble() * System.Math.PI * 2.0);
                var distance = radius <= 0f ? 0f : (float)random.NextDouble() * radius;
                var position = origin + new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance);
                if (spawner.Spawn(prefab, position, species, role, genome: null, policy, generation: 0) != null)
                {
                    spawned++;
                }
            }

            return spawned;
        }

        public int RemoveRole(CreatureRole role, int count)
        {
            if (lifecycleHub == null || count <= 0)
            {
                return 0;
            }

            lifecycleHub.CopyLiveInstances(liveBuffer);
            var removed = 0;
            for (var i = 0; i < liveBuffer.Count && removed < count; i++)
            {
                var instance = liveBuffer[i];
                if (instance == null)
                {
                    continue;
                }

                var identity = instance.GetComponent<CreatureIdentity>();
                var vitals = instance.GetComponent<CreatureVitals>();
                if (identity == null || vitals == null || !vitals.IsAlive || identity.Role != role)
                {
                    continue;
                }

                vitals.Die(DeathCause.Environmental);
                removed++;
            }

            return removed;
        }

        GameObject PrefabFor(CreatureRole role)
        {
            switch (role)
            {
                case CreatureRole.Herbivore:
                    return herbivorePrefab;
                case CreatureRole.Predator:
                    return predatorPrefab;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }
    }
}
