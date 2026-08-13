using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Thin bootstrap that ticks registered systems with the simulation clock.
    /// Intentionally small — do not turn this into a god manager.
    /// </summary>
    public sealed class SimulationRunner : MonoBehaviour
    {
        [SerializeField] SimulationClock clock;
        [SerializeField] SimulationConfig config;
        [SerializeField] List<MonoBehaviour> tickableBehaviours = new List<MonoBehaviour>();

        readonly List<ISimulationTickable> tickables = new List<ISimulationTickable>();

        public SimulationConfig Config => config;
        public ISimulationClock Clock => clock;

        void Awake()
        {
            tickables.Clear();
            for (var i = 0; i < tickableBehaviours.Count; i++)
            {
                if (tickableBehaviours[i] is ISimulationTickable tickable)
                {
                    tickables.Add(tickable);
                }
            }

            if (clock != null && config != null)
            {
                clock.SetTimeScale(config.DefaultTimeScale);
            }
        }

        void Update()
        {
            if (clock == null)
            {
                return;
            }

            var dt = clock.DeltaTimeSeconds;
            if (dt <= 0f)
            {
                return;
            }

            for (var i = 0; i < tickables.Count; i++)
            {
                tickables[i].Tick(dt);
            }
        }

        public void RegisterTickable(ISimulationTickable tickable)
        {
            if (tickable != null && !tickables.Contains(tickable))
            {
                tickables.Add(tickable);
            }
        }
    }
}
