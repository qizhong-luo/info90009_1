using UnityEngine;
using UnityEngine.EventSystems;

namespace Sleepet
{
    public sealed class DragPet : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        public RectTransform bounds;
        public CameraCompanion cameraCompanion;
        public void OnDrag(PointerEventData e)
        {
            GetComponent<PetController>()?.SetDragging(true);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, e.position, e.pressEventCamera, out var local)) return;
            var rt = (RectTransform)transform;
            float halfW = rt.rect.width * rt.localScale.x * 0.5f;
            float halfH = rt.rect.height * rt.localScale.y * 0.5f;
            // The parent has a top-left pivot, but the pet is anchored at its centre.
            local -= bounds.rect.center;
            float limitX = Mathf.Max(0, bounds.rect.width * 0.5f - halfW);
            float limitY = Mathf.Max(0, bounds.rect.height * 0.5f - halfH);
            rt.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -limitX, limitX), Mathf.Clamp(local.y, -limitY, limitY));
        }
        public void OnEndDrag(PointerEventData e)
        {
            GetComponent<PetController>()?.SetDragging(false);
            cameraCompanion.app?.Logger?.Log("PET_REPOSITIONED", "", "camera screen overlay");
        }
    }
}
