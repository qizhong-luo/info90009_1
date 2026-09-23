#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace Sleepet.Tests
{
    public sealed class SceneArchitectureTests
    {
        SleepetSceneSession session;
        SleepetDemo demo;
        SleepetHighFi ui;
        string dataRoot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            dataRoot = Path.GetFullPath(Path.Combine("Validation", "SceneTestData", Guid.NewGuid().ToString("N")));
            SleepetSceneSession.TestDataDirectoryOverride = dataRoot;
            SceneManager.LoadScene("Sleepet_Home");
            yield return null; yield return null; yield return null;
            session = SleepetSceneSession.Instance;
            Assert.NotNull(session);
            demo = session.Demo;
            Assert.NotNull(demo.Store);
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.NotNull(ui);
            Assert.AreEqual(HighFiPage.Home, ui.scenePage);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (session != null) Object.Destroy(session.gameObject);
            yield return null;
            SleepetSceneSession.TestDataDirectoryOverride = null;
        }

        [Test]
        public void BuildContainsOnlyTheEightRequestedScreens()
        {
            var names = EditorBuildSettings.scenes.Where(s => s.enabled)
                .Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();
            CollectionAssert.AreEqual(new[] { "Sleepet_Home", "Sleepet_Sleep", "Sleepet_Me", "Sleepet_Routine",
                "Sleepet_Daily", "Sleepet_Weekly", "Sleepet_AR", "Sleepet_DayDetail" }, names);
        }

        [UnityTest]
        public IEnumerator EachSceneHasOneEditablePage()
        {
            for (int i = 0; i < 8; i++)
            {
                if (i != 0) { session.Navigate((HighFiPage)i); yield return null; yield return null; }
                ui = Object.FindFirstObjectByType<SleepetHighFi>();
                Assert.NotNull(ui);
                Assert.AreEqual((HighFiPage)i, ui.scenePage);
                Assert.AreEqual(1, ui.pages.Count(p => p != null));
                Assert.AreEqual(1, SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<SleepetHighFi>(true)).Count());
                Assert.AreSame(demo, session.Demo);
            }
            Assert.AreEqual(7, session.SceneTransitionCount);
        }

        [UnityTest]
        public IEnumerator CounterAndSavedStateContinueAcrossScenes()
        {
            var store = demo.Store;
            ui.BeginSleepFromHome();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.Sleep, ui.scenePage);
            Assert.AreSame(store, demo.Store);
            Assert.IsTrue(demo.Detector.Running);
            Assert.AreEqual(1, session.SceneTransitionCount);
            ui.OpenTimeSheet();
            ui.timePicker.SetDraft("22:15", "07:45", 25, 30);
            ui.SaveTimeSheet();
            Assert.AreEqual("22:15", store.Preferences.reminderTime);
            Assert.AreEqual("7:45AM", ui.sleepAlarm.text);
            Assert.AreEqual(30, store.Preferences.windDownSeconds);
            ui.EndSleepBySlide();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.Daily, ui.scenePage);
            Assert.AreEqual(1, store.History.records.Count);
            ui.OpenWeekly();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.Weekly, ui.scenePage);
            Assert.AreEqual(3, session.SceneTransitionCount);
            Assert.GreaterOrEqual(session.StateRevision, 3);
            Assert.AreEqual("22:15", new SleepetStore(dataRoot).Preferences.reminderTime);
            Assert.AreEqual(1, new SleepetStore(dataRoot).History.records.Count);
            ui.OpenMe();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            ui.OpenPetSheet();
            ui.petNameInput.SetTextWithoutNotify("Luna");
            ui.SavePet();
            Assert.AreEqual("Luna", store.Preferences.petName);
            ui.OpenHome();
            yield return null; yield return null;
            Assert.AreEqual(HighFiPage.Home, Object.FindFirstObjectByType<SleepetHighFi>().scenePage);
            Assert.AreEqual(5, session.SceneTransitionCount);
        }

        [UnityTest]
        public IEnumerator RoutineAndARReturnKeepSession()
        {
            var store = demo.Store;
            ui.OpenRoutine();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            ui.routineBreakfast.isOn = true;
            ui.SaveRoutine();
            Assert.IsTrue(File.Exists(Path.Combine(dataRoot, "tomorrow-plan.json")));
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.Home, ui.scenePage);
            ui.OpenAR();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.AR, ui.scenePage);
            ui.ARTestBackground(); ui.PlaceOrChat();
            Assert.IsTrue(ui.arPet.gameObject.activeSelf);
            ui.arInput.SetTextWithoutNotify("Hello Mocha");
            ui.SendARMessage();
            yield return new WaitForSecondsRealtime(demo.config.mockReplyDelay + 0.2f);
            Assert.IsFalse(demo.ChatBusy);
            ui.CloseAR();
            Assert.IsFalse(ui.arPet.gameObject.activeSelf);
            Assert.IsTrue(ui.arPlaceControls.activeSelf);
            Assert.AreEqual(HighFiPage.AR, ui.scenePage);
            ui.CloseAR();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.Home, ui.scenePage);
            Assert.AreSame(store, demo.Store);
            Assert.AreEqual(4, session.SceneTransitionCount);
        }

        [UnityTest]
        public IEnumerator ARUsesTheSavedPetOptionAcrossScenes()
        {
            ui.OpenMe();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            ui.OpenPetSheet();
            ui.NextPet();
            ui.petNameInput.SetTextWithoutNotify("Luna");
            ui.SavePet();
            var selectedSprite = ui.mePet.sprite;
            Assert.AreEqual(1, demo.Store.Preferences.petOption);
            ui.OpenAR();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreSame(selectedSprite, ui.arPet.sprite);
            ui.ARTestBackground(); ui.PlaceOrChat();
            Assert.IsTrue(ui.arPet.gameObject.activeSelf);
            Assert.AreSame(selectedSprite, ui.arPet.sprite);
            StringAssert.Contains("Luna", ui.arStatus.text);
            Capture("ARWithCustomizedPet");
        }

        [UnityTest]
        public IEnumerator TimePickerValidatesAndPetPreviewDoesNotChangeSelection()
        {
            ui.OpenMe(); yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            ui.OpenTimeSheet();
            ui.timePicker.SetDraft("00:10", "12:52", 20, 15);
            Assert.AreEqual("12", ui.timePicker.bedHour.text);
            Assert.IsTrue(ui.timePicker.TryRead(out string bed, out string wake, out _, out _));
            Assert.AreEqual("00:10", bed); Assert.AreEqual("12:52", wake);
            ui.timePicker.bedMinute.text = "60"; ui.SaveTimeSheet();
            Assert.IsTrue(ui.timeSheet.activeSelf); Assert.IsTrue(ui.noticeSheet.activeSelf);
            Assert.AreEqual("20:10", demo.Store.Preferences.reminderTime);
            ui.CloseNotice(); ui.timePicker.SetDraft("20:10", "09:52", 20, 15);
            Capture("SettingTimePicker"); ui.SaveTimeSheet();
            ui.OpenPetSheet(); ui.ChangePetAppearance();
            var appearance = ui.petPreview.sprite;
            Assert.AreSame(ui.goldenPet, appearance);
            ui.PreviewPet(); yield return new WaitForSecondsRealtime(.9f);
            Assert.AreSame(appearance, ui.petPreview.sprite);
            Capture("SettingPetPicker"); ui.SavePet(); ui.CycleSound();
            Assert.AreEqual(1, demo.Store.Preferences.petAppearance);
            ui.OpenAR(); yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreSame(appearance, ui.arPet.sprite);
            ui.OpenMe(); yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            ui.OpenPetSheet(); ui.ChangePetAppearance(); ui.ChangePetPose(); ui.SavePet();
            Assert.AreSame(ui.restingPet, ui.mePet.sprite);
            ui.OpenAR(); yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreSame(ui.restingPet, ui.arPet.sprite);
        }

        [UnityTest]
        public IEnumerator HomeAndMeHaveNoAuthoredSystemStatusAndNavigationIsTranslucent()
        {
            foreach (var page in new[] { HighFiPage.Home, HighFiPage.Me })
            {
                if (page != HighFiPage.Home) { ui.OpenMe(); yield return null; yield return null; }
                ui = Object.FindFirstObjectByType<SleepetHighFi>();
                var named = ui.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToArray();
                CollectionAssert.DoesNotContain(named, "iOS status time");
                CollectionAssert.DoesNotContain(named, "iOS status indicators");
                Assert.Less(ui.bottomNavigation.GetComponent<Image>().color.a, 0.8f);
                int active = page == HighFiPage.Home ? 0 : 2;
                for (int i = 0; i < 3; i++)
                    Assert.AreEqual(i == active ? 0.14f : 0f, ui.navHighlights[i].color.a, 0.001f);
                if (page == HighFiPage.Me)
                {
                    var setting = ui.pages[(int)HighFiPage.Me].transform;
                    StringAssert.StartsWith("setting-web-dial-base", setting.GetComponentsInChildren<Image>(true)
                        .Single(image => image.name == "Plan dial").sprite.name);
                    var petCircle = setting.GetComponentsInChildren<Image>(true)
                        .SingleOrDefault(image => image.name == "Pet avatar circle");
                    Assert.NotNull(petCircle);
                    Assert.NotNull(petCircle.GetComponent<Mask>());
                    Assert.AreSame(petCircle.transform, ui.mePet.transform.parent);
                    Assert.AreEqual(113f, ((RectTransform)setting.GetComponentsInChildren<Transform>(true)
                        .Single(t => t.name == "Preferences card")).sizeDelta.y);
                    Assert.IsTrue(setting.GetComponentsInChildren<Button>(true).Single(b => b.name == "Previous mode").gameObject.activeInHierarchy);
                    Assert.NotNull(ui.meVolumePercent);
                }
            }
            foreach (var name in new[] { "nav-home", "nav-sleep", "nav-me" })
            {
                var path = Path.Combine("Assets", "Sleepet", "Art", "Figma", name + ".png");
                var texture = new Texture2D(2, 2);
                texture.LoadImage(File.ReadAllBytes(path));
                Assert.AreEqual(0, texture.GetPixel(0, texture.height - 1).a, 0.001f,
                    name + " top corner background must be transparent");
                Object.Destroy(texture);
            }
        }

        [UnityTest]
        public IEnumerator WebSettingProfileAndControlsPersistAcrossScenes()
        {
            ui.OpenMe(); yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            var profile = ui.profileEditor;
            var edit = ui.GetComponentsInChildren<Button>(true).Single(b => b.name == "Edit profile name");
            AssertButtonCanBeHit(edit);
            edit.onClick.Invoke(); Assert.IsTrue(profile.sheet.activeSelf);
            Assert.AreEqual(10, profile.nameInput.characterLimit);
            Assert.AreEqual('\0', SleepetProfileEditor.ValidateLetter("", 0, '1'));
            Assert.AreEqual('\0', SleepetProfileEditor.ValidateLetter("", 0, '中'));
            Assert.IsFalse(SleepetStore.ValidProfileName("ABCDEFGHIJK"));
            profile.nameInput.SetTextWithoutNotify(""); profile.Save(); Assert.IsTrue(profile.sheet.activeSelf);
            Assert.AreEqual("Sally", demo.Store.Preferences.profileName);
            profile.nameInput.SetTextWithoutNotify("Alex"); profile.Select(2); profile.Cancel();
            Assert.AreEqual("Sally", demo.Store.Preferences.profileName);
            profile.Open(); profile.nameInput.SetTextWithoutNotify("AlexandraZ"); profile.Select(2);
            Capture("SettingProfileEditor");
            profile.Save(); Assert.IsFalse(profile.sheet.activeSelf);
            yield return null; yield return null;
            Assert.AreEqual("AlexandraZ", profile.displayName.text);
            Assert.AreSame(profile.presets[2], profile.avatar.sprite);
            int sound = demo.Store.Preferences.sound;
            var previous = ui.GetComponentsInChildren<Button>(true).Single(b => b.name == "Previous sound");
            AssertButtonCanBeHit(previous); previous.onClick.Invoke();
            Assert.AreEqual((sound + 2) % 3, demo.Store.Preferences.sound);
            ui.CycleSound(); Assert.AreEqual(sound, demo.Store.Preferences.sound);
            var mode = demo.Store.Preferences.companion;
            var previousMode = ui.GetComponentsInChildren<Button>(true).Single(b => b.name == "Previous mode");
            AssertButtonCanBeHit(previousMode); previousMode.onClick.Invoke(); ui.CycleMode();
            Assert.AreEqual(mode, demo.Store.Preferences.companion);
            ui.meVolume.value = .73f;
            Assert.AreEqual(.73f, demo.Store.Preferences.volume, .001f);
            ui.scheduleDial.SetMinutes(true, 22 * 60 + 15, true);
            ui.scheduleDial.SetMinutes(false, 7 * 60 + 30, true);
            Assert.AreEqual("22:15", demo.Store.Preferences.reminderTime);
            Assert.AreEqual("07:30", demo.Store.Preferences.wakeTime);
            Assert.AreEqual(0, SleepetScheduleDial.MinutesAt(Vector2.up));
            Assert.AreEqual(360, SleepetScheduleDial.MinutesAt(Vector2.right));
            Assert.AreEqual(720, SleepetScheduleDial.MinutesAt(Vector2.down));
            Assert.AreEqual(1080, SleepetScheduleDial.MinutesAt(Vector2.left));
            ui.OpenHome(); yield return null; yield return null;
            Assert.AreEqual("Good evening, AlexandraZ.", Object.FindFirstObjectByType<SleepetHighFi>().homeGreeting.text);
            Object.FindFirstObjectByType<SleepetHighFi>().OpenMe(); yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual("AlexandraZ", ui.profileEditor.displayName.text);
            Assert.AreEqual("10:15PM", ui.meBedtime.text);
            var disk = new SleepetStore(dataRoot);
            Assert.AreEqual("AlexandraZ", disk.Preferences.profileName);
            Assert.AreEqual(2, disk.Preferences.profileAvatar);
            Assert.AreEqual(.73f, disk.Preferences.volume, .001f);
            Capture("SettingCustomized");
            var scroll = ui.pages[(int)HighFiPage.Me].GetComponentInChildren<ScrollRect>();
            scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
            Capture("SettingSupport");
        }

        static void AssertButtonCanBeHit(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = button.GetComponent<RectTransform>();
            var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = position };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsTrue(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button,
                button.name + " must receive pointer input at its visible center");
        }

        [UnityTest]
        public IEnumerator HealthEntryHasAnExplicitFallback()
        {
            session.Navigate(HighFiPage.Daily);
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            ui.OpenHealthApp();
            Assert.IsTrue(ui.noticeSheet.activeSelf);
            StringAssert.Contains("unavailable", ui.noticeText.text);
            ui.CloseNotice();
            Assert.IsFalse(ui.noticeSheet.activeSelf);
        }

        [UnityTest]
        public IEnumerator DailyNormalHasOnlyBottomPageSwitching()
        {
            session.Navigate(HighFiPage.Daily);
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            var page = ui.pages[(int)HighFiPage.Daily].transform;
            Assert.IsNull(page.Find("Back"));
            Assert.IsNull(page.Find("Daily period display").GetComponent<Button>());
            Assert.IsFalse(ui.dailyDate.text.Contains("›"));

            var nav = ui.bottomNavigation.transform;
            var home = nav.Find("Nav 0/Tap").GetComponent<Button>();
            var me = nav.Find("Nav 2/Tap").GetComponent<Button>();
            Assert.Greater(home.onClick.GetPersistentEventCount(), 0);
            Assert.Greater(me.onClick.GetPersistentEventCount(), 0);
            home.onClick.Invoke();
            yield return null; yield return null;
            Assert.AreEqual(HighFiPage.Home, Object.FindFirstObjectByType<SleepetHighFi>().scenePage);
            session.Navigate(HighFiPage.Daily);
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            ui.bottomNavigation.transform.Find("Nav 2/Tap").GetComponent<Button>().onClick.Invoke();
            yield return null; yield return null;
            Assert.AreEqual(HighFiPage.Me, Object.FindFirstObjectByType<SleepetHighFi>().scenePage);
        }

        [UnityTest]
        public IEnumerator MoonOpensWeeklyAndEachDateOpensItsOwnSavedReport()
        {
            var store = demo.Store;
            ui.bottomNavigation.transform.Find("Nav 1/Tap").GetComponent<Button>().onClick.Invoke();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.Weekly, ui.scenePage);
            Assert.IsNull(ui.pages[(int)HighFiPage.Weekly].transform.Find("Back"));
            Assert.AreEqual(7, ui.weeklyDayButtons.Length);
            Assert.NotNull(ui.weeklyAddTestButton);

            ui.weeklyAddTestButton.onClick.Invoke();
            Assert.AreEqual(7, store.History.records.Count(r => r.sample));
            foreach (var sample in store.History.records.Where(r => r.sample))
            {
                Assert.Greater(sample.deepMinutes, 0);
                Assert.Greater(sample.lightMinutes, 0);
                Assert.Greater(sample.remMinutes, 0);
                Assert.AreEqual(sample.durationSeconds / 60f,
                    sample.deepMinutes + sample.lightMinutes + sample.remMinutes);
            }
            Assert.AreEqual(1, session.StateRevision);
            ui.CloseNotice();
            ui.weeklyAddTestButton.onClick.Invoke();
            Assert.AreEqual(7, store.History.records.Count(r => r.sample));
            Assert.AreEqual(7, new SleepetStore(dataRoot).History.records.Count(r => r.sample));
            ui.CloseNotice();
            Capture("WeeklyWithTestData");
            Assert.AreEqual("11:50 pm", ui.weeklyAverage.text);
            Assert.AreEqual("0", ui.weeklyCount.text);
            Assert.AreEqual("7", ui.weeklyWakeCount.text);
            StringAssert.Contains("TEST DATA", ui.weeklyDataNote.text);

            ui.weeklyDayButtons[0].onClick.Invoke();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.DayDetail, ui.scenePage);
            Assert.AreEqual(DateTime.Today.AddDays(-6), session.SelectedReportDate);
            Assert.AreEqual("7h 20m", ui.dailyDuration.text);
            Assert.IsTrue(ui.dailyStageNote.text.Contains("FIXED TEST DATA"));
            Assert.AreEqual("1h 40m", ui.dailyStageDeep.text);
            Assert.AreEqual("3h 50m", ui.dailyStageLight.text);
            Assert.AreEqual("1h 50m", ui.dailyStageRem.text);
            Assert.AreEqual(3, ui.dailyStageArcs.Length);
            Assert.IsTrue(ui.dailyStageVisuals.All(v => v.activeSelf));
            Assert.IsTrue(ui.dailyStageTimeline.gameObject.activeSelf);
            Assert.AreEqual(205f, ((RectTransform)ui.pages[(int)HighFiPage.DayDetail].transform.Find("Stages card")).sizeDelta.y);
            Assert.AreSame(store, demo.Store);
            Capture("DayDetailWithTestData");

            store.Add(new SleepSummary {
                sessionId = "real-session-today",
                startedAt = DateTime.Today.AddHours(22).ToUniversalTime().ToString("O"),
                durationSeconds = 3600, result = "Calm", sample = false
            });

            ui.bottomNavigation.transform.Find("Nav 1/Tap").GetComponent<Button>().onClick.Invoke();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.Weekly, ui.scenePage);
            ui.weeklyDayButtons[6].onClick.Invoke();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.DayDetail, ui.scenePage);
            Assert.AreEqual(DateTime.Today, session.SelectedReportDate);
            Assert.AreEqual("1h 00m", ui.dailyDuration.text);
            Assert.IsFalse(ui.dailyStageNote.text.Contains("FIXED TEST DATA"));
            Assert.IsTrue(ui.dailyStageVisuals.All(v => !v.activeSelf));
            Assert.IsFalse(ui.dailyStageTimeline.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator AuthoredButtonsCallTheirOwnSceneController()
        {
            var routineButton = ui.pages[(int)HighFiPage.Home].transform.Find("TomorrowRoutine").GetComponent<Button>();
            Assert.Greater(routineButton.onClick.GetPersistentEventCount(), 0);
            routineButton.onClick.Invoke();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.Routine, ui.scenePage);
            ui.pages[(int)HighFiPage.Routine].transform.Find("Back").GetComponent<Button>().onClick.Invoke();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            ui.pages[(int)HighFiPage.Home].transform.Find("ReadyToSleep").GetComponent<Button>().onClick.Invoke();
            yield return null; yield return null;
            ui = Object.FindFirstObjectByType<SleepetHighFi>();
            Assert.AreEqual(HighFiPage.Sleep, ui.scenePage);
            Assert.IsTrue(demo.Detector.Running);
            var slide = ui.pages[(int)HighFiPage.Sleep].GetComponentInChildren<SlideToEndHighFi>(true);
            Assert.NotNull(slide);
            Assert.AreSame(ui, slide.app);
        }

        [UnityTest]
        public IEnumerator EachAuthoredSceneRendersAtPhoneReferenceSize()
        {
            for (int i = 0; i < 8; i++)
            {
                if (i != 0) { session.Navigate((HighFiPage)i); yield return null; yield return null; }
                ui = Object.FindFirstObjectByType<SleepetHighFi>();
                Capture(((HighFiPage)i).ToString());
            }
        }

        void Capture(string name)
        {
            string folder = Path.GetFullPath(Path.Combine("Validation", "SceneScreens"));
            Directory.CreateDirectory(folder);
            var layout = ui.GetComponent<HighFiAdaptiveLayout>();
            layout.enabled = false;
            var scaler = ui.GetComponent<CanvasScaler>();
            float savedMatch = scaler.matchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Canvas.ForceUpdateCanvases();
            layout.ApplyLayout(874, 0);
            var page = (RectTransform)ui.pages[(int)ui.scenePage].transform;
            page.anchoredPosition = new Vector2(0, page.anchoredPosition.y);
            if (ui.bottomNavigation != null)
            {
                var nav = (RectTransform)ui.bottomNavigation.transform;
                nav.anchoredPosition = new Vector2(0, nav.anchoredPosition.y);
            }
            foreach (var sheet in new[] { ui.timeSheet, ui.petSheet, ui.eventSheet, ui.noticeSheet, ui.profileEditor ? ui.profileEditor.sheet : null })
                if (sheet) { var rect = (RectTransform)sheet.transform; rect.anchoredPosition = new Vector2(0, rect.anchoredPosition.y); }
            Canvas.ForceUpdateCanvases();
            var canvas = ui.GetComponent<Canvas>();
            var cameraObject = new GameObject("Scene evidence camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var target = new RenderTexture(402, 874, 24);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); camera.Render();
            var prior = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(402, 874, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 402, 874), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            RenderTexture.active = prior;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            scaler.matchWidthOrHeight = savedMatch;
            Canvas.ForceUpdateCanvases();
            layout.ApplyLayout(((RectTransform)canvas.transform).rect.height, 0);
            camera.targetTexture = null;
            target.Release();
            Object.Destroy(texture); Object.Destroy(target); Object.Destroy(cameraObject);
            layout.enabled = true;
        }
    }
}
#endif
