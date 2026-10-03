using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Sleepet
{
    [Serializable] public sealed class CompanionAIConfig
    {
        public string backendUrl = "http://127.0.0.1:8787/api/chat";
        public int timeoutSeconds = 45;
        public static CompanionAIConfig Load()
        {
            var asset = Resources.Load<TextAsset>("CompanionAIConfig");
            return asset == null ? new CompanionAIConfig() : JsonUtility.FromJson<CompanionAIConfig>(asset.text);
        }
    }
    [Serializable] public sealed class CompanionResponse
    {
        public string reply, skillId, source, provider;
    }
    [Serializable] sealed class CompanionRequest { public string message; public CompanionContext context; }

    // The client never receives an OpenAI key. A backend session token can be injected at login.
    public sealed class OpenAICompanion : ICompanionAI, ICancellableCompanionAI
    {
        readonly CompanionAIConfig config;
        readonly string token;
        UnityWebRequest pending;
        TaskCompletionSource<string> completion;
        public CompanionResponse LastResponse { get; private set; }
        public string LastError { get; private set; }
        public OpenAICompanion(CompanionAIConfig config, string sessionToken = null)
        { this.config = config; token = sessionToken; }

        public Task<string> SendMessage(string userText, CompanionContext context)
        {
            CancelPending(); LastResponse = null; LastError = null;
            if (!Uri.TryCreate(config.backendUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)))
                throw new InvalidOperationException("Use HTTPS for a remote AI backend.");
            var request = new UnityWebRequest(uri.AbsoluteUri, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new CompanionRequest { message = userText, context = context })));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrWhiteSpace(token)) request.SetRequestHeader("Authorization", "Bearer " + token);
            request.timeout = Mathf.Clamp(config.timeoutSeconds, 5, 90);
            var result = new TaskCompletionSource<string>();
            pending = request; completion = result;
            try
            {
                request.SendWebRequest().completed += operation =>
                {
                    if (pending != request) return;
                    pending = null; completion = null;
                    try
                    {
                        if (request.result != UnityWebRequest.Result.Success)
                            throw new InvalidOperationException("AI backend unavailable (HTTP " + request.responseCode + ").");
                        var response = JsonUtility.FromJson<CompanionResponse>(request.downloadHandler.text);
                        if (response == null || string.IsNullOrWhiteSpace(response.reply) || response.reply.Length > 6000 || response.skillId != context.skillId)
                            throw new InvalidOperationException("Invalid AI reply.");
                        LastResponse = response;
                        result.TrySetResult(response.reply);
                    }
                    catch (Exception e)
                    {
                        LastError = request.responseCode == 401 ? "Backend access denied. Check the session token."
                            : request.responseCode == 503 ? "Backend needs an API key and model."
                            : request.responseCode == 429 ? "Too many requests. Wait before retrying."
                            : request.responseCode == 502 ? "AI provider failed. Check the backend model and credentials."
                            : request.responseCode == 504 ? "AI request timed out. Please retry."
                            : request.responseCode == 0 ? "Cannot reach the AI backend. Check that it is running."
                            : "The AI service could not complete the request.";
                        result.TrySetException(e);
                    }
                    finally { request.Dispose(); }
                };
            }
            catch (Exception e)
            { pending = null; completion = null; LastError = "Could not connect to the AI backend."; request.Dispose(); result.TrySetException(e); }
            return result.Task;
        }
        public void CancelPending()
        {
            var request = pending; var result = completion;
            pending = null; completion = null;
            result?.TrySetCanceled();
            if (request != null) { request.Abort(); request.Dispose(); }
        }
    }
}
