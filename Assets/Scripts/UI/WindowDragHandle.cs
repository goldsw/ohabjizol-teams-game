using UnityEngine;
using UnityEngine.EventSystems;

namespace Ohabjizol.UI
{
    public sealed class WindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        WindowToolbar _window;
        Vector2 _grabLocal;
        Vector2 _grabAnchored;
        bool _armed;

        public void Attach(WindowToolbar window)
        {
            _window = window;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _armed = false;
            if (_window == null || _window.Root == null)
                return;

            RectTransform parent = _window.Root.parent as RectTransform;
            if (parent == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, eventData.position, eventData.pressEventCamera, out _grabLocal))
                return;

            _grabAnchored = _window.Root.anchoredPosition;
            _armed = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_armed || _window == null || _window.Root == null)
                return;

            RectTransform parent = _window.Root.parent as RectTransform;
            if (parent == null)
                return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, eventData.position, eventData.pressEventCamera, out local))
                return;

            _window.Root.anchoredPosition = _grabAnchored + (local - _grabLocal);
            ClampToolbar(parent, _window.Root, _window.Toolbar);
        }

        static void ClampToolbar(RectTransform parent, RectTransform window, RectTransform toolbar)
        {
            if (parent.rect.width < 1f || parent.rect.height < 1f || toolbar == null)
                return;

            Vector3[] corners = new Vector3[4];
            toolbar.GetWorldCorners(corners);
            Vector2 min = parent.InverseTransformPoint(corners[0]);
            Vector2 max = min;
            for (int i = 1; i < 4; i++)
            {
                Vector2 point = parent.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            Rect area = parent.rect;
            Vector2 shift = Vector2.zero;
            if (min.y < area.yMin)
                shift.y += area.yMin - min.y;
            if (max.y + shift.y > area.yMax)
                shift.y -= max.y + shift.y - area.yMax;

            float span = max.x - min.x;
            float keep = span < WindowToolbar.ToolbarHeight ? span : WindowToolbar.ToolbarHeight;
            if (max.x + shift.x < area.xMin + keep)
                shift.x += area.xMin + keep - (max.x + shift.x);
            if (min.x + shift.x > area.xMax - keep)
                shift.x -= min.x + shift.x - (area.xMax - keep);

            if (shift != Vector2.zero)
                window.anchoredPosition += shift;
        }
    }
}
