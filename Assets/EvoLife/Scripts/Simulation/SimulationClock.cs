using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Owns simulation time and pause/speed. Not a god-object — only clock concerns.
    /// Experiment pause/unpause authority lives on <c>ExperimentOrchestrator</c>:
    /// initialization stays paused until <c>BeginRunning</c>.
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

        /// <summary>
        /// Advances simulation time without waiting for <c>Update</c>. Used by
        /// reproduction cooldown tests and any explicit sim-time step.
        /// </summary>
        public void Advance(float deltaTimeSeconds)
        {
            if (isPaused || deltaTimeSeconds <= 0f)
            {
                return;
            }

            simulationTimeSeconds += deltaTimeSeconds * Mathf.Max(0f, timeScale);
        }

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
