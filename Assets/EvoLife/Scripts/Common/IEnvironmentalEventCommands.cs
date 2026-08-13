using System.Collections.Generic;

namespace EvoLife.Common
{
    /// <summary>
    /// Request port for configured ecological events. Environment owns effects;
    /// UI only asks the existing manager to trigger a kind.
    /// </summary>
    public interface IEnvironmentalEventCommands
    {
        void Trigger(EnvironmentalEventKind kind);

        IReadOnlyList<IReadOnlyEnvironmentalEvent> ActiveEvents { get; }
    }
}
