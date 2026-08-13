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

        Task<bool> BeginAsync();

        Task<bool> FinishAsync(string status, string stopReason);
    }
}
