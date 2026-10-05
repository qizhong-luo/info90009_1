#if UNITY_EDITOR
using NUnit.Framework;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Sleepet.Tests
{
    public sealed class PetThoughtTests
    {
        Mouse testMouse;
        InputSettings.BackgroundBehavior savedBackground;
        InputSettings.EditorInputBehaviorInPlayMode savedEditorInput;
        [SetUp] public void AllowBatchInput()
        {
            savedBackground = InputSystem.settings.backgroundBehavior;
            savedEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }
        [TearDown] public void RestoreInputSettings()
        {
            InputSystem.settings.backgroundBehavior = savedBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorInput;
        }
        void MouseAt(Vector2 position, bool down) => InputSystem.QueueStateEvent(testMouse, new MouseState { position = position, buttons = (ushort)(down ? 1 : 0) });
        [UnityTest]
        public IEnumerator RealPointerTapSurvivesWobbleAndDragDoesNotTap()
        {
            SleepetSceneSession.TestDataDirectoryOverride = Path.GetFullPath(Path.Combine("Validation", "ThoughtTestData", Guid.NewGuid().ToString("N")));
            SceneManager.LoadScene("Sleepet_AR");
            yield return null; yield return null; yield return null;
            var ui = UnityEngine.Object.FindFirstObjectByType<SleepetHighFi>();
            var app = SleepetSceneSession.Instance.Demo;
            var ai = new ThoughtAI(); app.AI = ai;
            ui.ARTestBackground(); ui.PlaceOrChat();
            var thoughts = ui.arPet.GetComponent<PetThoughts>();
            thoughts.SendMessage("OnApplicationFocus", true);
            yield return null; Canvas.ForceUpdateCanvases();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, ui.arPet.rectTransform.TransformPoint(ui.arPet.rectTransform.rect.center));
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Assert.IsNotEmpty(hits, "No UI raycast hit on the pet");
            Assert.AreEqual(ui.arPet.gameObject, ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject),
                "Pet hit is blocked by " + hits[0].gameObject.name);
            testMouse = InputSystem.AddDevice<Mouse>();
            MouseAt(point, false); yield return null; yield return null;
            MouseAt(point, true); yield return new WaitForSecondsRealtime(.45f);
            MouseAt(point + Vector2.right * 8, true); yield return null; yield return null;
            MouseAt(point + Vector2.right * 8, false); yield return null; yield return null; yield return null;
            Assert.AreEqual(1, ai.Calls, "A slow click with small movement must still count as one tap");
            var before = ui.arPet.rectTransform.localPosition;
            MouseAt(point, true); yield return null; yield return null;
            MouseAt(point + Vector2.right * 60, true); yield return null; yield return null;
            MouseAt(point + Vector2.right * 60, false); yield return null; yield return null;
            Assert.Greater(Vector3.Distance(before, ui.arPet.rectTransform.localPosition), 5);
            Assert.AreEqual(1, ai.Calls, "Dragging must not generate a tap thought");
            typeof(PetThoughts).GetField("nextAmbient", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(thoughts, Time.unscaledTime - 1);
            yield return null; yield return null; yield return null;
            Assert.AreEqual(2, ai.Calls, "Ambient reactions must resume after dragging");
        }
        [UnityTest]
        public IEnumerator HomeTapUsesFramesWithoutThoughtsAndCustomPetStaysStatic()
        {
            SleepetSceneSession.TestDataDirectoryOverride = Path.GetFullPath(Path.Combine("Validation", "ThoughtTestData", Guid.NewGuid().ToString("N")));
            SceneManager.LoadScene("Sleepet_Home");
            yield return null; yield return null; yield return null;
            var ui = UnityEngine.Object.FindFirstObjectByType<SleepetHighFi>();
            var app = SleepetSceneSession.Instance.Demo; var ai = new ThoughtAI(); app.AI = ai;
            var original = ui.homePet.sprite;
            Canvas.ForceUpdateCanvases();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, ui.homePet.rectTransform.TransformPoint(ui.homePet.rectTransform.rect.center));
            testMouse = InputSystem.AddDevice<Mouse>();
            MouseAt(point, false); yield return null; yield return null;
            MouseAt(point, true); yield return null; yield return null;
            MouseAt(point, false); yield return new WaitForSecondsRealtime(.25f);
            Assert.IsTrue(ui.homePet.GetComponent<HomePetReaction>()?.IsPlaying == true, "Real home pet click did not play animation");
            Assert.AreNotSame(original, ui.homePet.sprite);
            Assert.AreEqual(0, ai.Calls); Assert.IsEmpty(app.ChatTranscript);
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.AreSame(original, ui.homePet.sprite);
            app.Store.Preferences.petOption = 1; ui.RefreshMe();
            var custom = ui.homePet.sprite; ui.TapPetHighFi(); yield return null;
            Assert.AreSame(custom, ui.homePet.sprite);
            Assert.IsFalse(ui.homePet.GetComponent<HomePetReaction>().IsPlaying);
            Assert.AreEqual(0, ai.Calls);
        }
        [UnityTest, Timeout(300000)]
        public IEnumerator SelectingOriginalPresetClearsStaleCustomizationAndAnimatesAfterReload()
        {
            string directory = Path.GetFullPath(Path.Combine("Validation", "ThoughtTestData", Guid.NewGuid().ToString("N")));
            SleepetSceneSession.TestDataDirectoryOverride = directory;
            SceneManager.LoadScene("Sleepet_Me");
            yield return null; yield return null; yield return null;
            var ui = UnityEngine.Object.FindFirstObjectByType<SleepetHighFi>();
            var app = SleepetSceneSession.Instance.Demo;
            app.Store.Preferences.petColour = "Brown";
            app.Store.Preferences.petPhoto = "old-custom.png";
            ui.OpenPetSheet(); ui.SavePet();
            Assert.AreEqual("Brown", app.Store.Preferences.petColour, "Saving a name must not reset customization");
            Assert.IsFalse(PetThoughtRules.AnimatedAppearance(app.Store.Preferences));
            ui.OpenPetSheet(); ui.SelectPet(0); ui.SavePet();
            var reloaded = new SleepetStore(directory).Preferences;
            Assert.AreEqual("Black & white", reloaded.petColour);
            Assert.AreEqual("Border Collie", reloaded.petBreed);
            Assert.IsEmpty(reloaded.petPhoto);
            Assert.IsTrue(PetThoughtRules.AnimatedAppearance(reloaded));
            SleepetSceneSession.Instance.Navigate(HighFiPage.Home);
            yield return null; yield return null; yield return null;
            ui = UnityEngine.Object.FindFirstObjectByType<SleepetHighFi>();
            var original = ui.homePet.sprite;
            ui.TapPetHighFi(); yield return new WaitForSecondsRealtime(.25f);
            Assert.AreNotSame(original, ui.homePet.sprite);
            SleepetSceneSession.Instance.Navigate(HighFiPage.AR);
            yield return null; yield return null; yield return null;
            ui = UnityEngine.Object.FindFirstObjectByType<SleepetHighFi>();
            app.AI = new ThoughtAI(); ui.ARTestBackground(); ui.PlaceOrChat();
            yield return null;
            var thoughts = ui.arPet.GetComponent<PetThoughts>(); thoughts.SendMessage("OnApplicationFocus", true);
            original = ui.arPet.sprite;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            thoughts.OnPointerDown(pointer); thoughts.OnPointerUp(pointer);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreNotSame(original, ui.arPet.sprite);
        }
        [UnityTest, Timeout(300000)]
        public IEnumerator BundledModelProducesMotionScopedThoughts()
        {
            string modelRoot = Path.GetFullPath("LocalAI");
            if (!File.Exists(Path.Combine(modelRoot, "models", LocalCompanionAI.ModelFile))) Assert.Ignore("Bundled model not installed.");
            using (var provider = new LocalCompanionAI(modelRoot))
            {
                var evidence = new System.Collections.Generic.List<string>();
                var invalid = new System.Collections.Generic.List<string>();
                foreach (string motion in new[] { "bark", "licking1", "licking2", "itching", "stretching", "sitting" })
                {
                    var context = new CompanionContext { skillId = CompanionSkills.Thoughts, petName = "Mocha", today = DateTime.Now.ToString("yyyy-MM-dd"),
                        shareData = true, thoughtMotion = motion, thoughtTrigger = PetThoughtRules.Allowed(motion, true) ? "ambient" : "tap",
                        plan = new CompanionPlan { status = "missing", activities = Array.Empty<string>() } };
                    var task = provider.SendMessage("Write one short pet thought for the fixed motion and theme.", context);
                    float deadline = Time.realtimeSinceStartup + 245;
                    while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.IsTrue(task.IsCompleted, "Local inference timed out");
                    Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
                    string answer = task.Result.Trim().Trim('"');
                    evidence.Add(motion + ": " + answer);
                    Directory.CreateDirectory("Validation/Thoughts");
                    File.WriteAllLines("Validation/Thoughts/local-model-responses.txt", evidence);
                    if (!PetThoughtRules.ValidText(answer, motion)) invalid.Add(motion + ": " + answer);
                }
                CollectionAssert.IsEmpty(invalid, string.Join("\n", invalid));
            }
        }
        sealed class ThoughtAI : ICompanionAI, ICancellableCompanionAI
        {
            public CompanionContext Context;
            public int Calls, Cancels;
            public Task<string> SendMessage(string text, CompanionContext context)
            { Context = context; Calls++; return Task.FromResult("I am happy sharing this little moment with you."); }
            public void CancelPending() { Cancels++; }
        }
        [UnityTest]
        public IEnumerator VisibleARPetAnimatesShowsBubbleAndStopsForChat()
        {
            SleepetSceneSession.TestDataDirectoryOverride = Path.GetFullPath(Path.Combine("Validation", "ThoughtTestData", Guid.NewGuid().ToString("N")));
            SceneManager.LoadScene("Sleepet_AR");
            yield return null; yield return null; yield return null;
            var ui = UnityEngine.Object.FindFirstObjectByType<SleepetHighFi>();
            var app = SleepetSceneSession.Instance.Demo;
            var ai = new ThoughtAI(); app.AI = ai;
            ui.ARTestBackground(); ui.PlaceOrChat();
            yield return null;
            var thoughts = ui.arPet.GetComponent<PetThoughts>();
            thoughts.SendMessage("OnApplicationFocus", true);
            Assert.IsTrue(thoughts.Active);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            thoughts.OnPointerDown(pointer); thoughts.OnPointerUp(pointer);
            yield return null; yield return null;
            Assert.AreEqual(1, ai.Calls);
            Assert.AreEqual(CompanionSkills.Thoughts, ai.Context.skillId);
            Assert.IsTrue(PetThoughtRules.Allowed(thoughts.CurrentMotion, false));
            yield return new WaitForSecondsRealtime(1.8f);
            var bubble = ui.arPet.transform.parent.Find("Pet thought bubble");
            Assert.NotNull(bubble);
            Assert.Greater(bubble.GetComponent<CanvasGroup>().alpha, .9f);
            Assert.AreEqual("I am happy sharing this little moment with you.", bubble.GetComponentInChildren<Text>().text);
            typeof(CompanionAITests).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { ui, "AR-thought" });
            Assert.IsEmpty(app.ChatTranscript);
            // Advance only the scheduler, without a multi-minute test delay.
            typeof(PetThoughts).GetField("nextAmbient", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(thoughts, Time.unscaledTime - 1);
            yield return null; yield return null; yield return null;
            Assert.AreEqual("ambient", ai.Context.thoughtTrigger);
            CollectionAssert.Contains(new[] { "itching", "stretching", "sitting" }, ai.Context.thoughtMotion);
            Assert.AreEqual("disabled", ai.Context.plan.status);
            ui.OpenSleepetChat();
            yield return null;
            Assert.IsFalse(thoughts.Active);
            Assert.AreEqual(0, bubble.GetComponent<CanvasGroup>().alpha);
            ui.CloseAR();
            app.Store.Preferences.petOption = 1;
            ui.RefreshMe();
            ui.ARTestBackground(); ui.PlaceOrChat();
            yield return null;
            var customSprite = ui.arPet.sprite;
            thoughts.OnPointerDown(pointer); thoughts.OnPointerUp(pointer);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreSame(customSprite, ui.arPet.sprite, "Customized pets must never switch to animated frames.");
            Assert.AreEqual(Vector3.one, ui.arPet.transform.localScale);
            thoughts.SendMessage("OnApplicationFocus", false);
            Assert.AreEqual(0, bubble.GetComponent<CanvasGroup>().alpha);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (testMouse != null) { InputSystem.RemoveDevice(testMouse); testMouse = null; }
            if (SleepetSceneSession.Instance != null) UnityEngine.Object.Destroy(SleepetSceneSession.Instance.gameObject);
            SleepetSceneSession.TestDataDirectoryOverride = null;
            yield return null;
        }
        [Test]
        public void AmbientCannotSelectTapOnlyMotionsOrRepeat()
        {
            string previous = null;
            for (int i = 0; i < 1000; i++)
            {
                string motion = PetThoughtRules.Pick(true, previous);
                CollectionAssert.Contains(new[] { "itching", "stretching", "sitting" }, motion);
                Assert.AreNotEqual(previous, motion); previous = motion;
            }
            foreach (string motion in new[] { "bark", "licking1", "licking2" })
            { Assert.IsTrue(PetThoughtRules.Allowed(motion, false)); Assert.IsFalse(PetThoughtRules.Allowed(motion, true)); }
            Assert.IsFalse(PetThoughtRules.Allowed("run", false));
        }
        [Test]
        public void AmbientPromptCannotReadPlansAndRejectsCrossThemeReminder()
        {
            var context = new CompanionContext { skillId = CompanionSkills.Thoughts, shareData = true,
                thoughtTrigger = "ambient", thoughtMotion = "sitting",
                plan = new CompanionPlan { status = "available", activities = new[] { "SECRET_MEETING" } } };
            StringAssert.DoesNotContain("SECRET_MEETING", LocalSkillPrompt.Build(context));
            Assert.IsFalse(PetThoughtRules.ValidText("You have a meeting tomorrow.", "sitting"));
            Assert.IsTrue(PetThoughtRules.ValidText("I am happy just sitting quietly here beside you.", "sitting"));
            context.thoughtMotion = "bark";
            Assert.Throws<System.ArgumentException>(() => LocalSkillPrompt.Build(context));
        }
        [Test]
        public void OnlyOriginalBlackWhiteBorderCollieCanAnimate()
        {
            var p = new UserPreferences();
            Assert.IsTrue(PetThoughtRules.AnimatedAppearance(p));
            p.petAppearance = 1; Assert.IsFalse(PetThoughtRules.AnimatedAppearance(p));
            p.petAppearance = 0; p.petOption = 1; Assert.IsFalse(PetThoughtRules.AnimatedAppearance(p));
            p.petOption = 0; p.petPhoto = "custom.png"; Assert.IsFalse(PetThoughtRules.AnimatedAppearance(p));
            p.petPhoto = ""; p.petBreed = "Golden Retriever"; Assert.IsFalse(PetThoughtRules.AnimatedAppearance(p));
        }
        [Test]
        public void AllSixBorderCollieClipsHaveSixFrames()
        {
            foreach (string motion in new[] { "bark", "licking1", "licking2", "itching", "stretching", "sitting" })
            {
                var frames = Resources.LoadAll<Sprite>("BorderCollieThoughts/" + motion);
                Assert.AreEqual(6, frames.Length, motion);
                foreach (var frame in frames) Assert.AreEqual(256, frame.rect.width);
            }
        }
    }
}
#endif
