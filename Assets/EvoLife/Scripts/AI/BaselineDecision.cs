using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Selected scripted behavior. Priority is evaluated from vitals + local sensors,
    /// not a fixed animation timeline.
    /// </summary>
    public enum BaselineMotive : byte
    {
        Wander = 0,
        SeekWater = 1,
        SeekFood = 2,
        Flee = 3,
        Rest = 4,
        Hunt = 5
    }

    /// <summary>
    /// Output of one baseline decision. Locomotion is CreatureActionSchema v2
    /// (forward / turn / sprint). Interaction is the same discrete branch PPO uses.
    /// </summary>
    public readonly struct BaselineDecision
    {
        public BaselineDecision(
            BaselineMotive motive,
            float forward,
            float turn,
            float sprintOrEffort,
            int interaction)
        {
            Motive = motive;
            Forward = Mathf.Clamp(forward, -1f, 1f);
            Turn = Mathf.Clamp(turn, -1f, 1f);
            SprintOrEffort = Mathf.Clamp01(sprintOrEffort);
            Interaction = CreatureActionSchema.ClampInteraction(interaction);
        }

        public BaselineMotive Motive { get; }
        public float Forward { get; }
        public float Turn { get; }
        public float SprintOrEffort { get; }
        public int Interaction { get; }

        public bool TryEat => Interaction == CreatureActionSchema.InteractionEat;
        public bool TryDrink => Interaction == CreatureActionSchema.InteractionDrink;
        public bool TryAttack => Interaction == CreatureActionSchema.InteractionAttack;
        public bool Rest => Interaction == CreatureActionSchema.InteractionRest;
        public bool TryReproduce => Interaction == CreatureActionSchema.InteractionReproduceRequest;
    }

    /// <summary>
    /// Sticky target / wander / chase state. Owned by one policy instance.
    /// </summary>
    public sealed class BaselineMemory
    {
        public BaselineMotive CurrentMotive { get; set; } = BaselineMotive.Wander;
        public float MotiveHoldSeconds { get; set; }
        public float WanderHeadingX { get; set; }
        public float WanderHeadingZ { get; set; } = 1f;
        public bool HasWanderHeading { get; set; }
        public float WanderElapsedSeconds { get; set; }
        public float ChaseElapsedSeconds { get; set; }
        public float HuntCooldownRemaining { get; set; }
        public float InteractCooldownRemaining { get; set; }
        public bool HasHuntTarget { get; set; }

        public void ResetMotive(BaselineMotive motive)
        {
            CurrentMotive = motive;
            MotiveHoldSeconds = 0f;
            if (motive != BaselineMotive.Hunt)
            {
                ChaseElapsedSeconds = 0f;
                HasHuntTarget = false;
            }
        }

        public void BeginInteractCooldown(float seconds)
        {
            InteractCooldownRemaining = Mathf.Max(0f, seconds);
        }
    }
}
