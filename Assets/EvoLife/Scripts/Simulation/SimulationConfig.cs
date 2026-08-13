using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    [CreateAssetMenu(fileName = "SimulationConfig", menuName = "EvoLife/Simulation/Config")]
    public sealed class SimulationConfig : ScriptableObject
    {
        [SerializeField] string experimentName = "baseline";
        [SerializeField] int randomSeed = 42;
        [SerializeField] int initialHerbivores = 20;
        [SerializeField] int initialPredators = 5;
        [SerializeField] float defaultTimeScale = 1f;
        [SerializeField] AgentPolicyKind herbivorePolicy = AgentPolicyKind.ScriptedBaseline;
        [SerializeField] AgentPolicyKind predatorPolicy = AgentPolicyKind.ScriptedBaseline;
        [SerializeField] string scenarioId = "";
        [SerializeField] string trainingModelId = "";
        [SerializeField] EcosystemSettings ecosystem = new EcosystemSettings();

        public string ExperimentName => experimentName;
        public int RandomSeed => randomSeed;
        public int InitialHerbivores => initialHerbivores;
        public int InitialPredators => initialPredators;
        public float DefaultTimeScale => defaultTimeScale;
        public AgentPolicyKind HerbivorePolicy => herbivorePolicy;
        public AgentPolicyKind PredatorPolicy => predatorPolicy;
        public string ScenarioId => scenarioId;
        public string TrainingModelId => trainingModelId;
        public EcosystemSettings Ecosystem
        {
            get
            {
                if (ecosystem == null)
                {
                    ecosystem = new EcosystemSettings();
                }

                return ecosystem;
            }
        }

        public void SetInitialPopulation(int herbivores, int predators)
        {
            initialHerbivores = Mathf.Max(0, herbivores);
            initialPredators = Mathf.Max(0, predators);
        }
    }
}
