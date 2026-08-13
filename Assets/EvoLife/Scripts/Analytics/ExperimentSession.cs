using System;
using System.Threading.Tasks;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Simulation;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Creates a backend run with enough metadata to reproduce/identify the experiment.
    /// Failures are logged; the simulation continues.
    /// </summary>
    public sealed class ExperimentSession : MonoBehaviour
    {
        [SerializeField] SimulationConfig config;
        [SerializeField] BackendClient backendClient;
        [SerializeField] PopulationStatisticCollector collector;
        [SerializeField] bool createRunOnStart = true;

        public string RunId { get; private set; }
        public ExperimentRunMetadata Metadata { get; private set; }
        public bool RunReady { get; private set; }
        public bool StartupComplete { get; private set; }

        void Start()
        {
            if (createRunOnStart)
            {
                _ = BeginAsync();
            }
            else
            {
                StartupComplete = true;
            }
        }

        public async Task<bool> BeginAsync()
        {
            Metadata = ExperimentRunMetadata.FromConfig(
                config,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            try
            {
                if (backendClient == null)
                {
                    RunId = collector != null ? collector.ExperimentIdValue : "local-dev";
                    collector?.SetExperimentId(new ExperimentId(RunId));
                    RunReady = false;
                    return false;
                }

                var created = await backendClient.CreateRunAsync(Metadata);
                if (!string.IsNullOrEmpty(created))
                {
                    RunId = created;
                    RunReady = true;
                    collector?.SetExperimentId(new ExperimentId(RunId));
                    return true;
                }

                RunId = collector != null ? collector.ExperimentIdValue : "local-dev";
                collector?.SetExperimentId(new ExperimentId(RunId));
                RunReady = false;
                Debug.LogWarning("ExperimentSession: backend run create failed; simulation will continue locally.");
                return false;
            }
            finally
            {
                StartupComplete = true;
            }
        }
    }
}
