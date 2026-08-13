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
    public sealed class CreatureBrain : MonoBehaviour, IPolicyKindOwner, IEpisodeMetrics, IPolicySeedOwner, IReadOnlyCreatureAiDebug
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

        int policySeed;
        bool hasPolicySeed;

        IObservationSource observations;
        IEpisodeRewardCalculator rewards;
        ICreaturePolicy policy;
        CreatureControlMode controlMode;
        readonly float[] ppoObservationScratch = new float[CreatureObservationSchema.Size];

        string debugControlMode = "None";
        string debugBehaviorName = "";
        float debugForward;
        float debugTurn;
        float debugSprint;
        string debugInteraction = "none";
        bool debugHasMotive;
        string debugMotive = "";
        float debugSenseRange;
        float debugInteractRange;
        float debugHeadingX;
        float debugHeadingZ;
        SensedTargetDebug debugFood;
        SensedTargetDebug debugWater;
        SensedTargetDebug debugHerbivore;
        SensedTargetDebug debugPredator;
        bool debugHasHeuristic;
        SensedTargetDebug debugHeuristic;

        public AgentPolicyKind PolicyKind => policyKind;

        public CreatureControlMode ActiveControlMode => controlMode;

        public string ControlMode => debugControlMode;

        public string BehaviorName => debugBehaviorName;

        public float Forward => debugForward;

        public float Turn => debugTurn;

        public float SprintOrEffort => debugSprint;

        public string InteractionRequest => debugInteraction;

        public bool HasScriptedMotive => debugHasMotive;

        public string ScriptedMotive => debugMotive;

        public float SensoryRange => debugSenseRange;

        public float InteractionRange => debugInteractRange;

        public float HeadingX => debugHeadingX;

        public float HeadingZ => debugHeadingZ;

        public SensedTargetDebug NearestFood => debugFood;

        public SensedTargetDebug NearestWater => debugWater;

        public SensedTargetDebug NearestHerbivore => debugHerbivore;

        public SensedTargetDebug NearestPredator => debugPredator;

        public bool HasHeuristicTarget => debugHasHeuristic;

        public SensedTargetDebug HeuristicTarget => debugHeuristic;

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
            BindCanonicalInteractor();
            ApplyControlMode();
        }

        public void SetPolicyKind(AgentPolicyKind kind)
        {
            policyKind = kind;
            ApplyControlMode();
        }

        public void SetPolicySeed(int seed)
        {
            policySeed = seed;
            hasPolicySeed = true;
            if (controlMode == CreatureControlMode.ScriptedBaseline)
            {
                policy = CreateScriptedBaseline();
            }
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

        void LateUpdate()
        {
            RefreshAiDebug();
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
            var seed = hasPolicySeed ? policySeed : identity != null ? identity.Id.Value : 1;
            return new ScriptedBaselinePolicy(ResolveBaselineSettings(role), role, seed);
        }

        void BindCanonicalInteractor()
        {
            if (actionExecutor == null)
            {
                return;
            }

            var role = identity != null ? identity.Role : CreatureRole.Herbivore;
            var settings = ResolveBaselineSettings(role);
            ICreatureInteractor interactor = vitals != null
                ? new LocalCreatureInteractor(
                    vitals,
                    transform,
                    resourceRegistry,
                    identity,
                    () => CreatureObservationFactory.ResolveSenseRange(motor),
                    settings,
                    new CreatureReproductionRequestProxy(gameObject))
                : null;
            actionExecutor.BindInteractor(interactor);
        }

        ScriptedBaselineSettings ResolveBaselineSettings(CreatureRole role)
        {
            if (baselineProfile != null)
            {
                return baselineProfile.Settings;
            }

            if (useInlineBaselineSettings && baselineSettings != null)
            {
                return baselineSettings;
            }

            return ScriptedBaselineSettings.ForRole(role);
        }

        ICreaturePolicy CreatePpoFallbackOrNull()
        {
#if EVOLIFE_MLAGENTS
            return mlAgent != null ? null : new PpoPolicyAdapter();
#else
            return new PpoPolicyAdapter();
#endif
        }

        void RefreshAiDebug()
        {
            debugControlMode = ControlModeName(controlMode);
            debugBehaviorName = MlAgentsBehaviorNames.ForRole(
                identity != null ? identity.Role : CreatureRole.Herbivore);

            if (actionExecutor != null)
            {
                debugForward = actionExecutor.LastForward;
                debugTurn = actionExecutor.LastTurn;
                debugSprint = actionExecutor.LastSprintOrEffort;
                debugInteraction = InteractionName(actionExecutor.LastInteraction);
            }
            else
            {
                debugForward = 0f;
                debugTurn = 0f;
                debugSprint = 0f;
                debugInteraction = "none";
            }

            debugSenseRange = CreatureObservationFactory.ResolveSenseRange(motor);
            var role = identity != null ? identity.Role : CreatureRole.Herbivore;
            var settings = ResolveBaselineSettings(role);
            debugInteractRange = debugSenseRange * Mathf.Clamp01(settings.InteractDistance);

            var forward = transform.forward;
            debugHeadingX = forward.x;
            debugHeadingZ = forward.z;

            var scripted = policy as ScriptedBaselinePolicy;
            debugHasMotive = scripted != null;
            debugMotive = scripted != null ? MotiveName(scripted.LastMotive) : "";

            BaselineSensedWorld world;
            if (scripted != null)
            {
                world = scripted.LastSensedWorld;
            }
            else if (mlAgent != null && mlAgent.TryCopyLastObservations(ppoObservationScratch))
            {
                world = BaselineSensedWorld.FromObservations(ppoObservationScratch);
            }
            else
            {
                world = BaselineSensedWorld.Empty;
            }

            debugFood = ToTarget(world.FoodPresent, world.FoodDirX, world.FoodDirZ, world.FoodDistance);
            debugWater = ToTarget(world.WaterPresent, world.WaterDirX, world.WaterDirZ, world.WaterDistance);
            debugHerbivore = ToTarget(
                world.HerbivorePresent,
                world.HerbivoreDirX,
                world.HerbivoreDirZ,
                world.HerbivoreDistance);
            debugPredator = ToTarget(
                world.PredatorPresent,
                world.PredatorDirX,
                world.PredatorDirZ,
                world.PredatorDistance);

            if (scripted != null)
            {
                debugHeuristic = HeuristicFromMotive(scripted.LastMotive, world);
                debugHasHeuristic = debugHeuristic.Present;
            }
            else
            {
                debugHeuristic = SensedTargetDebug.None;
                debugHasHeuristic = false;
            }
        }

        static SensedTargetDebug HeuristicFromMotive(BaselineMotive motive, BaselineSensedWorld world)
        {
            switch (motive)
            {
                case BaselineMotive.SeekFood:
                    return ToTarget(world.FoodPresent, world.FoodDirX, world.FoodDirZ, world.FoodDistance);
                case BaselineMotive.SeekWater:
                    return ToTarget(world.WaterPresent, world.WaterDirX, world.WaterDirZ, world.WaterDistance);
                case BaselineMotive.Hunt:
                    return ToTarget(
                        world.HerbivorePresent,
                        world.HerbivoreDirX,
                        world.HerbivoreDirZ,
                        world.HerbivoreDistance);
                case BaselineMotive.Flee:
                    return ToTarget(
                        world.PredatorPresent,
                        world.PredatorDirX,
                        world.PredatorDirZ,
                        world.PredatorDistance);
                case BaselineMotive.Wander:
                case BaselineMotive.Rest:
                    return SensedTargetDebug.None;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(motive), motive, "Unhandled BaselineMotive.");
            }
        }

        static SensedTargetDebug ToTarget(bool present, float dirX, float dirZ, float distance) =>
            present ? new SensedTargetDebug(true, dirX, dirZ, distance) : SensedTargetDebug.None;

        static string ControlModeName(CreatureControlMode mode)
        {
            switch (mode)
            {
                case CreatureControlMode.None:
                    return "None";
                case CreatureControlMode.ScriptedBaseline:
                    return "ScriptedBaseline";
                case CreatureControlMode.LearnedPpo:
                    return "LearnedPpo";
                case CreatureControlMode.PpoFallbackIdle:
                    return "PpoFallbackIdle";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(mode), mode, "Unhandled CreatureControlMode.");
            }
        }

        static string MotiveName(BaselineMotive motive)
        {
            switch (motive)
            {
                case BaselineMotive.Wander:
                    return "Wander";
                case BaselineMotive.SeekWater:
                    return "SeekWater";
                case BaselineMotive.SeekFood:
                    return "SeekFood";
                case BaselineMotive.Flee:
                    return "Flee";
                case BaselineMotive.Rest:
                    return "Rest";
                case BaselineMotive.Hunt:
                    return "Hunt";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(motive), motive, "Unhandled BaselineMotive.");
            }
        }

        static string InteractionName(int interaction)
        {
            var clamped = CreatureActionSchema.ClampInteraction(interaction);
            return CreatureActionSchema.InteractionNames[clamped];
        }
    }

    /// <summary>
    /// Resolves the Simulation-owned reproduction handler at request time so spawn
    /// wiring can bind after <c>Awake</c>. Does not implement mating rules.
    /// </summary>
    sealed class CreatureReproductionRequestProxy : IReproductionRequestHandler
    {
        readonly GameObject host;

        public CreatureReproductionRequestProxy(GameObject host)
        {
            this.host = host;
        }

        public void HandleReproduceRequest()
        {
            if (host == null)
            {
                return;
            }

            var components = host.GetComponents<MonoBehaviour>();
            if (components == null)
            {
                return;
            }

            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] is IReproductionRequestHandler handler)
                {
                    handler.HandleReproduceRequest();
                    return;
                }
            }
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
