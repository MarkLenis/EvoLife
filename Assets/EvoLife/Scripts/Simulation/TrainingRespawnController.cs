using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Optional training-support respawn. Disabled in persistent ecosystem mode.
    /// Spawns founders through <see cref="CreatureSpawner"/>; does not live in biology.
    /// </summary>
    public sealed class TrainingRespawnController : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] CreatureSpawner spawner;
        [SerializeField] PopulationTracker populationTracker;
        [SerializeField] SimulationConfig config;
        [SerializeField] GameObject herbivorePrefab;
        [SerializeField] GameObject predatorPrefab;

        float cooldown;
        System.Random random = new System.Random(1);

        public void Configure(
            CreatureSpawner creatureSpawner,
            PopulationTracker tracker,
            SimulationConfig simulationConfig,
            GameObject herbivore,
            GameObject predator,
            int seed = 1)
        {
            spawner = creatureSpawner;
            populationTracker = tracker;
            config = simulationConfig;
            herbivorePrefab = herbivore;
            predatorPrefab = predator;
            random = new System.Random(seed);
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (spawner == null || populationTracker == null || config == null)
            {
                return;
            }

            var ecosystem = config.Ecosystem;
            if (ecosystem == null || !ecosystem.AllowsTrainingRespawn)
            {
                return;
            }

            if (deltaTimeSeconds > 0f)
            {
                cooldown -= deltaTimeSeconds;
            }

            if (cooldown > 0f)
            {
                return;
            }

            var spawned = TryFillFloor(CreatureRole.Herbivore, herbivorePrefab)
                          | TryFillFloor(CreatureRole.Predator, predatorPrefab);
            if (spawned)
            {
                cooldown = ecosystem.TrainingRespawnIntervalSeconds;
            }
        }

        bool TryFillFloor(CreatureRole role, GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            var ecosystem = config.Ecosystem;
            var alive = populationTracker.CountFor(role);
            var floor = ecosystem.FloorFor(role);
            if (alive >= floor)
            {
                return false;
            }

            var cap = ecosystem.CapFor(role);
            if (cap > 0 && alive >= cap)
            {
                return false;
            }

            var radius = ecosystem.FounderSpawnRadius;
            var angle = (float)(random.NextDouble() * System.Math.PI * 2.0);
            var distance = radius <= 0f ? 0f : (float)random.NextDouble() * radius;
            var position = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            var policy = role == CreatureRole.Predator ? config.PredatorPolicy : config.HerbivorePolicy;
            spawner.Spawn(
                prefab,
                position,
                ecosystem.SpeciesIdFor(role),
                role,
                genome: null,
                policy,
                generation: 0);
            return true;
        }
    }
}
