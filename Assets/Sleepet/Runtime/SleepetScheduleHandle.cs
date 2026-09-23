using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sleepet
{
    public sealed class SleepetScheduleHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public SleepetScheduleDial dial;
        public bool bedtime;
        public GameObject tooltip;
        public ScrollRect scroll;
        bool dragging, scrollWasEnabled;
        public void OnBeginDrag(PointerEventData e)
        {
            dragging = true; scrollWasEnabled = scroll && scroll.enabled;
            if (scroll) scroll.enabled = false;
            tooltip.SetActive(true); OnDrag(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dial.rectTransform, e.position, e.pressEventCamera, out var point))
                dial.SetMinutes(bedtime, SleepetScheduleDial.MinutesAt(point - dial.rectTransform.rect.center), false);
        }
        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false; tooltip.SetActive(false);
            if (scroll) scroll.enabled = scrollWasEnabled;
            dial.Commit();
        }
        public void OnPointerClick(PointerEventData e) { if (!e.dragging) dial.app.OpenTimeSheet(); }
        void OnDisable()
        {
            if (!dragging) return;
            dragging = false; if (tooltip) tooltip.SetActive(false);
            if (scroll) scroll.enabled = scrollWasEnabled;
            if (dial && dial.app && dial.app.demo && dial.app.demo.Store != null) dial.Restore();
        }
    }
}
