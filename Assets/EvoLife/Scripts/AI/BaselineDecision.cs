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
    /// Output of one baseline decision. Locomotion is always legal [-1, 1] after steering.
    /// Interaction flags request owner APIs; they do not mutate vitals themselves.
    /// </summary>
    public readonly struct BaselineDecision
    {
        public BaselineDecision(
            BaselineMotive motive,
            float moveX,
            float moveZ,
            bool tryEat,
            bool tryDrink,
            bool tryAttack,
            bool rest)
        {
            Motive = motive;
            MoveX = moveX;
            MoveZ = moveZ;
            TryEat = tryEat;
            TryDrink = tryDrink;
            TryAttack = tryAttack;
            Rest = rest;
        }

        public BaselineMotive Motive { get; }
        public float MoveX { get; }
        public float MoveZ { get; }
        public bool TryEat { get; }
        public bool TryDrink { get; }
        public bool TryAttack { get; }
        public bool Rest { get; }
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
