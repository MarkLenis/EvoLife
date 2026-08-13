using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Places founder-generation creatures. Does not implement mating or respawn.
    /// </summary>
    public static class InitialPopulationSpawner
    {
        public static void SpawnFounders(
            CreatureSpawner spawner,
            SimulationConfig config,
            GameObject herbivorePrefab,
            GameObject predatorPrefab,
            Vector3 origin,
            System.Random random)
        {
            if (spawner == null || config == null)
            {
                return;
            }

            var ecosystem = config.Ecosystem ?? new EcosystemSettings();
            SpawnRole(
                spawner,
                herbivorePrefab,
                origin,
                config.InitialHerbivores,
                ecosystem.HerbivoreSpeciesId,
                CreatureRole.Herbivore,
                config.HerbivorePolicy,
                ecosystem.FounderSpawnRadius,
                random);
            SpawnRole(
                spawner,
                predatorPrefab,
                origin,
                config.InitialPredators,
                ecosystem.PredatorSpeciesId,
                CreatureRole.Predator,
                config.PredatorPolicy,
                ecosystem.FounderSpawnRadius,
                random);
        }

        static void SpawnRole(
            CreatureSpawner spawner,
            GameObject prefab,
            Vector3 origin,
            int count,
            string speciesId,
            CreatureRole role,
            AgentPolicyKind policyKind,
            float radius,
            System.Random random)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            var rng = random ?? new System.Random(1);
            for (var i = 0; i < count; i++)
            {
                var angle = (float)(rng.NextDouble() * System.Math.PI * 2.0);
                var distance = radius <= 0f ? 0f : (float)rng.NextDouble() * radius;
                var position = origin + new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance);
                spawner.Spawn(
                    prefab,
                    position,
                    speciesId,
                    role,
                    genome: null,
                    policyKind,
                    generation: 0);
            }
        }
    }
}
