namespace EvoLife.Biology
{
    /// <summary>
    /// Read-only access to creature biology for AI and external observers.
    /// </summary>
    public interface ICreatureStateView
    {
        CreatureState Snapshot { get; }
        MetabolicRates BaseRates { get; }
        MetabolicModifiers Modifiers { get; }
        bool IsAlive { get; }
        DeathCause? CauseOfDeath { get; }
    }
}
