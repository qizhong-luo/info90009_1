#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Sleepet.Tests
{
    public sealed class SceneIssueTests
    {
        SleepetSceneSession session;
        SleepetHighFi UI => Object.FindFirstObjectByType<SleepetHighFi>();
        SleepetFlow Flow => Object.FindFirstObjectByType<SleepetFlow>();
        [UnitySetUp] public IEnumerator Setup()
        {
            SleepetSceneSession.TestDataDirectoryOverride = Path.GetFullPath("Validation/SceneIssues/Data/" + Guid.NewGuid().ToString("N"));
            SceneManager.LoadScene("Sleepet_Home"); yield return null; yield return null; yield return null;
            session = SleepetSceneSession.Instance;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (session) Object.Destroy(session.gameObject); yield return null;
            SleepetSceneSession.TestDataDirectoryOverride = null;
        }
        IEnumerator Open(HighFiPage page) { session.Navigate(page); yield return null; yield return null; yield return null; }
        [UnityTest] public IEnumerator PetRespondsAndReturnsToOriginalTransform()
        {
            var scale = UI.homePet.transform.localScale; var rotation = UI.homePet.transform.localRotation;
            UI.TapPetHighFi(); yield return new WaitForSecondsRealtime(.2f);
            Assert.AreNotEqual(scale, UI.homePet.transform.localScale);
            UI.TapPetHighFi(); yield return new WaitForSecondsRealtime(.9f);
            Assert.AreEqual(scale, UI.homePet.transform.localScale); Assert.AreEqual(rotation, UI.homePet.transform.localRotation);
        }
        [UnityTest] public IEnumerator DropdownShowsReadableOptionsAndCanSelect()
        {
            session.BeginOnboarding(); session.OpenFlow("PersonalisePet"); yield return null; yield return null; yield return null;
            Flow.breed.Show(); yield return new WaitForSecondsRealtime(.2f);
            var list = Flow.breed.transform.Find("Dropdown List"); Assert.NotNull(list);
            var options = list.GetComponentsInChildren<Toggle>(); Assert.Greater(options.Length, 3);
            Assert.IsTrue(options.All(t => t.GetComponentInChildren<Text>().font && !string.IsNullOrWhiteSpace(t.GetComponentInChildren<Text>().text)));
            Capture("01-dropdown", Flow.GetComponentInParent<Canvas>());
            options[1].isOn = true; Assert.AreEqual(1, Flow.breed.value);
        }
        [UnityTest] public IEnumerator DateAndEventsPersistAndDeleteIndividually()
        {
            yield return Open(HighFiPage.Routine);
            UI.NextRoutineDate(); UI.dateYear.text = "2028"; UI.dateMonth.text = "2"; UI.dateDay.text = "30";
            UI.SaveRoutineDate(); Assert.IsTrue(UI.dateSheet.activeSelf); Assert.IsNotEmpty(UI.dateError.text);
            UI.dateDay.text = "29"; Capture("02-date-picker", UI.GetComponent<Canvas>()); UI.SaveRoutineDate();
            Assert.AreEqual("2028/02/29", UI.routineDate.text);
            foreach (string title in new[] { "Library", "Walk" }) { UI.OpenEventSheet(); UI.eventInput.text = title; UI.SaveEvent(); yield return null; }
            Assert.AreEqual(2, UI.customEventContent.GetComponentsInChildren<Toggle>().Length);
            var library = UI.customEventContent.GetComponentsInChildren<Toggle>().Single(t => t.GetComponentInChildren<Text>().text == "Library");
            var textRect = library.GetComponentInChildren<Text>().rectTransform;
            Assert.LessOrEqual(textRect.anchoredPosition.x + textRect.rect.width, ((RectTransform)library.transform).rect.width);
            library.isOn = false; yield return null;
            Assert.IsFalse(UI.selectedEventContent.GetComponentsInChildren<Button>().Any(b => b.GetComponentInChildren<Text>().text == "Library"));
            UI.ShowRoutineHelp(); Assert.IsTrue(UI.noticeSheet.activeSelf); StringAssert.Contains("saved", UI.noticeText.text);
            var disk = session.Demo.Store.ReadPlan(); Assert.AreEqual(2, disk.events.Count); Assert.IsFalse(disk.events[0].selected);
            Capture("03-saved", UI.GetComponent<Canvas>()); UI.Back();
            var walk = UI.selectedEventContent.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == "Walk");
            walk.onClick.Invoke(); yield return null;
            Capture("04-events", UI.GetComponent<Canvas>());
            UI.ShowRoutineHelp(); disk = session.Demo.Store.ReadPlan(); Assert.AreEqual(1, disk.events.Count); Assert.IsTrue(disk.groceries);
            yield return Open(HighFiPage.Home); yield return Open(HighFiPage.Routine);
            Assert.AreEqual("2028/02/29", UI.routineDate.text); Assert.AreEqual(1, UI.customEventContent.GetComponentsInChildren<Toggle>().Length);
        }
        [UnityTest] public IEnumerator AudioMatchesPreferencesAcrossSceneAndSoundChanges()
        {
            var demo = session.Demo;
            demo.view.sound.value = 0; demo.view.volume.value = .6f; demo.view.behaviour.value = (int)SleepBehaviour.PlayAllNight; demo.SaveSettings();
            UI.BeginSleepFromHome(); yield return null; yield return null; yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(demo.Detector.Running); Assert.IsTrue(demo.Media.IsPlaying, "Selected rain must actually play after navigation");
            Assert.AreSame(demo.Media.rain, demo.Media.source.clip); Assert.AreEqual(.6f, demo.Media.Volume, .01f);
            UI.CycleSound(); yield return null; Assert.AreSame(demo.Media.ocean, demo.Media.source.clip); Assert.IsTrue(demo.Media.IsPlaying);
            UI.CycleSound(); yield return null; Assert.IsFalse(demo.Media.IsPlaying);
            UI.CycleSound(); yield return null; Assert.IsTrue(demo.Media.IsPlaying, "Leaving Silence during an active session must start sound");
            demo.Detector.Force(SleepState.LikelyAsleep); yield return null; Assert.IsTrue(demo.Media.IsPlaying);
            demo.EndSleep(); Assert.IsFalse(demo.Media.IsPlaying);
        }
        [UnityTest] public IEnumerator WeeklyLineTracksMeanOfVisibleBars()
        {
            yield return Open(HighFiPage.Weekly); Assert.IsFalse(UI.weeklyAverageLine.gameObject.activeSelf);
            UI.AddFixedWeekTestData(); UI.Back();
            var bars = UI.weeklyDayButtons.Select(b => (RectTransform)b.transform.Find("Bar")).ToArray();
            Assert.AreEqual(-102 - bars.Average(b => b.sizeDelta.y), UI.weeklyAverageLine.anchoredPosition.y, .01f);
            Capture("05-weekly", UI.GetComponent<Canvas>());
            float before = UI.weeklyAverageLine.anchoredPosition.y;
            session.Demo.Store.Add(new SleepSummary { sessionId = "short", startedAt = DateTime.Now.ToString("O"), durationSeconds = 3600 });
            UI.OpenWeekly(); Assert.AreNotEqual(before, UI.weeklyAverageLine.anchoredPosition.y);
        }
        [UnityTest] public IEnumerator ChatComposerHasNoUnwantedCopy()
        {
            yield return Open(HighFiPage.AR); UI.OpenSleepetChat(); yield return null;
            Assert.IsFalse(UI.arModelNotice.gameObject.activeSelf); Assert.AreEqual("", UI.arAIStatus.text);
            UI.arInput.text = "Hello"; yield return null; Assert.IsTrue(UI.arSendButton.interactable);
            Assert.AreEqual(InputField.LineType.MultiLineNewline, UI.arInput.lineType);
            Capture("06-chat", UI.GetComponent<Canvas>());
        }
        [UnityTest] public IEnumerator NotifyCircleAndSwipeOnlyMorningCompletion()
        {
            session.Demo.StartSleep(); session.Demo.EndSleep(); session.BeginMorning(); session.OpenFlow("MorningTodos");
            yield return null; yield return null; yield return new WaitForSecondsRealtime(1.5f);
            var toggle = Flow.notify;
            var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            bool before = toggle.isOn; ExecuteEvents.Execute(toggle.gameObject, click, ExecuteEvents.pointerClickHandler);
            Assert.AreNotEqual(before, toggle.isOn); Assert.AreNotEqual(toggle.targetGraphic, toggle.graphic);
            toggle.isOn = true; yield return new WaitForSecondsRealtime(.2f);
            Assert.NotNull(((Image)toggle.graphic).sprite);
            StringAssert.Contains("Checkmark", ((Image)toggle.graphic).sprite.name);
            var corners = new Vector3[4]; ((RectTransform)toggle.targetGraphic.transform).GetWorldCorners(corners);
            click.position = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * .5f);
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(click, hits);
            Assert.IsTrue(hits.Count > 0); Assert.AreSame(toggle.gameObject, ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));
            Capture("07-notify", Flow.GetComponentInParent<Canvas>());
            Flow.FinishTodos(); yield return null; yield return null;
            Flow.primary.onClick.Invoke(); yield return null; Assert.AreEqual("MorningEnd", Flow.page);
            Assert.AreSame(Flow.gameObject, ExecuteEvents.GetEventHandler<IDragHandler>(Flow.primary.gameObject));
            float scale = Flow.GetComponentInParent<Canvas>().scaleFactor;
            var drag = new PointerEventData(EventSystem.current) { position = new Vector2(100, 100) };
            Flow.OnBeginDrag(drag); drag.position += new Vector2(100, 20) * scale; Flow.OnDrag(drag); Flow.OnEndDrag(drag);
            Assert.IsFalse(Flow.transition.gameObject.activeSelf);
            drag.position = new Vector2(100, 100); Flow.OnBeginDrag(drag); drag.position += new Vector2(0, 90) * scale;
            Flow.OnDrag(drag); Flow.OnEndDrag(drag); yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual("Sleepet_Home", SceneManager.GetActiveScene().name);
            Assert.IsTrue(session.Demo.Store.History.records.Single(r => r.sessionId == session.MorningSessionId).morningCompleted);
        }
        static void Capture(string name, Canvas canvas)
        {
            string directory = Path.GetFullPath("Validation/SceneIssues/Screens"); Directory.CreateDirectory(directory);
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.enabled = false; canvas.scaleFactor = 1;
            var cameraObject = new GameObject("Evidence camera", typeof(Camera)); var camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(402, 874, 24); camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            var layout = canvas.GetComponent<HighFiAdaptiveLayout>();
            if (layout) { layout.enabled = false; layout.ApplyLayout(874, 0); }
            var flowLayout = canvas.GetComponent<SleepetFlowLayout>();
            if (flowLayout) { flowLayout.enabled = false; flowLayout.ApplyLayout(402, 874, 0); }
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(402, 874, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 402, 874), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG()); RenderTexture.active = previous;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; scaler.enabled = true;
            if (layout) layout.enabled = true; if (flowLayout) flowLayout.enabled = true;
            camera.targetTexture = null; target.Release(); Object.Destroy(target); Object.Destroy(image); Object.Destroy(cameraObject);
        }
    }
}
#endif
