namespace EvoLife.Common
{
    /// <summary>
    /// Pause and speed controls owned by Simulation. UI may request these; it must not
    /// set <c>Time.timeScale</c> itself.
    /// </summary>
    public interface ISimulationClockControl : ISimulationClock
    {
        void SetPaused(bool paused);

        void SetTimeScale(float scale);
    }
}
