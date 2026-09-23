using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sleepet
{
    public sealed class SleepetTimePicker : MonoBehaviour
    {
        public InputField bedHour, bedMinute, wakeHour, wakeMinute, windMinute, windSecond;
        public Image bedAM, bedPM, wakeAM, wakePM;
        bool bedIsPM, wakeIsPM;
        public void SetBedAM() { bedIsPM = false; Refresh(); }
        public void SetBedPM() { bedIsPM = true; Refresh(); }
        public void SetWakeAM() { wakeIsPM = false; Refresh(); }
        public void SetWakePM() { wakeIsPM = true; Refresh(); }
        public void SetDraft(string bed, string wake, int wind, int seconds)
        {
            SleepetStore.TryTime(bed, out var b); SleepetStore.TryTime(wake, out var w);
            bedHour.SetTextWithoutNotify((b.Hours % 12 == 0 ? 12 : b.Hours % 12).ToString("00"));
            wakeHour.SetTextWithoutNotify((w.Hours % 12 == 0 ? 12 : w.Hours % 12).ToString("00"));
            bedMinute.SetTextWithoutNotify(b.Minutes.ToString("00")); wakeMinute.SetTextWithoutNotify(w.Minutes.ToString("00"));
            windMinute.SetTextWithoutNotify(wind.ToString("00")); windSecond.SetTextWithoutNotify(seconds.ToString("00"));
            bedIsPM = b.Hours >= 12; wakeIsPM = w.Hours >= 12; Refresh();
        }
        void Refresh()
        {
            var selected = new Color(.61f, .80f, .91f); var normal = new Color(.86f, .94f, .97f);
            bedAM.color = bedIsPM ? normal : selected; bedPM.color = bedIsPM ? selected : normal;
            wakeAM.color = wakeIsPM ? normal : selected; wakePM.color = wakeIsPM ? selected : normal;
        }
        public bool TryRead(out string bed, out string wake, out int wind, out int seconds)
        {
            bed = wake = ""; wind = seconds = 0;
            if (!Read(bedHour, 1, 12, out int bh) || !Read(bedMinute, 0, 59, out int bm) ||
                !Read(wakeHour, 1, 12, out int wh) || !Read(wakeMinute, 0, 59, out int wm) ||
                !Read(windMinute, 0, 180, out wind) || !Read(windSecond, 0, 59, out seconds)) return false;
            bed = (bh % 12 + (bedIsPM ? 12 : 0)).ToString("00") + ":" + bm.ToString("00");
            wake = (wh % 12 + (wakeIsPM ? 12 : 0)).ToString("00") + ":" + wm.ToString("00");
            return true;
        }
        static bool Read(InputField field, int min, int max, out int value)
        { return int.TryParse(field.text, out value) && value >= min && value <= max; }
    }
}
