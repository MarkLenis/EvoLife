using System;
using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Scriptable event catalog plus optional deterministic schedule.
    /// </summary>
    [CreateAssetMenu(fileName = "EnvironmentalEvents", menuName = "EvoLife/Environment/Event Config")]
    public sealed class EnvironmentalEventConfig : ScriptableObject
    {
        [SerializeField] int seed = 42;
        [SerializeField] List<EnvironmentalEventDefinition> definitions = new List<EnvironmentalEventDefinition>();
        [SerializeField] List<ScheduledEnvironmentalEvent> schedule = new List<ScheduledEnvironmentalEvent>();

        public int Seed
        {
            get => seed;
            set => seed = value;
        }

        public IReadOnlyList<EnvironmentalEventDefinition> Definitions =>
            definitions ?? (definitions = new List<EnvironmentalEventDefinition>());

        public IReadOnlyList<ScheduledEnvironmentalEvent> Schedule =>
            schedule ?? (schedule = new List<ScheduledEnvironmentalEvent>());

        public void SetSchedule(IEnumerable<ScheduledEnvironmentalEvent> entries)
        {
            if (schedule == null)
            {
                schedule = new List<ScheduledEnvironmentalEvent>();
            }

            schedule.Clear();
            if (entries == null)
            {
                return;
            }

            foreach (var entry in entries)
            {
                if (entry != null)
                {
                    schedule.Add(entry);
                }
            }
        }

        public void SetDefinitions(IEnumerable<EnvironmentalEventDefinition> next)
        {
            if (definitions == null)
            {
                definitions = new List<EnvironmentalEventDefinition>();
            }

            definitions.Clear();
            if (next == null)
            {
                return;
            }

            foreach (var definition in next)
            {
                if (definition != null)
                {
                    definitions.Add(definition);
                }
            }
        }

        public EnvironmentalEventDefinition Resolve(EnvironmentalEventKind kind)
        {
            if (definitions != null)
            {
                for (var i = 0; i < definitions.Count; i++)
                {
                    if (definitions[i] != null && definitions[i].Kind == kind)
                    {
                        return definitions[i].Clone();
                    }
                }
            }

            return EnvironmentalEventDefinition.Defaults(kind);
        }
    }
}
