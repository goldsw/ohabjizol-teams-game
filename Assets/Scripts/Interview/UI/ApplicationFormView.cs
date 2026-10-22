using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    /// <summary>Personal fields over the authored application paper; skill stars stay in ApplicantStatsView.</summary>
    [ExecuteAlways]
    public sealed class ApplicationFormView : MonoBehaviour
    {
        [SerializeField] Image pageImage;
        [SerializeField] RectTransform paper;
        [SerializeField] Text personalInfo;
        [SerializeField] LayoutElement sizing;
        [SerializeField] DocumentScribbleGraphic career;
        [SerializeField] DocumentScribbleGraphic projects;
        [SerializeField] Image portrait;
        [SerializeField] ApplicantVisualCatalog visualCatalog;
        [SerializeField, Min(1f)] float maxPaperWidth = 540f;
        bool resizing;

        public bool HasAuthoredUi => pageImage && pageImage.sprite && paper && personalInfo &&
            sizing && career && projects && portrait && visualCatalog && pageImage.transform == paper &&
            paper.IsChildOf(transform) && personalInfo.transform.IsChildOf(paper) &&
            career.transform.IsChildOf(paper) && projects.transform.IsChildOf(paper) &&
            portrait.transform.IsChildOf(paper);

        public void SetPortrait(Sprite sprite)
        {
            if (!portrait) return;
            portrait.sprite = sprite;
            portrait.enabled = sprite != null;
        }

        public void Bind(ApplicantDocumentPacket packet)
        {
            if (personalInfo)
                personalInfo.text = packet == null ? string.Empty :
                    packet.DisplayName + "\n" + JobRoleText.Korean(packet.Role);
            Sprite sprite = packet != null && visualCatalog &&
                visualCatalog.TryGet(packet.CharacterId, out var visual) ? visual.Idle : null;
            SetPortrait(sprite);
            ResizePaper();
        }

        void OnEnable() => ResizePaper();
        void OnRectTransformDimensionsChange() => ResizePaper();

        void ResizePaper()
        {
            if (resizing || !paper || !pageImage || !pageImage.sprite || !sizing) return;
            var host = transform as RectTransform;
            if (!host) return;
            resizing = true;
            try
            {
                float width = Mathf.Min(maxPaperWidth, host.rect.width > 1f ? host.rect.width : maxPaperWidth);
                Rect spriteRect = pageImage.sprite.rect;
                float height = width * spriteRect.height / Mathf.Max(1f, spriteRect.width);
                if (!Mathf.Approximately(paper.rect.width, width))
                    paper.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                if (!Mathf.Approximately(paper.rect.height, height))
                    paper.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                if (!Mathf.Approximately(sizing.minHeight, height)) sizing.minHeight = height;
                if (!Mathf.Approximately(sizing.preferredHeight, height)) sizing.preferredHeight = height;
                if (!Mathf.Approximately(sizing.flexibleHeight, 0f)) sizing.flexibleHeight = 0f;
            }
            finally { resizing = false; }
        }

#if UNITY_EDITOR
        public void ConfigureEditor(Image page, RectTransform paperRect, Text personal,
            LayoutElement layout, DocumentScribbleGraphic careerInk, DocumentScribbleGraphic projectInk)
        {
            pageImage = page;
            paper = paperRect;
            personalInfo = personal;
            sizing = layout;
            career = careerInk;
            projects = projectInk;
            ResizePaper();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void ResizePaperEditor() => ResizePaper();

        public void ConfigurePortraitEditor(Image image, ApplicantVisualCatalog catalog)
        {
            portrait = image;
            visualCatalog = catalog;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
