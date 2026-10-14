using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class DocumentTaskbarItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Text label;
        Action activate;
        public DocumentKind Kind { get; private set; }

        public void ConfigureEditor(Text authoredLabel) { label = authoredLabel; }

        public void Bind(DocumentKind kind, Action open)
        {
            Kind = kind;
            activate = open;
            if (label != null)
                label.text = (kind == DocumentKind.Application ? "지원서" : "포트폴리오") + ".pdf";
        }

        public void Bind(DownloadedDocument document, Action open)
        {
            Kind = document.Key.Kind;
            activate = open;
            if (label != null) label.text = document.DisplayName;
            var layout = GetComponent<LayoutElement>();
            if (layout == null) layout = gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 168f;
            layout.minHeight = 52f;
            layout.preferredHeight = 52f;
            layout.flexibleHeight = 0f;
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
                activate?.Invoke();
        }

        public void Unbind() { activate = null; }
        void OnDestroy() { Unbind(); }
    }
}