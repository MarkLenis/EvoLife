using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace EvoLife.Analytics
{
    /// <summary>
    /// HTTP client for the FastAPI analytics backend. Owns transport only.
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

        public async Task<bool> PostSnapshotAsync(SimulationStatsSnapshot snapshot)
        {
            if (!enableUploads || snapshot == null)
            {
                return false;
            }

            var json = JsonUtility.ToJson(snapshot);
            var url = $"{baseUrl.TrimEnd('/')}/api/v1/stats";

            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                var body = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
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
                    Debug.LogWarning($"BackendClient POST failed: {request.responseCode} {request.error}");
                }

                return ok;
            }
        }
    }
}
