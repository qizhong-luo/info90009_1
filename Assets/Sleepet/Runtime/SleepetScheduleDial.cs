using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sleepet
{
    // The arc is a native editable uGUI Graphic; handles are serialized scene objects.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SleepetScheduleDial : MaskableGraphic
    {
        public SleepetHighFi app;
        public RectTransform bedtimeHandle, wakeHandle;
        public Text bedtimeTooltip, wakeTooltip;
        public float radius = 128, thickness = 24;
        public Color startColor = new Color(0.012f, 0.271f, 0.475f);
        public Color endColor = new Color(0, 0.541f, 0.733f);
        [SerializeField] int bedMinutes = 1210, wakeMinutes = 592;

        public void Refresh(string bed, string wake)
        {
            if (SleepetStore.TryTime(bed, out var b)) bedMinutes = (int)b.TotalMinutes;
            if (SleepetStore.TryTime(wake, out var w)) wakeMinutes = (int)w.TotalMinutes;
            UpdateVisuals();
        }
        public static int MinutesAt(Vector2 local)
        { return Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg, 360) * 4) % 1440; }
        public void SetMinutes(bool bedtime, int minutes, bool save)
        {
            minutes = (minutes % 1440 + 1440) % 1440;
            if (bedtime) bedMinutes = minutes; else wakeMinutes = minutes;
            UpdateVisuals();
            if (!save) return;
            var p = JsonUtility.FromJson<UserPreferences>(JsonUtility.ToJson(app.demo.Store.Preferences));
            p.reminderTime = Time24(bedMinutes); p.wakeTime = Time24(wakeMinutes);
            if (!app.demo.Store.SavePreferences(p)) { app.ShowNotice(app.demo.Store.Error); app.RefreshMe(); return; }
            app.demo.RestoreSettings(); app.RefreshMe(); app.RefreshSleep();
            SleepetSceneSession.Instance?.MarkStateChanged();
        }
        public void Commit() { SetMinutes(true, bedMinutes, true); }
        public void Restore() { app.RefreshMe(); }
        static string Time24(int minutes) { return (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00"); }
        static string Time12(int minutes) { return DateTime.Today.AddMinutes(minutes).ToString("h:mmtt", System.Globalization.CultureInfo.InvariantCulture); }
        void UpdateVisuals()
        {
            if (bedtimeHandle) bedtimeHandle.anchoredPosition = Point(bedMinutes / 4f) * radius;
            if (wakeHandle) wakeHandle.anchoredPosition = Point(wakeMinutes / 4f) * radius;
            if (bedtimeTooltip) bedtimeTooltip.text = Time12(bedMinutes);
            if (wakeTooltip) wakeTooltip.text = Time12(wakeMinutes);
            if (app && app.meBedtime) app.meBedtime.text = Time12(bedMinutes);
            if (app && app.meWake) app.meWake.text = Time12(wakeMinutes);
            SetVerticesDirty();
        }
        static Vector2 Point(float degree) { float r = degree * Mathf.Deg2Rad; return new Vector2(Mathf.Sin(r), Mathf.Cos(r)); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float sweep = Mathf.Repeat(wakeMinutes - bedMinutes, 1440) / 4f;
            int segments = Mathf.Max(1, Mathf.CeilToInt(sweep / 2));
            Vector2 center = rectTransform.rect.center;
            for (int i = 0; i <= segments; i++)
            {
                float f = i / (float)segments;
                var direction = Point(bedMinutes / 4f + sweep * f);
                Color tint = Color.Lerp(startColor, endColor, f);
                vh.AddVert(center + direction * (radius - thickness / 2), tint, Vector2.zero);
                vh.AddVert(center + direction * (radius + thickness / 2), tint, Vector2.zero);
                if (i == 0) continue;
                int v = i * 2; vh.AddTriangle(v - 2, v, v - 1); vh.AddTriangle(v - 1, v, v + 1);
            }
        }
    }
}
