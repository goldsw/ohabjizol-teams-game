#if UNITY_EDITOR
using System;
using Ohabjizol.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewScreen
    {
        public void BakeNotepadWindowEditor()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Notepad baking requires EditMode.");
            if (!_windowParent || !_itemParent)
                throw new InvalidOperationException("Notepad requires authored window/item parents.");

            Font font = NotepadFontEditor();
            Transform windowTransform = _windowParent.Find("NotepadWindow");
            Transform fixedTransform = _itemParent.Find("FixedApps");
            if (fixedTransform && !(fixedTransform is RectTransform))
                throw new InvalidOperationException("FixedApps requires RectTransform.");
            Transform buttonTransform = fixedTransform ? fixedTransform.Find("NotepadButton") : null;
            Transform looseButton = _itemParent.Find("NotepadButton");
            if (buttonTransform && looseButton)
                throw new InvalidOperationException("Duplicate NotepadButton objects.");
            if (!buttonTransform) buttonTransform = looseButton;
            var window = windowTransform ? windowTransform.GetComponent<InterviewNotepadWindow>() : _notepadWindow;
            var button = buttonTransform ? buttonTransform.GetComponent<Button>() : _notepadButton;
            if ((_notepadWindow && window != _notepadWindow) || (_notepadButton && button != _notepadButton))
                throw new InvalidOperationException("Notepad serialized references conflict with authored hierarchy.");
            if (windowTransform && !window || buttonTransform && !button || (window != null) != (button != null))
                throw new InvalidOperationException("Partial notepad UI: repair existing window/button before baking.");

            InputField input = null;
            Text title = null;
            Text buttonLabel = null;
            InterviewNotepadLauncher launcher = null;
            if (window)
            {
                var serialized = new SerializedObject(window);
                input = serialized.FindProperty("_input").objectReferenceValue as InputField;
                title = window.transform.Find("Toolbar/Title")?.GetComponent<Text>();
                buttonLabel = button.GetComponentInChildren<Text>(true);
                launcher = button.GetComponent<InterviewNotepadLauncher>();
                if (window.transform.parent != _windowParent || !window.transform.Find("Info")
                    || !window.transform.Find("Info").GetComponent<Image>()
                    || (button.transform.parent != _itemParent && button.transform.parent != fixedTransform)
                    || !window.HasAuthoredInput || !input || !input.textComponent || !(input.placeholder is Text)
                    || !input.targetGraphic || !input.GetComponent<RectMask2D>() || !title || !buttonLabel
                    || !button.GetComponent<Image>() || !launcher
                    || !input.transform.IsChildOf(window.transform)
                    || (_notepadLauncher && _notepadLauncher != launcher))
                    throw new InvalidOperationException("Partial notepad UI: authored input, clipping, title or launcher is invalid.");
            }
            else if (_notepadLauncher)
                throw new InvalidOperationException("Partial notepad UI: launcher exists without window/button.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Bake stage notepad");
            try
            {
                NotepadRecordEditor(this);
                RectTransform fixedApps = fixedTransform as RectTransform;
                if (!fixedApps) fixedApps = NotepadRectEditor(_itemParent, "FixedApps");
                var layout = fixedApps.GetComponent<HorizontalLayoutGroup>();
                if (!layout)
                {
                    layout = Undo.AddComponent<HorizontalLayoutGroup>(fixedApps.gameObject);
                    layout.spacing = 8f;
                    layout.childAlignment = TextAnchor.MiddleLeft;
                    layout.childControlWidth = layout.childControlHeight = true;
                    layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                    NotepadSaveEditor(layout);
                }
                if (!window)
                {
                    RectTransform root = NotepadRectEditor(_windowParent, "NotepadWindow");
                    root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
                    root.sizeDelta = new Vector2(600f, 440f);
                    window = Undo.AddComponent<InterviewNotepadWindow>(root.gameObject);
                    var toolbar = NotepadRectEditor(root, "Toolbar");
                    toolbar.anchorMin = new Vector2(0f, 1f);
                    toolbar.anchorMax = Vector2.one;
                    toolbar.pivot = new Vector2(.5f, 1f);
                    toolbar.sizeDelta = new Vector2(0f, WindowToolbar.ToolbarHeight);
                    var toolbarImage = Undo.AddComponent<Image>(toolbar.gameObject);
                    toolbarImage.color = new Color(.16f, .17f, .19f);
                    Undo.AddComponent<WindowDragHandle>(toolbar.gameObject);
                    title = NotepadTextEditor(toolbar, "Title", "메모장", font);
                    title.alignment = TextAnchor.MiddleLeft;
                    title.color = Color.white;
                    title.rectTransform.offsetMin = new Vector2(12f, 0f);
                    title.rectTransform.offsetMax = new Vector2(-80f, 0f);
                    var info = NotepadRectEditor(root, "Info");
                    info.offsetMax = new Vector2(0f, -WindowToolbar.ToolbarHeight);
                    var infoImage = Undo.AddComponent<Image>(info.gameObject);
                    infoImage.color = new Color32(246, 244, 232, 255);
                    infoImage.raycastTarget = false;
                    var inputRect = NotepadRectEditor(info, "Input");
                    inputRect.offsetMin = new Vector2(12f, 12f);
                    inputRect.offsetMax = new Vector2(-12f, -12f);
                    var surface = Undo.AddComponent<Image>(inputRect.gameObject);
                    surface.color = Color.clear;
                    surface.raycastTarget = true;
                    Undo.AddComponent<RectMask2D>(inputRect.gameObject);
                    input = Undo.AddComponent<InputField>(inputRect.gameObject);
                    input.targetGraphic = surface;
                    input.textComponent = NotepadTextEditor(inputRect, "Text", string.Empty, font);
                    var placeholder = NotepadTextEditor(inputRect, "Placeholder", "메모를 입력하세요", font);
                    placeholder.color = new Color(.4f, .4f, .4f, .7f);
                    input.placeholder = placeholder;
                    window.ConfigureEditor(input);
                    Undo.AddComponent<WindowPointerFocus>(root.gameObject);
                    window.CaptureEditorReferences();
                    root.gameObject.SetActive(false);

                    var buttonRect = NotepadRectEditor(fixedApps, "NotepadButton");
                    var buttonSurface = Undo.AddComponent<Image>(buttonRect.gameObject);
                    buttonSurface.color = new Color32(255, 174, 76, 255);
                    button = Undo.AddComponent<Button>(buttonRect.gameObject);
                    button.targetGraphic = buttonSurface;
                    var size = Undo.AddComponent<LayoutElement>(buttonRect.gameObject);
                    size.minWidth = size.preferredWidth = 112f;
                    size.minHeight = size.preferredHeight = 52f;
                    buttonLabel = NotepadTextEditor(buttonRect, "Label", "메모장", font);
                    buttonLabel.alignment = TextAnchor.MiddleCenter;
                    launcher = Undo.AddComponent<InterviewNotepadLauncher>(buttonRect.gameObject);
                }
                else if (button.transform.parent != fixedApps)
                    Undo.SetTransformParent(button.transform, fixedApps, "Move notepad to FixedApps");

                NotepadRecordEditor(window);
                NotepadRecordEditor(input);
                NotepadRecordEditor(input.targetGraphic);
                NotepadRecordEditor(launcher);
                var infoSurface = window.transform.Find("Info").GetComponent<Image>();
                NotepadRecordEditor(infoSurface);
                infoSurface.raycastTarget = false;
                input.lineType = InputField.LineType.MultiLineNewline;
                input.characterLimit = 0;
                input.targetGraphic.raycastTarget = true;
                foreach (Text text in new[] { title, input.textComponent, (Text)input.placeholder, buttonLabel })
                {
                    NotepadRecordEditor(text);
                    text.font = font;
                    text.supportRichText = false;
                    text.raycastTarget = false;
                    NotepadSaveEditor(text);
                }
                launcher.ConfigureEditor(window, button, button.GetComponent<Image>());
                _notepadWindow = window;
                _notepadButton = button;
                _notepadLauncher = launcher;
                RecalculateFixedAppsWidthEditor(fixedApps);
                foreach (UnityEngine.Object target in new UnityEngine.Object[] { window, input, input.targetGraphic, infoSurface, launcher, this })
                    NotepadSaveEditor(target);
                ConfigureNotepadImeEditor();
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        Font NotepadFontEditor()
        {
            Transform library = _windowParent.Find("DocumentLibrary");
            if (library)
                foreach (Text text in library.GetComponentsInChildren<Text>(true))
                {
                    Font font = text.font;
                    if (font && EditorUtility.IsPersistent(font)
                        && font.name.IndexOf("NotoSansKR", StringComparison.OrdinalIgnoreCase) >= 0)
                        return font;
                }
            Font asset = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/InterviewRefresh/Fonts/NotoSansKR.ttf");
            if (asset && EditorUtility.IsPersistent(asset)) return asset;
            throw new InvalidOperationException("Notepad preflight: persistent NotoSansKR font missing from DocumentLibrary and Assets/Art/InterviewRefresh/Fonts/NotoSansKR.ttf.");
        }

        static RectTransform NotepadRectEditor(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(obj, "Create notepad UI");
            Undo.SetTransformParent(obj.transform, parent, "Parent notepad UI");
            var rect = (RectTransform)obj.transform;
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static Text NotepadTextEditor(Transform parent, string name, string value, Font font)
        {
            var rect = NotepadRectEditor(parent, name);
            var text = Undo.AddComponent<Text>(rect.gameObject);
            text.font = font;
            text.text = value;
            text.fontSize = 20;
            text.color = new Color(.12f, .12f, .12f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }

        static void RecalculateFixedAppsWidthEditor(RectTransform fixedApps)
        {
            var layout = fixedApps.GetComponent<HorizontalLayoutGroup>();
            if (!layout) throw new InvalidOperationException("FixedApps requires HorizontalLayoutGroup.");
            var size = fixedApps.GetComponent<LayoutElement>();
            if (!size) size = Undo.AddComponent<LayoutElement>(fixedApps.gameObject);
            NotepadRecordEditor(size);
            float width = layout.padding.horizontal;
            int count = 0;
            foreach (Transform child in fixedApps)
            {
                if (!child.gameObject.activeSelf || !(child is RectTransform rect)) continue;
                var ignored = child.GetComponent<LayoutElement>();
                if (ignored && ignored.ignoreLayout) continue;
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                width += Mathf.Max(LayoutUtility.GetMinWidth(rect), LayoutUtility.GetPreferredWidth(rect));
                count++;
            }
            size.minWidth = size.preferredWidth = width + Mathf.Max(0, count - 1) * layout.spacing;
            NotepadSaveEditor(size);
        }

        static void NotepadRecordEditor(UnityEngine.Object target) => Undo.RecordObject(target, "Bake stage notepad");
        static void NotepadSaveEditor(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
#endif
