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
        /// <summary>Explicit editor authoring for the ending star summary.</summary>
        public void BakeSummaryUi()
        {
            if (UsesReferenceDesign) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Ending authoring requires idle Edit Mode.");
            if (!HasAuthoredUi)
                throw new InvalidOperationException("Existing ending UI required.");
            Undo.RecordObject(this, "Bake ending summary");
            foreach (var panel in new[] { goodPanel, normalPanel, badPanel })
            {
                PlaceSummaryRect((RectTransform)panel.transform, .035f, .59f, .635f, .98f);
                var image = panel.GetComponent<Image>();
                if (image != null) { Undo.RecordObject(image, "Fit ending art"); image.preserveAspect = true; DirtySummary(image); }
            }
            foreach (var text in new[] { goodText, normalText, badText })
            {
                PlaceSummaryRect(text.rectTransform, 1.02f, .25f, 1.57f, .75f);
                StyleSummaryText(text, 28);
            }
            var title = authoredRoot.Find("EndingTitle");
            if (title != null)
            {
                PlaceSummaryRect((RectTransform)title, .65f, .90f, .98f, .98f);
                var label = title.GetComponent<Text>();
                if (label != null) StyleSummaryText(label, 32);
            }
            if (endingStarsRoot == null)
            {
                var existing = authoredRoot.Find("EndingStarsRoot");
                endingStarsRoot = existing == null ? CreateSummaryRect("EndingStarsRoot") : (RectTransform)existing;
            }
            if (endingStarsRoot.parent != authoredRoot)
                throw new InvalidOperationException("EndingStarsRoot must be a direct authoredRoot child.");
            PlaceSummaryRect(endingStarsRoot, .67f, .61f, .97f, .68f);
            if (endingStars == null)
                endingStars = endingStarsRoot.GetComponent<TenPointStarsView>() ?? Undo.AddComponent<TenPointStarsView>(endingStarsRoot.gameObject);
            if (endingStars.gameObject != endingStarsRoot.gameObject)
                throw new InvalidOperationException("Stars view must belong to EndingStarsRoot.");
            var proxyTransform = authoredRoot.Find("EndingStarsPercentageProxy");
            var proxy = proxyTransform == null ? null : proxyTransform.GetComponent<Text>();
            if (proxyTransform != null && proxy == null)
                throw new InvalidOperationException("Ending star percentage proxy requires Text.");
            if (proxy == null) proxy = Undo.AddComponent<Text>(CreateSummaryRect("EndingStarsPercentageProxy").gameObject);
            StyleSummaryText(proxy, 52);
            if (endingStarsRoot.childCount == 0) endingStars.BakeAuthoredUi(endingStarsRoot, proxy, 52);
            else endingStars.CaptureAuthoredUi(endingStarsRoot, proxy);
            Undo.RecordObject(proxy.gameObject, "Hide percentage proxy");
            proxy.gameObject.SetActive(false);
            foreach (var glyph in endingStarsRoot.GetComponentsInChildren<Text>(true)) StyleSummaryText(glyph, 52);
            PlaceSummaryRect(totalStarsText.rectTransform, .65f, .535f, .98f, .605f);
            StyleSummaryText(totalStarsText, 24);
            PlaceSummaryRect(newRunButton.GetComponent<RectTransform>(), .75f, .025f, .975f, .085f);
            DirtySummary(proxy.gameObject);
            DirtySummary(endingStars);
            DirtySummary(this);
        }

        RectTransform CreateSummaryRect(string objectName)
        {
            var created = new GameObject(objectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(created, "Create ending summary UI");
            created.transform.SetParent(authoredRoot, false);
            return (RectTransform)created.transform;
        }

        static void PlaceSummaryRect(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            Undo.RecordObject(rect, "Position ending summary UI");
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            DirtySummary(rect);
        }

        static void StyleSummaryText(Text text, int size)
        {
            Undo.RecordObject(text, "Style ending summary text");
            text.font = UiKit.Font;
            text.fontSize = size;
            text.resizeTextForBestFit = false;
            DirtySummary(text);
        }

        static void DirtySummary(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
#endif
