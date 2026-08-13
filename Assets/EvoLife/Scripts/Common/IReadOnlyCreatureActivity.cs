namespace EvoLife.Common
{
    /// <summary>
    /// Optional read-only activity label for inspectors. Creatures own the value;
    /// missing implementations mean the inspector shows unavailable.
    /// </summary>
    public interface IReadOnlyCreatureActivity
    {
        string CurrentActivity { get; }
    }
}
