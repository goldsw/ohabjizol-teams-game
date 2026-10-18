#if UNITY_EDITOR
using System;
using System.IO;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One-time, editor-only authoring of editable public portfolio documents.</summary>
public static class PortfolioUiBake
{
    public const string FontAssetPath = "Assets/Art/InterviewRefresh/Fonts/NotoSansKR.ttf";
    public const string CatalogAssetPath = "Assets/Art/InterviewRefresh/PortfolioCatalog.asset";
    public const string PrefabFolder = "Assets/Prefabs/InterviewRefresh/Portfolio";
    static readonly Color Ink = new Color32(53, 43, 36, 255);
    static readonly Color Paper = new Color32(255, 224, 182, 255);
    static readonly Color Orange = new Color32(244, 151, 67, 255);

    [MenuItem("Tools/Interview Refresh/Build Missing Portfolio Assets")]
    public static void BuildAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Portfolio authoring requires Edit Mode.");
        EnsureFolder(Path.GetDirectoryName(FontAssetPath).Replace('\\', '/'));
        AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceSynchronousImport);
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontAssetPath);
        if (font == null)
            throw new InvalidOperationException("Persistent Korean font missing or not imported: " + FontAssetPath);
        EnsureFolder(PrefabFolder);
        EnsureFolder("Assets/Art/InterviewRefresh");
        var catalog = AssetDatabase.LoadAssetAtPath<PortfolioSpriteCatalog>(CatalogAssetPath);
        if (catalog == null)
        {
            if (File.Exists(CatalogAssetPath))
                throw new InvalidOperationException("Existing catalog could not be loaded; preserving it.");
            catalog = ScriptableObject.CreateInstance<PortfolioSpriteCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
        }
        bool changed = false;
        foreach (JobRole role in new[] { JobRole.Planning, JobRole.Programming, JobRole.Graphics })
        foreach (PortfolioDisplayState state in new[] { PortfolioDisplayState.Good, PortfolioDisplayState.Normal, PortfolioDisplayState.Bad })
        {
            // Existing entries and hand-edited prefabs always win, even when their paths differ.
            if (catalog.GetCard(role, state) != null) continue;
            string path = PrefabFolder + "/" + role + state.ToString() + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                if (File.Exists(path))
                    throw new InvalidOperationException("Existing prefab could not be loaded; preserving " + path);
                GameObject root = MakeCard(role, state, font);
                try
                {
                    bool saved;
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out saved);
                    if (!saved || !File.Exists(path))
                        throw new InvalidOperationException("Portfolio prefab save failed: " + path);
                    if (prefab == null)
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    }
                    if (prefab == null)
                        throw new InvalidOperationException("Portfolio prefab saved but could not be imported: " + path);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            RectTransform card = prefab == null ? null : prefab.GetComponent<RectTransform>();
            if (card == null) throw new InvalidOperationException("Portfolio prefab must have RectTransform: " + path);
            catalog.SetCard(role, state, card);
            changed = true;
        }
        if (changed) EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/').TrimEnd('/');
        if (AssetDatabase.IsValidFolder(path)) return;
        if (path != "Assets")
        {
            int separator = path.LastIndexOf('/');
            if (separator < 0 || !path.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid portfolio authoring folder: " + path);
            string parent = path.Substring(0, separator);
            EnsureFolder(parent);
            if (!Directory.Exists(path))
                AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
        // Filesystem folders created by another authoring tool may not yet have imported metadata.
        if (!AssetDatabase.IsValidFolder(path) && Directory.Exists(path))
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }
        if (!AssetDatabase.IsValidFolder(path))
            throw new InvalidOperationException("Portfolio folder is not recognized by AssetDatabase: " + path);
    }

    static GameObject MakeCard(JobRole role, PortfolioDisplayState state, Font font)
    {
        var root = new GameObject("Portfolio Document", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 620);
        root.GetComponent<Image>().color = Paper;
        root.GetComponent<Image>().raycastTarget = false;
        Color header = state == PortfolioDisplayState.Good ? new Color32(64, 136, 214, 255)
            : state == PortfolioDisplayState.Normal ? new Color32(245, 200, 62, 255)
            : new Color32(218, 77, 65, 255);
        Box(rect, "Header", 0, 0, 500, 108, header);
        string label = role == JobRole.Planning ? "기획" : role == JobRole.Programming ? "프로그래머" : "아트";
        TextAt(rect, font, "Title", "포트폴리오", 28, 18, 340, 38, 30);
        TextAt(rect, font, "Role", label + " · 공개 프로젝트", 28, 62, 444, 28, 18);
        TextAt(rect, font, "Project", "샘플 프로젝트: 작은 정원", 28, 130, 444, 34, 23);
        TextAt(rect, font, "Intro", "정원을 가꾸며 이웃과 이야기를 나누는 게임", 28, 173, 444, 42, 16);
        if (role == JobRole.Planning) Planning(rect, font);
        else if (role == JobRole.Programming) Programming(rect, font);
        else Art(rect, font);
        Box(rect, "FooterRule", 28, 555, 444, 2, Orange);
        TextAt(rect, font, "Footer", "공개 예시 자료 · 문서 내용은 편집 가능", 28, 574, 444, 26, 15);
        return root;
    }

    static void Planning(RectTransform parent, Font font)
    {
        Section(parent, font, "Goal", "목표", "씨앗을 심고 정원을 완성하기", 224);
        Section(parent, font, "Rule", "규칙", "물을 주면 성장 · 꽃을 모아 이웃에게 선물", 316);
        TextAt(parent, font, "FlowTitle", "플레이 흐름", 28, 420, 444, 30, 19);
        string[] steps = { "심기", "돌보기", "선물하기" };
        for (int i = 0; i < steps.Length; i++)
        {
            float x = 28 + i * 158;
            Box(parent, "FlowBlock", x, 462, 128, 57, Orange);
            TextAt(parent, font, "FlowLabel", steps[i], x + 8, 477, 112, 28, 17);
            if (i < 2) TextAt(parent, font, "Arrow", "→", x + 133, 477, 24, 28, 18);
        }
    }

    static void Programming(RectTransform parent, Font font)
    {
        Box(parent, "CodePanel", 28, 224, 444, 190, new Color32(255, 242, 220, 255));
        TextAt(parent, font, "CodeHeading", "정원 성장 로직", 42, 236, 415, 28, 19);
        TextAt(parent, font, "Code", "void Water(Plant plant)\n{\n    plant.Grow();\n    garden.Refresh();\n}", 46, 277, 405, 128, 17);
        TextAt(parent, font, "PipelineTitle", "실행 흐름", 28, 431, 444, 28, 19);
        string[] steps = { "입력", "처리", "화면" };
        for (int i = 0; i < steps.Length; i++)
        {
            float x = 28 + i * 158;
            Box(parent, "PipelineBlock", x, 475, 128, 53, Orange);
            TextAt(parent, font, "PipelineLabel", steps[i], x + 12, 487, 105, 28, 17);
            if (i < 2) TextAt(parent, font, "Arrow", "→", x + 133, 487, 24, 28, 18);
        }
    }

    static void Art(RectTransform parent, Font font)
    {
        TextAt(parent, font, "PaletteTitle", "정원 색상 팔레트", 28, 224, 444, 30, 19);
        Color[] colors = { new Color32(78, 128, 84, 255), new Color32(140, 184, 99, 255),
            new Color32(250, 181, 81, 255), new Color32(230, 113, 115, 255), new Color32(112, 169, 207, 255) };
        for (int i = 0; i < colors.Length; i++) Box(parent, "PaletteSwatch", 28 + i * 90, 268, 76, 48, colors[i]);
        TextAt(parent, font, "SamplesTitle", "픽셀 오브젝트 스케치", 28, 339, 444, 30, 19);
        Box(parent, "SampleArea", 28, 382, 444, 135, new Color32(255, 242, 220, 255));
        PixelShape(parent, "Flower", 67, 400, new[] { "00100", "01110", "00100", "00100", "01100" }, colors[3], colors[0]);
        PixelShape(parent, "Tree", 211, 400, new[] { "00100", "01110", "11111", "00100", "00100" }, colors[1], colors[0]);
        PixelShape(parent, "Pot", 355, 400, new[] { "01010", "00100", "11111", "01110", "01110" }, colors[2], colors[0]);
    }

    static void PixelShape(RectTransform parent, string name, float x, float y, string[] rows, Color top, Color bottom)
    {
        for (int row = 0; row < rows.Length; row++)
        for (int col = 0; col < rows[row].Length; col++)
            if (rows[row][col] == '1') Box(parent, name + "Pixel", x + col * 18, y + row * 18, 18, 18, row < 3 ? top : bottom);
    }

    static void Section(RectTransform parent, Font font, string name, string heading, string body, float y)
    {
        Box(parent, name + "Panel", 28, y, 444, 78, new Color32(255, 242, 220, 255));
        TextAt(parent, font, name + "Heading", heading, 42, y + 8, 410, 28, 19);
        TextAt(parent, font, name + "Body", body, 42, y + 42, 410, 28, 16);
    }

    static RectTransform Element(RectTransform parent, string name, float x, float y, float width, float height)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    static void Box(RectTransform parent, string name, float x, float y, float width, float height, Color color)
    {
        var rect = Element(parent, name, x, y, width, height);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = null;
        image.color = color;
        image.raycastTarget = false;
    }

    static void TextAt(RectTransform parent, Font font, string name, string value, float x, float y, float width, float height, int size)
    {
        var rect = Element(parent, name, x, y, width, height);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = Ink;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
    }
}
#endif