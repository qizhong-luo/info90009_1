using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sleepet
{
    public interface IExternalHealthAppLauncher
    {
        bool TryOpen(string url);
    }

    public sealed class UnityExternalHealthAppLauncher : IExternalHealthAppLauncher
    {
        public bool TryOpen(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
#if UNITY_IOS && !UNITY_EDITOR
            Application.OpenURL(url);
            return true; // The OS owns the external app transition and back-to-app affordance.
#else
            return false;
#endif
        }
    }

    public enum HighFiPage { Home, Sleep, Me, Routine, Daily, Weekly, AR, DayDetail }

    [Serializable]
    public sealed class TomorrowPlan
    {
        public string date = "";
        public bool water = true, stretch = true, breakfast;
        public bool groceries = true, gym, meeting = true, call;
        public string shortEvent = "";
    }

    // The UI owns navigation and presentation. SleepetDemo remains the session/data backend.
    public sealed class SleepetHighFi : MonoBehaviour
    {
        public HighFiPage scenePage = HighFiPage.Home;
        public SleepetDemo demo;
        public GameObject[] pages;
        public GameObject bottomNavigation, timeSheet, petSheet, eventSheet, noticeSheet;
        public Text clock, sleepClock, sleepAlarm, sleepMusic, sleepBehaviour;
        public Text meBedtime, meWake, meWinddown, meSound, meMode, mePetName;
        public Text dailyDate, dailyDuration, dailyGoal, dailyStageNote, dailyWatch, weeklyRange, weeklyCount, weeklyAverage, weeklyChart;
        public Text dailyGoalBadge, dailyStageTotal, dailyStageDeep, dailyStageLight, dailyStageRem, dailyStart, dailyEnd;
        public Image[] dailyStageArcs;
        public GameObject[] dailyStageVisuals;
        public Image dailyStageTimeline;
        public Button[] weeklyDayButtons;
        public Button weeklyAddTestButton;
        public Text routineDate, routineEvents, routineNotice, arStatus, arChat, noticeText;
        public Toggle routineWater, routineStretch, routineBreakfast, routineGroceries, routineGym, routineMeeting, routineCall;
        public InputField eventInput, timeBedtimeInput, timeWakeInput, timeWindInput, petNameInput, arInput;
        public Slider meVolume;
        public Text meVolumePercent;
        public Text homeGreeting;
        public Text homeReadyLabel, homeARLabel, homeTapHint;
        public Text weeklyWakeCount, weeklyAverageSleep, weeklyBedtimeLabel, weeklyDataNote;
        public RectTransform weeklyAverageLine;
        public SleepetTimePicker timePicker;
        public Text dailyDateCaption, dailyRelativeDate;
        public SleepetProfileEditor profileEditor;
        public SleepetScheduleDial scheduleDial;
        public RawImage arPreview;
        public GameObject arRoom;
        public GameObject arPlaceControls, arChatControls;
        public Image arPet;
        public Button arPlaceButton, arSendButton, sleepStartButton;
        public Text arModelLabel;
        public string[] arModelLabels = { "Offline demo", "ChatGPT", "Claude", "Doubao" };
        public Sprite[] petOptions;
        public Sprite restingPet;
        public Sprite goldenPet;
        public Image petPosePreview;
        public Image[] petSelection;
        public Image petPreview, homePet, sleepPet, mePet;
        public Text[] navLabels;
        public Image[] navIcons;
        public Image[] navHighlights;
        public Color navActive = new Color(1f, 1f, 1f, 0.14f);
        public HighFiPage CurrentPage { get; private set; }
        public bool WatchConnected { get; private set; }
        public string HealthAppUrl = ""; // Assign only after validating a supported target URL on iPhone.
        public IExternalHealthAppLauncher HealthLauncher { get; set; } = new UnityExternalHealthAppLauncher();

        TomorrowPlan plan;
        bool placed, arChatOpen, applyingPlan;
        int selectedPet, arModel;
        int selectedAppearance, selectedPose;
        Coroutine petAnimation;
        string lastPageBeforeAR = "Home";
        float previousReportScroll;
        ScrollRect reportScroll;
        string dataPath;

        IEnumerator Start()
        {
#if UNITY_IOS || UNITY_ANDROID
            Screen.orientation = ScreenOrientation.Portrait;
#endif
            if (SleepetSceneSession.Instance != null) demo = SleepetSceneSession.Instance.Demo;
            if (demo == null) demo = GetComponentInParent<SleepetDemo>();
            while (demo == null || demo.Store == null) yield return null;
            CurrentPage = scenePage;
            SleepetSceneSession.Instance?.AdoptPage(scenePage);
            dataPath = Path.Combine(demo.Store.DirectoryPath, "tomorrow-plan.json");
            LoadPlan();
            ApplyPlanToUI();
            if (meVolume != null)
            {
                meVolume.SetValueWithoutNotify(demo.Store.Preferences.volume);
                meVolume.onValueChanged.AddListener(v => { demo.view.volume.SetValueWithoutNotify(v); demo.OnVolumeChanged(v); demo.SaveSettings(); SleepetSceneSession.Instance?.MarkStateChanged(); if (meVolumePercent) meVolumePercent.text = Mathf.RoundToInt(v * 100f) + "%"; });
            }
            if (scenePage == HighFiPage.AR)
            {
                placed = false; arChatOpen = false;
                arPet.gameObject.SetActive(false); arChat.text = "";
                arPlaceControls.SetActive(true); arChatControls.SetActive(false);
                demo.OpenCamera();
            }
            else if (scenePage == HighFiPage.Home) demo.OpenHome();
            else if (scenePage == HighFiPage.Sleep) demo.OpenSleep();
            else if (scenePage == HighFiPage.Me) demo.OpenMe();
            Show(scenePage);
            RefreshMe(); RefreshReports();
        }

        void Update()
        {
            if (demo == null || demo.Store == null) return;
            var now = DateTime.Now;
            string time = now.ToString("h:mm", CultureInfo.InvariantCulture);
            if (clock != null) clock.text = time;
            if (sleepClock != null) sleepClock.text = time + now.ToString(" tt", CultureInfo.InvariantCulture);
            if (CurrentPage == HighFiPage.AR) RefreshAR();
            if (CurrentPage == HighFiPage.Sleep) RefreshSleep();
        }

        void Show(HighFiPage page)
        {
            if (page != CurrentPage && SleepetSceneSession.Instance != null)
            {
                SleepetSceneSession.Instance.Navigate(page);
                return;
            }
            CurrentPage = page;
            for (int i = 0; i < pages.Length; i++) if (pages[i] != null) pages[i].SetActive(i == (int)page);
            bool nav = page == HighFiPage.Home || page == HighFiPage.Me || page == HighFiPage.Daily || page == HighFiPage.Weekly || page == HighFiPage.DayDetail;
            if (bottomNavigation != null) bottomNavigation.SetActive(nav);
            int active = page == HighFiPage.Home ? 0 : page == HighFiPage.Me ? 2 : 1;
            for (int i = 0; i < navHighlights.Length; i++) if (navHighlights[i] != null) navHighlights[i].color = i == active ? navActive : Color.clear;
            for (int i = 0; i < navLabels.Length; i++) if (navLabels[i] != null) navLabels[i].color = i == active ? Color.white : new Color(0.78f, 0.86f, 0.95f);
            for (int i = 0; i < navIcons.Length; i++) if (navIcons[i] != null) navIcons[i].color = i == active ? Color.white : new Color(0.78f, 0.86f, 0.95f);
        }
        public void OpenHome()
        {
            if (demo != null && demo.Store != null) demo.OpenHome();
            CloseSheets(); Show(HighFiPage.Home);
        }
        public void TapPetHighFi() { if (demo != null && demo.Store != null) demo.TapPet(); }
        public void OpenSleep()
        {
            if (demo != null && demo.Store != null) demo.OpenSleep();
            CloseSheets(); Show(HighFiPage.Sleep); RefreshSleep();
        }
        public void BeginSleepFromHome()
        {
            if (demo != null && demo.Store != null && !demo.Detector.Running)
            { demo.StartSleep(); SleepetSceneSession.Instance?.MarkStateChanged(); }
            OpenSleep();
        }
        public void OpenMe()
        {
            if (demo != null && demo.Store != null) demo.OpenMe();
            CloseSheets(); Show(HighFiPage.Me); RefreshMe();
        }
        public void OpenRoutine() { CloseSheets(); Show(HighFiPage.Routine); ApplyPlanToUI(); }
        public void OpenDaily() { CloseSheets(); Show(HighFiPage.Daily); RefreshReports(); }
        public void OpenWeekly() { CloseSheets(); Show(HighFiPage.Weekly); RefreshReports(); }
        public void OpenWeekDay(int index)
        {
            if (index < 0 || index > 6) return;
            CloseSheets();
            SleepetSceneSession.Instance?.SelectReportDate(DateTime.Today.AddDays(index - 6));
        }
        public void AddFixedWeekTestData()
        {
            if (demo == null || demo.Store == null) return;
            if (!demo.Store.AddFixedWeekSamples(DateTime.Today)) { ShowNotice(demo.Store.Error); return; }
            SleepetSceneSession.Instance?.MarkStateChanged();
            RefreshReports();
            ShowNotice("Seven fixed test nights added. Tap a date to open its daily report.");
        }
        public void Back()
        {
            if (profileEditor && profileEditor.sheet.activeSelf) { profileEditor.Cancel(); return; }
            if ((eventSheet && eventSheet.activeSelf) || (timeSheet && timeSheet.activeSelf) ||
                (petSheet && petSheet.activeSelf) || (noticeSheet && noticeSheet.activeSelf)) { CloseSheets(); return; }
            if (CurrentPage == HighFiPage.AR) { CloseAR(); return; }
            if (CurrentPage == HighFiPage.Routine) { OpenHome(); return; }
            if (CurrentPage == HighFiPage.DayDetail) { OpenWeekly(); return; }
            if (CurrentPage == HighFiPage.Weekly) { OpenHome(); return; }
            OpenHome();
        }
        void CloseSheets()
        {
            if (profileEditor) profileEditor.Cancel();
            if (timeSheet) timeSheet.SetActive(false);
            if (petSheet) petSheet.SetActive(false);
            if (eventSheet) eventSheet.SetActive(false);
            if (noticeSheet) noticeSheet.SetActive(false);
        }
        public void CloseTimeSheet() { if (timeSheet) timeSheet.SetActive(false); }
        public void ClosePetSheet() { if (petSheet) petSheet.SetActive(false); }
        public void CloseEventSheet() { if (eventSheet) eventSheet.SetActive(false); }
        public void ShowRoutineHelp() { ShowNotice("Use the checkboxes to change your gentle start."); }
        public void ShowPrivacy() { ShowSupport("Privacy & data"); }
        public void ShowAccessibility() { ShowSupport("Accessibility"); }
        public void ShowDevices() { ShowSupport("Connected Devices"); }
        public void ShowNotifications() { ShowSupport("Notifications"); }
        public void ShowHelp() { ShowSupport("Help & Q&A"); }
        public void StartOrEndSleep()
        {
            if (demo.Detector.Running) { demo.EndSleep(); SleepetSceneSession.Instance?.MarkStateChanged(); OpenDaily(); }
            else { demo.StartSleep(); SleepetSceneSession.Instance?.MarkStateChanged(); RefreshSleep(); }
        }
        public void EndSleepBySlide()
        {
            if (!demo.Detector.Running) { ShowNotice("Start a sleep session first."); return; }
            demo.EndSleep(); SleepetSceneSession.Instance?.MarkStateChanged(); OpenDaily();
        }
        public void CycleBehaviour()
        {
            var d = demo.view.behaviour;
            d.value = (d.value + 1) % d.options.Count;
            demo.SaveSettings(); RefreshSleep(); RefreshMe();
            SleepetSceneSession.Instance?.MarkStateChanged();
        }
        public void CycleSound()
        { ChangeSound(1); }
        public void PreviousSound() { ChangeSound(-1); }
        void ChangeSound(int direction)
        {
            var d = demo.view.sound;
            d.value = (d.value + direction + d.options.Count) % d.options.Count;
            demo.SaveSettings(); RefreshSleep(); RefreshMe();
            SleepetSceneSession.Instance?.MarkStateChanged();
        }
        public void CycleMode()
        { ChangeMode(1); }
        public void PreviousMode() { ChangeMode(-1); }
        void ChangeMode(int direction)
        {
            var d = demo.view.companion;
            d.value = (d.value + direction + d.options.Count) % d.options.Count;
            demo.SaveSettings(); RefreshMe();
            SleepetSceneSession.Instance?.MarkStateChanged();
        }
        public void RefreshSleep()
        {
            if (demo == null || demo.Store == null) return;
            var p = demo.Store.Preferences;
            if (sleepStartButton) sleepStartButton.gameObject.SetActive(!demo.Detector.Running);
            if (sleepAlarm) sleepAlarm.text = Format12(p.wakeTime);
            if (sleepMusic) sleepMusic.text = demo.view.sound.options[p.sound].text;
            if (sleepBehaviour) sleepBehaviour.text = SleepetDemo.BehaviourLabel(p.behaviour) + "  ▾";
        }
        public void RefreshMe()
        {
            if (demo == null || demo.Store == null) return;
            var p = demo.Store.Preferences;
            if (homeGreeting) homeGreeting.text = "Good evening, " + p.profileName + ".";
            if (homeReadyLabel) homeReadyLabel.text = "Ready to sleep with " + p.petName;
            if (homeARLabel) homeARLabel.text = "Meet " + p.petName + " in AR";
            if (homeTapHint) homeTapHint.text = "Tap " + p.petName + " to interact";
            if (meBedtime) meBedtime.text = Format12(p.reminderTime);
            if (profileEditor) profileEditor.Refresh();
            if (scheduleDial) scheduleDial.Refresh(p.reminderTime, p.wakeTime);
            if (meWake) meWake.text = Format12(p.wakeTime);
            if (meWinddown) meWinddown.text = p.windDownMinutes + "MIN";
            if (meWinddown && p.windDownSeconds > 0) meWinddown.text = p.windDownMinutes + "m " + p.windDownSeconds + "s";
            if (meSound) meSound.text = demo.view.sound.options[p.sound].text;
            if (meMode) meMode.text = p.companion.ToString();
            if (mePetName) mePetName.text = string.IsNullOrWhiteSpace(p.petName) ? "Mocha" : p.petName;
            if (meVolume) meVolume.SetValueWithoutNotify(p.volume);
            if (meVolumePercent) meVolumePercent.text = Mathf.RoundToInt(p.volume * 100f) + "%";
            if (petPreview && petOptions != null && petOptions.Length > 0) petPreview.sprite = petOptions[Mathf.Clamp(p.petOption, 0, petOptions.Length - 1)];
            if (homePet && petOptions != null && petOptions.Length > 0) homePet.sprite = petOptions[Mathf.Clamp(p.petOption, 0, petOptions.Length - 1)];
            if (sleepPet && petOptions != null && petOptions.Length > 0) sleepPet.sprite = p.petOption == 0 && restingPet != null ? restingPet : petOptions[Mathf.Clamp(p.petOption, 0, petOptions.Length - 1)];
            if (mePet && petOptions != null && petOptions.Length > 0) mePet.sprite = petOptions[Mathf.Clamp(p.petOption, 0, petOptions.Length - 1)];
            if (arPet && petOptions != null && petOptions.Length > 0) arPet.sprite = petOptions[Mathf.Clamp(p.petOption, 0, petOptions.Length - 1)];
            if (petOptions != null && petOptions.Length > 0)
            {
                var saved = PetSprite(p.petOption, p.petAppearance, p.petPose);
                foreach (var target in new[] { homePet, mePet, arPet }) if (target) target.sprite = saved;
                if (sleepPet) sleepPet.sprite = PetSprite(p.petOption, p.petAppearance, p.petOption == 0 && p.petAppearance == 0 ? 1 : p.petPose);
            }
        }
        static string Format12(string value)
        {
            if (!SleepetStore.TryTime(value, out var t)) return value;
            return DateTime.Today.Add(t).ToString("h:mmtt", CultureInfo.InvariantCulture);
        }
        static string FormatStage(int minutes)
        {
            return (minutes / 60).ToString(CultureInfo.InvariantCulture) + "h "
                + (minutes % 60).ToString("00", CultureInfo.InvariantCulture) + "m";
        }
        static string FormatAsleep(float seconds)
        {
            int minutes = Mathf.RoundToInt(seconds / 60f);
            return minutes / 60 + "h " + (minutes % 60).ToString("00", CultureInfo.InvariantCulture) + "m";
        }
        public void OpenTimeSheet()
        {
            var p = demo.Store.Preferences;
            if (timePicker) { timePicker.SetDraft(p.reminderTime, p.wakeTime, p.windDownMinutes, p.windDownSeconds); timeSheet.SetActive(true); return; }
            timeBedtimeInput.SetTextWithoutNotify(p.reminderTime);
            timeWakeInput.SetTextWithoutNotify(p.wakeTime);
            timeWindInput.SetTextWithoutNotify(p.windDownMinutes.ToString(CultureInfo.InvariantCulture));
            timeSheet.SetActive(true);
        }
        public void SaveTimeSheet()
        {
            string bed = timeBedtimeInput.text.Trim(), wake = timeWakeInput.text.Trim();
            int seconds = 0;
            if (timePicker)
            {
                if (!timePicker.TryRead(out bed, out wake, out int minutes, out seconds)) { ShowNotice("Use hours 1–12, minutes and seconds 00–59, and wind-down minutes 0–180."); return; }
                SaveSchedule(bed, wake, minutes, seconds); return;
            }
            if (!SleepetStore.TryTime(bed, out _) || !SleepetStore.TryTime(wake, out _) ||
                !int.TryParse(timeWindInput.text, out int wind) || wind < 0 || wind > 180)
            { ShowNotice("Use HH:mm for both times and 0–180 wind-down minutes."); return; }
            SaveSchedule(bed, wake, wind, seconds);
        }
        void SaveSchedule(string bed, string wake, int wind, int seconds)
        {
            var p = JsonUtility.FromJson<UserPreferences>(JsonUtility.ToJson(demo.Store.Preferences));
            p.reminderTime = bed; p.wakeTime = wake; p.windDownMinutes = wind; p.windDownSeconds = seconds;
            if (!demo.Store.SavePreferences(p)) { ShowNotice(demo.Store.Error); return; }
            demo.RestoreSettings(); RefreshMe(); RefreshSleep(); timeSheet.SetActive(false);
            SleepetSceneSession.Instance?.MarkStateChanged();
        }
        public void OpenPetSheet()
        {
            var p = demo.Store.Preferences;
            selectedPet = Mathf.Clamp(p.petOption, 0, petOptions.Length - 1);
            selectedAppearance = p.petAppearance; selectedPose = p.petPose;
            petNameInput.SetTextWithoutNotify(p.petName);
            RefreshPetDraft(); petSheet.SetActive(true);
        }
        public void NextPet()
        {
            SelectPet((selectedPet + 1) % petOptions.Length);
        }
        public void SelectPet(int option) { selectedPet = Mathf.Clamp(option, 0, petOptions.Length - 1); selectedAppearance = selectedPose = 0; RefreshPetDraft(); }
        public void ChangePetAppearance()
        {
            if (selectedPet != 0) { ShowNotice("This pet currently has one preset appearance."); return; }
            selectedAppearance = 1 - selectedAppearance; selectedPose = 0; RefreshPetDraft();
        }
        public void ChangePetPose()
        {
            if (selectedPet != 0 || selectedAppearance != 0) { ShowNotice("This appearance currently has one preset pose."); return; }
            selectedPose = 1 - selectedPose; RefreshPetDraft();
        }
        public void MorePets() { ShowNotice("Choose one of the three available preset pets. Custom pet import is not available in this version."); }
        Sprite PetSprite(int option, int appearance, int pose)
        {
            if (option == 0 && appearance == 1 && goldenPet) return goldenPet;
            if (option == 0 && pose == 1 && restingPet) return restingPet;
            return petOptions[Mathf.Clamp(option, 0, petOptions.Length - 1)];
        }
        void RefreshPetDraft()
        {
            petPreview.sprite = PetSprite(selectedPet, selectedAppearance, 0);
            if (petPosePreview) petPosePreview.sprite = PetSprite(selectedPet, selectedAppearance, selectedPose);
            if (petSelection != null) for (int i = 0; i < petSelection.Length; i++) petSelection[i].color = new Color(1, 1, 1, i == selectedPet ? .4f : .08f);
        }
        public void PreviewPet()
        {
            if (petAnimation != null) StopCoroutine(petAnimation);
            petAnimation = StartCoroutine(AnimatePetPreview());
        }
        IEnumerator AnimatePetPreview()
        {
            float elapsed = 0;
            while (elapsed < .8f && petSheet.activeSelf)
            {
                elapsed += Time.unscaledDeltaTime;
                (petPosePreview ? petPosePreview : petPreview).rectTransform.localScale = Vector3.one * (1 + .08f * Mathf.Sin(elapsed / .8f * Mathf.PI * 2));
                yield return null;
            }
            (petPosePreview ? petPosePreview : petPreview).rectTransform.localScale = Vector3.one;
        }
        public void SavePet()
        {
            var p = JsonUtility.FromJson<UserPreferences>(JsonUtility.ToJson(demo.Store.Preferences));
            p.petOption = selectedPet; p.petName = string.IsNullOrWhiteSpace(petNameInput.text) ? "Mocha" : petNameInput.text.Trim();
            p.petAppearance = selectedAppearance; p.petPose = selectedPose;
            if (!demo.Store.SavePreferences(p)) { ShowNotice(demo.Store.Error); return; }
            petSheet.SetActive(false); RefreshMe();
            SleepetSceneSession.Instance?.MarkStateChanged();
        }
        public void SaveMePreferences()
        {
            demo.view.volume.SetValueWithoutNotify(meVolume.value);
            demo.SaveSettings(); RefreshMe(); ShowNotice("Preferences saved on this device.");
            SleepetSceneSession.Instance?.MarkStateChanged();
        }
        public void ShowSupport(string title)
        {
            string detail;
            switch (title)
            {
                case "Privacy & data": detail = "Your profile, sleep sessions and plans are saved on this device. Test reports are labelled separately. No health data is uploaded."; break;
                case "Accessibility": detail = "Volume and sound can be adjusted in Preferences. Device display and accessibility options are managed in iPhone Settings."; break;
                case "Connected Devices": OpenHealthApp(); return;
                case "Notifications": detail = "Your bedtime and wake-up times are saved in My Plan. Background alarms and system notifications are not enabled in this version."; break;
                default: detail = "Set your sleep plan, choose a pet and start a session from Home. The moon opens weekly reports; tap a date for its daily report. TEST + adds a labelled sample week."; break;
            }
            ShowNotice(title + "\n\n" + detail);
        }
        public void ShowNotice(string message)
        {
            if (noticeText) noticeText.text = message;
            if (noticeSheet) noticeSheet.SetActive(true);
        }
        public void CloseNotice() { if (noticeSheet) noticeSheet.SetActive(false); }

        void LoadPlan()
        {
            try { plan = File.Exists(dataPath) ? JsonUtility.FromJson<TomorrowPlan>(File.ReadAllText(dataPath)) : null; }
            catch (Exception) { plan = null; }
            if (plan == null) plan = new TomorrowPlan();
            if (string.IsNullOrEmpty(plan.date)) plan.date = DateTime.Today.AddDays(1).ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        }
        void ApplyPlanToUI()
        {
            if (!routineWater || !routineDate || !routineEvents) return;
            applyingPlan = true;
            routineWater.SetIsOnWithoutNotify(plan.water); routineStretch.SetIsOnWithoutNotify(plan.stretch);
            routineBreakfast.SetIsOnWithoutNotify(plan.breakfast); routineGroceries.SetIsOnWithoutNotify(plan.groceries);
            routineGym.SetIsOnWithoutNotify(plan.gym); routineMeeting.SetIsOnWithoutNotify(plan.meeting);
            routineCall.SetIsOnWithoutNotify(plan.call);
            routineDate.text = plan.date;
            routineEvents.text = (plan.groceries ? "Go grocery shopping\n" : "") + (plan.meeting ? "Group meeting\n" : "") +
                (plan.gym ? "Gym day\n" : "") + (plan.call ? "Call family or a friend\n" : "") +
                (string.IsNullOrWhiteSpace(plan.shortEvent) ? "" : plan.shortEvent);
            if (string.IsNullOrWhiteSpace(routineEvents.text)) routineEvents.text = "No extra events yet";
            applyingPlan = false;
        }
        public void RoutineChanged(bool unused)
        {
            if (applyingPlan) return;
            plan.water = routineWater.isOn; plan.stretch = routineStretch.isOn; plan.breakfast = routineBreakfast.isOn;
            plan.groceries = routineGroceries.isOn; plan.gym = routineGym.isOn; plan.meeting = routineMeeting.isOn; plan.call = routineCall.isOn;
            ApplyPlanToUI(); routineNotice.text = "Changes not saved yet";
        }
        public void NextRoutineDate()
        {
            if (!DateTime.TryParseExact(plan.date, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) date = DateTime.Today;
            plan.date = date.AddDays(1).ToString("yyyy/MM/dd", CultureInfo.InvariantCulture); ApplyPlanToUI();
        }
        public void OpenEventSheet() { eventInput.SetTextWithoutNotify(plan.shortEvent); eventSheet.SetActive(true); }
        public void SaveEvent() { plan.shortEvent = eventInput.text.Trim(); eventSheet.SetActive(false); ApplyPlanToUI(); }
        public void ClearEvent() { plan.shortEvent = ""; ApplyPlanToUI(); }
        public void SaveRoutine()
        {
            RoutineChanged(false);
            try { Directory.CreateDirectory(Path.GetDirectoryName(dataPath)); File.WriteAllText(dataPath, JsonUtility.ToJson(plan, true)); routineNotice.text = "Saved for " + plan.date; SleepetSceneSession.Instance?.MarkStateChanged(); OpenHome(); }
            catch (Exception) { routineNotice.text = "Could not save on this device"; }
        }

        void RefreshReports()
        {
            if (demo == null || demo.Store == null) return;
            var all = demo.Store.History.records;
            DateTime date = scenePage == HighFiPage.DayDetail && SleepetSceneSession.Instance != null
                ? SleepetSceneSession.Instance.SelectedReportDate : DateTime.Today;
            SleepSummary latest = null;
            foreach (var record in all)
            {
                if (!DateTime.TryParse(record.startedAt, null, DateTimeStyles.RoundtripKind, out var recordDate) || recordDate.ToLocalTime().Date != date.Date) continue;
                if (latest == null || (latest.sample && !record.sample)) latest = record;
            }
            if (scenePage == HighFiPage.Daily)
            {
                latest = all.Find(r => !r.sample);
                if (latest != null && DateTime.TryParse(latest.startedAt, null, DateTimeStyles.RoundtripKind, out var parsed)) date = parsed.ToLocalTime();
            }
            if (dailyDate) dailyDate.text = date.ToString("dddd, MMMM d", CultureInfo.InvariantCulture).ToUpperInvariant();
            if (dailyDateCaption) dailyDateCaption.text = date.ToString("dddd, MMMM d", CultureInfo.InvariantCulture).ToUpperInvariant();
            if (dailyRelativeDate) dailyRelativeDate.text = date.Date == DateTime.Today ? "Last night" : "Selected night";
            bool hasStages = latest != null && latest.sample && latest.deepMinutes + latest.lightMinutes + latest.remMinutes > 0;
            if (dailyDuration) dailyDuration.text = latest == null ? "—" : FormatAsleep(latest.durationSeconds);
            int goalDifference = latest == null ? 0 : Mathf.RoundToInt(latest.durationSeconds / 60f) - 480;
            if (dailyGoal) dailyGoal.text = latest == null ? "No session recorded for this date"
                : latest.sample ? Math.Abs(goalDifference) + " min " + (goalDifference >= 0 ? "above" : "below") + " your 8-hour goal"
                : "Session recorded · " + latest.result + " feedback";
            if (dailyGoalBadge) dailyGoalBadge.text = latest == null ? "No Data" : goalDifference >= 0 ? "Goal Met" : "Below Goal";
            if (dailyStageNote)
            {
                dailyStageNote.text = hasStages ? "FIXED TEST DATA" : "Sleep stages require compatible sensor data.\nThis session measured app activity only.";
                var noteRect = (RectTransform)dailyStageNote.transform;
                noteRect.anchoredPosition = hasStages ? new Vector2(190, -174) : new Vector2(20, -55);
                noteRect.sizeDelta = hasStages ? new Vector2(145, 16) : new Vector2(313, 111);
            }
            if (dailyStageVisuals != null) foreach (var visual in dailyStageVisuals) if (visual) visual.SetActive(hasStages);
            if (dailyStageTotal) dailyStageTotal.text = hasStages ? FormatAsleep(latest.durationSeconds) : "";
            if (dailyStageDeep) dailyStageDeep.text = hasStages ? FormatStage(latest.deepMinutes) : "";
            if (dailyStageLight) dailyStageLight.text = hasStages ? FormatStage(latest.lightMinutes) : "";
            if (dailyStageRem) dailyStageRem.text = hasStages ? FormatStage(latest.remMinutes) : "";
            if (hasStages && dailyStageArcs != null && dailyStageArcs.Length == 3)
            {
                int stageTotalMinutes = latest.deepMinutes + latest.lightMinutes + latest.remMinutes;
                int[] lengths = { latest.deepMinutes, latest.lightMinutes, latest.remMinutes };
                float start = 0f;
                for (int i = 0; i < 3; i++)
                {
                    if (dailyStageArcs[i])
                    {
                        dailyStageArcs[i].fillAmount = Mathf.Max(0, lengths[i] / (float)stageTotalMinutes - 0.012f);
                        dailyStageArcs[i].rectTransform.localEulerAngles = new Vector3(0, 0, -start * 360f);
                    }
                    start += lengths[i] / (float)stageTotalMinutes;
                }
            }
            if (dailyStageTimeline) dailyStageTimeline.gameObject.SetActive(hasStages);
            if (dailyStart) dailyStart.text = latest != null && DateTime.TryParse(latest.startedAt, null, DateTimeStyles.RoundtripKind, out var startTime)
                ? startTime.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture).ToLowerInvariant() : "";
            if (dailyEnd) dailyEnd.text = latest != null && DateTime.TryParse(latest.startedAt, null, DateTimeStyles.RoundtripKind, out var endStart)
                ? endStart.ToLocalTime().AddSeconds(latest.durationSeconds).ToString("h:mm tt", CultureInfo.InvariantCulture).ToLowerInvariant() : "";
            if (dailyWatch) dailyWatch.text = WatchConnected ? "Device connected · open Apple Health" : "Connect to Apple Health for device details";
            var firstDay = DateTime.Today.AddDays(-6);
            if (weeklyRange) weeklyRange.text = firstDay.Month == DateTime.Today.Month
                ? firstDay.ToString("MMMM d", CultureInfo.InvariantCulture).ToUpperInvariant() + " — " + DateTime.Today.ToString("d, yyyy", CultureInfo.InvariantCulture)
                : firstDay.ToString("MMM d", CultureInfo.InvariantCulture).ToUpperInvariant() + " — " + DateTime.Today.ToString("MMM d, yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
            if (!weeklyCount || !weeklyAverage || !weeklyChart) return;
            int count = 0, bedGoals = 0, wakeGoals = 0, sampleCount = 0; float total = 0, startMinutes = 0;
            SleepetStore.TryTime(demo.Store.Preferences.reminderTime, out var bedtimeGoal);
            SleepetStore.TryTime(demo.Store.Preferences.wakeTime, out var wakeGoal);
            var bars = new string[7];
            for (int i = 0; i < 7; i++)
            {
                SleepSummary chosen = null;
                foreach (var record in all)
                    if (DateTime.TryParse(record.startedAt, null, DateTimeStyles.RoundtripKind, out var rd) && rd.ToLocalTime().Date == firstDay.AddDays(i))
                    { if (chosen == null || (chosen.sample && !record.sample)) chosen = record; }
                bool found = chosen != null;
                DateTime start = default, end = default;
                if (found)
                {
                    count++; total += chosen.durationSeconds;
                    start = DateTime.Parse(chosen.startedAt, null, DateTimeStyles.RoundtripKind).ToLocalTime();
                    end = start.AddSeconds(chosen.durationSeconds);
                    startMinutes += NightMinutes(start.TimeOfDay);
                    if (chosen.sample)
                    {
                        sampleCount++;
                        if (NightMinutes(start.TimeOfDay) <= NightMinutes(bedtimeGoal)) bedGoals++;
                        if (NightMinutes(end.TimeOfDay) <= NightMinutes(wakeGoal)) wakeGoals++;
                    }
                }
                bars[i] = found ? "●" : "○";
                if (weeklyDayButtons != null && i < weeklyDayButtons.Length && weeklyDayButtons[i] != null)
                {
                    var label = weeklyDayButtons[i].GetComponentInChildren<Text>();
                    if (label != null) label.text = firstDay.AddDays(i).ToString("ddd", CultureInfo.InvariantCulture).Substring(0, 1).ToUpperInvariant()
                        + " " + firstDay.AddDays(i).Day.ToString(CultureInfo.InvariantCulture);
                    var bar = weeklyDayButtons[i].transform.Find("Bar") as RectTransform;
                    if (bar != null)
                    {
                        float top = found ? NightPlotY(NightMinutes(start.TimeOfDay)) : 103;
                        float bottom = found ? NightPlotY(NightMinutes(end.TimeOfDay)) : 105;
                        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0, 1);
                        bar.anchoredPosition = new Vector2(7, -top);
                        bar.sizeDelta = new Vector2(22, Mathf.Max(2, bottom - top));
                        var barImage = bar.GetComponent<Image>();
                        barImage.color = !found ? new Color(0.84f, 0.91f, 0.93f)
                            : i == 6 ? new Color(0.004f, 0.11f, 0.28f) : new Color(0.0f, 0.54f, 0.74f);
                    }
                }
            }
            weeklyCount.text = sampleCount > 0 ? bedGoals.ToString() : "—";
            if (weeklyWakeCount) weeklyWakeCount.text = sampleCount > 0 ? wakeGoals.ToString() : "—";
            weeklyAverage.text = count > 0 ? DateTime.Today.AddMinutes(startMinutes / count % 1440).ToString("h:mm tt", CultureInfo.InvariantCulture).ToLowerInvariant() : "—";
            if (weeklyBedtimeLabel) weeklyBedtimeLabel.text = count > sampleCount ? "Average session start" : "Average bedtime";
            if (weeklyAverageSleep) weeklyAverageSleep.text = count > 0 ? FormatAsleep(total / count) + (count > sampleCount ? " average session" : " average sleep") : "No sessions this week";
            if (weeklyDataNote) weeklyDataNote.text = sampleCount > 0 ? "TEST DATA · goals use sample sleep times" : "Sleep goal counts need sensor data";
            if (weeklyAverageLine) { weeklyAverageLine.gameObject.SetActive(count > 0); weeklyAverageLine.anchoredPosition = new Vector2(63, -102 - NightPlotY(startMinutes / Mathf.Max(1, count))); }
            weeklyChart.text = weeklyDayButtons != null && weeklyDayButtons.Length == 7 ? "" : "M    T    W    T    F    S    S\n" + string.Join("    ", bars);
        }
        static float NightMinutes(TimeSpan time) { float m = (float)time.TotalMinutes; return m < 720 ? m + 1440 : m; }
        static float NightPlotY(float minute)
        {
            // Match the design's compressed overnight axis: 23:00, 00:00, 08:00, 09:00.
            if (minute <= 1440) return Mathf.Clamp((minute - 1380) / 60 * 35, 0, 35);
            if (minute <= 1920) return 35 + (minute - 1440) / 480 * 35;
            return Mathf.Clamp(70 + (minute - 1920) / 60 * 35, 70, 105);
        }
        public void OpenHealthApp()
        {
            if (HealthLauncher != null && HealthLauncher.TryOpen(HealthAppUrl)) return;
            ShowNotice("Apple Health direct link is unavailable on this device. Open Health from the Home Screen.");
        }
        public void OpenAR()
        {
            CloseSheets(); Show(HighFiPage.AR);
        }
        public void CloseAR()
        {
            if (placed)
            {
                placed = false; arChatOpen = false; arPet.gameObject.SetActive(false);
                arPlaceControls.SetActive(true); arChatControls.SetActive(false); RefreshAR(); return;
            }
            demo.CloseCamera();
            var target = SleepetSceneSession.Instance != null ? SleepetSceneSession.Instance.ReturnPage : HighFiPage.Home;
            Show(target);
        }
        void RefreshAR()
        {
            var c = demo.cameraCompanion;
            arPreview.texture = c.preview.texture;
            arPreview.gameObject.SetActive(c.preview.texture != null && c.IsLive);
            if (arRoom != null) arRoom.SetActive(!c.IsLive);
            string petName = string.IsNullOrWhiteSpace(demo.Store.Preferences.petName) ? "Mocha" : demo.Store.Preferences.petName;
            arStatus.text = placed ? petName + " is here!" + (c.IsTestMode ? "\n<size=14>Test background · no camera</size>" : "") : c.IsLive ? "Move your phone slowly to find a flat area" : c.IsTestMode ? "Test background · no camera" : c.status.text;
            arPlaceButton.GetComponentInChildren<Text>().text = "Place " + petName;
            arSendButton.interactable = !string.IsNullOrWhiteSpace(arInput.text) && !demo.ChatBusy;
            if (arChatOpen && demo.view.chatBubbles.Length > 0)
            {
                var parts = new List<string>();
                foreach (var t in demo.view.chatBubbles) if (t.transform.parent.gameObject.activeSelf) parts.Add(t.text);
                arChat.text = string.Join("\n\n", parts);
            }
        }
        public void PlaceOrChat()
        {
            if (!placed)
            {
                if (!demo.cameraCompanion.IsLive && !demo.cameraCompanion.IsTestMode) { ShowNotice("Camera unavailable. Use Test background to try placement."); return; }
                demo.cameraCompanion.PlacePet(); placed = true; arPet.gameObject.SetActive(true);
                arPlaceControls.SetActive(false); arChatControls.SetActive(true);
                RefreshAR();
            }
            else { arChatOpen = true; demo.OpenChat(); RefreshAR(); }
        }
        public void ARTestBackground() { demo.cameraCompanion.UseTestBackground(); RefreshAR(); }
        public void ARSwitchCamera() { demo.cameraCompanion.SwitchCamera(); RefreshAR(); }
        public void CycleARModel()
        {
            arModel = (arModel + 1) % arModelLabels.Length;
            arModelLabel.text = arModelLabels[arModel] + " ▾";
            if (arModel > 0) ShowNotice(arModelLabels[arModel] + " is not connected. Replies remain from the offline demo.");
        }
        public void SendARMessage()
        {
            if (string.IsNullOrWhiteSpace(arInput.text)) return;
            if (!arChatOpen) { arChatOpen = true; demo.OpenChat(); }
            demo.view.chatInput.SetTextWithoutNotify(arInput.text);
            demo.SendChat(); arInput.SetTextWithoutNotify(""); RefreshAR();
        }
    }

}
