using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Sleepet
{
    [Serializable] public sealed class UserPreferences
    {
        public bool reminderEnabled;
        public string reminderTime = "20:10";
        public string lastReminderDate = "";
        public int sound = 1;
        public float volume = 0.5f;
        public SleepBehaviour behaviour = SleepBehaviour.FadeOutGently;
        public CompanionMode companion = CompanionMode.Quiet;
        public string wakeTime = "09:52";
        public int windDownMinutes = 20;
        public int windDownSeconds;
        public string petName = "Mocha";
        public int petOption;
        public int petAppearance, petPose;
        public string profileName = "Sally";
        public int profileAvatar;
    }
    [Serializable] public sealed class SleepSummary
    {
        public string sessionId, startedAt, result, behaviour, companion, sound;
        public float durationSeconds;
        public bool sample;
        public int deepMinutes, lightMinutes, remMinutes;
    }
    [Serializable] public sealed class SleepHistory { public List<SleepSummary> records = new List<SleepSummary>(); }

    public sealed class SleepetStore
    {
        public string DirectoryPath { get; }
        public UserPreferences Preferences { get; private set; } = new UserPreferences();
        public SleepHistory History { get; private set; } = new SleepHistory();
        public string Error { get; private set; }
        public SleepetStore(string path)
        {
            DirectoryPath = path;
            try
            {
                if (File.Exists(Path.Combine(path, "settings.json")))
                    Preferences = JsonUtility.FromJson<UserPreferences>(File.ReadAllText(Path.Combine(path, "settings.json"))) ?? new UserPreferences();
                if (File.Exists(Path.Combine(path, "history.json")))
                    History = JsonUtility.FromJson<SleepHistory>(File.ReadAllText(Path.Combine(path, "history.json"))) ?? new SleepHistory();
                if (History.records == null) History.records = new List<SleepSummary>();
                Preferences.sound = Mathf.Clamp(Preferences.sound, 0, 2);
                Preferences.volume = Mathf.Clamp01(Preferences.volume);
                if (!TryTime(Preferences.reminderTime, out _)) Preferences.reminderTime = "20:10";
                if (!TryTime(Preferences.wakeTime, out _)) Preferences.wakeTime = "09:52";
                Preferences.windDownMinutes = Mathf.Clamp(Preferences.windDownMinutes, 0, 180);
                Preferences.windDownSeconds = Mathf.Clamp(Preferences.windDownSeconds, 0, 59);
                if (string.IsNullOrWhiteSpace(Preferences.petName)) Preferences.petName = "Mocha";
                if (!ValidProfileName(Preferences.profileName)) Preferences.profileName = "Sally";
                Preferences.profileAvatar = Mathf.Clamp(Preferences.profileAvatar, 0, 3);
                if (!Enum.IsDefined(typeof(SleepBehaviour), Preferences.behaviour)) Preferences.behaviour = SleepBehaviour.FadeOutGently;
                if (!Enum.IsDefined(typeof(CompanionMode), Preferences.companion)) Preferences.companion = CompanionMode.Quiet;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { Error = "Local data could not be read: " + e.Message; }
        }
        public static bool TryTime(string text, out TimeSpan value)
        {
            value = default;
            if (text == null || text.Length != 5 || text[2] != ':') return false;
            if (!int.TryParse(text.Substring(0, 2), out int h) || !int.TryParse(text.Substring(3, 2), out int m)
                || h < 0 || h > 23 || m < 0 || m > 59) return false;
            value = new TimeSpan(h, m, 0);
            return true;
        }
        public bool SavePreferences(UserPreferences value)
        {
            if (!ValidProfileName(value.profileName)) { Error = "Use 1–10 English letters for your name."; return false; }
            if (!TryTime(value.reminderTime, out _)) { Error = "Use a valid 24-hour time, such as 22:30."; return false; }
            if (!TryTime(value.wakeTime, out _)) { Error = "Use a valid wake time, such as 08:00."; return false; }
            if (!Write("settings.json", value)) return false;
            Preferences = value;
            return true;
        }
        public static bool ValidProfileName(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Length <= 10 &&
                value.All(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));
        }
        public void Add(SleepSummary summary)
        {
            History.records.RemoveAll(r => r.sessionId == summary.sessionId);
            History.records.Add(summary);
            History.records = History.records.OrderByDescending(r => r.startedAt, StringComparer.Ordinal).ToList();
            Write("history.json", History);
        }

        public bool AddFixedWeekSamples(DateTime today)
        {
            var next = new SleepHistory { records = History.records
                .Where(r => r.sessionId == null || !r.sessionId.StartsWith("fixed-week-", StringComparison.Ordinal)).ToList() };
            int[] minutes = { 440, 490, 465, 415, 480, 450, 495 };
            int[] deep = { 100, 110, 105, 90, 115, 100, 110 };
            int[] rem = { 110, 120, 115, 95, 125, 110, 120 };
            for (int i = 0; i < 7; i++)
            {
                var date = today.Date.AddDays(i - 6);
                next.records.Add(new SleepSummary
                {
                    sessionId = "fixed-week-" + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                    startedAt = date.AddHours(23).AddMinutes(50).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                    durationSeconds = minutes[i] * 60,
                    deepMinutes = deep[i], remMinutes = rem[i],
                    lightMinutes = minutes[i] - deep[i] - rem[i],
                    result = "Sample", behaviour = "Fixed week", companion = "Mocha", sound = "Sample",
                    sample = true
                });
            }
            next.records = next.records.OrderByDescending(r => r.startedAt, StringComparer.Ordinal).ToList();
            if (!Write("history.json", next)) return false;
            History = next;
            return true;
        }
        public void ImportExistingLogs(string path)
        {
            if (!System.IO.Directory.Exists(path)) return;
            bool changed = false;
            foreach (string file in System.IO.Directory.EnumerateFiles(path, "sleepet-*.json"))
            {
                try
                {
                    var log = JsonUtility.FromJson<SessionRecord>(File.ReadAllText(file));
                    if (log == null || log.events == null || History.records.Any(r => r.sessionId == log.sessionId)) continue;
                    var start = log.events.FirstOrDefault(e => e.eventType == "SLEEP_SESSION_STARTED");
                    var end = log.events.LastOrDefault(e => e.eventType == "SLEEP_SESSION_ENDED");
                    if (start == null || end == null) continue;
                    float.TryParse(end.value, NumberStyles.Float, CultureInfo.InvariantCulture, out float duration);
                    History.records.Add(new SleepSummary {
                        sessionId = log.sessionId, startedAt = start.timestamp, durationSeconds = duration,
                        result = log.events.LastOrDefault(e => e.eventType == "MORNING_RESULT_SHOWN")?.value ?? "Calm",
                        behaviour = log.events.LastOrDefault(e => e.eventType == "SLEEP_BEHAVIOUR_SELECTED")?.value ?? "",
                        companion = log.events.LastOrDefault(e => e.eventType == "COMPANION_MODE_SELECTED")?.value ?? "",
                        sound = "Previous test audio"
                    });
                    changed = true;
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
                { Error = "One unreadable session was skipped."; }
            }
            History.records = History.records.OrderByDescending(r => r.startedAt, StringComparer.Ordinal).ToList();
            if (changed) Write("history.json", History);
        }
        public bool CheckReminder(DateTime now)
        {
            if (!Preferences.reminderEnabled || !TryTime(Preferences.reminderTime, out var time)
                || now.Hour != time.Hours || now.Minute != time.Minutes) return false;
            string today = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (Preferences.lastReminderDate == today) return false;
            Preferences.lastReminderDate = today;
            Write("settings.json", Preferences);
            return true;
        }
        bool Write(string name, object value)
        {
            try
            {
                System.IO.Directory.CreateDirectory(DirectoryPath);
                string target = Path.Combine(DirectoryPath, name), temp = target + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(value, true));
                if (File.Exists(target)) File.Replace(temp, target, null);
                else File.Move(temp, target);
                Error = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Error = "Could not save local data: " + e.Message; return false; }
        }
    }
}
