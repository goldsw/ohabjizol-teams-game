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
    /// <summary>Explicit, narrowly scoped authoring of the shared application document.</summary>
    public static class ApplicationDocumentBake
    {
        const string PrefabPath = "Assets/Prefabs/InterviewRefresh/Documents/PdfViewer.prefab";
        const string ArtPath = "Assets/Art/Background/UI/Documents/";

        [MenuItem("Tools/Interview/Apply Application Document")]
        public static void Apply() => Bake(false);

        // An authored application is intentionally left alone, including manual positioning.
        public static void EnsurePrefab() => Bake(true);

        static void Bake(bool onlyIfMissing)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Application document authoring requires EditMode.");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var viewer = root.GetComponentInChildren<PdfDocumentViewer>(true);
                if (!viewer) throw new InvalidOperationException("Shared PDF prefab has no PdfDocumentViewer.");
                var serialized = new SerializedObject(viewer);
                var existing = Reference<ApplicationFormView>(serialized, "applicationForm");
                if (onlyIfMissing && existing && existing.HasAuthoredUi && viewer.HasDocumentAppearance) return;

                Sprite paper = SpriteAt("지원서.aseprite");
                Font font = UiKit.Font;
                if (!font) throw new InvalidOperationException("UiKit.Font is unavailable.");
                // Back up only the one asset this helper may save; never save other dirty assets.
                string project = Directory.GetParent(Application.dataPath).FullName;
                string backup = Path.Combine(project, "Logs", "ApplicationForm", "Before",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss-fffffff"));
                Directory.CreateDirectory(backup);
                File.Copy(Path.Combine(project, PrefabPath), Path.Combine(backup, "PdfViewer.prefab"));
                Author(root, viewer, serialized, paper, font);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success) throw new InvalidOperationException("Failed to save shared PDF prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static Sprite SpriteAt(string file)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(ArtPath + file).OfType<Sprite>().SingleOrDefault();
            if (!sprite) throw new InvalidOperationException("Expected one imported sprite at " + ArtPath + file);
            return sprite;
        }

        static void Author(GameObject root, PdfDocumentViewer viewer, SerializedObject data,
            Sprite paperSprite, Font font)
        {
            var rect = root.GetComponent<RectTransform>();
            if (!rect) throw new InvalidOperationException("Shared PDF root must be a RectTransform.");
            Vector2 applicationSize = new Vector2(540f, 540f * paperSprite.rect.height / paperSprite.rect.width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, applicationSize.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, applicationSize.y);
            var background = Component<Image>(rect);
            background.sprite = Reference<Sprite>(data, "portfolioBackgroundSprite");
            background.type = Image.Type.Simple;
            background.color = Color.white;
            background.raycastTarget = false;
            background.enabled = false;

            var scroll = root.GetComponentInChildren<ScrollRect>(true);
            if (!scroll || !scroll.viewport || !scroll.content)
                throw new InvalidOperationException("Shared PDF requires its existing ScrollRect, viewport and content.");
            var info = root.transform.Find("Info") as RectTransform;
            if (!info) throw new InvalidOperationException("Shared PDF requires its existing Info container.");
            Place(info, 0, 0, 1, 1);
            ClearImage(info);
            var infoImage = info.GetComponent<Image>();
            if (infoImage) infoImage.raycastTarget = false;
            var toolbar = info.Find("Toolbar") ?? root.transform.Find("Toolbar");
            if (!toolbar) throw new InvalidOperationException("Shared PDF requires its existing Toolbar.");
            ClearImage(toolbar);
            toolbar.SetAsLastSibling();
            var title = Reference<Text>(data, "_titleLabel");
            if (!title)
            {
                var titleTransform = toolbar.Find("Title");
                title = titleTransform ? titleTransform.GetComponent<Text>() : null;
            }
            if (!title)
                title = toolbar.GetComponentsInChildren<Text>(true)
                    .Where(text => !text.GetComponentInParent<Button>()).SingleOrDefault();
            if (!title) throw new InvalidOperationException("Cannot identify existing PDF toolbar title.");
            title.transform.SetParent(toolbar, false);
            Place(title.rectTransform, 0, 0, 1, 1);
            title.rectTransform.offsetMin = new Vector2(16f, 0f);
            title.rectTransform.offsetMax = new Vector2(-80f, 0f);
            Style(title, font, 18, Color.black);
            title.gameObject.SetActive(false);
            var documentTitle = Reference<Text>(data, "documentTitle");
            if (!documentTitle) throw new InvalidOperationException("Missing serialized documentTitle.");
            documentTitle.transform.SetParent(info, false);
            Place(documentTitle.rectTransform, 0, 1, 1, 1);
            documentTitle.rectTransform.offsetMin = new Vector2(20f, -64f);
            documentTitle.rectTransform.offsetMax = new Vector2(-20f, -40f);
            Style(documentTitle, font, 24, Color.black);
            documentTitle.gameObject.SetActive(false);
            Place(scroll.viewport, 0, 0, 1, 1);
            var scrollSurface = Component<Image>(scroll.viewport);
            scrollSurface.color = Color.clear;
            scrollSurface.raycastTarget = true;
            var layout = Component<VerticalLayoutGroup>(scroll.content);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(0, 0, 0, 0);
            Component<ContentSizeFitter>(scroll.content).verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var previous = Reference<ApplicationFormView>(data, "applicationForm");
            var formRect = previous ? (RectTransform)previous.transform : Child(scroll.content, "ApplicationForm");
            formRect.SetParent(scroll.content, false);
            formRect.SetAsFirstSibling();
            var sizing = Component<LayoutElement>(formRect);
            var paper = Child(formRect, "Paper");
            paper.anchorMin = paper.anchorMax = new Vector2(.5f, 1f);
            paper.pivot = new Vector2(.5f, 1f);
            paper.anchoredPosition = Vector2.zero;
            var page = Component<Image>(paper);
            page.sprite = paperSprite;
            page.type = Image.Type.Simple;
            page.color = Color.white;
            page.raycastTarget = false;
            var personal = Component<Text>(Child(paper, "Personal"));
            Place(personal.rectTransform, .312f, .655f, .883f, .748f);
            Style(personal, font, 22, Color.black);
            personal.alignment = TextAnchor.MiddleLeft;
            personal.text = "강서윤\n기획";
            var career = Scribble(paper, "Career", .489f, .605f, 3);
            var projects = Scribble(paper, "Projects", .304f, .438f, 4);
            var statsRect = Child(paper, "Stats");
            Place(statsRect, .312f, .108f, .884f, .246f);
            var stats = Reference<ApplicantStatsView>(data, "applicantStats");
            if (!stats || !stats.HasAuthoredUi)
            {
                viewer.BakeApplicantStatsUi(font);
                data.Update();
                stats = Reference<ApplicantStatsView>(data, "applicantStats");
            }
            if (!stats || !stats.HasAuthoredUi)
                throw new InvalidOperationException("Applicant stats authoring did not produce valid UI.");
            stats.transform.SetParent(statsRect, false);
            Place((RectTransform)stats.transform, 0, 0, 1, 1);
            Component<LayoutElement>(stats.transform).ignoreLayout = true;
            RestyleStats(stats, font);
            stats.gameObject.SetActive(true);
            var form = Component<ApplicationFormView>(formRect);
            var portrait = Component<Image>(Child(paper, "ApplicantPortrait"));
            Place(portrait.rectTransform, .785f, .815f, .934f, .945f);
            portrait.color = Color.white;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.enabled = portrait.sprite != null;
            var visualCatalog = AssetDatabase.LoadAssetAtPath<ApplicantVisualCatalog>(
                "Assets/Art/Applicants/ApplicantVisualCatalog.asset");
            if (!visualCatalog) throw new InvalidOperationException("Applicant visual catalog is missing.");
            form.ConfigurePortraitEditor(portrait, visualCatalog);
            form.ConfigureEditor(page, paper, personal, sizing, career, projects);
            form.gameObject.SetActive(true);
            var body = Reference<Text>(data, "bodyText");
            if (body) body.gameObject.SetActive(false);
            viewer.ConfigureApplicationFormEditor(form);
            viewer.ConfigureDocumentAppearanceEditor(background, title, scroll.viewport, applicationSize);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            form.ResizePaperEditor();
        }

        static void RestyleStats(ApplicantStatsView stats, Font font)
        {
            var data = new SerializedObject(stats);
            var heading = Reference<Text>(data, "heading");
            if (heading) heading.gameObject.SetActive(false);
            var labels = data.FindProperty("roleLabels");
            string[] keys = { "planning", "programming", "graphics" };
            for (int i = 0; i < keys.Length; i++)
            {
                var stars = Reference<TenPointStarsView>(data, keys[i]);
                if (!stars) throw new InvalidOperationException("Missing stats row: " + keys[i]);
                Place((RectTransform)stars.transform, 0, (2 - i) / 3f, 1, (3 - i) / 3f);
                Component<LayoutElement>(stars.transform).ignoreLayout = true;
                var label = labels.GetArrayElementAtIndex(i).objectReferenceValue as Text;
                Style(label, font, 16, Color.black);
                var starData = new SerializedObject(stars);
                Style(Reference<Text>(starData, "percentageLabel"), font, 16, Color.black);
                foreach (string key in new[] { "emptyStars", "fullStars" })
                {
                    var glyphs = starData.FindProperty(key);
                    for (int j = 0; j < glyphs.arraySize; j++)
                    {
                        var glyph = glyphs.GetArrayElementAtIndex(j).objectReferenceValue as Text;
                        if (glyph) Style(glyph, font, 24, glyph.color);
                    }
                }
            }
        }

        static DocumentScribbleGraphic Scribble(RectTransform paper, string name, float bottom, float top, int lines)
        {
            var rect = Child(paper, name);
            Place(rect, .312f, bottom, .884f, top);
            Component<CanvasRenderer>(rect);
            var graphic = Component<DocumentScribbleGraphic>(rect);
            graphic.color = Color.black;
            graphic.raycastTarget = false;
            graphic.ConfigureEditor(lines, 1.6f);
            return graphic;
        }

        static T Reference<T>(SerializedObject data, string name) where T : UnityEngine.Object
            => data.FindProperty(name)?.objectReferenceValue as T;

        static T Component<T>(Transform transform) where T : UnityEngine.Component
        {
            var value = transform.GetComponent<T>();
            if (!value) value = transform.gameObject.AddComponent<T>();
            return value;
        }

        static RectTransform Child(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing)
                return existing as RectTransform ?? throw new InvalidOperationException(name + " must be a RectTransform.");
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static void ClearImage(Transform transform)
        {
            var image = transform.GetComponent<Image>();
            if (image) image.color = Color.clear;
        }

        static void Place(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void Style(Text text, Font font, int size, Color color)
        {
            if (!text) throw new InvalidOperationException("Missing authored text reference.");
            text.font = font;
            text.fontSize = size;
            text.resizeTextForBestFit = false;
            text.color = color;
            text.raycastTarget = false;
        }
    }
}
#endif
