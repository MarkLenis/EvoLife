namespace EvoLife.Biology
{
    /// <summary>
    /// Reason a creature died. Used by downstream systems for analytics and behavior.
    /// </summary>
    public enum DeathCause
    {
        Unknown = 0,
        Predation,
        Starvation,
        Dehydration,
        OldAge,
        Environmental,
    }
}
