using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Sleepet
{
    // The bundled process is owned by this application, never an installed system service.
    // Windows Job Objects terminate it even if the player crashes or is force-closed.
    public sealed class LocalCompanionAI : ICompanionAI, ICancellableCompanionAI, IDisposable
    {
        public const string ModelFile = "Qwen3-1.7B-Q4_K_M.gguf";
        readonly string root;
        readonly string sessionKey = Guid.NewGuid().ToString("N");
        Process process;
        WindowsChildJob job;
        Task loading;
        CancellationTokenSource requestCancellation;
        bool disposed;
        string baseUrl;
        public bool Ready { get; private set; }
        public string Status { get; private set; } = "Local AI | Preparing model...";
        public string LastError { get; private set; }
        public string Source { get; private set; } = "Conversation only | On this device";
        public double LastReplySeconds { get; private set; }
        public int ProcessId => process != null && !process.HasExited ? process.Id : 0;
        public string Endpoint => baseUrl;

        public LocalCompanionAI(string modelRoot = null)
        { root = modelRoot ?? Path.Combine(Path.GetDirectoryName(Application.dataPath), "LocalAI"); }

        public Task InitializeAsync()
        {
            if (disposed) throw new ObjectDisposedException(nameof(LocalCompanionAI));
            if (loading == null || loading.IsFaulted || loading.IsCanceled || (Ready && (process == null || process.HasExited)))
                loading = LoadAsync();
            return loading;
        }
        async Task LoadAsync()
        {
            StopProcess(); Ready = false; LastError = null;
            try
            {
#if !UNITY_EDITOR_WIN && !UNITY_STANDALONE_WIN
                throw new PlatformNotSupportedException("This bundled local model is available on Windows x64.");
#else
                var executable = Path.Combine(root, "runtime", "llama-server.exe");
                var model = Path.Combine(root, "models", ModelFile);
                if (!File.Exists(executable) || !File.Exists(model))
                    throw new FileNotFoundException("Local model files are missing. Keep the complete Sleepet folder together.");
                if (new FileInfo(model).Length != 1282439264L)
                    throw new InvalidDataException("Local model is incomplete. Extract the complete Sleepet package again.");
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
                baseUrl = "http://127.0.0.1:" + port;
                int threads = Math.Max(1, Math.Min(6, Environment.ProcessorCount / 2));
                var info = new ProcessStartInfo {
                    FileName = executable, WorkingDirectory = Path.GetDirectoryName(executable),
                    Arguments = "-m \"" + model + "\" --host 127.0.0.1 --port " + port +
                        " --ctx-size 8192 --parallel 1 --threads " + threads + " --threads-batch " + threads +
                        " --n-gpu-layers 0 --offline --no-webui --log-disable --api-key " + sessionKey,
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                // Do not allow external llama environment settings to override our private runtime.
                foreach (string key in new List<string>(EnvironmentKeys(info)))
                    if (key.StartsWith("LLAMA_ARG_", StringComparison.Ordinal)) info.EnvironmentVariables.Remove(key);
                job = new WindowsChildJob();
                process = new Process { StartInfo = info };
                process.OutputDataReceived += DrainOutput; process.ErrorDataReceived += DrainOutput;
                if (!process.Start()) throw new InvalidOperationException("Could not start the local model.");
                job.Add(process);
                process.BeginOutputReadLine(); process.BeginErrorReadLine();
                Status = "Local AI | Loading Qwen3 model...";
                var timer = Stopwatch.StartNew();
                while (!disposed && timer.Elapsed.TotalSeconds < 120)
                {
                    if (process.HasExited) throw new InvalidOperationException("Local model stopped during startup (" + process.ExitCode + ").");
                    using (var request = UnityWebRequest.Get(baseUrl + "/health"))
                    {
                        request.timeout = 2; request.SetRequestHeader("Authorization", "Bearer " + sessionKey);
                        var operation = request.SendWebRequest();
                        while (!operation.isDone && !disposed) await Task.Yield();
                        if (disposed) { request.Abort(); throw new OperationCanceledException(); }
                        if (request.result == UnityWebRequest.Result.Success)
                        { Ready = true; Status = "Local AI ready | No Internet needed"; return; }
                    }
                    await Task.Delay(150);
                }
                throw new TimeoutException("Local model startup timed out. Try selecting Local AI again.");
#endif
            }
            catch (Exception e)
            {
                LastError = e.Message; Status = "Local AI unavailable | " + e.Message;
                StopProcess(); throw;
            }
        }
        static IEnumerable<string> EnvironmentKeys(ProcessStartInfo info)
        { foreach (string key in info.EnvironmentVariables.Keys) yield return key; }
        static void DrainOutput(object sender, DataReceivedEventArgs e) { /* Never persist prompts or private snapshots. */ }

        public async Task<string> SendMessage(string userText, CompanionContext context)
        {
            CancelPending();
            var cancellation = new CancellationTokenSource(); requestCancellation = cancellation;
            var timer = Stopwatch.StartNew(); LastError = null;
            try
            {
                await InitializeAsync(); cancellation.Token.ThrowIfCancellationRequested();
                Status = "Local AI | Generating on this device...";
                Source = context.shareData ? CompanionSkills.Label(context.skillId) + " | Saved data on this device" : "Conversation only | Saved data off";
                var messages = new List<LocalMessage> {
                    new LocalMessage { role = "system", content = LocalSkillPrompt.Build(context) }
                };
                if (context.conversation != null)
                    foreach (var turn in context.conversation.Skip(Math.Max(0, context.conversation.Length - 4)))
                        if (turn.role == "user" || turn.role == "assistant")
                            messages.Add(new LocalMessage { role = turn.role, content = CompanionSnapshots.Limit(turn.content, 400) });
                messages.Add(new LocalMessage { role = "user", content = userText });
                var payload = new LocalChatRequest { messages = messages.ToArray() };
                using (var request = new UnityWebRequest(baseUrl + "/v1/chat/completions", "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                    request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = 120;
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + sessionKey);
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        if (cancellation.IsCancellationRequested || disposed) { request.Abort(); throw new OperationCanceledException(); }
                        await Task.Yield();
                    }
                    cancellation.Token.ThrowIfCancellationRequested();
                    if (request.result != UnityWebRequest.Result.Success)
                        throw new InvalidOperationException("Local inference failed (HTTP " + request.responseCode + "). Retry or restart Local AI.");
                    var response = JsonUtility.FromJson<LocalChatResponse>(request.downloadHandler.text);
                    string answer = response?.choices != null && response.choices.Length > 0 ? response.choices[0].message?.content : null;
                    if (string.IsNullOrWhiteSpace(answer)) throw new InvalidOperationException("The local model returned an empty answer. Please retry.");
                    LastReplySeconds = timer.Elapsed.TotalSeconds;
                    Status = "Local AI ready | No Internet needed";
                    return answer.Trim();
                }
            }
            catch (OperationCanceledException) { Status = Ready ? "Local AI ready | Request cancelled" : Status; throw; }
            catch (Exception e) { LastError = e.Message; Status = "Local AI | " + e.Message; throw; }
            finally { if (requestCancellation == cancellation) requestCancellation = null; cancellation.Dispose(); }
        }
        public void CancelPending() { requestCancellation?.Cancel(); }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; CancelPending(); Ready = false; StopProcess();
        }
        void StopProcess()
        {
            job?.Dispose(); job = null;
            if (process == null) return;
            try { if (!process.HasExited) process.Kill(); } catch (InvalidOperationException) { }
            process.Dispose(); process = null;
        }
    }
    [Serializable] public sealed class LocalMessage { public string role, content; }
    [Serializable] sealed class LocalChatRequest
    {
        public string model = "local";
        public LocalMessage[] messages;
        public int max_tokens = 192;
        public float temperature = .2f;
        public float top_p = .9f;
        public bool stream;
        public LocalTemplateOptions chat_template_kwargs = new LocalTemplateOptions();
    }
    [Serializable] sealed class LocalTemplateOptions { public bool enable_thinking = false; }
    [Serializable] sealed class LocalChatChoice { public LocalMessage message; }
    [Serializable] sealed class LocalChatResponse { public LocalChatChoice[] choices; }

    sealed class WindowsChildJob : IDisposable
    {
        IntPtr handle;
        public WindowsChildJob()
        {
            handle = CreateJobObject(IntPtr.Zero, null);
            if (handle == IntPtr.Zero) throw new InvalidOperationException("Cannot create local AI process owner.");
            var limits = new ExtendedLimits(); limits.basic.flags = 0x2000; // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            int size = Marshal.SizeOf(typeof(ExtendedLimits)); var ptr = Marshal.AllocHGlobal(size);
            try {
                Marshal.StructureToPtr(limits, ptr, false);
                if (!SetInformationJobObject(handle, 9, ptr, (uint)size))
                { Dispose(); throw new InvalidOperationException("Cannot configure local AI process owner."); }
            } finally { Marshal.FreeHGlobal(ptr); }
        }
        public void Add(Process process)
        { if (!AssignProcessToJobObject(handle, process.Handle)) throw new InvalidOperationException("Cannot attach local AI to the application lifecycle."); }
        public void Dispose() { if (handle != IntPtr.Zero) { CloseHandle(handle); handle = IntPtr.Zero; } }
        [StructLayout(LayoutKind.Sequential)] struct BasicLimits
        { public long processTime, jobTime; public uint flags; public UIntPtr min, max; public uint active; public UIntPtr affinity; public uint priority, scheduling; }
        [StructLayout(LayoutKind.Sequential)] struct IoCounters { public ulong a, b, c, d, e, f; }
        [StructLayout(LayoutKind.Sequential)] struct ExtendedLimits
        { public BasicLimits basic; public IoCounters io; public UIntPtr processMemory, jobMemory, peakProcess, peakJob; }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateJobObject(IntPtr attributes, string name);
        [DllImport("kernel32.dll")] static extern bool SetInformationJobObject(IntPtr job, int kind, IntPtr data, uint length);
        [DllImport("kernel32.dll")] static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr value);
    }
}
