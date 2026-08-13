using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Genetics;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Spawns creature prefabs and wires identity/genome/vitals. Does not implement AI or analytics.
    /// </summary>
    public sealed class CreatureSpawner : MonoBehaviour
    {
        [SerializeField] PopulationTracker populationTracker;
        [SerializeField] SpeciesVitalsDefinition defaultVitals;
        [SerializeField] int nextCreatureId = 1;
        [SerializeField] int geneCount = 4;

        readonly IGeneticOperators geneticOperators = new DefaultGeneticOperators();
        readonly IGenomeDecoder genomeDecoder = new LinearGenomeDecoder();
        System.Random random = new System.Random(1);

        public void SetSeed(int seed) => random = new System.Random(seed);

        public GameObject Spawn(
            GameObject prefab,
            Vector3 position,
            string speciesId,
            CreatureRole role,
            Genome genome = null)
        {
            if (prefab == null)
            {
                Debug.LogError("CreatureSpawner: prefab is null.");
                return null;
            }

            var instance = Instantiate(prefab, position, Quaternion.identity);
            var id = new CreatureId(nextCreatureId++);

            var identity = instance.GetComponent<CreatureIdentity>();
            if (identity != null)
            {
                identity.Assign(id, speciesId, role);
            }

            var vitals = instance.GetComponent<CreatureVitals>();
            if (vitals != null && defaultVitals != null)
            {
                vitals.Initialize(defaultVitals);
            }

            var creatureGenome = instance.GetComponent<CreatureGenome>();
            var resolved = genome ?? geneticOperators.CreateRandom(geneCount, random);
            if (creatureGenome != null)
            {
                creatureGenome.Initialize(resolved, genomeDecoder);
            }

            var motor = instance.GetComponent<CreatureCapabilityMotor>();
            if (motor != null && creatureGenome != null)
            {
                motor.ApplyPhenotype(creatureGenome);
            }

            populationTracker?.Register(id, role);
            return instance;
        }
    }
}
