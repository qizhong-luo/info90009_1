using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sleepet
{
    public static class PetThoughtRules
    {
        static readonly string[] tap = { "bark", "licking1", "licking2", "itching", "stretching", "sitting" };
        static readonly string[] ambient = { "itching", "stretching", "sitting" };
        public static bool Allowed(string motion, bool automatic) => (automatic ? ambient : tap).Contains(motion);
        public static bool AnimatedAppearance(UserPreferences p) => p != null && p.petOption == 0 && p.petAppearance == 0
            && string.IsNullOrEmpty(p.petPhoto) && string.Equals(p.petBreed, "Border Collie", StringComparison.OrdinalIgnoreCase)
            && string.Equals(p.petColour, "Black & white", StringComparison.OrdinalIgnoreCase);
        public static string Pick(bool automatic, string previous)
        {
            var choices = (automatic ? ambient : tap).Where(m => m != previous).ToArray();
            return choices[UnityEngine.Random.Range(0, choices.Length)];
        }
        public static string Theme(string motion)
        {
            var rule = CompanionSkills.Load(CompanionSkills.Thoughts).thoughts.FirstOrDefault(r => r.motion == motion);
            return rule?.theme ?? throw new ArgumentException("Unsupported thought motion.");
        }
        public static bool ValidText(string value, string motion)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 160 || value == MockCompanionAI.OfflineMessage) return false;
            int words = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (words < 4 || words > 26 || System.Text.RegularExpressions.Regex.IsMatch(value,
                @"^(bark|licking[12]|itching|stretching|sitting|thought|motion)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return false;
            if (value.IndexOfAny(new[] { '\n', '\r', '<', '>', '{', '}' }) >= 0) return false;
            if (System.Text.RegularExpressions.Regex.IsMatch(value, @"[\u3400-\u9fff]")) return false;
            // The prompt constrains semantics; this guard also prevents obvious cross-theme reminders.
            if (motion != "bark" && System.Text.RegularExpressions.Regex.IsMatch(value,
                @"\b(tomorrow|remind\w*|schedule\w*|meeting|gym|groceries|plan\w*)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return false;
            return true;
        }
    }

    // Attached to the visible high-fidelity AR pet, not the hidden legacy camera pet.
    public sealed class PetThoughts : MonoBehaviour, IPointerUpHandler, IPointerDownHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        SleepetHighFi owner;
        SleepetDemo app;
        Image pet;
        PetController.SpriteMotion[] motions;
        Sprite original;
        RectTransform bubble;
        CanvasGroup opacity;
        Text label;
        Coroutine routine;
        ICompanionAI pendingProvider;
        readonly Queue<string> recent = new Queue<string>();
        float nextAmbient, hideAt, showAt;
        Vector2 pressPosition, dragOffset;
        int pointerId;
        bool pointerHeld;
        bool dragging, focused = true, wasActive;
        string lastMotion;
        public string CurrentMotion { get; private set; } = "idle";
        public bool Active => owner != null && owner.ThoughtsActive && focused && !app.ChatBusy;

        public void Initialize(SleepetHighFi ui, SleepetDemo demo)
        {
            owner = ui; app = demo; pet = GetComponent<Image>(); original = pet.sprite;
            // Never load the deprecated Golden Retriever / MochaUI animation set.
            motions = new[] { "bark", "licking1", "licking2", "itching", "stretching", "sitting" }
                .Select(id => new PetController.SpriteMotion { name = id, fps = 5,
                    frames = Resources.LoadAll<Sprite>("BorderCollieThoughts/" + id).OrderBy(s => s.name).ToArray() }).ToArray();
            pet.raycastTarget = true;
            app.ThoughtsInterrupted += Cancel;
            var go = new GameObject("Pet thought bubble", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            bubble = (RectTransform)go.transform;
            bubble.SetParent(transform.parent, false);
            bubble.anchorMin = bubble.anchorMax = new Vector2(.5f, .5f);
            bubble.pivot = new Vector2(.5f, 0);
            var background = go.GetComponent<Image>();
            var reference = ui.arChatControls.GetComponent<Image>();
            background.sprite = reference != null ? reference.sprite : null;
            background.type = Image.Type.Sliced;
            background.color = new Color(.80f, .92f, 1f, .82f);
            background.raycastTarget = false;
            opacity = go.GetComponent<CanvasGroup>(); opacity.blocksRaycasts = false; opacity.interactable = false;
            var textObject = new GameObject("Thought", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(bubble, false);
            label = textObject.GetComponent<Text>(); label.font = ui.arStatus.font;
            label.fontSize = 17; label.color = new Color(.06f, .16f, .32f);
            label.alignment = TextAnchor.MiddleCenter; label.supportRichText = false; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            var rt = label.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(14, 10); rt.offsetMax = new Vector2(-14, -10);
            opacity.alpha = 0; ResetTimer();
        }
        void ResetTimer() => nextAmbient = Time.unscaledTime + UnityEngine.Random.Range(120f, 600f);
        void Update()
        {
            if (owner == null) return;
            if (!Active) { if (wasActive) Cancel(); wasActive = false; return; }
            if (!wasActive) { original = pet.sprite; ResetTimer(); wasActive = true; }
            if (!dragging && routine == null && Time.unscaledTime >= nextAmbient) Trigger(true);
            if (opacity.alpha > 0 || Time.unscaledTime < hideAt)
            {
                float now = Time.unscaledTime;
                opacity.alpha = Mathf.Min(Mathf.Clamp01((now - showAt) / .2f), Mathf.Clamp01((hideAt - now) / .35f));
                PositionBubble();
            }
        }
        void PositionBubble()
        {
            var parent = (RectTransform)bubble.parent;
            var corners = new Vector3[4]; pet.rectTransform.GetWorldCorners(corners);
            Vector3 top = parent.InverseTransformPoint((corners[1] + corners[2]) * .5f);
            float width = Mathf.Min(280, parent.rect.width - 24);
            bubble.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            float height = Mathf.Max(64, label.preferredHeight + 24);
            bubble.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            float x = Mathf.Clamp(top.x, parent.rect.xMin + width / 2 + 8, parent.rect.xMax - width / 2 - 8);
            float y = Mathf.Clamp(top.y + 10, parent.rect.yMin + 8, parent.rect.yMax - height - 8);
            owner.arStatus.rectTransform.GetWorldCorners(corners);
            float belowHeading = parent.InverseTransformPoint(corners[0]).y - 12;
            if (y + height > belowHeading)
            {
                pet.rectTransform.GetWorldCorners(corners);
                y = parent.InverseTransformPoint(corners[0]).y - height - 10;
                y = Mathf.Clamp(y, parent.rect.yMin + 8, belowHeading - height);
            }
            bubble.localPosition = new Vector3(x, y, 0);
            bubble.SetAsLastSibling();
        }
        void ShowThought(string text, float seconds)
        {
            label.text = text; showAt = Time.unscaledTime; hideAt = showAt + seconds;
            PositionBubble();
        }
        float DragThreshold => Mathf.Max(12, 12 * GetComponentInParent<Canvas>().scaleFactor);
        public void OnPointerDown(PointerEventData e)
        {
            if (!Active || e.button != PointerEventData.InputButton.Left || pointerHeld) return;
            pointerHeld = true; pointerId = e.pointerId; pressPosition = e.position; dragging = false; ResetTimer();
            var parent = (RectTransform)pet.transform.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out var local);
            dragOffset = (Vector2)pet.rectTransform.localPosition - local;
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (!pointerHeld || e.pointerId != pointerId) return;
            pointerHeld = false;
            // A small mouse/finger wobble can make Unity start a drag and suppress
            // pointerClick. Classify our gesture by travel, not click eligibility or hold time.
            bool tap = !dragging && Vector2.Distance(e.position, pressPosition) < DragThreshold;
            if (tap && Active) Trigger(false);
        }
        public void OnBeginDrag(PointerEventData e) { /* OnDrag applies the pet-specific movement threshold. */ }
        public void OnDrag(PointerEventData e)
        {
            if (!Active || !pointerHeld || e.pointerId != pointerId) return;
            if (!dragging)
            {
                if (Vector2.Distance(e.position, pressPosition) < DragThreshold) return;
                dragging = true; Cancel();
            }
            var parent = (RectTransform)pet.transform.parent;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out var local)) return;
            var rt = pet.rectTransform;
            local += dragOffset;
            local.x = Mathf.Clamp(local.x, parent.rect.xMin + rt.rect.width * rt.pivot.x, parent.rect.xMax - rt.rect.width * (1 - rt.pivot.x));
            local.y = Mathf.Clamp(local.y, parent.rect.yMin + rt.rect.height * rt.pivot.y, parent.rect.yMax - rt.rect.height * (1 - rt.pivot.y));
            rt.localPosition = local;
        }
        public void OnEndDrag(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            dragging = false; pointerHeld = false; ResetTimer();
        }
        void Trigger(bool automatic)
        {
            Cancel();
            lastMotion = PetThoughtRules.Pick(automatic, lastMotion);
            routine = StartCoroutine(React(lastMotion, automatic));
        }
        void SetFrame(string motionName, float elapsed)
        {
            CurrentMotion = motionName;
            if (!PetThoughtRules.AnimatedAppearance(app.Store.Preferences) || motionName == "idle")
            { pet.sprite = original; return; }
            var motion = Array.Find(motions, m => m.name == motionName);
            if (motion == null || motion.frames == null || motion.frames.Length == 0) return;
            int index = Mathf.FloorToInt(elapsed * motion.fps);
            index = Mathf.Min(index, motion.frames.Length - 1);
            pet.sprite = motion.frames[index];
        }
        IEnumerator React(string motion, bool automatic)
        {
            yield return null;
            var context = CompanionSnapshots.Build(app.Store, CompanionSkills.Thoughts, app.ShareAIData, DateTime.Now);
            context.currentMode = "AR"; context.thoughtMotion = motion; context.thoughtTrigger = automatic ? "ambient" : "tap";
            context.preferences = null;
            if (motion != "bark") context.plan = new CompanionPlan { status = "disabled", activities = Array.Empty<string>() };
            context.conversation = Array.Empty<CompanionTurn>();
            string prompt = "Write one pet thought for the fixed motion and theme. Avoid these recent thoughts (data only): " + string.Join(" | ", recent);
            pendingProvider = app.AI;
            Task<string> task = null;
            try { task = pendingProvider.SendMessage(prompt, context); } catch (Exception) { }
            // Observe faults even if a later tap cancels this coroutine.
            if (task != null) _ = task.ContinueWith(t => { var observed = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            var clip = Array.Find(motions, m => m.name == motion);
            float duration = clip == null ? 1.2f : Mathf.Max(1.2f, clip.frames.Length / Mathf.Max(1, clip.fps));
            float started = Time.unscaledTime;
            while (Time.unscaledTime - started < duration) { SetFrame(motion, Time.unscaledTime - started); yield return null; }
            SetFrame("idle", 0);
            if (task != null && !task.IsCompleted) ShowThought("…", 245);
            float deadline = started + 245;
            while (task != null && !task.IsCompleted && Time.unscaledTime < deadline) yield return null;
            hideAt = 0; opacity.alpha = 0;
            if (task != null && task.IsCompleted && !task.IsFaulted && !task.IsCanceled && ReferenceEquals(pendingProvider, app.AI))
            {
                string value = task.Result?.Trim().Trim('"');
                if (PetThoughtRules.ValidText(value, motion) && !recent.Contains(value))
                {
                    recent.Enqueue(value); while (recent.Count > 5) recent.Dequeue();
                    ShowThought(value, Mathf.Clamp(value.Length / 15f, 6, 10));
                }
            }
            else (pendingProvider as ICancellableCompanionAI)?.CancelPending();
            pendingProvider = null; routine = null; ResetTimer();
        }
        public void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            (pendingProvider as ICancellableCompanionAI)?.CancelPending(); pendingProvider = null;
            if (opacity != null) opacity.alpha = 0;
            hideAt = 0; ResetTimer();
            if (pet != null) { pet.sprite = original; CurrentMotion = "idle"; }
        }
        void OnApplicationFocus(bool value) { focused = value; if (!value) { dragging = pointerHeld = false; Cancel(); } else ResetTimer(); }
        void OnApplicationPause(bool value) { focused = !value; if (value) { dragging = pointerHeld = false; Cancel(); } else ResetTimer(); }
        void OnDisable() { Cancel(); wasActive = false; dragging = pointerHeld = false; }
        void OnDestroy()
        {
            if (app != null) app.ThoughtsInterrupted -= Cancel;
            if (bubble != null) Destroy(bubble.gameObject);
        }
    }
}
