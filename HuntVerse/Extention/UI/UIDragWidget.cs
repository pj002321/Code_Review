using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hunt
{
    public class UIDragWidget : UIControlBase, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform target;
        [SerializeField] private bool clampToScreen = true;

        RectTransform rectTransform;
        Canvas canvas;
        RectTransform canvasRect;
        Vector2 originalPos;
        bool initialized = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            target ??= rectTransform;
            canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvasRect = canvas.GetComponent<RectTransform>();
            originalPos = target.anchoredPosition;
            initialized = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsActive) return;
            if (!initialized) Awake();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsActive || !initialized) return;
            if (canvas == null) return;

            Vector2 delta = eventData.delta / canvas.scaleFactor;
            target.anchoredPosition += delta;

            if (clampToScreen)
                ClampToScreen();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsActive) return;
        }

        private void ClampToScreen()
        {
            if (canvasRect == null) return;

            Rect canvasRect_rect = canvasRect.rect;
            Rect targetRect_rect = target.rect;

            Vector2 pos = target.anchoredPosition;

            float halfW = targetRect_rect.width * 0.5f;
            float halfH = targetRect_rect.height * 0.5f;

            float minX = canvasRect_rect.xMin + halfW;
            float maxX = canvasRect_rect.xMax - halfW;
            float minY = canvasRect_rect.yMin + halfH;
            float maxY = canvasRect_rect.yMax - halfH;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            target.anchoredPosition = pos;
        }

        public void ResetPosition()
        {
            if (!initialized) Awake();
            target.anchoredPosition = originalPos;
        }
    }
}