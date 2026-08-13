using System.Collections.Generic;
using System.Globalization;
using EvoLife.Common;

namespace EvoLife.UI
{
    /// <summary>
    /// Formats active ecological events for the control panel. Does not apply effects.
    /// </summary>
    public static class EventPanelPresenter
    {
        public static readonly EnvironmentalEventKind[] TriggerableKinds =
        {
            EnvironmentalEventKind.Drought,
            EnvironmentalEventKind.Wildfire,
            EnvironmentalEventKind.HeatWave,
            EnvironmentalEventKind.FoodBoom,
            EnvironmentalEventKind.DiseasePressure,
            EnvironmentalEventKind.PredatorIntroduction,
            EnvironmentalEventKind.PredatorRemoval
        };

        public static string FormatKind(EnvironmentalEventKind kind) =>
            EnvironmentalEventKindNames.ToWireName(kind);

        public static string FormatActiveList(IReadOnlyList<IReadOnlyEnvironmentalEvent> events, float simulationTime)
        {
            if (events == null || events.Count == 0)
            {
                return "none";
            }

            var parts = new string[events.Count];
            var written = 0;
            for (var i = 0; i < events.Count; i++)
            {
                var item = events[i];
                if (item == null || !item.IsActive)
                {
                    continue;
                }

                parts[written++] = FormatActiveEvent(item, simulationTime);
            }

            if (written == 0)
            {
                return "none";
            }

            return string.Join(", ", parts, 0, written);
        }

        public static string FormatActiveEvent(IReadOnlyEnvironmentalEvent occurrence, float simulationTime)
        {
            if (occurrence == null)
            {
                return "none";
            }

            var name = FormatKind(occurrence.Kind);
            var remaining = RemainingSeconds(occurrence, simulationTime);
            if (!remaining.HasValue)
            {
                return name;
            }

            return name + " (" + remaining.Value.ToString("0.0", CultureInfo.InvariantCulture) + "s remaining)";
        }

        public static float? RemainingSeconds(IReadOnlyEnvironmentalEvent occurrence, float simulationTime)
        {
            if (occurrence == null || !occurrence.IsActive)
            {
                return null;
            }

            var duration = occurrence.EndsAtSimulationTime - occurrence.StartedAtSimulationTime;
            if (duration <= 0f)
            {
                return null;
            }

            var remaining = occurrence.EndsAtSimulationTime - simulationTime;
            return remaining < 0f ? 0f : remaining;
        }

        public static void RequestTrigger(IEnvironmentalEventCommands commands, EnvironmentalEventKind kind)
        {
            commands?.Trigger(kind);
        }
    }
}
