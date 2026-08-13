namespace EvoLife.AI
{
    /// <summary>
    /// Executes policy actions on locomotion / interaction components.
    /// </summary>
    public interface IActionExecutor
    {
        int ActionSize { get; }
        void ApplyActions(float[] actions);
    }
}
