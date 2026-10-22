#if UNITY_EDITOR
using System;
using Ohabjizol.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class EndingBoard
    {
        public void BakeReferenceLayoutEditor(EndingKind kind)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Reference bake requires idle Edit Mode.");
            if (authoredRoot == null || (kind != EndingKind.Good && kind != EndingKind.Normal && kind != EndingKind.Bad))
                throw new InvalidOperationException("Authored ending UI and a supported ending kind required.");

            if (kind != EndingKind.Good)
            {
                bool normal = kind == EndingKind.Normal;
                var panel = normal ? normalPanel : badPanel;
                var subtitle = normal ? normalText : badText;
                var titleTransform = authoredRoot.Find("EndingTitle");
                var title = titleTransform == null ? null : titleTransform.GetComponent<Text>();
                if (panel == null || subtitle == null || title == null || endingStarsRoot == null)
                    throw new InvalidOperationException("Existing panel, title, subtitle and authored stars required.");
                var image = panel.GetComponent<Image>();
                if (image == null) throw new InvalidOperationException("Ending panel Image required.");
                ReferencePlace((RectTransform)panel.transform, Vector2.zero, Vector2.one);
                Undo.RecordObject(image, "Stretch reference ending art");
                image.preserveAspect = false;
                DirtySummary(image);
                ReferenceLabel(title, normal ? "NORMAL ENDING" : "BAD ENDING", 80,
                    normal ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
                ReferencePlace(title.rectTransform, normal ? new Vector2(.025f,.70f) : new Vector2(.15f,.565f),
                    normal ? new Vector2(.40f,.79f) : new Vector2(.43f,.665f));
                ReferenceLabel(subtitle, "프로젝트의 끝", 48, normal ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
                ReferenceSubtitle(subtitle.rectTransform, (RectTransform)panel.transform,
                    normal ? new Vector2(.025f,.625f) : new Vector2(.26f,.505f),
                    normal ? new Vector2(.40f,.705f) : new Vector2(.425f,.58f));
                if (totalStarsText != null)
                {
                    Undo.RecordObject(totalStarsText.gameObject, "Hide legacy ending total");
                    totalStarsText.gameObject.SetActive(false);
                    DirtySummary(totalStarsText.gameObject);
                }
                ReferencePlace(endingStarsRoot, normal ? new Vector2(.025f,.80f) : new Vector2(.04f,.69f),
                    normal ? new Vector2(.40f,.94f) : new Vector2(.43f,.81f));
                foreach (var glyph in endingStarsRoot.GetComponentsInChildren<Text>(true))
                {
                    Undo.RecordObject(glyph, "Style reference star glyph");
                    glyph.fontSize = 88;
                    glyph.resizeTextForBestFit = false;
                    glyph.color = Color.white;
                    DirtySummary(glyph);
                }
            }

            var rootTransform = authoredRoot.Find("ResultStats");
            RectTransform root;
            if (rootTransform == null)
            {
                root = CreateSummaryRect("ResultStats");
                ReferencePlace(root,
                    kind == EndingKind.Normal ? new Vector2(.80f,.235f) : kind == EndingKind.Good ? new Vector2(.825f,.20f) : new Vector2(.05f,.18f),
                    kind == EndingKind.Normal ? new Vector2(.95f,.50f) : kind == EndingKind.Good ? new Vector2(.97f,.465f) : new Vector2(.24f,.45f));
            }
            else root = rootTransform as RectTransform;
            if (root == null) throw new InvalidOperationException("ResultStats requires RectTransform.");
            var alignment = kind == EndingKind.Bad ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            ReferenceStat(root, "Header", "—— RESULT ——", .8f, 1f, alignment);
            var total = ReferenceStat(root, "Total", "TOTAL SCORE : 0", .55f, .75f, alignment);
            var director = ReferenceStat(root, "Director", "DIRECTOR : 0", .28f, .43f, alignment);
            var programmer = ReferenceStat(root, "Programmer", "PROGRAMMER : 0", .145f, .295f, alignment);
            var artist = ReferenceStat(root, "Artist", "ARTIST : 0", .01f, .16f, alignment);
            var view = GetComponent<EndingResultStatsView>();
            if (view == null) view = Undo.AddComponent<EndingResultStatsView>(gameObject);
            Undo.RecordObject(view, "Configure ending result stats");
            view.ConfigureEditor(this, total, director, programmer, artist);
            DirtySummary(view);
            DirtySummary(this);
        }

        public void BakeVisibleStarSlotsEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Star visibility authoring requires idle Edit Mode.");
            if (!useFixedKind || (fixedKind != EndingKind.Normal && fixedKind != EndingKind.Bad) || !HasSummaryUi)
                throw new InvalidOperationException("Only authored Normal and Bad ending stars may be changed.");
            Undo.RecordObject(endingStarsRoot.gameObject, "Show ending stars");
            endingStarsRoot.gameObject.SetActive(true);
            DirtySummary(endingStarsRoot.gameObject);
            foreach (var glyph in endingStarsRoot.GetComponentsInChildren<Text>(true))
            {
                Undo.RecordObject(glyph, "Show five ending star slots");
                glyph.color = Color.white;
                if (glyph.name == "Empty") glyph.enabled = true;
                DirtySummary(glyph);
            }
        }

        static Text ReferenceStat(RectTransform root, string name, string value, float bottom, float top, TextAnchor alignment)
        {
            var existing = root.Find(name);
            if (existing != null)
            {
                var label = existing.GetComponent<Text>();
                if (label == null) throw new InvalidOperationException("ResultStats/" + name + " requires Text.");
                return label;
            }
            var created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            Undo.RegisterCreatedObjectUndo(created, "Create ending result label");
            created.transform.SetParent(root, false);
            var text = created.GetComponent<Text>();
            ReferencePlace(text.rectTransform, new Vector2(0f,bottom), new Vector2(1f,top));
            ReferenceLabel(text, value, 30, alignment);
            text.raycastTarget = false;
            DirtySummary(text);
            return text;
        }

        static void ReferenceLabel(Text text, string value, int size, TextAnchor alignment)
        {
            Undo.RecordObject(text, "Style reference ending label");
            text.text = value;
            text.font = UiKit.Font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.resizeTextForBestFit = false;
            DirtySummary(text);
        }

        static void ReferencePlace(RectTransform rect, Vector2 min, Vector2 max)
        {
            Undo.RecordObject(rect, "Position reference ending UI");
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            DirtySummary(rect);
        }

        static void ReferenceSubtitle(RectTransform rect, RectTransform panel, Vector2 min, Vector2 max)
        {
            var parent = rect.parent as RectTransform;
            if (parent == null) throw new InvalidOperationException("Subtitle RectTransform parent required.");
            panel.ForceUpdateRectTransforms();
            parent.ForceUpdateRectTransforms();
            // Convert panel normalized coordinates without changing the existing parent.
            Vector3 lower = parent.InverseTransformPoint(panel.TransformPoint(panel.rect.min + Vector2.Scale(panel.rect.size, min)));
            Vector3 upper = parent.InverseTransformPoint(panel.TransformPoint(panel.rect.min + Vector2.Scale(panel.rect.size, max)));
            if (parent.rect.width == 0f || parent.rect.height == 0f)
                throw new InvalidOperationException("Subtitle parent has zero size.");
            ReferencePlace(rect,
                new Vector2((lower.x-parent.rect.xMin)/parent.rect.width, (lower.y-parent.rect.yMin)/parent.rect.height),
                new Vector2((upper.x-parent.rect.xMin)/parent.rect.width, (upper.y-parent.rect.yMin)/parent.rect.height));
        }
    }
}
#endif
