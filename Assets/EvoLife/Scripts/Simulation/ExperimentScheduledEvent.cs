using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// One scheduled ecological event in an experiment config.
    /// </summary>
    [Serializable]
    public sealed class ExperimentScheduledEvent
    {
        [SerializeField] string kind = EnvironmentalEventKindNames.Drought;
        [SerializeField] float atSimulationTime;

        public string Kind
        {
            get => string.IsNullOrEmpty(kind) ? EnvironmentalEventKindNames.Drought : kind;
            set => kind = value;
        }

        public float AtSimulationTime
        {
            get => atSimulationTime;
            set => atSimulationTime = Mathf.Max(0f, value);
        }

        public bool TryGetKind(out EnvironmentalEventKind eventKind) =>
            EnvironmentalEventKindNames.TryParse(Kind, out eventKind);

        public ExperimentScheduledEvent Clone()
        {
            return new ExperimentScheduledEvent
            {
                Kind = Kind,
                AtSimulationTime = AtSimulationTime
            };
        }
    }
}
