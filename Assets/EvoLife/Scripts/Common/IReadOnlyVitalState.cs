namespace EvoLife.Common
{
    /// <summary>
    /// Read-only biological vitals owned by the Creatures module.
    /// AI, Analytics, and UI consume this contract; they must not mutate vitals directly.
    /// </summary>
    public interface IReadOnlyVitalState
    {
        float Health { get; }
        float Hunger { get; }
        float Thirst { get; }
        float Energy { get; }
        float Age { get; }
        bool IsAlive { get; }
    }
}
