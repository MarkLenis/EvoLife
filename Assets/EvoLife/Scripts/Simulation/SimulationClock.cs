using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Owns simulation time and pause/speed. Not a god-object — only clock concerns.
    /// </summary>
    public sealed class SimulationClock : MonoBehaviour, ISimulationClock
    {
        [SerializeField] float timeScale = 1f;
        [SerializeField] bool isPaused;

        float simulationTimeSeconds;

        public float SimulationTimeSeconds => simulationTimeSeconds;
        public float DeltaTimeSeconds => isPaused ? 0f : Time.deltaTime * timeScale;
        public float TimeScale => timeScale;
        public bool IsPaused => isPaused;

        public void SetPaused(bool paused) => isPaused = paused;

        public void SetTimeScale(float scale) => timeScale = Mathf.Max(0f, scale);

        void Update()
        {
            var dt = DeltaTimeSeconds;
            if (dt > 0f)
            {
                simulationTimeSeconds += dt;
            }
        }
    }
}
