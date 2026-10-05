using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Sleepet
{
    [Serializable] public sealed class CompanionSkill
    {
        public string id, label, version, instructions;
        public string[] tools;
        public CompanionThoughtRule[] thoughts;
    }
    [Serializable] public sealed class CompanionThoughtRule { public string motion, theme, example; }

    public static class CompanionSkills
    {
        public const string Daily = "daily_companionship", Records = "record_review", Tomorrow = "tomorrow_preparation";
        public const string Thoughts = "thoughts";
        public static readonly string[] Ids = { Daily, Records, Tomorrow, Thoughts };
        public static string Label(string id) => id == Thoughts ? "Thoughts" : id == Records ? "Record review" : id == Tomorrow ? "Tomorrow preparation" : "Daily companionship";
        public static CompanionSkill Load(string id)
        {
            if (!Ids.Contains(id)) throw new ArgumentException("Unknown companion skill.");
            var asset = Resources.Load<TextAsset>("CompanionSkills/" + id);
            if (asset == null) throw new InvalidOperationException("Missing companion skill: " + id);
            return JsonUtility.FromJson<CompanionSkill>(asset.text);
        }
    }

    [Serializable] public sealed class CompanionTurn { public string role, content; }
    [Serializable] public sealed class CompanionPreferences
    {
        public string wakeTime, reminderTime, companion;
        public bool reminderEnabled;
        public int windDownMinutes;
    }
    [Serializable] public sealed class CompanionRecord
    {
        public string date, source;
        public float durationSeconds;
        public bool sample;
    }
    [Serializable] public sealed class CompanionPlan
    {
        public string date, status;
        public string[] activities;
    }

    public static class CompanionSnapshots
    {
        public static CompanionContext Build(SleepetStore store, string skill, bool share, DateTime now)
        {
            var context = new CompanionContext {
                petName = share ? Limit(store.Preferences.petName, 40) : "Mocha",
                skillId = skill, today = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                capturedAt = new DateTimeOffset(now).ToString("O"), shareData = share,
                records = Array.Empty<CompanionRecord>(),
                plan = new CompanionPlan { status = share ? "missing" : "disabled", activities = Array.Empty<string>() }
            };
            if (!share) return context;
            var p = store.Preferences;
            if (skill != CompanionSkills.Records)
                context.preferences = new CompanionPreferences { wakeTime = p.wakeTime, reminderTime = p.reminderTime,
                    reminderEnabled = p.reminderEnabled, windDownMinutes = p.windDownMinutes, companion = p.companion.ToString() };
            if (skill == CompanionSkills.Records)
            {
                var records = new List<CompanionRecord>();
                foreach (var r in store.History.records)
                {
                    if (r == null || !DateTime.TryParse(r.startedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)) continue;
                    var date = at.ToLocalTime().Date;
                    if (date < now.Date.AddDays(-6) || date > now.Date || float.IsNaN(r.durationSeconds) || float.IsInfinity(r.durationSeconds)) continue;
                    records.Add(new CompanionRecord { date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        durationSeconds = Mathf.Clamp(r.durationSeconds, 0, 86400), sample = r.sample,
                        source = r.sample ? "sample" : "app_session_not_measured_sleep" });
                }
                context.records = records.OrderByDescending(r => r.date).Take(30).ToArray();
            }
            if (skill == CompanionSkills.Tomorrow || skill == CompanionSkills.Thoughts)
            {
                string path = Path.Combine(store.DirectoryPath, "tomorrow-plan.json");
                try
                {
                    if (!File.Exists(path)) return context;
                    if (new FileInfo(path).Length > 16384) { context.plan.status = "unavailable"; return context; }
                    var plan = JsonUtility.FromJson<TomorrowPlan>(File.ReadAllText(path));
                    if (plan == null || !DateTime.TryParseExact(plan.date, new[] { "yyyy/MM/dd", "yyyy-MM-dd" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) { context.plan.status = "unavailable"; return context; }
                    context.plan.date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    if (date.Date != now.Date.AddDays(1)) { context.plan.status = "outdated"; return context; }
                    context.plan.status = "available";
                    var activities = new List<string>();
                    if (plan.water) activities.Add("Drink water");
                    if (plan.stretch) activities.Add("Stretch");
                    if (plan.breakfast) activities.Add("Have breakfast");
                    if (plan.groceries) activities.Add("Go grocery shopping");
                    if (plan.gym) activities.Add("Go to the gym");
                    if (plan.meeting) activities.Add("Group meeting");
                    if (plan.call) activities.Add("Call family or a friend");
                    plan.MigrateEvents();
                    foreach (var item in plan.events) if (item.selected) activities.Add(Limit(item.title, 300));
                    context.plan.activities = activities.ToArray();
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
                { context.plan.status = "unavailable"; }
            }
            return context;
        }

        public static string Limit(string value, int length) => string.IsNullOrWhiteSpace(value) ? "" : value.Substring(0, Math.Min(value.Length, length));
    }
}
