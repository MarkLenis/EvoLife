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
        [SerializeField] float resourceAbundance = ExperimentConfiguration.DefaultResourceAbundance;
        [SerializeField] float plantRegenerationMultiplier = ExperimentConfiguration.DefaultPlantRegeneration;
        [SerializeField] float mutationProbability = ExperimentConfiguration.DefaultMutationProbability;
        [SerializeField] float mutationMagnitudeScale = ExperimentConfiguration.DefaultMutationMagnitude;
        [SerializeField] float dayLengthSeconds = ExperimentConfiguration.DefaultDayLengthSeconds;
        [SerializeField] string[] enabledEnvironmentalEvents = new string[0];
        [SerializeField] ExperimentScheduledEvent[] scheduledEvents = new ExperimentScheduledEvent[0];
        [SerializeField] float predatorSpeedBias;
        [SerializeField] string curriculumStageId = "";
        [SerializeField] ExperimentStoppingConditions stopping = new ExperimentStoppingConditions();

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

        public float ResourceAbundance => resourceAbundance;
        public float PlantRegenerationMultiplier => plantRegenerationMultiplier;
        public float MutationProbability => mutationProbability;
        public float MutationMagnitudeScale => mutationMagnitudeScale;
        public float DayLengthSeconds => dayLengthSeconds;
        public string[] EnabledEnvironmentalEvents =>
            enabledEnvironmentalEvents ?? (enabledEnvironmentalEvents = new string[0]);
        public ExperimentScheduledEvent[] ScheduledEvents =>
            scheduledEvents ?? (scheduledEvents = new ExperimentScheduledEvent[0]);
        public float PredatorSpeedBias => predatorSpeedBias;
        public string CurriculumStageId => curriculumStageId ?? "";
        public ExperimentStoppingConditions Stopping
        {
            get
            {
                if (stopping == null)
                {
                    stopping = new ExperimentStoppingConditions();
                }

                return stopping;
            }
        }

        public void SetInitialPopulation(int herbivores, int predators)
        {
            initialHerbivores = Mathf.Max(0, herbivores);
            initialPredators = Mathf.Max(0, predators);
        }

        public void ApplyExperiment(ExperimentConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new System.ArgumentNullException(nameof(configuration));
            }

            experimentName = configuration.ExperimentName;
            randomSeed = configuration.RandomSeed;
            initialHerbivores = configuration.InitialHerbivores;
            initialPredators = configuration.InitialPredators;
            defaultTimeScale = configuration.DefaultTimeScale;
            herbivorePolicy = configuration.HerbivorePolicy;
            predatorPolicy = configuration.PredatorPolicy;
            scenarioId = configuration.ScenarioId;
            trainingModelId = configuration.ModelId;
            resourceAbundance = configuration.ResourceAbundance;
            plantRegenerationMultiplier = configuration.PlantRegenerationMultiplier;
            mutationProbability = configuration.MutationProbability;
            mutationMagnitudeScale = configuration.MutationMagnitudeScale;
            dayLengthSeconds = configuration.DayLengthSeconds;
            predatorSpeedBias = configuration.PredatorSpeedBias;
            curriculumStageId = configuration.CurriculumStageId;
            enabledEnvironmentalEvents = (string[])configuration.EnabledEnvironmentalEvents.Clone();
            var sourceEvents = configuration.ScheduledEvents;
            scheduledEvents = new ExperimentScheduledEvent[sourceEvents.Length];
            for (var i = 0; i < sourceEvents.Length; i++)
            {
                scheduledEvents[i] = sourceEvents[i] != null ? sourceEvents[i].Clone() : null;
            }

            stopping = configuration.Stopping.Clone();
            configuration.ApplyToEcosystem(Ecosystem);
        }

        public ExperimentConfiguration ToExperimentConfiguration()
        {
            var enabled = EnabledEnvironmentalEvents;
            var enabledCopy = new string[enabled.Length];
            System.Array.Copy(enabled, enabledCopy, enabled.Length);
            var sourceEvents = ScheduledEvents;
            var scheduledCopy = new ExperimentScheduledEvent[sourceEvents.Length];
            for (var i = 0; i < sourceEvents.Length; i++)
            {
                scheduledCopy[i] = sourceEvents[i] != null ? sourceEvents[i].Clone() : null;
            }

            return new ExperimentConfiguration
            {
                ExperimentName = ExperimentName,
                RandomSeed = RandomSeed,
                InitialHerbivores = InitialHerbivores,
                InitialPredators = InitialPredators,
                ResourceAbundance = ResourceAbundance,
                PlantRegenerationMultiplier = PlantRegenerationMultiplier,
                MutationProbability = MutationProbability,
                MutationMagnitudeScale = MutationMagnitudeScale,
                DayLengthSeconds = DayLengthSeconds,
                EnabledEnvironmentalEvents = enabledCopy,
                ScheduledEvents = scheduledCopy,
                HerbivorePolicy = HerbivorePolicy,
                PredatorPolicy = PredatorPolicy,
                MaxHerbivores = Ecosystem.MaxHerbivores,
                MaxPredators = Ecosystem.MaxPredators,
                MinHerbivores = Ecosystem.MinHerbivores,
                MinPredators = Ecosystem.MinPredators,
                EcosystemMode = Ecosystem.Mode,
                TrainingRespawnEnabled = Ecosystem.TrainingRespawnEnabled,
                TrainingRespawnIntervalSeconds = Ecosystem.TrainingRespawnIntervalSeconds,
                FounderSpawnRadius = Ecosystem.FounderSpawnRadius,
                DefaultTimeScale = DefaultTimeScale,
                ScenarioId = ScenarioId,
                ModelId = TrainingModelId,
                CurriculumStageId = CurriculumStageId,
                PredatorSpeedBias = PredatorSpeedBias,
                Stopping = Stopping.Clone()
            };
        }
    }
}
