#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewScreen
    {
        [SerializeField, HideInInspector] bool _callPortraitLayoutAuthored;
        static T LayoutComponent<T>(GameObject obj, out bool added) where T : Component
        {
            var value = obj.GetComponent<T>();
            added = !value;
            if (added) value = Undo.AddComponent<T>(obj);
            return value;
        }
        static void DirtyLayout(UnityEngine.Object obj)
        {
            EditorUtility.SetDirty(obj);
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }
        static RectTransform NewLayoutRect(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(obj, "Create call layout");
            obj.transform.SetParent(parent, false);
            return (RectTransform)obj.transform;
        }
        public static Font CaptionFontEditor(Text source)
        {
            var font = source ? source.font : null;
            if (!font || !AssetDatabase.GetAssetPath(font).Contains("NotoSansKR"))
                throw new InvalidOperationException("Caption requires existing NotoSansKR font asset reference.");
            return font;
        }
        internal static Text AuthorSlotCaptionEditor(Text original, Transform group, bool hideOriginal = true)
        {
            var found = group.Find("Caption");
            Text caption;
            if (found)
            {
                caption = found.GetComponent<Text>();
                if (!caption) throw new InvalidOperationException("Existing Caption requires Text.");
            }
            else
            {
                var font = CaptionFontEditor(original);
                var rect = NewLayoutRect(group, "Caption");
                caption = Undo.AddComponent<Text>(rect.gameObject);
                EditorUtility.CopySerialized(original, caption);
                caption.font = font;
                caption.alignment = TextAnchor.MiddleLeft;
                caption.horizontalOverflow = HorizontalWrapMode.Wrap;
                caption.verticalOverflow = VerticalWrapMode.Truncate;
                caption.raycastTarget = false;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var size = Undo.AddComponent<LayoutElement>(rect.gameObject);
                size.minWidth = 80; size.flexibleWidth = 1;
                size.minHeight = size.preferredHeight = 56;
            }
            if (hideOriginal && original != caption)
            {
                Undo.RecordObject(original.gameObject, "Hide original slot text");
                original.gameObject.SetActive(false);
                DirtyLayout(original.gameObject);
            }
            return caption;
        }
        internal static void AuthorRowLayoutEditor(RectTransform row)
        {
            var layout = LayoutComponent<HorizontalLayoutGroup>(row.gameObject, out bool added);
            if (added)
            {
                layout.spacing = 8; layout.padding = new RectOffset(8, 8, 10, 10);
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            }
            var size = LayoutComponent<LayoutElement>(row.gameObject, out added);
            if (added) size.minHeight = size.preferredHeight = 76;
            var profileRoot = row.Find("Profile");
            if (profileRoot)
            {
                var rootSize = LayoutComponent<LayoutElement>(profileRoot.gameObject, out added);
                Undo.RecordObject(rootSize, "Size profile cell");
                rootSize.ignoreLayout = false;
                rootSize.minWidth = rootSize.preferredWidth = 56;
                rootSize.minHeight = rootSize.preferredHeight = 56;
                rootSize.flexibleWidth = rootSize.flexibleHeight = 0;
                DirtyLayout(layout); DirtyLayout(size); DirtyLayout(rootSize);
                return;
            }
            var mask = row.Find("ProfileMask");
            var placeholder = row.Find("Placeholder");
            if (!mask || !placeholder) throw new InvalidOperationException("Row profile/mask missing.");
            var profileSize = LayoutComponent<LayoutElement>(mask.gameObject, out added);
            if (added)
            {
                profileSize.minWidth = profileSize.preferredWidth = 56;
                profileSize.minHeight = profileSize.preferredHeight = 56;
                profileSize.flexibleWidth = profileSize.flexibleHeight = 0;
            }
            // Placeholder overlays the same profile area and must not consume a second layout cell.
            var overlay = LayoutComponent<LayoutElement>(placeholder.gameObject, out added);
            if (added)
            {
                overlay.ignoreLayout = true;
                var rect = (RectTransform)placeholder;
                Undo.RecordObject(rect, "Align profile placeholder");
                rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
                rect.pivot = new Vector2(0, .5f);
                rect.sizeDelta = new Vector2(profileSize.preferredWidth, profileSize.preferredHeight);
                rect.anchoredPosition = new Vector2(layout.padding.left, 0);
                DirtyLayout(rect);
            }
            DirtyLayout(layout); DirtyLayout(size); DirtyLayout(profileSize); DirtyLayout(overlay);
        }
        internal static void AuthorTeamLayoutEditor(RectTransform team)
        {
            var layout = LayoutComponent<VerticalLayoutGroup>(team.gameObject, out bool added);
            if (added)
            {
                layout.padding = new RectOffset(12, 12, 12, 12); layout.spacing = 8;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            }
            foreach (Transform child in team)
            {
                if (child.GetComponent<InterviewTeamSlotView>()) continue;
                var size = LayoutComponent<LayoutElement>(child.gameObject, out added);
                if (added) size.ignoreLayout = true;
                DirtyLayout(size);
            }
            DirtyLayout(layout);
        }
        public void BakeCallUiLayoutEditor(RectTransform channel = null)
        {
            if (Application.isPlaying || !_call || !_call.HasAuthoredUi)
                throw new InvalidOperationException("Existing authored call UI required in EditMode.");
            if (!channel && _discordViewRoot)
            {
                foreach (var label in _discordViewRoot.GetComponentsInChildren<Text>(true))
                {
                    if (label.text == null || !label.text.Contains("통화")) continue;
                    if (channel && channel != label.transform.parent)
                        throw new InvalidOperationException("Ambiguous call channel; pass exact channel RectTransform.");
                    channel = label.transform.parent as RectTransform;
                }
            }
            if (!channel)
            {
                if (!_discordViewRoot) throw new InvalidOperationException("Discord view root required.");
                var anchor = _discordViewRoot.Find("CallChannelAnchor");
                if (anchor)
                {
                    channel = anchor as RectTransform;
                    if (!channel) throw new InvalidOperationException("CallChannelAnchor requires RectTransform.");
                }
                else
                {
                    channel = NewLayoutRect(_discordViewRoot, "CallChannelAnchor");
                    channel.anchorMin = new Vector2(.01f, .55f);
                    channel.anchorMax = new Vector2(.16f, .72f);
                    channel.offsetMin = channel.offsetMax = Vector2.zero;
                }
            }
            CaptionFontEditor(_slotLabels[0]);
            BakeTeamProfilesEditor();
            var existing = channel.Find("CallParticipants");
            var list = existing ? (RectTransform)existing : NewLayoutRect(channel, "CallParticipants");
            if (!existing)
            {
                list.anchorMin = new Vector2(0, 0); list.anchorMax = new Vector2(1, 0);
                list.pivot = new Vector2(.5f, 1); list.anchoredPosition = Vector2.zero;
                list.sizeDelta = new Vector2(0, 84);
            }
            var layout = LayoutComponent<VerticalLayoutGroup>(list.gameObject, out bool added);
            if (added)
            {
                layout.spacing = 8; layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            }
            var existingRow = list.Find("SlotGroup0");
            bool newCaption = !existingRow || !existingRow.Find("Caption");
            var row = InterviewTeamSlotView.AuthorEditor(_slotLabels[0], list, 0, LoadTeamProfileSpriteEditor(), false);
            AuthorRowLayoutEditor((RectTransform)row.transform);
            var caption = row.transform.Find("Caption").GetComponent<Text>();
            if (newCaption) caption.text = "지원자 · 파트";
            Undo.RecordObject(_call, "Connect call participant");
            _call.ConfigureParticipantEditor(row, caption);
            if (!_callPortraitLayoutAuthored)
            {
                var rect = _call.PortraitRect;
                Undo.RecordObject(rect, "Author portrait top two thirds");
                Undo.RecordObject(this, "Mark portrait layout authored");
                var top = rect.TransformPoint(new Vector3(rect.rect.center.x, rect.rect.yMax, 0));
                var size = rect.rect.size * (2f / 3f);
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
                rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = size; rect.position = top;
                var sizing = LayoutComponent<LayoutElement>(rect.gameObject, out added);
                Undo.RecordObject(sizing, "Size central portrait");
                sizing.ignoreLayout = true;
                sizing.preferredWidth = size.x; sizing.preferredHeight = size.y;
                _callPortraitLayoutAuthored = true;
                DirtyLayout(rect); DirtyLayout(sizing);
            }
            DirtyLayout(this); DirtyLayout(_call); DirtyLayout(layout);
        }
    }
}
#endif
