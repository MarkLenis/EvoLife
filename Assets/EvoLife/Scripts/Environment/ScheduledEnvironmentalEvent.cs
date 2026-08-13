using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// One scheduled trigger time. Kind is resolved against <see cref="EnvironmentalEventConfig"/>.
    /// </summary>
    [Serializable]
    public sealed class ScheduledEnvironmentalEvent
    {
        [SerializeField] float atSimulationTime;
        [SerializeField] EnvironmentalEventKind kind;

        public float AtSimulationTime
        {
            get => atSimulationTime;
            set => atSimulationTime = Mathf.Max(0f, value);
        }

        public EnvironmentalEventKind Kind
        {
            get => kind;
            set => kind = value;
        }
    }
}
