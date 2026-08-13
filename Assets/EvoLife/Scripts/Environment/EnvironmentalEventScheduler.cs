using System;
using System.Collections.Generic;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Deterministic schedule walker. Same entries and time steps yield the same triggers.
    /// </summary>
    public sealed class EnvironmentalEventScheduler
    {
        readonly ScheduledEnvironmentalEvent[] entries;
        readonly bool[] fired;

        public EnvironmentalEventScheduler(IReadOnlyList<ScheduledEnvironmentalEvent> schedule)
        {
            if (schedule == null || schedule.Count == 0)
            {
                entries = Array.Empty<ScheduledEnvironmentalEvent>();
                fired = Array.Empty<bool>();
                return;
            }

            entries = new ScheduledEnvironmentalEvent[schedule.Count];
            for (var i = 0; i < schedule.Count; i++)
            {
                entries[i] = schedule[i];
            }

            Array.Sort(entries, Compare);
            fired = new bool[entries.Length];
        }

        public int Count => entries.Length;

        public int CollectDue(float previousTime, float currentTime, IList<EnvironmentalEventKind> destination)
        {
            if (destination == null || currentTime < previousTime)
            {
                return 0;
            }

            var added = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                if (fired[i] || entries[i] == null)
                {
                    continue;
                }

                if (entries[i].AtSimulationTime <= currentTime)
                {
                    destination.Add(entries[i].Kind);
                    fired[i] = true;
                    added++;
                }
            }

            return added;
        }

        static int Compare(ScheduledEnvironmentalEvent a, ScheduledEnvironmentalEvent b)
        {
            var atA = a != null ? a.AtSimulationTime : 0f;
            var atB = b != null ? b.AtSimulationTime : 0f;
            return atA.CompareTo(atB);
        }
    }
}
