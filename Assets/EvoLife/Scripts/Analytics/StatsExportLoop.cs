using UnityEngine;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Periodically captures and optionally uploads statistics.
    /// </summary>
    public sealed class StatsExportLoop : MonoBehaviour
    {
        [SerializeField] PopulationStatisticCollector collector;
        [SerializeField] BackendClient backendClient;
        [SerializeField] float intervalSeconds = 5f;
        [SerializeField] bool uploadToBackend;

        float elapsed;

        void Update()
        {
            if (collector == null || intervalSeconds <= 0f)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < intervalSeconds)
            {
                return;
            }

            elapsed = 0f;
            var snapshot = collector.Capture();

            if (uploadToBackend && backendClient != null)
            {
                _ = backendClient.PostSnapshotAsync(snapshot);
            }
        }
    }
}
