using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sleepet
{
    public sealed class HoldToEnd : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float duration = 1.5f;
        public Text label;
        public Action Completed;
        bool holding;
        float elapsed;
        int activePointer;

        public void OnPointerDown(PointerEventData e)
        {
            if (holding || e.button != PointerEventData.InputButton.Left) return;
            holding = true;
            activePointer = e.pointerId;
            elapsed = 0;
        }
        public void OnPointerUp(PointerEventData e) { if (e.pointerId == activePointer) Cancel(); }
        public void OnPointerExit(PointerEventData e) { if (e.pointerId == activePointer) Cancel(); }
        void OnDisable() { Cancel(); }
        void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
        void OnApplicationPause(bool paused) { if (paused) Cancel(); }
        void Cancel()
        {
            holding = false;
            elapsed = 0;
            if (label != null) label.text = "Hold to end sleep";
        }
        void Update()
        {
            if (!holding) return;
            elapsed += Time.unscaledDeltaTime;
            label.text = "Keep holding... " + Mathf.RoundToInt(100 * Mathf.Clamp01(elapsed / duration)) + "%";
            if (elapsed < duration) return;
            Cancel();
            Completed?.Invoke();
        }
    }
}
