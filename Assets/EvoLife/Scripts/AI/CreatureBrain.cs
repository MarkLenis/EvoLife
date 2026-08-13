using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Environment;
using EvoLife.Genetics;

namespace EvoLife.AI
{
    /// <summary>
    /// Policy entry point attached to a creature. Selects scripted vs learned PPO control.
    /// Only one control policy is active at a time. Does not own vitals or genetics.
    /// </summary>
    public sealed class CreatureBrain : MonoBehaviour, IPolicyKindOwner, IEpisodeMetrics
    {
        [SerializeField] AgentPolicyKind policyKind = AgentPolicyKind.ScriptedBaseline;
        [SerializeField] CreatureVitals vitals;
        [SerializeField] CreatureIdentity identity;
        [SerializeField] CreatureGenome genome;
        [SerializeField] CreatureCapabilityMotor motor;
        [SerializeField] PlanarMoveActionExecutor actionExecutor;
        [SerializeField] EvoLifeCreatureAgent mlAgent;
        [SerializeField] ResourceRegistry resourceRegistry;
        [SerializeField] TrainingRewardSettings rewardSettings = new TrainingRewardSettings();
        [Tooltip("Optional species/role profile. When unset, role default thresholds are used.")]
        [SerializeField] ScriptedBaselineProfile baselineProfile;
        [Tooltip("If enabled, ignore role defaults and use the inline settings below (when no profile is assigned).")]
        [SerializeField] bool useInlineBaselineSettings;
        [SerializeField] ScriptedBaselineSettings baselineSettings = new ScriptedBaselineSettings();

        IObservationSource observations;
        IEpisodeRewardCalculator rewards;
        ICreaturePolicy policy;
        CreatureControlMode controlMode;

        public AgentPolicyKind PolicyKind => policyKind;

        public CreatureControlMode ActiveControlMode => controlMode;

        public float EpisodeSurvivalSeconds => vitals != null ? vitals.Age : 0f;

        public bool HasEpisodeReturn =>
            policyKind == AgentPolicyKind.LearnedPpo && mlAgent != null && mlAgent.HasEpisodeReturn;

        public float EpisodeReturn => HasEpisodeReturn ? mlAgent.EpisodeReturn : 0f;

        public int CompletedEpisodeCount => mlAgent != null ? mlAgent.CompletedEpisodeCount : 0;

        void Awake()
        {
            if (vitals == null)
            {
                vitals = GetComponent<CreatureVitals>();
            }

            if (identity == null)
            {
                identity = GetComponent<CreatureIdentity>();
            }

            if (genome == null)
            {
                genome = GetComponent<CreatureGenome>();
            }

            if (motor == null)
            {
                motor = GetComponent<CreatureCapabilityMotor>();
            }

            if (actionExecutor == null)
            {
                actionExecutor = GetComponent<PlanarMoveActionExecutor>();
            }

            if (mlAgent == null)
            {
                mlAgent = GetComponent<EvoLifeCreatureAgent>();
            }

            if (resourceRegistry == null)
            {
                resourceRegistry = FindObjectOfType<ResourceRegistry>();
            }

            observations = CreatureObservationFactory.Create(
                vitals,
                identity,
                genome,
                motor,
                transform,
                resourceRegistry);
            rewards = new TrainingRewardCalculator(rewardSettings);
            ApplyControlMode();
        }

        public void SetPolicyKind(AgentPolicyKind kind)
        {
            policyKind = kind;
            ApplyControlMode();
        }

        void FixedUpdate()
        {
            if (controlMode == CreatureControlMode.LearnedPpo)
            {
                return;
            }

            if (policy == null || vitals == null || !vitals.IsAlive)
            {
                return;
            }

            policy.Step(observations, actionExecutor, rewards, vitals);
        }

        void ApplyControlMode()
        {
            var learned = policyKind == AgentPolicyKind.LearnedPpo;
            if (learned)
            {
                policy = CreatePpoFallbackOrNull();
                controlMode = policy == null
                    ? CreatureControlMode.LearnedPpo
                    : CreatureControlMode.PpoFallbackIdle;
            }
            else
            {
                policy = CreateScriptedBaseline();
                controlMode = CreatureControlMode.ScriptedBaseline;
            }

            if (mlAgent != null)
            {
                mlAgent.Bind(observations, rewards, actionExecutor, vitals);
                mlAgent.SetControlEnabled(controlMode == CreatureControlMode.LearnedPpo);
            }
        }

        ICreaturePolicy CreateScriptedBaseline()
        {
            var role = identity != null ? identity.Role : CreatureRole.Herbivore;
            ScriptedBaselineSettings settings;
            if (baselineProfile != null)
            {
                settings = baselineProfile.Settings;
            }
            else if (useInlineBaselineSettings && baselineSettings != null)
            {
                settings = baselineSettings;
            }
            else
            {
                settings = ScriptedBaselineSettings.ForRole(role);
            }
            var seed = identity != null ? identity.Id.Value : 1;
            ICreatureInteractor interactor = vitals != null
                ? new LocalCreatureInteractor(
                    vitals,
                    transform,
                    resourceRegistry,
                    identity,
                    () => CreatureObservationFactory.ResolveSenseRange(motor),
                    settings)
                : null;
            return new ScriptedBaselinePolicy(settings, role, seed, interactor);
        }

        ICreaturePolicy CreatePpoFallbackOrNull()
        {
#if EVOLIFE_MLAGENTS
            return mlAgent != null ? null : new PpoPolicyAdapter();
#else
            return new PpoPolicyAdapter();
#endif
        }
    }

    /// <summary>
    /// Exclusive controller currently driving the creature. Scripted and PPO never run together.
    /// </summary>
    public enum CreatureControlMode : byte
    {
        None = 0,
        ScriptedBaseline = 1,
        LearnedPpo = 2,
        PpoFallbackIdle = 3
    }

    public interface ICreaturePolicy
    {
        void Step(
            IObservationSource observationSource,
            IActionExecutor actionExecutor,
            IRewardCalculator rewardCalculator,
            IReadOnlyVitalState vitals);
    }
}
