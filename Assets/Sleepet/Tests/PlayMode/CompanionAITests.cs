#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sleepet.Tests
{
    public sealed class CompanionAITests
    {
        [UnityTest]
        public IEnumerator OnlineClientRoundTripsThroughLocalBackend()
        {
            string url = Environment.GetEnvironmentVariable("SLEEPET_AI_TEST_URL");
            if (string.IsNullOrEmpty(url)) Assert.Ignore("Set SLEEPET_AI_TEST_URL and start the documented integration fixture.");
            var provider = new OpenAICompanion(new CompanionAIConfig { backendUrl = url, timeoutSeconds = 10 }, "fixture-token");
            var context = new CompanionContext { skillId = CompanionSkills.Tomorrow, today = "2026-10-02", petName = "Luna", shareData = true,
                plan = new CompanionPlan { date = "2026-10-03", status = "available", activities = new[] { "Group meeting" } },
                conversation = Array.Empty<CompanionTurn>() };
            var task = provider.SendMessage("What is planned for tomorrow?", context);
            float deadline = Time.realtimeSinceStartup + 12;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(task.IsCompleted); Assert.IsFalse(task.IsFaulted);
            StringAssert.Contains("Group meeting", task.Result);
            StringAssert.Contains("Tomorrow plan (available)", provider.LastResponse.source);
            var unauthorized = new OpenAICompanion(new CompanionAIConfig { backendUrl = url, timeoutSeconds = 10 }, "wrong-token");
            var denied = unauthorized.SendMessage("Hello", context);
            deadline = Time.realtimeSinceStartup + 12;
            while (!denied.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(denied.IsFaulted); Assert.NotNull(denied.Exception);
            StringAssert.Contains("access denied", unauthorized.LastError);
        }
        [Test]
        public void LegacyMockKeepsItsExplicitFallbackMessage()
        {
            var ai = new MockCompanionAI();
            foreach (var skill in CompanionSkills.Ids)
                foreach (var sharing in new[] { false, true })
                    Assert.AreEqual("you need access the Internet to generate the answers.",
                        ai.SendMessage("Review my records and tomorrow plan", new CompanionContext {
                            skillId = skill, shareData = sharing }).Result);
        }

        [Test]
        public void LocalPromptUsesCurrentPetNameForEverySkill()
        {
            foreach (string skill in CompanionSkills.Ids)
            {
                var context = new CompanionContext { skillId = skill, shareData = true, thoughtMotion = "sitting", thoughtTrigger = "tap" };
                foreach (string name in new[] { "Miia", "Luna", "小月" })
                {
                    context.petName = name;
                    string prompt = LocalSkillPrompt.Build(context);
                    StringAssert.Contains(name, prompt);
                    StringAssert.DoesNotContain("Mocha", prompt);
                }
                context.petName = " ";
                StringAssert.Contains("Mocha", LocalSkillPrompt.Build(context));
                context.petName = "Miia";
                context.shareData = false;
                StringAssert.DoesNotContain("Miia", LocalSkillPrompt.Build(context));
            }
        }

        [Test]
        public void LocalSkillsEnforceDataScopesAndExcludeSamples()
        {
            var context = new CompanionContext { skillId = CompanionSkills.Records, shareData = true,
                preferences = new CompanionPreferences { wakeTime = "PRIVATE_WAKE_TIME" },
                records = new[] { new CompanionRecord { date = "2026-10-03", durationSeconds = 600 },
                    new CompanionRecord { date = "2026-10-03", durationSeconds = 36000, sample = true } },
                plan = new CompanionPlan { status = "available", activities = new[] { "PRIVATE_PLAN" } } };
            string prompt = LocalSkillPrompt.Build(context);
            StringAssert.Contains("total minutes: 10", prompt);
            StringAssert.Contains("Sample records excluded: 1", prompt);
            StringAssert.DoesNotContain("PRIVATE_WAKE_TIME", prompt);
            StringAssert.DoesNotContain("PRIVATE_PLAN", prompt);
            context.shareData = false;
            prompt = LocalSkillPrompt.Build(context);
            StringAssert.Contains("DISABLED", prompt);
            StringAssert.DoesNotContain("total minutes", prompt);
            context.shareData = true; context.skillId = CompanionSkills.Tomorrow; context.plan.status = "outdated";
            StringAssert.DoesNotContain("PRIVATE_PLAN", LocalSkillPrompt.Build(context));
        }

        [Test]
        public void ThreeSkillsLoadAndSnapshotsRespectPermissionAndProvenance()
        {
            foreach (var id in CompanionSkills.Ids) Assert.AreEqual(id, CompanionSkills.Load(id).id);
            var store = new SleepetStore(Path.Combine("Validation", "AI", Guid.NewGuid().ToString("N")));
            store.Preferences.petName = "Luna";
            store.History.records.Add(new SleepSummary { startedAt = DateTime.Now.ToString("O"), durationSeconds = 600 });
            store.History.records.Add(new SleepSummary { startedAt = DateTime.Now.ToString("O"), durationSeconds = 36000, sample = true });
            var disabled = CompanionSnapshots.Build(store, CompanionSkills.Records, false, DateTime.Now);
            Assert.IsNull(disabled.preferences); Assert.IsEmpty(disabled.records); Assert.AreEqual("Mocha", disabled.petName);
            var enabled = CompanionSnapshots.Build(store, CompanionSkills.Records, true, DateTime.Now);
            Assert.AreEqual(2, enabled.records.Length);
            Assert.IsFalse(enabled.records[0].sample);
            Assert.IsTrue(enabled.records[1].sample);
            Assert.IsEmpty(CompanionSnapshots.Build(store, CompanionSkills.Daily, true, DateTime.Now).records);
        }

        [Test]
        public void TomorrowSnapshotRequiresMatchingSavedDate()
        {
            var path = Path.Combine("Validation", "AI", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            var store = new SleepetStore(path);
            Assert.AreEqual("missing", CompanionSnapshots.Build(store, CompanionSkills.Tomorrow, true, DateTime.Now).plan.status);
            var plan = new TomorrowPlan { date = DateTime.Today.ToString("yyyy/MM/dd"), shortEvent = "Do not reuse this" };
            File.WriteAllText(Path.Combine(path, "tomorrow-plan.json"), JsonUtility.ToJson(plan));
            var stale = CompanionSnapshots.Build(store, CompanionSkills.Tomorrow, true, DateTime.Now);
            Assert.AreEqual("outdated", stale.plan.status); Assert.IsEmpty(stale.plan.activities);
            plan.date = DateTime.Today.AddDays(1).ToString("yyyy/MM/dd");
            File.WriteAllText(Path.Combine(path, "tomorrow-plan.json"), JsonUtility.ToJson(plan));
            Assert.AreEqual("available", CompanionSnapshots.Build(store, CompanionSkills.Tomorrow, true, DateTime.Now).plan.status);
        }

        sealed class DeferredAI : ICompanionAI, ICancellableCompanionAI
        {
            public TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
            public bool cancelled;
            public CompanionContext context;
            public Task<string> SendMessage(string text, CompanionContext value) { context = value; return completion.Task; }
            public void CancelPending() { cancelled = true; }
        }
        [UnityTest]
        public IEnumerator ChatKeepsHistoryRetriesAndDiscardsLateReplies()
        {
            SleepetSceneSession.TestDataDirectoryOverride = Path.GetFullPath(Path.Combine("Validation", "AI", Guid.NewGuid().ToString("N")));
            SceneManager.LoadScene("Sleepet_Home");
            yield return null; yield return null; yield return null;
            var session = SleepetSceneSession.Instance;
            try
            {
                var demo = session.Demo;
                demo.Store.Preferences.petName = "Luna";
                demo.SetAIDataSharing(true);
                var first = new DeferredAI(); demo.AI = first;
                Assert.IsTrue(demo.SendChatMessage("Hello"));
                Assert.IsFalse(demo.SendChatMessage("Duplicate"));
                yield return null; yield return null;
                first.completion.SetResult("Welcome");
                yield return null; yield return null;
                StringAssert.Contains("Luna", demo.ChatTranscript);
                var second = new DeferredAI(); demo.AI = second;
                demo.SendChatMessage("What did I say?");
                yield return null; yield return null;
                Assert.AreEqual(2, second.context.conversation.Length);
                demo.CloseChat(); second.completion.SetResult("Late response");
                yield return null;
                Assert.IsTrue(second.cancelled); Assert.IsFalse(demo.ChatBusy);
                StringAssert.DoesNotContain("Late response", demo.ChatTranscript);
                Assert.AreEqual("What did I say?", demo.ChatDraft);
                Assert.IsTrue(demo.CanRetryChat);
                demo.SetAIDataSharing(false);
                Assert.IsEmpty(demo.ChatTranscript);
                var failure = new DeferredAI(); demo.AI = failure;
                demo.SendChatMessage("Keep this message"); yield return null; yield return null;
                failure.completion.SetException(new Exception("Network failure"));
                yield return null; yield return null;
                Assert.AreEqual("Keep this message", demo.ChatDraft); Assert.IsTrue(demo.CanRetryChat);
                var retry = new DeferredAI(); demo.AI = retry; demo.RetryChat();
                yield return null; yield return null;
                Assert.IsEmpty(retry.context.conversation); Assert.IsFalse(retry.context.shareData);
                retry.completion.SetResult("Recovered"); yield return null; yield return null;
                Assert.IsFalse(demo.ChatBusy); StringAssert.Contains("Recovered", demo.ChatTranscript);
                Assert.AreEqual(1, demo.ChatTranscript.Split(new[] { "You: Keep this message" }, StringSplitOptions.None).Length - 1);
            }
            finally { Object.Destroy(session.gameObject); SleepetSceneSession.TestDataDirectoryOverride = null; }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChangedSavedFactsInvalidateModelHistoryButKeepTranscript()
        {
            SleepetSceneSession.TestDataDirectoryOverride = Path.GetFullPath(Path.Combine("Validation", "AI", Guid.NewGuid().ToString("N")));
            SceneManager.LoadScene("Sleepet_Home");
            yield return null; yield return null; yield return null;
            var session = SleepetSceneSession.Instance;
            try
            {
                var demo = session.Demo;
                var first = new DeferredAI(); demo.AI = first;
                demo.SendChatMessage("What is my wake time?"); yield return null; yield return null;
                first.completion.SetResult("Old wake time"); yield return null; yield return null;
                demo.Store.Preferences.wakeTime = "09:41";
                var next = new DeferredAI(); demo.AI = next;
                demo.SendChatMessage("What is my wake time?"); yield return null; yield return null;
                Assert.IsEmpty(next.context.conversation);
                Assert.AreEqual("09:41", next.context.preferences.wakeTime);
                StringAssert.Contains("Old wake time", demo.ChatTranscript);
                next.completion.SetResult("09:41"); yield return null; yield return null;
                demo.Store.Preferences.petName = "Miia";
                var renamed = new DeferredAI(); demo.AI = renamed;
                demo.SendChatMessage("Review my records"); yield return null; yield return null;
                Assert.AreEqual(CompanionSkills.Records, renamed.context.skillId);
                Assert.AreEqual("Miia", renamed.context.petName); Assert.IsEmpty(renamed.context.conversation);
                renamed.completion.SetResult("Current records"); yield return null; yield return null;
            }
            finally { Object.Destroy(session.gameObject); SleepetSceneSession.TestDataDirectoryOverride = null; }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ARSkillControlsReadLocalDataAndSurviveReturn()
        {
            SleepetSceneSession.TestDataDirectoryOverride = Path.GetFullPath(Path.Combine("Validation", "AI", Guid.NewGuid().ToString("N")));
            SceneManager.LoadScene("Sleepet_AR");
            yield return null; yield return null; yield return null;
            var session = SleepetSceneSession.Instance;
            try
            {
                // UI unit test uses an explicit stub; real offline inference is tested in the built player.
                session.Demo.AI = new MockCompanionAI();
                var ui = Object.FindFirstObjectByType<SleepetHighFi>();
                ui.ARTestBackground(); ui.PlaceOrChat();
                Assert.IsFalse(ui.arChatControls.activeSelf, "Placing the pet must not open chat.");
                Assert.IsTrue(session.Demo.ShareAIData);
                Assert.AreEqual(0, ui.arSkillButtons.Length); Assert.IsNull(ui.arDataLabel);
                Assert.NotNull(ui.arChatEntryButton);
                ui.CloseAR(); ui.arChatEntryButton.onClick.Invoke();
                Assert.IsTrue(ui.arChatControls.activeSelf);
                Assert.IsFalse(session.Demo.cameraCompanion.IsLive);
                ui.arInput.text = "Review my records"; ui.SendARMessage();
                yield return new WaitForSecondsRealtime(.8f);
                Assert.AreEqual(CompanionSkills.Records, session.Demo.ChatSkill);
                ui.arInput.text = "What is planned for tomorrow?"; ui.SendARMessage();
                yield return new WaitForSecondsRealtime(.8f);
                Assert.AreEqual(CompanionSkills.Tomorrow, session.Demo.ChatSkill);
                StringAssert.Contains("You: Review my records", session.Demo.ChatTranscript);
                ui.CloseAR(); Assert.IsFalse(ui.arChatControls.activeSelf);
                ui.OpenSleepetChat(); Assert.IsTrue(ui.arChatControls.activeSelf);
                ui.CycleARModel(); Assert.IsTrue(session.Demo.OnlineAI); Assert.IsTrue(session.Demo.ShareAIData);
                ui.CycleARModel(); Assert.IsInstanceOf<LocalCompanionAI>(session.Demo.AI); Assert.IsTrue(session.Demo.ShareAIData);
            }
            finally { Object.Destroy(session.gameObject); SleepetSceneSession.TestDataDirectoryOverride = null; }
            yield return null;
        }

        [TestCase("What did I save for tomorrow?", CompanionSkills.Tomorrow)]
        [TestCase("Review my last seven days", CompanionSkills.Records)]
        [TestCase("How long did I sleep last night?", CompanionSkills.Records)]
        [TestCase("What is my wake time?", CompanionSkills.Daily)]
        [TestCase("I am tired", CompanionSkills.Daily)]
        [TestCase("明天有什么安排", CompanionSkills.Tomorrow)]
        [TestCase("看看最近的睡眠记录", CompanionSkills.Records)]
        [TestCase("陪我聊聊天", CompanionSkills.Daily)]
        public void AutomaticSkillsMatchConversationIntent(string input, string expected)
        { Assert.AreEqual(expected, CompanionSkillRouter.Resolve(input)); }
        [Test]
        public void FollowupKeepsRelevantSkillUntilTopicChanges()
        {
            Assert.AreEqual(CompanionSkills.Records, CompanionSkillRouter.Resolve("Tell me more", CompanionSkills.Records));
            Assert.AreEqual(CompanionSkills.Daily, CompanionSkillRouter.Resolve("I feel lonely", CompanionSkills.Records));
        }

        static IEnumerator WaitForChat(SleepetDemo demo)
        {
            float until = Time.realtimeSinceStartup + 15;
            while (demo.ChatBusy && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsFalse(demo.ChatBusy, "Chat did not finish before the test deadline.");
        }

        static void Capture(SleepetHighFi ui, string name = "AR-chat")
        {
            Directory.CreateDirectory("Validation/AI");
            var layout = ui.GetComponent<HighFiAdaptiveLayout>(); layout.enabled = false;
            var canvas = ui.GetComponent<Canvas>();
            var scaler = ui.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.matchWidthOrHeight = .5f;
            layout.ApplyLayout(874, 0);
            var page = (RectTransform)ui.pages[(int)ui.scenePage].transform;
            page.anchoredPosition = new Vector2(0, page.anchoredPosition.y);
            var go = new GameObject("AI evidence camera", typeof(Camera)); var camera = go.GetComponent<Camera>();
            camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor;
            var target = new RenderTexture(804, 1748, 24); camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); camera.Render();
            var prior = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(804, 1748, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 804, 1748), 0, 0); image.Apply();
            File.WriteAllBytes("Validation/AI/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = prior; canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
            camera.targetTexture = null; target.Release();
            Object.Destroy(image); Object.Destroy(target); Object.Destroy(go); layout.enabled = true;
        }
    }
}
#endif
