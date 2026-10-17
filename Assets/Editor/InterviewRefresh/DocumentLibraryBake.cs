#if UNITY_EDITOR
using System;
using Ohabjizol.EditorTools;
using Ohabjizol.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public static class DocumentLibraryBake
    {
        public const string LibraryPath = DocumentsUiBake.Folder + "/DocumentLibrary.prefab";
        public const string ButtonPath = DocumentsUiBake.Folder + "/DocumentLibraryButton.prefab";

        public static void BuildAssets()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Library bake requires EditMode.");
            Font font = AssetDatabase.LoadAssetAtPath<Font>(DocumentsUiBake.FontPath);
            GameObject rowAsset = AssetDatabase.LoadAssetAtPath<GameObject>(DocumentsUiBake.TaskbarItemPath);
            if (font == null || rowAsset == null) throw new InvalidOperationException("Bake PDF row and font first.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LibraryPath) == null)
                BuildLibrary(font, rowAsset.GetComponent<DocumentTaskbarItem>());
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath) == null) BuildButton(font);
        }

        static void BuildButton(Font font)
        {
            GameObject parent = new GameObject("BakeLibraryButton", typeof(RectTransform));
            try
            {
                Button button = UiKit.Button(parent.transform, "DocumentLibraryButton", "문서함",
                    new Color32(240, 158, 74, 255), 18);
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(168f, 52f);
                LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
                layout.minWidth = layout.preferredWidth = 168f;
                layout.minHeight = layout.preferredHeight = 52f;
                button.gameObject.AddComponent<DocumentLibraryButton>();
                Normalize(button.gameObject, font);
                PrefabUtility.SaveAsPrefabAsset(button.gameObject, ButtonPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); }
        }

        static void BuildLibrary(Font font, DocumentTaskbarItem row)
        {
            GameObject parent = new GameObject("BakeDocumentLibrary", typeof(RectTransform));
            try
            {
                DocumentLibraryView view = WindowFactory.Create<DocumentLibraryView>(
                    (RectTransform)parent.transform, "문서함", new Vector2(680f, 580f), Vector2.zero);
                view.Info.GetComponent<Image>().color = new Color32(255, 238, 215, 255);
                view.Controls.sizeDelta = new Vector2(WindowToolbar.ToolbarHeight * 2f, 0f);
                RectTransform close = view.Controls.Find("Close").GetComponent<RectTransform>();
                close.anchorMin = new Vector2(.5f, 0f);
                close.anchorMax = Vector2.one;
                close.offsetMin = close.offsetMax = Vector2.zero;
                Button minimize = UiKit.Button(view.Controls, "Minimize", "-", new Color32(240, 158, 74, 255), 22);
                RectTransform minimizeRect = minimize.GetComponent<RectTransform>();
                minimizeRect.anchorMin = Vector2.zero;
                minimizeRect.anchorMax = new Vector2(.5f, 1f);
                minimizeRect.offsetMin = minimizeRect.offsetMax = Vector2.zero;
                view.Toolbar.Find("Drag").GetComponent<RectTransform>().offsetMax = new Vector2(-80f, 0f);
                view.Toolbar.Find("Title").GetComponent<RectTransform>().offsetMax = new Vector2(-80f, 0f);
                Image viewportImage = UiKit.Image(view.Info, "Viewport", Color.clear);
                RectTransform viewport = viewportImage.rectTransform;
                UiKit.Stretch(viewport, 16f);
                viewport.gameObject.AddComponent<RectMask2D>();
                ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
                scroll.viewport = viewport;
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                var contentObject = new GameObject("Content", typeof(RectTransform));
                RectTransform content = contentObject.GetComponent<RectTransform>();
                content.SetParent(viewport, false);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = Vector2.one;
                content.pivot = new Vector2(.5f, 1f);
                content.sizeDelta = Vector2.zero;
                VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.padding = new RectOffset(8, 8, 8, 8);
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.content = content;
                view.ConfigureEditor(content, row);
                view.CaptureEditorReferences();
                Normalize(view.gameObject, font);
                view.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(view.gameObject, LibraryPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); }
        }

        static void Normalize(GameObject host, Font font)
        {
            foreach (Text text in host.GetComponentsInChildren<Text>(true))
            { text.font = font; text.raycastTarget = false; text.color = new Color32(43, 31, 24, 255); }
        }

        public static void Apply(string prefabPath)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Migration requires EditMode.");
            DocumentsUiBake.BuildAssets();
            StageCalendarUiBake.BuildAssets();
            GameObject libraryAsset = AssetDatabase.LoadAssetAtPath<GameObject>(LibraryPath);
            GameObject buttonAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
            GameObject calendarAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StageCalendarUiBake.WindowPath);
            StageScheduleConfig config = AssetDatabase.LoadAssetAtPath<StageScheduleConfig>(StageCalendarUiBake.ConfigPath);
            if (libraryAsset == null || buttonAsset == null || calendarAsset == null || config == null)
                throw new InvalidOperationException("Document/calendar authored assets are missing.");
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                InterviewScreen screen = root.GetComponentInChildren<InterviewScreen>(true);
                if (screen == null) throw new InvalidOperationException("InterviewScreen missing in " + prefabPath);
                screen.BakeDocumentAppsUi(libraryAsset, buttonAsset, calendarAsset, config);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
#endif
