using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public static class InterviewSpeechBubbleBake
    {
        public static void Apply(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var view = root.GetComponentInChildren<InterviewCallView>(true);
                if (!view) throw new InvalidOperationException("InterviewCallView missing: " + prefabPath);
                var serialized = new SerializedObject(view);
                var portrait = serialized.FindProperty("_portrait").objectReferenceValue as Image;
                var dots = serialized.FindProperty("_dots").objectReferenceValue as Text;
                var bubble = serialized.FindProperty("speechBubble").objectReferenceValue as Image;
                if (!portrait || !portrait.rectTransform.parent)
                    throw new InvalidOperationException("Applicant portrait missing: " + prefabPath);
                var portraitRect = portrait.rectTransform;
                var parent = portraitRect.parent;
                if (!bubble)
                {
                    var existing = parent.Find("InterviewSpeechBubble");
                    if (existing) bubble = existing.GetComponent<Image>();
                    if (!bubble)
                    {
                        var created = new GameObject("InterviewSpeechBubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        created.transform.SetParent(parent, false);
                        bubble = created.GetComponent<Image>();
                    }
                }
                var bubbleRect = bubble.rectTransform;
                bubbleRect.SetParent(parent, false);
                bubbleRect.anchorMin = portraitRect.anchorMin;
                bubbleRect.anchorMax = portraitRect.anchorMax;
                bubbleRect.pivot = new Vector2(0f, .5f);
                bubbleRect.localScale = Vector3.one;
                bubbleRect.localRotation = Quaternion.identity;
                bubbleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100f);
                bubbleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 56f);
                bubbleRect.anchoredPosition = portraitRect.anchoredPosition + new Vector2(
                    (1f - portraitRect.pivot.x) * portraitRect.rect.width + 12f,
                    (.5f - portraitRect.pivot.y) * portraitRect.rect.height);
                bubbleRect.SetAsLastSibling();
                bubble.color = new Color(1f, .94f, .84f, .98f);
                bubble.raycastTarget = false;
                if (!dots)
                {
                    var existingDots = bubbleRect.Find("Dots");
                    if (existingDots) dots = existingDots.GetComponent<Text>();
                    if (!dots)
                    {
                        var createdDots = new GameObject("Dots", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                        createdDots.transform.SetParent(bubbleRect, false);
                        dots = createdDots.GetComponent<Text>();
                    }
                    dots.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/InterviewRefresh/Fonts/NotoSansKR.ttf");
                }
                dots.rectTransform.SetParent(bubbleRect, false);
                dots.rectTransform.anchorMin = Vector2.zero;
                dots.rectTransform.anchorMax = Vector2.one;
                dots.rectTransform.pivot = new Vector2(.5f, .5f);
                dots.rectTransform.offsetMin = new Vector2(8f, 4f);
                dots.rectTransform.offsetMax = new Vector2(-8f, -4f);
                dots.rectTransform.localScale = Vector3.one;
                dots.rectTransform.localRotation = Quaternion.identity;
                dots.alignment = TextAnchor.MiddleCenter;
                dots.fontSize = 28;
                dots.color = new Color(.28f, .22f, .18f, 1f);
                dots.raycastTarget = false;
                view.ConfigureSpeechBubbleEditor(bubble, dots);
                EditorUtility.SetDirty(view);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}