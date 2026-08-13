using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace EvoLife.Analytics
{
    /// <summary>
    /// HTTP client for the FastAPI analytics backend. Owns transport only.
    /// Failures are logged and returned as false so the simulation can continue.
    /// </summary>
    public sealed class BackendClient : MonoBehaviour
    {
        [SerializeField] string baseUrl = "http://127.0.0.1:8000";
        [SerializeField] bool enableUploads = true;

        public string BaseUrl
        {
            get => baseUrl;
            set => baseUrl = value;
        }

        public bool EnableUploads
        {
            get => enableUploads;
            set => enableUploads = value;
        }

        public async Task<bool> PostSnapshotAsync(SimulationStatsSnapshot snapshot)
        {
            if (!enableUploads || snapshot == null)
            {
                return false;
            }

            return await PostJsonAsync("/api/v1/stats", JsonUtility.ToJson(snapshot));
        }

        public async Task<string> CreateRunAsync(ExperimentRunMetadata metadata)
        {
            if (!enableUploads || metadata == null)
            {
                return null;
            }

            var payload = new RunCreateDto
            {
                ExperimentName = string.IsNullOrEmpty(metadata.ExperimentName) ? "unnamed" : metadata.ExperimentName,
                RandomSeed = metadata.RandomSeed,
                Status = "running",
                Configuration = metadata.ToConfigurationDictionary(),
                Metadata = new Dictionary<string, object>
                {
                    ["started_at_unix"] = metadata.StartedAtUnix,
                    ["source"] = "unity"
                }
            };

            var json = await PostJsonForBodyAsync("/api/v1/runs", AnalyticsJson.Serialize(payload));
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                var runId = ExtractRunId(json);
                return string.IsNullOrEmpty(runId) ? null : runId;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"BackendClient: could not parse run response ({ex.Message}).");
                return null;
            }
        }

        static string ExtractRunId(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            const string key = "\"run_id\"";
            var index = json.IndexOf(key, StringComparison.Ordinal);
            if (index < 0)
            {
                return null;
            }

            var colon = json.IndexOf(':', index + key.Length);
            var start = json.IndexOf('"', colon + 1);
            var end = start >= 0 ? json.IndexOf('"', start + 1) : -1;
            if (start < 0 || end <= start)
            {
                return null;
            }

            return json.Substring(start + 1, end - start - 1);
        }

        public Task<bool> PostSnapshotBatchAsync(string runId, IList<SnapshotCreateDto> snapshots)
        {
            if (!enableUploads || string.IsNullOrEmpty(runId) || snapshots == null || snapshots.Count == 0)
            {
                return Task.FromResult(false);
            }

            var payload = new SnapshotBatchDto { Snapshots = new List<SnapshotCreateDto>(snapshots) };
            return PostJsonAsync($"/api/v1/runs/{runId}/snapshots/batch", AnalyticsJson.Serialize(payload));
        }

        public Task<bool> PostCreatureRecordsAsync(string runId, IList<CreatureLifeRecordDto> records)
        {
            if (!enableUploads || string.IsNullOrEmpty(runId) || records == null || records.Count == 0)
            {
                return Task.FromResult(false);
            }

            var payload = new CreatureBatchDto { Records = new List<CreatureLifeRecordDto>(records) };
            return PostJsonAsync($"/api/v1/runs/{runId}/creatures", AnalyticsJson.Serialize(payload));
        }

        public Task<bool> PostGenerationSummariesAsync(string runId, IList<GenerationSummaryDto> summaries)
        {
            if (!enableUploads || string.IsNullOrEmpty(runId) || summaries == null || summaries.Count == 0)
            {
                return Task.FromResult(false);
            }

            var payload = new GenerationBatchDto { Summaries = new List<GenerationSummaryDto>(summaries) };
            return PostJsonAsync($"/api/v1/runs/{runId}/generations", AnalyticsJson.Serialize(payload));
        }

        public Task<bool> FinishRunAsync(string runId, string status = "completed")
        {
            if (!enableUploads || string.IsNullOrEmpty(runId))
            {
                return Task.FromResult(false);
            }

            var json = "{\"status\":\"" + (status ?? "completed") + "\"}";
            return PostJsonAsync($"/api/v1/runs/{runId}/finish", json);
        }

        async Task<bool> PostJsonAsync(string path, string json)
        {
            var body = await PostJsonForBodyAsync(path, json);
            return body != null;
        }

        async Task<string> PostJsonForBodyAsync(string path, string json)
        {
            if (!enableUploads)
            {
                return null;
            }

            try
            {
                var url = $"{baseUrl.TrimEnd('/')}{path}";
                using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    var bytes = Encoding.UTF8.GetBytes(json ?? "{}");
                    request.uploadHandler = new UploadHandlerRaw(bytes);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");

                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

#if UNITY_2020_2_OR_NEWER
                    var ok = request.result == UnityWebRequest.Result.Success;
#else
                    var ok = !request.isNetworkError && !request.isHttpError;
#endif
                    if (!ok)
                    {
                        Debug.LogWarning($"BackendClient POST {path} failed: {request.responseCode} {request.error}");
                        return null;
                    }

                    return request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"BackendClient POST {path} exception: {ex.Message}");
                return null;
            }
        }
    }
}
