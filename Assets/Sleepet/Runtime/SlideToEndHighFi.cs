using UnityEngine;
using UnityEngine.EventSystems;

namespace Sleepet
{
    public sealed class SlideToEndHighFi : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public SleepetHighFi app;
        public RectTransform handle;
        float startX;

        public void OnBeginDrag(PointerEventData eventData) { startX = eventData.position.x; }
        public void OnDrag(PointerEventData eventData)
        {
            if (handle == null) return;
            handle.anchoredPosition = new Vector2(Mathf.Clamp(eventData.position.x - startX, 0, 214), handle.anchoredPosition.y);
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            if (handle != null) handle.anchoredPosition = new Vector2(0, handle.anchoredPosition.y);
            if (eventData.position.x - startX >= 180) app.EndSleepBySlide();
        }
    }
}
