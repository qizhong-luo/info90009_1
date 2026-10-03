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
        ICompanionAI ai = new MockCompanionAI();
        public ICompanionAI AI { get => ai; set { if (ReferenceEquals(ai, value)) return; (ai as IDisposable)?.Dispose(); ai = value; } }
        public SleepBehaviour Behaviour { get; private set; }
        public CompanionMode Companion { get; private set; }
        public MorningResult Result { get; private set; }
        public string Page { get; private set; } = "Home";
        public PetController Pet => view.resultPanel.activeSelf ? view.resultPet : Page == "Sleep" ? view.sleepPet : view.homePet;
        public bool ChatBusy { get; private set; }
        public string ChatSkill { get; private set; } = CompanionSkills.Daily;
        public bool ShareAIData { get; private set; } = true;
        public bool OnlineAI => AI is OpenAICompanion;
        public string ChatStatus { get; private set; } = "Offline LLM | Saved data on";
        public string ChatDraft { get; private set; } = "";
        public string ChatTranscript => string.Join("\n\n", messages);
        public bool CanRetryChat => !ChatBusy && !string.IsNullOrEmpty(retryInput);
        public int ChatReplyRevision { get; private set; }
        public bool ChatOpen => view.chatPanel.activeSelf;
        public bool DebugOpen => view.debugPanel.activeSelf;
        public InputField ChatInput => view.chatInput;
        public HoldToEnd EndControl => view.holdToEnd;
        public string CompanionPrompt { get; private set; } = "";
        public readonly Dictionary<string, Button> Buttons = new Dictionary<string, Button>();
        readonly List<string> messages = new List<string>();
        readonly List<CompanionTurn> conversation = new List<CompanionTurn>();
        readonly Dictionary<string, string> skillSnapshots = new Dictionary<string, string>();
        Coroutine chatRoutine;
        string pendingInput, retryInput;
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
            SetOnlineAI(false);
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
            CancelChat(); messages.Clear(); conversation.Clear(); retryInput = null; ChatDraft = "";
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
            CancelChat();
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
            CancelChat();
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
        public void CloseCamera() { CloseChat(); view.cameraPanel.SetActive(false); }
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

        public void OpenChat() { Activity(); view.chatPanel.SetActive(true); Logger.Log("AI_CHAT_OPENED", OnlineAI ? "OpenAI" : "Offline"); RefreshChat(); }
        public void CloseChat() { CancelChat(); view.chatPanel.SetActive(false); Logger?.Log("AI_CHAT_CLOSED"); }
        public void ChatInputChanged(string text) { Activity(); RefreshChat(); }
        public void SelectChatSkill(string id)
        {
            CompanionSkills.Load(id); // Validate the versioned preset before selection.
            if (ChatSkill == id) return;
            CancelChat(); ChatSkill = id; retryInput = null; conversation.Clear(); messages.Clear();
            ChatStatus = (OnlineAI ? "OpenAI" : "Offline LLM") + " | " + CompanionSkills.Label(id);
            RefreshChat();
        }
        public void SetAIDataSharing(bool enabled)
        {
            CancelChat(); ShareAIData = enabled; skillSnapshots.Clear();
            // Previous assistant messages may contain saved data; do not resend them after revocation.
            conversation.Clear(); messages.Clear(); retryInput = null;
            ChatStatus = enabled ? "Saved data ON | Local in Offline; sent to OpenAI when online" : "Saved data off | Conversation cleared";
            RefreshChat();
        }
        public void SetOnlineAI(bool online)
        {
            CancelChat();
            AI = online ? (ICompanionAI)new OpenAICompanion(CompanionAIConfig.Load(), Environment.GetEnvironmentVariable("SLEEPET_BACKEND_TOKEN")) : new LocalCompanionAI();
            conversation.Clear(); messages.Clear(); retryInput = null;
            ChatStatus = online ? "OpenAI selected | Sends chat to the configured backend" : "Offline LLM | Model loads on first message";
            RefreshChat();
        }
        public void SetChatDraft(string text) { ChatDraft = text ?? ""; }
        public void SendChat() { SendChatMessage(view.chatInput.text); }
        public bool SendChatMessage(string text)
        {
            string input = (text ?? "").Trim();
            if (ChatBusy || input.Length == 0) return false;
            if (input.Length > 2000) { ChatStatus = "Use 2000 characters or fewer."; RefreshChat(); return false; }
            ChatSkill = CompanionSkillRouter.Resolve(input, ChatSkill);
            Activity(); Logger.Log("AI_MESSAGE_SENT", input.Length.ToString(), "character count only");
            // Retrying the same failed turn must not duplicate its visible message.
            if (retryInput != input || messages.Count == 0 || messages[messages.Count - 1] != "You: " + input)
                AddMessage("You: " + input);
            view.chatInput.SetTextWithoutNotify("");
            ChatDraft = ""; pendingInput = input; retryInput = null;
            ChatBusy = true; ChatStatus = (OnlineAI ? "OpenAI" : "Offline") + " | " + CompanionSkills.Label(ChatSkill) + " | Replying...";
            RefreshChat(); chatRoutine = StartCoroutine(Reply(input, chatVersion)); return true;
        }
        public void RetryChat()
        {
            if (!CanRetryChat) return;
            var input = retryInput;
            SendChatMessage(input);
        }
        public void CancelChat()
        {
            chatVersion++;
            (AI as ICancellableCompanionAI)?.CancelPending();
            if (chatRoutine != null) { StopCoroutine(chatRoutine); chatRoutine = null; }
            if (ChatBusy && !string.IsNullOrEmpty(pendingInput))
            { ChatDraft = pendingInput; retryInput = pendingInput; ChatStatus = "Request cancelled | Message kept for retry"; }
            pendingInput = null; ChatBusy = false;
        }
        IEnumerator Reply(string input, int version)
        {
            // Yield once so the coroutine handle is assigned even for immediate providers.
            yield return null;
            if (AI is MockCompanionAI) yield return new WaitForSecondsRealtime(config.mockReplyDelay);
            var context = CompanionSnapshots.Build(Store, ChatSkill, ShareAIData && !(AI is MockCompanionAI), DateTime.Now);
            // Keep the visible transcript, but never send old model context after saved facts change.
            var comparable = context; comparable.capturedAt = null;
            string snapshot = JsonUtility.ToJson(comparable);
            if (skillSnapshots.TryGetValue(ChatSkill, out var previousSnapshot) && previousSnapshot != snapshot)
                conversation.Clear();
            skillSnapshots[ChatSkill] = snapshot;
            context.currentMode = view.cameraPanel.activeSelf ? "AR" : Page;
            context.companionMode = Companion; context.sleepState = Detector.State;
            context.conversation = conversation.ToArray();
            System.Threading.Tasks.Task<string> task;
            try { task = AI.SendMessage(input, context); } catch (Exception) { task = null; }
            float deadline = Time.realtimeSinceStartup + (OnlineAI ? Mathf.Clamp(CompanionAIConfig.Load().timeoutSeconds, 5, 90) + 2 : AI is LocalCompanionAI ? 245 : 8);
            while (task != null && !task.IsCompleted && Time.realtimeSinceStartup < deadline)
            { if (version != chatVersion) yield break; yield return null; }
            if (version != chatVersion) yield break;
            bool failed = task == null || !task.IsCompleted || task.IsCanceled || task.IsFaulted || string.IsNullOrWhiteSpace(task.Result);
            ChatBusy = false; chatRoutine = null; pendingInput = null;
            if (failed)
            {
                if (task != null && task.IsFaulted) { var observed = task.Exception; }
                (AI as ICancellableCompanionAI)?.CancelPending();
                retryInput = input; ChatDraft = input;
                view.chatInput.SetTextWithoutNotify(input);
                ChatStatus = ((AI as LocalCompanionAI)?.LastError ?? (AI as OpenAICompanion)?.LastError ?? "Connection failed.") + " Please retry.";
                Logger.Log("AI_REQUEST_FAILED", OnlineAI ? "OpenAI" : "Offline");
                RefreshChat(); yield break;
            }
            string response = task.Result;
            string petName = string.IsNullOrWhiteSpace(Store.Preferences.petName) ? "Mocha" : Store.Preferences.petName;
            AddMessage(petName + ": " + response);
            conversation.Add(new CompanionTurn { role = "user", content = input });
            conversation.Add(new CompanionTurn { role = "assistant", content = response });
            while (conversation.Count > 12) conversation.RemoveRange(0, 2);
            string source = (AI as OpenAICompanion)?.LastResponse?.source;
            if (!OnlineAI) source = (AI as LocalCompanionAI)?.Source ?? "Internet access required";
            ChatStatus = (OnlineAI ? "OpenAI" : "Offline LLM") + " | " + source;
            ChatReplyRevision++;
            Logger.Log("AI_RESPONSE_RECEIVED", response.Length.ToString(), OnlineAI ? "OpenAI" : "Offline"); RefreshChat();
        }
        void AddMessage(string text) { messages.Add(text); while (messages.Count > 24) messages.RemoveAt(0); }
        void RefreshChat()
        {
            int start = Mathf.Max(0, messages.Count - view.chatBubbles.Length);
            for (int i = 0; i < view.chatBubbles.Length; i++)
            {
                view.chatBubbles[i].supportRichText = false;
                view.chatBubbles[i].transform.parent.gameObject.SetActive(start + i < messages.Count);
                view.chatBubbles[i].text = start + i < messages.Count ? messages[start + i] : "";
            }
            view.sendChat.interactable = !ChatBusy && !string.IsNullOrWhiteSpace(view.chatInput.text);
            view.chatStatus.supportRichText = false;
            view.chatStatus.text = ChatStatus;
        }
        public static string FormatDuration(float seconds) => seconds >= 3600 ? (seconds / 3600).ToString("0.0") + " hr" : seconds >= 60 ? (seconds / 60).ToString("0.0") + " min" : seconds.ToString("0") + " sec";
        static string StateLabel(SleepState s) => s == SleepState.LowAttention ? "Settling down" : s == SleepState.LikelyAsleep ? "Likely asleep (simulated)" : "Awake";
        public static string BehaviourLabel(SleepBehaviour b) => b == SleepBehaviour.StopWhenAsleep ? "Stop when asleep" : b == SleepBehaviour.FadeOutGently ? "Fade out gently" : "Play all night";
        void OnApplicationPause(bool paused) { if (paused) { CancelChat(); Logger?.Save(); } else Activity(); }
        void OnDisable() { CancelChat(); }
        void OnDestroy() { (AI as IDisposable)?.Dispose(); }
        void OnApplicationQuit() { PrepareToQuit(); (AI as IDisposable)?.Dispose(); }
    }
}
