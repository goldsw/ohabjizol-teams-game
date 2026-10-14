using UnityEngine;
using UnityEngine.EventSystems;

namespace Ohabjizol.UI
{
    public sealed class WindowPointerFocus : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            WindowToolbar window = GetComponentInParent<WindowToolbar>();
            if (window != null)
                window.BringToFront();
        }
    }
}
