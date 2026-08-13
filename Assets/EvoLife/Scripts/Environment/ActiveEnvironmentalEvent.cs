using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Runtime occurrence of a configured ecological event.
    /// </summary>
    public sealed class ActiveEnvironmentalEvent : IReadOnlyEnvironmentalEvent
    {
        public ActiveEnvironmentalEvent(
            int eventId,
            EnvironmentalEventDefinition definition,
            float startedAtSimulationTime)
        {
            EventId = eventId;
            Definition = definition ?? EnvironmentalEventDefinition.Defaults(EnvironmentalEventKind.Drought);
            Kind = Definition.Kind;
            StartedAtSimulationTime = startedAtSimulationTime;
            var duration = Definition.DurationSeconds;
            EndsAtSimulationTime = duration <= 0f
                ? startedAtSimulationTime
                : startedAtSimulationTime + duration;
            IsActive = true;
        }

        public int EventId { get; }
        public EnvironmentalEventKind Kind { get; }
        public float StartedAtSimulationTime { get; }
        public float EndsAtSimulationTime { get; }
        public bool IsActive { get; private set; }
        public EnvironmentalEventDefinition Definition { get; }

        public void MarkEnded() => IsActive = false;
    }
}
