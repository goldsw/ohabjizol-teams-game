#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Ohabjizol.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public static class DocumentLibrarySpriteBake
    {
        const string ArtPath = "Assets/Art/Background/UI/Documents/PDF.aseprite";
        static readonly Vector2 ViewportMin = new Vector2(.245f, .035f);
        static readonly Vector2 ViewportMax = new Vector2(.985f, .72f);

        static void RequireEditMode()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Document library sprite bake requires EditMode.");
        }

        static Sprite LoadArt()
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(ArtPath).OfType<Sprite>().Single();
            if (!sprite || sprite.rect.width != 1366f || sprite.rect.height != 768f)
                throw new InvalidOperationException("Expected a single 1366x768 PDF sprite: " + ArtPath);
            return sprite;
        }

        public static void EnsurePrefab()
        {
            RequireEditMode();
            Sprite sprite = LoadArt();
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(DocumentLibraryBake.LibraryPath);
            if (!asset) throw new InvalidOperationException("DocumentLibrary prefab is missing.");
            Image image = asset.GetComponent<Image>();
            ScrollRect scroll = asset.GetComponentInChildren<ScrollRect>(true);
            if (image && image.sprite == sprite && scroll && scroll.viewport &&
                scroll.viewport.anchorMin == ViewportMin && scroll.viewport.anchorMax == ViewportMax)
                return;
            Apply();
        }

        public static void Apply()
        {
            RequireEditMode();
            LoadArt();
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string source = Path.Combine(project, DocumentLibraryBake.LibraryPath);
            string backup = Path.Combine(project, "Logs", "DocumentSpriteCorrection", "Before",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
            Directory.CreateDirectory(backup);
            File.Copy(source, Path.Combine(backup, "DocumentLibrary.prefab"), false);
            GameObject root = PrefabUtility.LoadPrefabContents(DocumentLibraryBake.LibraryPath);
            try
            {
                Author(root.GetComponent<DocumentLibraryView>());
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, DocumentLibraryBake.LibraryPath);
                if (!saved) throw new InvalidOperationException("Could not save DocumentLibrary prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void Author(DocumentLibraryView view)
        {
            RequireEditMode();
            if (!view) throw new ArgumentNullException(nameof(view));
            Sprite sprite = LoadArt();
            Font font = UiKit.Font;
            if (!font) throw new InvalidOperationException("Document library font is missing.");
            var serialized = new SerializedObject(view);
            SerializedProperty titleProperty = serialized.FindProperty("_titleLabel");
            Text title = titleProperty == null ? null : titleProperty.objectReferenceValue as Text;
            var infoRect = serialized.FindProperty("_info")?.objectReferenceValue as RectTransform;
            var toolbar = serialized.FindProperty("_toolbar")?.objectReferenceValue as RectTransform;
            ScrollRect scroll = view.GetComponentInChildren<ScrollRect>(true);
            if (!title || !scroll || !scroll.viewport || !scroll.content || !infoRect || !toolbar)
                throw new InvalidOperationException("DocumentLibrary references are incomplete.");

            RectTransform root = (RectTransform)view.transform;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1100f);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1100f * 768f / 1366f);
            Image background = GetImage(root);
            background.sprite = sprite;
            background.type = Image.Type.Simple;
            background.color = Color.white;
            background.raycastTarget = false;

            Pin(infoRect, Vector2.zero, Vector2.one);
            Image info = GetImage(infoRect);
            info.color = Color.clear;
            info.raycastTarget = false;
            GetImage(toolbar).color = Color.clear;
            toolbar.SetAsLastSibling();

            title.rectTransform.SetParent(infoRect, false);
            Pin(title.rectTransform, new Vector2(.064f, .858f), new Vector2(.20f, .929f));
            Style(title, font, 18, Color.white, "문서함");

            Pin(scroll.viewport, ViewportMin, ViewportMax);
            Image viewport = GetImage(scroll.viewport);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;

            Transform headingChild = infoRect.Find("LibraryHeading");
            Text heading;
            if (headingChild)
            {
                heading = headingChild.GetComponent<Text>();
                if (!heading) heading = headingChild.gameObject.AddComponent<Text>();
            }
            else
            {
                var host = new GameObject("LibraryHeading", typeof(RectTransform), typeof(Text));
                host.transform.SetParent(infoRect, false);
                heading = host.GetComponent<Text>();
            }
            Pin(heading.rectTransform, new Vector2(.276f, .752f), new Vector2(.65f, .825f));
            Style(heading, font, 24, Color.black, "다운로드한 문서");
        }

        static Image GetImage(RectTransform rect)
        {
            Image image = rect.GetComponent<Image>();
            if (!image) image = rect.gameObject.AddComponent<Image>();
            return image;
        }

        static void Pin(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void Style(Text text, Font font, int size, Color color, string value)
        {
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.text = value;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
        }
    }
}
#endif
