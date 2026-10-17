#if UNITY_EDITOR
using System;
using System.IO;
using Ohabjizol.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public static class DocumentsUiBake
    {
        public const string Folder = "Assets/Prefabs/InterviewRefresh/Documents";
        public const string TaskbarItemPath = Folder + "/PdfTaskbarItem.prefab";
        public const string ViewerPath = Folder + "/PdfViewer.prefab";
        public const string FontPath = "Assets/Art/InterviewRefresh/Fonts/NotoSansKR.ttf";

        public static void BuildAssets()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Document assets must be baked in Edit Mode.");
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
                throw new InvalidOperationException("Persistent document font is missing: " + FontPath);
            EnsureFolder(Folder);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TaskbarItemPath) == null)
                BuildTaskbar(font);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ViewerPath) == null)
                BuildViewer(font);
            DocumentLibraryBake.BuildAssets();
            TaskbarWindowBake.BuildAssets();
        }

        static void BuildTaskbar(Font font)
        {
            GameObject host = new GameObject("PdfTaskbarItem", typeof(RectTransform), typeof(Image),
                typeof(DocumentTaskbarItem));
            try
            {
                RectTransform root = (RectTransform)host.transform;
                root.sizeDelta = new Vector2(168f, 52f);
                Image background = host.GetComponent<Image>();
                background.sprite = null;
                background.color = new Color32(240, 158, 74, 255);
                background.raycastTarget = true;
                Text icon = UiKit.Text(root, "PdfIcon", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
                icon.text = "PDF";
                Pin(icon.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                    new Vector2(4f, 4f), new Vector2(40f, -4f));
                Text label = UiKit.Text(root, "Label", 17, FontStyle.Normal, TextAnchor.MiddleLeft);
                label.text = "문서.pdf";
                UiKit.Stretch(label.rectTransform, 4f);
                label.rectTransform.offsetMin = new Vector2(48f, 4f);
                host.GetComponent<DocumentTaskbarItem>().ConfigureEditor(label);
                NormalizeText(host, font);
                PrefabUtility.SaveAsPrefabAsset(host, TaskbarItemPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        static void BuildViewer(Font font)
        {
            GameObject parent = new GameObject("BakeDocuments", typeof(RectTransform));
            try
            {
                PdfDocumentViewer viewer = WindowFactory.Create<PdfDocumentViewer>(
                    (RectTransform)parent.transform, "PDF", new Vector2(640f, 740f), Vector2.zero);
                Image infoImage = viewer.Info.GetComponent<Image>();
                infoImage.sprite = null;
                infoImage.color = new Color32(255, 238, 215, 255);
                viewer.Controls.sizeDelta = new Vector2(WindowToolbar.ToolbarHeight * 2f, 0f);
                Button close = viewer.Controls.Find("Close").GetComponent<Button>();
                Pin(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), Vector2.one,
                    Vector2.zero, Vector2.zero);
                Button minimize = UiKit.Button(viewer.Controls, "Minimize", "-", new Color32(240, 158, 74, 255), 22);
                Pin(minimize.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0.5f, 1f),
                    Vector2.zero, Vector2.zero);
                viewer.Toolbar.Find("Drag").GetComponent<RectTransform>().offsetMax =
                    new Vector2(-WindowToolbar.ToolbarHeight * 2f, 0f);
                viewer.Toolbar.Find("Title").GetComponent<RectTransform>().offsetMax =
                    new Vector2(-WindowToolbar.ToolbarHeight * 2f, 0f);

                Text title = UiKit.Text(viewer.Info, "DocumentTitle", 25, FontStyle.Bold, TextAnchor.MiddleLeft);
                title.text = "문서";
                Pin(title.rectTransform, new Vector2(0f, 1f), Vector2.one,
                    new Vector2(20f, -60f), new Vector2(-20f, -8f));

                Image viewportImage = UiKit.Image(viewer.Info, "Viewport", Color.clear);
                RectTransform viewport = viewportImage.rectTransform;
                UiKit.Stretch(viewport, 16f);
                viewport.offsetMax = new Vector2(-16f, -68f);
                viewport.gameObject.AddComponent<RectMask2D>();
                ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
                scroll.viewport = viewport;
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;

                RectTransform content = NewRect(viewport, "Content");
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = Vector2.one;
                content.pivot = new Vector2(0.5f, 1f);
                content.sizeDelta = Vector2.zero;
                VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 16f;
                layout.padding = new RectOffset(8, 8, 8, 8);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.content = content;

                Text body = UiKit.Text(content, "BodyText", 22, FontStyle.Normal, TextAnchor.UpperLeft);
                body.text = "";
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                body.verticalOverflow = VerticalWrapMode.Overflow;
                Image bodyImage = UiKit.Image(content, "PortfolioImage", Color.white);
                bodyImage.sprite = null;
                bodyImage.preserveAspect = true;
                bodyImage.raycastTarget = false;
                LayoutElement imageLayout = bodyImage.gameObject.AddComponent<LayoutElement>();
                imageLayout.preferredHeight = 460f;
                bodyImage.gameObject.SetActive(false);
                RectTransform portfolioHost = NewRect(content, "PortfolioHost");
                LayoutElement cardLayout = portfolioHost.gameObject.AddComponent<LayoutElement>();
                cardLayout.preferredHeight = 460f;
                portfolioHost.gameObject.SetActive(false);
                viewer.ConfigureEditor(body, bodyImage, title, portfolioHost);
                viewer.CaptureEditorReferences();
                NormalizeText(viewer.gameObject, font);
                viewer.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(viewer.gameObject, ViewerPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); }
        }

        static RectTransform NewRect(RectTransform parent, string name)
        {
            var host = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)host.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static void Pin(RectTransform rect, Vector2 min, Vector2 max, Vector2 insetMin, Vector2 insetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = insetMin;
            rect.offsetMax = insetMax;
        }

        static void NormalizeText(GameObject host, Font font)
        {
            foreach (Text text in host.GetComponentsInChildren<Text>(true))
            {
                text.font = font;
                text.raycastTarget = false;
                text.color = new Color32(43, 31, 24, 255);
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string diskPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            if (Directory.Exists(diskPath))
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.IsValidFolder(path)) return;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
                if (AssetDatabase.IsValidFolder(path)) return;
                throw new InvalidOperationException("Existing document folder could not be imported: " + path);
            }
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            string guid = AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            if (string.IsNullOrEmpty(guid) || !AssetDatabase.IsValidFolder(path))
                throw new InvalidOperationException("Document folder could not be created: " + path);
        }
    }
}
#endif