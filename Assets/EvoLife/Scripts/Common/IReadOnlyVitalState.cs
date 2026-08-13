namespace EvoLife.Common
{
    /// <summary>
    /// Read-only biological vitals owned by the Creatures module.
    /// AI, Analytics, and UI consume this contract; they must not mutate vitals directly.
    /// </summary>
    public interface IReadOnlyVitalState
    {
        float Health { get; }
        float MaxHealth { get; }
        float Hunger { get; }
        float Thirst { get; }
        float Energy { get; }
        float MaxEnergy { get; }
        float Age { get; }
        float MaxAge { get; }
        bool IsAlive { get; }
        DeathCause? CauseOfDeath { get; }
    }
}
