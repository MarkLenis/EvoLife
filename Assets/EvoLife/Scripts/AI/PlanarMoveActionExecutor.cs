using UnityEngine;
using EvoLife.Creatures;

namespace EvoLife.AI
{
    /// <summary>
    /// Applies continuous actions as a planar move intent.
    /// Action layout: see <see cref="CreatureActionSchema"/>.
    /// Uses <see cref="CreatureCapabilityMotor"/> speed when present; never mutates vitals fields.
    /// </summary>
    public sealed class PlanarMoveActionExecutor : MonoBehaviour, IActionExecutor
    {
        [SerializeField] float moveSpeed = 3.5f;
        [SerializeField] CreatureCapabilityMotor motor;
        [SerializeField] CreatureVitals vitals;

        readonly float[] clamped = new float[CreatureActionSchema.ContinuousCount];
        Vector3 pendingVelocity;

        public int ActionSize => CreatureActionSchema.ContinuousCount;

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
        }

        public void ApplyActions(float[] actions)
        {
            CreatureActionSchema.ClampTo(actions, clamped);
            var speed = motor != null ? motor.MaxSpeed : moveSpeed;
            pendingVelocity = new Vector3(
                clamped[CreatureActionSchema.IndexMoveX],
                0f,
                clamped[CreatureActionSchema.IndexMoveZ]) * speed;

            if (vitals != null)
            {
                vitals.CurrentActivity = pendingVelocity.sqrMagnitude > 0.0001f
                    ? ActivityLevel.Walking
                    : ActivityLevel.Idle;
            }
        }

        void Update()
        {
            if (pendingVelocity.sqrMagnitude > 0f)
            {
                transform.position += pendingVelocity * Time.deltaTime;
            }
        }
    }
}
