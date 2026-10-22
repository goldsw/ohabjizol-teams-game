using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    [DisallowMultipleComponent]
    public sealed class InterviewResponsePanel : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Image _background;
        [SerializeField] Text _text;
        string _presented;

        // Uses the panel that owns the response text; the legacy single-Text layout has no Background child.
        public static InterviewResponsePanel Ensure(Text response)
        {
            if (!response) return null;
            var panel = response.GetComponentInParent<InterviewResponsePanel>(true);
            if (!panel)
            {
                var root = response.transform.parent;
                var background = root ? root.Find("Background") : null;
                if (!background) return null;
                panel = root.gameObject.AddComponent<InterviewResponsePanel>();
                panel._background = background.GetComponent<Image>();
            }
            panel._text = response;
            if (panel._background) panel._background.raycastTarget = true;
            return panel;
        }

        // Visibility changes only when the answer changes, so a dismissed answer stays closed on re-render.
        public void Present(string response)
        {
            response = response ?? string.Empty;
            if (_text) _text.text = response;
            if (response == _presented) return;
            _presented = response;
            gameObject.SetActive(response.Length > 0);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) gameObject.SetActive(false);
        }
    }
}
