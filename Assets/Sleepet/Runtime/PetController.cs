using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sleepet
{
    public sealed class PetController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Serializable] public sealed class SpriteMotion
        {
            public string name;
            public Sprite[] frames;
            public float fps = 10;
        }
        public RectTransform body;
        public Text face;
        public Text stateLabel;
        public Image spriteImage;
        public SpriteMotion[] motions = Array.Empty<SpriteMotion>();
        public string CurrentAnimation { get; private set; } = "Idle";
        public string CurrentMotion { get; private set; } = "idle";
        float reactionUntil, restStarted, pressedAt, motionStarted, nextAmbient;
        bool resting, pressed, stroked, dragging;
        int tapCount;
        string reaction = "bark";
        Vector3 authoredScale;

        void Awake() { authoredScale = body.localScale; }
        void OnEnable() { nextAmbient = Time.unscaledTime + 6; motionStarted = Time.unscaledTime; }
        void OnDisable()
        {
            pressed = stroked = dragging = false;
            reactionUntil = 0;
            body.localScale = authoredScale;
        }
        public void Tap()
        {
            // The Button's release click must not interrupt a long stroke.
            if (stroked) { stroked = false; return; }
            reaction = tapCount++ % 2 == 0 ? "bark" : "licking1";
            reactionUntil = Time.unscaledTime + 0.9f;
            motionStarted = Time.unscaledTime;
        }
        public void Rest(bool value)
        {
            if (resting == value) return;
            resting = value;
            if (value) restStarted = Time.unscaledTime;
            else { reaction = "stretching"; reactionUntil = Time.unscaledTime + 0.9f; }
        }
        public void SetDragging(bool value)
        {
            dragging = value;
            if (value) pressed = false;
            else { reaction = "bark"; reactionUntil = Time.unscaledTime + 0.9f; }
        }
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            pressed = true; stroked = false; pressedAt = Time.unscaledTime;
        }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        public void OnPointerExit(PointerEventData e) { pressed = false; }

        void Update()
        {
            float now = Time.unscaledTime;
            if (pressed && now - pressedAt >= 0.35f)
            {
                stroked = true; reaction = "licking2"; reactionUntil = now + 0.5f;
            }
            bool reacting = now < reactionUntil;
            CurrentAnimation = dragging ? "Walking" : reacting ? "Positive reaction" : resting ? "Rest" : "Idle";
            string desired = dragging ? "walk" : reacting ? reaction : resting
                ? (now - restStarted < 0.7f ? "lying-down" : "sleeping") : "idle";
            if (!dragging && !reacting && !resting && now >= nextAmbient)
            {
                desired = "itching";
                if (now >= nextAmbient + 0.8f) { nextAmbient = now + 8; desired = "idle"; }
            }
            if (desired != CurrentMotion) { CurrentMotion = desired; motionStarted = now; }
            var motion = Array.Find(motions, item => item.name == CurrentMotion);
            if (spriteImage != null && motion != null && motion.frames.Length > 0)
            {
                int frame = Mathf.FloorToInt((now - motionStarted) * motion.fps);
                if (CurrentMotion == "lying-down" || CurrentMotion == "stretching") frame = Mathf.Min(frame, motion.frames.Length - 1);
                else frame %= motion.frames.Length;
                spriteImage.sprite = motion.frames[frame];
            }
            float breath = resting && !reacting ? Mathf.Sin(now * 1.4f) * 0.018f : 0;
            body.localScale = Vector3.Scale(authoredScale, new Vector3(1, 1 + breath, 1));
            if (stateLabel != null) stateLabel.text = "Mocha - " + (dragging ? "Walking" : reacting ? "Happy" : resting ? "Resting" : "Here with you");
        }
    }
}
