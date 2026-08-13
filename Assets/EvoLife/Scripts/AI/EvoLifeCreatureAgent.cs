#if EVOLIFE_MLAGENTS
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
#endif
using UnityEngine;
using EvoLife.Creatures;
using EvoLife.Environment;
using EvoLife.Genetics;

namespace EvoLife.AI
{
    /// <summary>
    /// ML-Agents Agent bridge for one EvoLife creature.
    /// Reads observations, applies CreatureActionSchema v2 through <see cref="IActionExecutor"/>
    /// (local locomotion plus discrete interaction), and scores rewards through
    /// <see cref="IEpisodeRewardCalculator"/>. Does not own vitals or genetics.
    /// </summary>
#if EVOLIFE_MLAGENTS
    [RequireComponent(typeof(BehaviorParameters))]
    public sealed class EvoLifeCreatureAgent : Agent
#else
    public sealed class EvoLifeCreatureAgent : MonoBehaviour
#endif
    {
        [SerializeField] CreatureVitals vitals;
        [SerializeField] CreatureIdentity identity;
        [SerializeField] CreatureGenome genome;
        [SerializeField] CreatureCapabilityMotor motor;
        [SerializeField] PlanarMoveActionExecutor actionExecutor;
        [SerializeField] ResourceRegistry resourceRegistry;
        [SerializeField] TrainingRewardSettings rewardSettings = new TrainingRewardSettings();
        [SerializeField] int maxEpisodeSteps = 5000;
        [Tooltip("Experimental: move this creature back to its spawn pose when an episode begins. Does not reset the ecosystem.")]
        [SerializeField] bool resetLocalPoseOnEpisodeBegin;
        [Tooltip("Experimental: reinitialize this creature's vitals on episode begin. Does not reset the ecosystem.")]
        [SerializeField] bool reinitializeVitalsOnEpisodeBegin;

        IObservationSource observations;
        IEpisodeRewardCalculator rewards;
        IActionExecutor executor;
        readonly float[] observationBuffer = new float[CreatureObservationSchema.Size];
        readonly float[] actionBuffer = new float[CreatureActionSchema.ContinuousCount];
        bool controlEnabled;
        bool episodeClosing;
        Vector3 episodeStartPosition;
        Quaternion episodeStartRotation;
        bool startPoseCaptured;
        float lastCompletedEpisodeReturn;
        int completedEpisodeCount;
        bool hasCompletedEpisodeReturn;

        public string BehaviorName => MlAgentsBehaviorNames.ForRole(
            identity != null ? identity.Role : CreatureRoleOrDefault());

        public int ObservationSize => CreatureObservationSchema.Size;

        public int ActionSize => CreatureActionSchema.ContinuousCount;

        public int DiscreteBranchSize => CreatureActionSchema.InteractionBranchSize;

        public int DiscreteBranchCount => CreatureActionSchema.DiscreteBranchCount;

        public bool ControlEnabled => controlEnabled;

        public TrainingRewardSettings RewardSettings => rewardSettings;

#if EVOLIFE_MLAGENTS
        public bool HasEpisodeReturn => true;

        public float EpisodeReturn => GetCumulativeReward();
#else
        public bool HasEpisodeReturn => hasCompletedEpisodeReturn;

        public float EpisodeReturn => lastCompletedEpisodeReturn;
#endif

        public int CompletedEpisodeCount => completedEpisodeCount;

        public void Bind(
            IObservationSource observationSource,
            IEpisodeRewardCalculator rewardCalculator,
            IActionExecutor actionExecutorBinding,
            CreatureVitals creatureVitals)
        {
            observations = observationSource;
            rewards = rewardCalculator;
            executor = actionExecutorBinding;
            if (creatureVitals != null)
            {
                vitals = creatureVitals;
            }
        }

        public void SetControlEnabled(bool enabled)
        {
            controlEnabled = enabled;
            this.enabled = enabled;

#if EVOLIFE_MLAGENTS
            var requester = GetComponent<DecisionRequester>();
            if (requester == null && enabled)
            {
                requester = gameObject.AddComponent<DecisionRequester>();
                requester.DecisionPeriod = 5;
                requester.TakeActionsBetweenDecisions = true;
            }

            if (requester != null)
            {
                requester.enabled = enabled;
            }
#endif
        }

#if EVOLIFE_MLAGENTS
        public override void Initialize()
        {
            CaptureStartPose();
            EnsureBindings();
            ApplyBehaviorConfiguration();
            MaxStep = Mathf.Max(0, maxEpisodeSteps);

            if (vitals != null)
            {
                vitals.Died -= OnCreatureDied;
                vitals.Died += OnCreatureDied;
            }
        }

        public override void OnEpisodeBegin()
        {
            episodeClosing = false;
            rewards?.OnEpisodeBegin();
            if (resetLocalPoseOnEpisodeBegin && startPoseCaptured)
            {
                transform.SetPositionAndRotation(episodeStartPosition, episodeStartRotation);
            }

            if (reinitializeVitalsOnEpisodeBegin && vitals != null)
            {
                vitals.Reinitialize();
                if (motor != null && genome != null)
                {
                    motor.ApplyPhenotype(genome);
                }
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            EnsureBindings();
            for (var i = 0; i < observationBuffer.Length; i++)
            {
                observationBuffer[i] = 0f;
            }

            observations?.WriteObservations(observationBuffer);
            for (var i = 0; i < CreatureObservationSchema.Size; i++)
            {
                sensor.AddObservation(observationBuffer[i]);
            }
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!controlEnabled || episodeClosing)
            {
                return;
            }

            EnsureBindings();
            actionBuffer[CreatureActionSchema.IndexForward] = 0f;
            actionBuffer[CreatureActionSchema.IndexTurn] = 0f;
            actionBuffer[CreatureActionSchema.IndexSprintOrEffort] = 0f;
            var continuous = actions.ContinuousActions;
            if (continuous.Length >= CreatureActionSchema.ContinuousCount)
            {
                actionBuffer[CreatureActionSchema.IndexForward] = continuous[CreatureActionSchema.IndexForward];
                actionBuffer[CreatureActionSchema.IndexTurn] = continuous[CreatureActionSchema.IndexTurn];
                actionBuffer[CreatureActionSchema.IndexSprintOrEffort] =
                    continuous[CreatureActionSchema.IndexSprintOrEffort];
            }

            var interaction = CreatureActionSchema.InteractionNone;
            var discrete = actions.DiscreteActions;
            if (discrete.Length > 0)
            {
                interaction = discrete[0];
            }

            CreatureActionSchema.ClampTo(actionBuffer, actionBuffer);
            interaction = CreatureActionSchema.ClampInteraction(interaction);
            executor?.ApplyActions(actionBuffer, interaction);

            var signal = rewards != null
                ? rewards.Evaluate(vitals)
                : RewardSignal.None;
            AddReward(signal.Reward);

            if (signal.TerminateEpisode || (vitals != null && !vitals.IsAlive))
            {
                CloseEpisode();
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuous = actionsOut.ContinuousActions;
            if (continuous.Length >= CreatureActionSchema.ContinuousCount)
            {
                continuous[CreatureActionSchema.IndexForward] = Input.GetAxisRaw("Vertical");
                continuous[CreatureActionSchema.IndexTurn] = Input.GetAxisRaw("Horizontal");
                continuous[CreatureActionSchema.IndexSprintOrEffort] = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;
            }

            var discrete = actionsOut.DiscreteActions;
            if (discrete.Length > 0)
            {
                discrete[0] = CreatureActionSchema.InteractionNone;
            }
        }

        void OnDestroy()
        {
            if (vitals != null)
            {
                vitals.Died -= OnCreatureDied;
            }
        }

        void OnCreatureDied(CreatureDiedEventArgs _)
        {
            if (!controlEnabled || !isActiveAndEnabled || episodeClosing)
            {
                return;
            }

            var signal = rewards != null
                ? rewards.Evaluate(vitals)
                : new RewardSignal(rewardSettings != null ? rewardSettings.DeathPenalty : -1f, true);
            AddReward(signal.Reward);
            CloseEpisode();
        }

        void CloseEpisode()
        {
            if (episodeClosing)
            {
                return;
            }

            episodeClosing = true;
#if EVOLIFE_MLAGENTS
            lastCompletedEpisodeReturn = GetCumulativeReward();
            hasCompletedEpisodeReturn = true;
            completedEpisodeCount++;
            EndEpisode();
#endif
        }

        void ApplyBehaviorConfiguration()
        {
            var parameters = GetComponent<BehaviorParameters>();
            if (parameters == null)
            {
                return;
            }

            parameters.BehaviorName = MlAgentsBehaviorNames.ForRole(
                identity != null ? identity.Role : CreatureRoleOrDefault());
            parameters.BrainParameters.VectorObservationSize = CreatureObservationSchema.Size;
            parameters.BrainParameters.NumStackedVectorObservations = 1;
            parameters.BrainParameters.ActionSpec = new ActionSpec(
                CreatureActionSchema.ContinuousCount,
                new[] { CreatureActionSchema.InteractionBranchSize });
        }
#else
        void Awake()
        {
            CaptureStartPose();
            EnsureBindings();
        }
#endif

        void CaptureStartPose()
        {
            episodeStartPosition = transform.position;
            episodeStartRotation = transform.rotation;
            startPoseCaptured = true;
        }

        void EnsureBindings()
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

            if (executor == null)
            {
                executor = actionExecutor;
            }

            if (rewards == null)
            {
                rewards = new TrainingRewardCalculator(rewardSettings);
            }

            if (observations == null)
            {
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
            }
        }

        static Common.CreatureRole CreatureRoleOrDefault() => Common.CreatureRole.Herbivore;
    }
}
