using UnityEngine;
using EvoLife.Creatures;

namespace EvoLife.AI
{
    /// <summary>
    /// Applies CreatureActionSchema v2 as local-forward movement, yaw turn, and optional
    /// sprint effort, then dispatches the discrete interaction through
    /// <see cref="ICreatureInteractor"/>. Uses <see cref="CreatureCapabilityMotor"/> speeds
    /// when present. If a Rigidbody is attached, motion goes through it instead of fighting
    /// Transform writes.
    /// </summary>
    public sealed class PlanarMoveActionExecutor : MonoBehaviour, IActionExecutor
    {
        [SerializeField] float moveSpeed = 3.5f;
        [SerializeField] float fallbackSprintSpeed = 7f;
        [SerializeField] float turnSpeedDegrees = 180f;
        [SerializeField] CreatureCapabilityMotor motor;
        [SerializeField] CreatureVitals vitals;

        readonly float[] clamped = new float[CreatureActionSchema.ContinuousCount];
        ICreatureInteractor interactor;
        Rigidbody body;
        float pendingForwardSpeed;
        float pendingYawDegreesPerSecond;

        public int ActionSize => CreatureActionSchema.ContinuousCount;

        public void BindInteractor(ICreatureInteractor value) => interactor = value;

        void Awake()
        {
            if (motor == null)
            {
                motor = GetComponent<CreatureCapabilityMotor>();
            }

            if (vitals == null)
            {
                vitals = GetComponent<CreatureVitals>();
            }

            body = GetComponent<Rigidbody>();
        }

        public void ApplyActions(float[] actions) =>
            ApplyActions(actions, CreatureActionSchema.InteractionNone);

        public void ApplyActions(float[] continuousActions, int interaction)
        {
            CreatureActionSchema.ClampTo(continuousActions, clamped);
            interaction = CreatureActionSchema.ClampInteraction(interaction);

            var rest = interaction == CreatureActionSchema.InteractionRest;
            var walkSpeed = motor != null ? motor.MaxSpeed : moveSpeed;
            var sprintSpeed = motor != null ? motor.SprintSpeed : fallbackSprintSpeed;
            LocalLocomotionMath.Evaluate(
                rest ? 0f : clamped[CreatureActionSchema.IndexForward],
                rest ? 0f : clamped[CreatureActionSchema.IndexTurn],
                rest ? 0f : clamped[CreatureActionSchema.IndexSprintOrEffort],
                walkSpeed,
                sprintSpeed,
                turnSpeedDegrees,
                out pendingForwardSpeed,
                out pendingYawDegreesPerSecond);

            if (vitals != null && !rest)
            {
                if (Mathf.Abs(pendingForwardSpeed) > 0.0001f)
                {
                    var effort = clamped[CreatureActionSchema.IndexSprintOrEffort];
                    vitals.CurrentActivity = effort > 0.5f ? ActivityLevel.Sprinting : ActivityLevel.Walking;
                }
                else
                {
                    vitals.CurrentActivity = ActivityLevel.Idle;
                }
            }

            CreatureActionExecution.TryApplyInteraction(interactor, interaction);
        }

        void Update()
        {
            if (body != null)
            {
                return;
            }

            ApplyMotion(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            ApplyMotion(Time.fixedDeltaTime);
        }

        void ApplyMotion(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            var yaw = pendingYawDegreesPerSecond * deltaTime;
            if (body != null)
            {
                var rotation = Quaternion.Euler(0f, yaw, 0f) * body.rotation;
                body.MoveRotation(rotation);
                body.MovePosition(body.position + rotation * Vector3.forward * (pendingForwardSpeed * deltaTime));
                return;
            }

            if (Mathf.Abs(yaw) > 0f)
            {
                transform.Rotate(0f, yaw, 0f, Space.World);
            }

            if (Mathf.Abs(pendingForwardSpeed) > 0.0001f)
            {
                transform.position += transform.forward * (pendingForwardSpeed * deltaTime);
            }
        }
    }
}
