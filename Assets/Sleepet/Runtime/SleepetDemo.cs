using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Sleepet
{
    public sealed class SleepetDemo : MonoBehaviour
    {
        public DemoConfig config;
        public SleepetView view;
        public SleepMediaController media;
        public CameraCompanion cameraCompanion;
        [Tooltip("Leave blank for normal local storage. Tests use an isolated folder.")]
        public string dataDirectoryOverride;
        public SleepDetector Detector { get; private set; }
        public SleepMediaController Media => media;
        public EventLogger Logger { get; private set; }
        public SleepetStore Store { get; private set; }
        public ICompanionAI AI { get; set; } = new MockCompanionAI();
        public SleepBehaviour Behaviour { get; private set; }
        public CompanionMode Companion { get; private set; }
        public MorningResult Result { get; private set; }
        public string Page { get; private set; } = "Home";
        public PetController Pet => view.resultPanel.activeSelf ? view.resultPet : Page == "Sleep" ? view.sleepPet : view.homePet;
        public bool ChatBusy { get; private set; }
        public bool ChatOpen => view.chatPanel.activeSelf;
        public bool DebugOpen => view.debugPanel.activeSelf;
        public InputField ChatInput => view.chatInput;
        public HoldToEnd EndControl => view.holdToEnd;
        public string CompanionPrompt { get; private set; } = "";
        public readonly Dictionary<string, Button> Buttons = new Dictionary<string, Button>();
        readonly List<string> messages = new List<string>();
        bool finished, occasionalShown, proactiveShown, previewing;
        float elapsed, previewUntil;
        int chatVersion, historyPage;
        string startedAt, logDirectory;
        SleepSummary currentSummary;

        void Start()
        {
            Application.runInBackground = true;
            foreach (var button in view.GetComponentsInChildren<Button>(true)) Buttons[button.name] = button;
            string dataRoot = string.IsNullOrEmpty(dataDirectoryOverride)
                ? Path.Combine(Path.GetDirectoryName(EventLogger.DefaultDirectory), "SleepetData") : dataDirectoryOverride;
            logDirectory = string.IsNullOrEmpty(dataDirectoryOverride) ? EventLogger.DefaultDirectory : Path.Combine(dataRoot, "SessionLogs");
            Store = new SleepetStore(dataRoot);
            Store.ImportExistingLogs(logDirectory);
            Detector = new SleepDetector(config.lowAttentionDelay, config.likelyAsleepDelay);
            Detector.Changed += OnSleepState;
            media.MediaEvent += type => Logger?.Log(type, media.SoundName, "Unity-owned audio");
            view.holdToEnd.duration = config.endHoldDuration;
            view.holdToEnd.Completed = EndSleep;
            RestoreSettings();
            ApplyPreferences();
            CreateSession(true);
            view.chatPanel.SetActive(false); view.resultPanel.SetActive(false); view.debugPanel.SetActive(false);
            view.cameraPanel.SetActive(false); view.reminderPanel.SetActive(false);
            OpenHome();
            RefreshChat();
            if (view.coverPanel != null) view.coverPanel.SetActive(true);
        }
        void Update()
        {
            if (Detector == null) return;
            if ((Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
                || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)) Activity();
            if (Detector.Running)
            {
                elapsed += Time.unscaledDeltaTime;
                Detector.Tick(Time.unscaledDeltaTime);
                media.Advance(Time.unscaledDeltaTime);
            }
            if (previewing && Time.unscaledTime >= previewUntil) StopPreview();
            view.sleepPet.Rest(Detector.Running && Detector.State != SleepState.Awake);
            view.homePet.Rest(Detector.Running && Detector.State != SleepState.Awake);
            view.clock.text = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
            view.sessionBadge.text = Detector.Running ? "Sleep session running" : "A quiet moment, together";
            string audio = media.IsFading ? "Fading" : media.IsPlaying ? "Playing" : "Stopped";
            view.sleepStatus.text = Detector.Running ? StateLabel(Detector.State) + "\n" + Detector.InactiveSeconds.ToString("0") + "s without input  /  " + audio
                : "Ready when you are\nStart a quiet session with Mocha.";
            view.companionPrompt.text = CompanionPrompt;
            view.logStatus.text = Logger.LastError == null && Store.Error == null ? "Saved on this device" : "Local save needs attention";
            if (Store.CheckReminder(DateTime.Now))
            {
                Logger.Log(Detector.Running ? "BEDTIME_REMINDER_SUPPRESSED" : "BEDTIME_REMINDER_SHOWN", Store.Preferences.reminderTime);
                if (!Detector.Running) ShowReminder();
            }
        }
        void CreateSession(bool first = false)
        {
            Logger?.Save();
            Logger = new EventLogger(logDirectory);
            Logger.Log(first ? "APP_STARTED" : "DEMO_SESSION_STARTED");
            finished = false; currentSummary = null; elapsed = 0;
            Result = MorningResult.Calm; CompanionPrompt = "";
            messages.Clear(); chatVersion++; ChatBusy = false;
        }
        public void BeginNewSession(bool appStarted = false)
        {
            if (Detector.Running) return;
            CreateSession(appStarted); view.resultPanel.SetActive(false); OpenHome(); RefreshChat();
        }
        public void NewSession() => BeginNewSession();
        public void Activity() => Detector?.Activity();
        public void EnterApp()
        {
            if (view.coverPanel != null) view.coverPanel.SetActive(false);
            OpenHome();
        }
        public void PrepareToQuit()
        {
            StopPreview();
            if (Detector != null && Detector.Running) EndSleep();
            view.cameraPanel.SetActive(false);
            media.Stop();
            Logger?.Save();
        }
        public void QuitApp()
        {
            PrepareToQuit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        public void OpenHome() => Navigate("Home");
        public void OpenSleep() => Navigate("Sleep");
        public void OpenMe() => Navigate("Me");
        void Navigate(string page)
        {
            if (Store == null) return;
            Activity();
            if (page != "Me") StopPreview();
            view.cameraPanel.SetActive(false); view.chatPanel.SetActive(false);
            view.resultPanel.SetActive(false); view.debugPanel.SetActive(false);
            Page = page;
            view.homePage.SetActive(page == "Home"); view.sleepPage.SetActive(page == "Sleep"); view.mePage.SetActive(page == "Me");
            SelectTab(view.homeTab, page == "Home"); SelectTab(view.sleepTab, page == "Sleep"); SelectTab(view.meTab, page == "Me");
            if (page == "Me") { historyPage = 0; RefreshHistory(); }
            if (page == "Sleep") { RefreshSleep(); Logger.Log("SLEEP_MODE_OPENED"); }
        }
        void SelectTab(Button tab, bool active)
        {
            tab.GetComponent<Image>().color = active ? view.activeTab : view.inactiveTab;
            tab.GetComponentInChildren<Text>().color = active ? Color.white : new Color(0.2f, 0.29f, 0.26f);
        }
        public void TapPet() { Activity(); Pet.Tap(); Logger.Log("PET_TAPPED", "Mocha", Page); }
        public void OpenCamera()
        {
            Activity(); StopPreview();
            view.cameraPanel.SetActive(true); cameraCompanion.StartCamera();
        }
        public void CloseCamera() => view.cameraPanel.SetActive(false);
        public void StartSleep()
        {
            if (Detector.Running) return;
            StopPreview();
            if (finished) CreateSession();
            ApplyPreferences();
            startedAt = DateTime.UtcNow.ToString("O");
            Logger.Log("SLEEP_BEHAVIOUR_SELECTED", Behaviour.ToString(), "effective at start");
            Logger.Log("COMPANION_MODE_SELECTED", Companion.ToString(), "effective at start");
            Logger.Log("SLEEP_SOUND_SELECTED", media.SoundName);
            Logger.Log("SLEEP_SESSION_STARTED", "", "inactivity demo; not measured sleep");
            elapsed = 0; occasionalShown = proactiveShown = false;
            Detector.Start(); media.Play();
            Navigate("Sleep");
        }
        public void ResumeMedia() { Activity(); media.Play(); Logger.Log("MEDIA_RESUMED_BY_USER"); }
        public void SelectBehaviour(SleepBehaviour value)
        {
            Activity(); Behaviour = value; Logger.Log("SLEEP_BEHAVIOUR_SELECTED", value.ToString()); RefreshSleep();
        }
        public void SelectCompanion(CompanionMode value)
        {
            Activity(); Companion = value; CompanionPrompt = "";
            Logger.Log("COMPANION_MODE_SELECTED", value.ToString()); RefreshSleep();
        }
        void OnSleepState(SleepState state)
        {
            Logger.Log("SLEEP_STATE_CHANGED", state.ToString());
            if (state == SleepState.Awake) { media.Wake(); CompanionPrompt = ""; }
            else if (state == SleepState.LowAttention)
            {
                Logger.Log("LOW_ATTENTION_DETECTED", "", "simulated inactivity");
                if (Companion == CompanionMode.Occasional && !occasionalShown)
                { CompanionPrompt = "Mocha settles quietly beside you."; occasionalShown = true; }
                if (Companion == CompanionMode.Proactive && !proactiveShown)
                { CompanionPrompt = "A slow breath, or a little quiet? I'm here."; proactiveShown = true; }
                if (CompanionPrompt.Length > 0) Logger.Log("COMPANION_PROMPT_SHOWN", Companion.ToString());
            }
            else if (state == SleepState.LikelyAsleep)
            {
                Logger.Log("LIKELY_ASLEEP_DETECTED", "", "simulated inactivity");
                CompanionPrompt = ""; media.Apply(Behaviour, config.fadeDuration);
            }
        }
        public void EndSleep()
        {
            if (!Detector.Running) return;
            Detector.End(); media.Stop(); finished = true;
            Logger.Log("SLEEP_SESSION_ENDED", elapsed.ToString("F1", CultureInfo.InvariantCulture), "duration seconds");
            Logger.Log("MORNING_RESULT_SHOWN", Result.ToString(), "simulated");
            currentSummary = new SleepSummary { sessionId = Logger.Record.sessionId, startedAt = startedAt,
                durationSeconds = elapsed, result = Result.ToString(), behaviour = Behaviour.ToString(), companion = Companion.ToString(), sound = media.SoundName };
            Store.Add(currentSummary);
            RefreshSleep(); RefreshResult(); RefreshHistory();
            view.resultPanel.SetActive(true);
            if (Result == MorningResult.Calm) view.resultPet.Tap();
        }
        public void CloseResult()
        {
            view.resultPanel.SetActive(false); OpenMe();
            Canvas.ForceUpdateCanvases();
            view.mePage.GetComponent<ScrollRect>().verticalNormalizedPosition = 0;
        }
        void RefreshSleep()
        {
            view.startSleep.gameObject.SetActive(!Detector.Running);
            view.holdToEnd.gameObject.SetActive(Detector.Running);
            view.resumeMedia.gameObject.SetActive(Detector.Running);
            view.sleepSettingsSummary.text = media.SoundName + "  /  " + BehaviourLabel(Behaviour) + "\n" + Companion + " companionship";
        }
        void RefreshResult()
        {
            view.resultMessage.text = Result == MorningResult.Calm ? "We had a calm night together.\nIt's nice to share a quiet moment." : "That looked like a difficult night.\nWe can keep things quiet today.";
            view.resultSummary.text = FormatDuration(elapsed) + " session  /  " + media.SoundName + "\nSimulated feedback, not a sleep score.";
            view.resultPet.Rest(false);
        }
        public void SetResult(MorningResult value)
        {
            Result = value; Logger.Log("DEBUG_RESULT_SELECTED", value.ToString());
            if (finished && currentSummary != null)
            {
                currentSummary.result = value.ToString(); Store.Add(currentSummary);
                Logger.Log("MORNING_RESULT_SHOWN", value.ToString(), "debug preview");
                RefreshResult(); RefreshHistory();
            }
        }
        public void SetCalmResult() => SetResult(MorningResult.Calm);
        public void SetDifficultResult() => SetResult(MorningResult.Difficult);
        public void ForceState(SleepState state)
        {
            if (!Detector.Running) return;
            Logger.Log("DEBUG_STATE_OVERRIDE", state.ToString()); Detector.Force(state);
        }
        public void ForceAwake() => ForceState(SleepState.Awake);
        public void ForceLowAttention() => ForceState(SleepState.LowAttention);
        public void ForceLikelyAsleep() => ForceState(SleepState.LikelyAsleep);
        public void ToggleDebug() => view.debugPanel.SetActive(!view.debugPanel.activeSelf);
        public void CloseDebug() => view.debugPanel.SetActive(false);

        public void RestoreSettings()
        {
            var p = Store.Preferences;
            view.reminderEnabled.SetIsOnWithoutNotify(p.reminderEnabled); view.reminderTime.SetTextWithoutNotify(p.reminderTime);
            view.sound.SetValueWithoutNotify(p.sound); view.volume.SetValueWithoutNotify(p.volume);
            view.behaviour.SetValueWithoutNotify((int)p.behaviour); view.companion.SetValueWithoutNotify((int)p.companion);
            view.volumeLabel.text = Mathf.RoundToInt(p.volume * 100) + "%";
        }
        public void SaveSettings()
        {
            Activity();
            var p = new UserPreferences { reminderEnabled = view.reminderEnabled.isOn, reminderTime = view.reminderTime.text.Trim(),
                lastReminderDate = Store.Preferences.lastReminderDate, sound = view.sound.value, volume = view.volume.value,
                behaviour = (SleepBehaviour)view.behaviour.value, companion = (CompanionMode)view.companion.value,
                wakeTime = Store.Preferences.wakeTime, windDownMinutes = Store.Preferences.windDownMinutes,
                windDownSeconds = Store.Preferences.windDownSeconds,
                petName = Store.Preferences.petName, petOption = Store.Preferences.petOption,
                petAppearance = Store.Preferences.petAppearance, petPose = Store.Preferences.petPose,
                profileName = Store.Preferences.profileName, profileAvatar = Store.Preferences.profileAvatar };
            if (!Store.SavePreferences(p)) { view.settingsNotice.text = Store.Error; return; }
            ApplyPreferences();
            view.settingsNotice.text = p.reminderEnabled ? "Saved. Reminder at " + p.reminderTime + " while the app is open." : "Saved on this device. Reminder is off.";
            Logger.Log("PREFERENCES_SAVED", p.reminderTime, "local preferences");
            Logger.Log("SLEEP_BEHAVIOUR_SELECTED", Behaviour.ToString()); Logger.Log("COMPANION_MODE_SELECTED", Companion.ToString());
        }
        void ApplyPreferences()
        {
            var p = Store.Preferences;
            Behaviour = p.behaviour; Companion = p.companion;
            media.Configure(p.sound, p.volume); RefreshSleep();
        }
        public void OnVolumeChanged(float value)
        {
            view.volumeLabel.text = Mathf.RoundToInt(value * 100) + "%";
            if (previewing) media.Configure(view.sound.value, value);
        }
        public void OnSoundChanged(int value)
        { if (previewing) { media.Configure(value, view.volume.value); media.Play(); } }
        public void PreviewSound()
        {
            if (Detector.Running) { view.settingsNotice.text = "A sleep session is running. Save settings to change its sound."; return; }
            if (previewing) { StopPreview(); return; }
            media.Configure(view.sound.value, view.volume.value); media.Play();
            previewing = true; previewUntil = Time.unscaledTime + 15;
            view.previewLabel.text = "Stop preview";
        }
        void StopPreview()
        {
            if (!previewing) return;
            previewing = false; media.Stop();
            view.previewLabel.text = "Preview sound";
            if (Store != null) media.Configure(Store.Preferences.sound, Store.Preferences.volume);
        }
        public void HistorySourceChanged(bool samples) { historyPage = 0; RefreshHistory(); }
        public void PreviousHistory() { historyPage--; RefreshHistory(); }
        public void NextHistory() { historyPage++; RefreshHistory(); }
        public void RefreshHistory()
        {
            if (Store == null) return;
            var records = view.sampleHistory.isOn && view.sampleData != null
                ? JsonUtility.FromJson<SleepHistory>(view.sampleData.text).records : Store.History.records;
            int size = view.historyRows.Length, pages = Mathf.Max(1, Mathf.CeilToInt((float)records.Count / size));
            historyPage = Mathf.Clamp(historyPage, 0, pages - 1);
            view.historySummary.text = view.sampleHistory.isOn ? "SAMPLE DATA / " + records.Count + " example nights" : records.Count + " saved sessions / inactivity demo";
            view.emptyHistory.gameObject.SetActive(records.Count == 0);
            for (int i = 0; i < size; i++)
            {
                int index = historyPage * size + i;
                view.historyRows[i].transform.parent.gameObject.SetActive(index < records.Count);
                if (index >= records.Count) continue;
                var r = records[index];
                string date = DateTime.TryParse(r.startedAt, null, DateTimeStyles.RoundtripKind, out var dt) ? dt.ToLocalTime().ToString("dd MMM  HH:mm", CultureInfo.InvariantCulture) : r.startedAt;
                view.historyRows[i].text = date + "    " + FormatDuration(r.durationSeconds) + "\n" + (r.result == "Difficult" ? "A gentler day" : "A calm moment") + "  /  " + r.sound + (view.sampleHistory.isOn ? "  /  SAMPLE" : "  /  DEMO");
            }
            view.historyPagination.text = (historyPage + 1) + " / " + pages;
            view.previousHistory.interactable = historyPage > 0; view.nextHistory.interactable = historyPage < pages - 1;
        }
        public void ShowReminder()
        {
            view.reminderMessage.text = "A gentle reminder: it's your wind-down time.\nMocha can stay with you whenever you're ready.";
            view.reminderPanel.SetActive(true);
        }
        public void TestReminder() { Logger.Log("REMINDER_TEST_SHOWN"); ShowReminder(); }
        public void CloseReminder() => view.reminderPanel.SetActive(false);
        public void ReminderOpenSleep() { CloseReminder(); OpenSleep(); }

        public void OpenChat() { Activity(); view.chatPanel.SetActive(true); Logger.Log("AI_CHAT_OPENED", "MOCK"); RefreshChat(); }
        public void CloseChat() { view.chatPanel.SetActive(false); Logger.Log("AI_CHAT_CLOSED"); }
        public void ChatInputChanged(string text) { Activity(); RefreshChat(); }
        public void SendChat()
        {
            string input = view.chatInput.text.Trim();
            if (ChatBusy || input.Length == 0) return;
            Activity(); Logger.Log("AI_MESSAGE_SENT", input.Length.ToString(), "character count only");
            AddMessage("You: " + input); view.chatInput.SetTextWithoutNotify("");
            ChatBusy = true; RefreshChat(); StartCoroutine(Reply(input, chatVersion));
        }
        IEnumerator Reply(string input, int version)
        {
            yield return new WaitForSecondsRealtime(config.mockReplyDelay);
            var context = new CompanionContext { petName = "Mocha", currentMode = Page, companionMode = Companion, sleepState = Detector.State };
            System.Threading.Tasks.Task<string> task;
            try { task = AI.SendMessage(input, context); } catch (Exception) { task = null; }
            float deadline = Time.realtimeSinceStartup + 8;
            while (task != null && !task.IsCompleted && Time.realtimeSinceStartup < deadline)
            { if (version != chatVersion) yield break; yield return null; }
            if (version != chatVersion) yield break;
            bool failed = task == null || !task.IsCompleted || task.IsCanceled || task.IsFaulted;
            string response = failed ? new MockCompanionAI().SendMessage(input, context).Result : task.Result;
            if (failed) Logger.Log("AI_FALLBACK_USED", "MOCK");
            AddMessage("Mocha: " + response); ChatBusy = false;
            Logger.Log("AI_RESPONSE_RECEIVED", response.Length.ToString(), "MOCK"); RefreshChat();
        }
        void AddMessage(string text) { messages.Add(text); while (messages.Count > view.chatBubbles.Length) messages.RemoveAt(0); }
        void RefreshChat()
        {
            for (int i = 0; i < view.chatBubbles.Length; i++)
            {
                view.chatBubbles[i].transform.parent.gameObject.SetActive(i < messages.Count);
                view.chatBubbles[i].text = i < messages.Count ? messages[i] : "";
            }
            view.sendChat.interactable = !ChatBusy && !string.IsNullOrWhiteSpace(view.chatInput.text);
            view.chatStatus.text = ChatBusy ? "Mocha is replying..." : "Short, quiet replies / offline demo";
        }
        public static string FormatDuration(float seconds) => seconds >= 3600 ? (seconds / 3600).ToString("0.0") + " hr" : seconds >= 60 ? (seconds / 60).ToString("0.0") + " min" : seconds.ToString("0") + " sec";
        static string StateLabel(SleepState s) => s == SleepState.LowAttention ? "Settling down" : s == SleepState.LikelyAsleep ? "Likely asleep (simulated)" : "Awake";
        public static string BehaviourLabel(SleepBehaviour b) => b == SleepBehaviour.StopWhenAsleep ? "Stop when asleep" : b == SleepBehaviour.FadeOutGently ? "Fade out gently" : "Play all night";
        void OnApplicationPause(bool paused) { if (paused) Logger?.Save(); else Activity(); }
        void OnApplicationQuit() { PrepareToQuit(); }
    }
}
