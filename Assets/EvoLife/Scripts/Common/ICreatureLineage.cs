namespace EvoLife.Common
{
    /// <summary>
    /// Spawn lineage metadata. Simulation records this at spawn; Genetics does not own it.
    /// Parent IDs are omitted for founders (generation 0).
    /// </summary>
    public interface ICreatureLineage
    {
        int Generation { get; }
        CreatureId? ParentA { get; }
        CreatureId? ParentB { get; }
    }
}
