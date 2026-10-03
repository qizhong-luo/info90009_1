using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sleepet
{
    public static class LocalAISkillValidation
    {
        public static string DataDirectory { get; private set; }
        static string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Configure()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "--local-ai-skills");
            if (i < 0 || i + 1 >= args.Length) return;
            output = Path.GetFullPath(args[i + 1]);
            DataDirectory = Path.Combine(output, "isolated-data");
            Directory.CreateDirectory(DataDirectory);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static async void Run()
        {
            if (output == null) return;
            var report = new StringBuilder();
            try
            {
                await Wait(() => SleepetSceneSession.Instance?.Demo?.Store != null);
                var session = SleepetSceneSession.Instance;
                var demo = session.Demo;
                if (!(demo.AI is LocalCompanionAI)) throw new Exception("Local model is not the default.");
                demo.Store.Preferences.wakeTime = "07:35";
                demo.Store.SavePreferences(demo.Store.Preferences);
                demo.Store.Add(new SleepSummary { sessionId = "local-validation", startedAt = DateTime.Now.ToString("O"), durationSeconds = 600 });
                demo.Store.Add(new SleepSummary { sessionId = "sample-validation", startedAt = DateTime.Now.ToString("O"), durationSeconds = 36000, sample = true });
                var plan = new TomorrowPlan { date = DateTime.Today.AddDays(1).ToString("yyyy/MM/dd"), shortEvent = "Visit the library" };
                File.WriteAllText(Path.Combine(DataDirectory, "tomorrow-plan.json"), JsonUtility.ToJson(plan));
                session.Navigate(HighFiPage.AR);
                await Task.Delay(500);
                var ui = UnityEngine.Object.FindFirstObjectByType<SleepetHighFi>();
                if (!demo.ShareAIData) throw new Exception("Saved data should be enabled by default.");
                Capture(ui, Path.Combine(output, "00-independent-actions.png"));
                ui.ARTestBackground(); ui.PlaceOrChat();
                if (ui.arChatControls.activeSelf) throw new Exception("Placement must not open chat.");
                Capture(ui, Path.Combine(output, "00-pet-placed.png"));
                ui.CloseAR(); ui.OpenSleepetChat();
                if (!ui.arChatControls.activeSelf || demo.cameraCompanion.IsLive) throw new Exception("Chat must work independently of the camera.");
                if (ui.arSkillButtons.Length != 0 || ui.arDataLabel != null) throw new Exception("Manual settings still visible.");
                await Ask(demo, ui, CompanionSkills.Daily, true, "Say hello in one short sentence.", "01-conversation", report);
                await Ask(demo, ui, CompanionSkills.Daily, true, "What is my saved wake time?", "02-preferences", report, "07:35");
                await Ask(demo, ui, CompanionSkills.Records, true, "How many real app sessions and total minutes are recorded? Exclude sample records.", "03-records", report, "10");
                await Ask(demo, ui, CompanionSkills.Tomorrow, true, "List all the activities I saved for tomorrow.", "04-tomorrow", report, "library");
                plan.date = DateTime.Today.ToString("yyyy/MM/dd");
                File.WriteAllText(Path.Combine(DataDirectory, "tomorrow-plan.json"), JsonUtility.ToJson(plan));
                string stale = await Ask(demo, ui, CompanionSkills.Tomorrow, true, "What is saved for tomorrow?", "05-stale-plan", report);
                if (stale.IndexOf("library", StringComparison.OrdinalIgnoreCase) >= 0) throw new Exception("Stale activity leaked.");
                await Ask(demo, ui, CompanionSkills.Daily, true, "I feel tired. Can you keep me company?", "06-companionship", report);
                demo.SendChatMessage("Tell me a story.");
                await Task.Delay(100); demo.CancelChat();
                if (demo.ChatBusy || !demo.CanRetryChat) throw new Exception("Cancellation did not preserve retry.");
                report.AppendLine("Cancellation: PASS");
                report.AppendLine("Model process: " + ((LocalCompanionAI)demo.AI).ProcessId);
                File.WriteAllText(Path.Combine(output, "results.txt"), "PASS\n" + report);
                Application.Quit(0);
            }
            catch (Exception e) { File.WriteAllText(Path.Combine(output, "results.txt"), "FAIL\n" + report + e); Application.Quit(1); }
        }
        static async Task<string> Ask(SleepetDemo demo, SleepetHighFi ui, string skill, bool share, string input, string name, StringBuilder report, string required = null)
        {
            if (demo.ShareAIData != share) throw new Exception("Unexpected saved data permission.");
            int revision = demo.ChatReplyRevision;
            ui.arInput.text = input; ui.SendARMessage();
            await Wait(() => !demo.ChatBusy);
            if (demo.ChatReplyRevision == revision) throw new Exception(demo.ChatStatus);
            if (demo.ChatSkill != skill) throw new Exception("Automatic skill routing failed for " + input);
            string transcript = demo.ChatTranscript;
            string answer = transcript.Substring(transcript.LastIndexOf("Mocha: ", StringComparison.Ordinal));
            if (required != null && answer.IndexOf(required, StringComparison.OrdinalIgnoreCase) < 0) throw new Exception(name + " missing fact: " + answer);
            report.AppendLine(name + " (" + ((LocalCompanionAI)demo.AI).LastReplySeconds.ToString("0.00") + " seconds)\n" + answer);
            await Task.Delay(100);
            Capture(ui, Path.Combine(output, name + ".png"));
            await Task.Delay(300);
            return answer;
        }
        static void Capture(SleepetHighFi ui, string path)
        {
            var layout = ui.GetComponent<HighFiAdaptiveLayout>();
            bool layoutEnabled = layout.enabled; layout.enabled = false;
            var scaler = ui.GetComponent<UnityEngine.UI.CanvasScaler>();
            float match = scaler.matchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            layout.ApplyLayout(874, 0);
            var page = (RectTransform)ui.pages[(int)ui.scenePage].transform;
            var position = page.anchoredPosition; page.anchoredPosition = new Vector2(0, position.y);
            var canvas = ui.GetComponent<Canvas>();
            var cameraObject = new GameObject("Offline verification camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor;
            var target = new RenderTexture(804, 1748, 24);
            var image = new Texture2D(804, 1748, TextureFormat.RGB24, false);
            var prior = RenderTexture.active;
            var mode = canvas.renderMode; var priorCamera = canvas.worldCamera; float distance = canvas.planeDistance;
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 804, 1748), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = prior; canvas.renderMode = mode; canvas.worldCamera = priorCamera; canvas.planeDistance = distance;
                camera.targetTexture = null; target.Release();
                UnityEngine.Object.Destroy(image); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(cameraObject);
                page.anchoredPosition = position; scaler.matchWidthOrHeight = match; layout.enabled = layoutEnabled;
            }
        }
        static async Task Wait(Func<bool> condition)
        {
            var until = DateTime.UtcNow.AddSeconds(250);
            while (!condition()) { if (DateTime.UtcNow > until) throw new TimeoutException(); await Task.Delay(20); }
        }
    }
}
