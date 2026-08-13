using System.Threading.Tasks;

namespace EvoLife.Common
{
    /// <summary>
    /// Analytics-owned experiment run recorder. Simulation starts/stops a run
    /// without referencing the Analytics assembly.
    /// </summary>
    public interface IExperimentAnalyticsSession
    {
        string RunId { get; }

        bool RunReady { get; }

        void SetAutoStart(bool enabled);

        /// <summary>
        /// Starts the analytics run. Return true if startup succeeded, including
        /// local-dev without a backend. Return false if the experiment must stay
        /// paused and must not enter Running.
        /// </summary>
        Task<bool> BeginAsync();

        Task<bool> FinishAsync(string status, string stopReason);
    }
}
