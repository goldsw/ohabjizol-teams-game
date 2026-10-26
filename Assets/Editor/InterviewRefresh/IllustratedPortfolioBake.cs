#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    /// <summary>Explicit authoring only; never opens or saves a scene.</summary>
    public static class IllustratedPortfolioBake
    {
        public const string CatalogPath = "Assets/Art/InterviewRefresh/PortfolioCatalog.asset";
        public const string PrefabFolder = "Assets/Prefabs/InterviewRefresh/Portfolio/Illustrated";
        [MenuItem("Tools/Interview Refresh/Apply Illustrated Planning Portfolio")]
        public static void Apply()
            => ApplyRole(JobRole.Planning, "Assets/Art/Portfolio/Director/",
                new[] { "Direc_Good", "Direc_Normal", "Direc_Bad" });

        [MenuItem("Tools/Interview Refresh/Apply Illustrated Graphics and Programming Portfolios")]
        public static void ApplyGraphicsAndProgramming()
        {
            ApplyRole(JobRole.Graphics, "Assets/Art/Portfolio/Graphics/",
                new[] { "Graphic_Good", "Graphic_Bormal", "Graphic_bad" });
            ApplyRole(JobRole.Programming, "Assets/Art/Portfolio/Programming/",
                new[] { "Pming_Good", "Pming_Normal", "Pming_Bad" });
        }

        static void ApplyRole(JobRole role, string artFolder, string[] assetNames)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Illustrated portfolio authoring requires Edit Mode.");
            var catalog = AssetDatabase.LoadAssetAtPath<PortfolioSpriteCatalog>(CatalogPath);
            if (!catalog) throw new InvalidOperationException("Existing portfolio catalog required: " + CatalogPath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(UiKit.FontAssetPath);
            if (!font) throw new InvalidOperationException("Persistent font required: " + UiKit.FontAssetPath);
            var states = new[] { PortfolioDisplayState.Good, PortfolioDisplayState.Normal, PortfolioDisplayState.Bad };
            var sprites = new Sprite[states.Length];
            var prefabs = new RectTransform[states.Length];
            // Validate all inputs and existing authored cards before creating anything.
            for (int i = 0; i < states.Length; i++)
            {
                sprites[i] = LoadNativeSprite(artFolder, assetNames[i]);
                string path = CardPath(role, states[i]);
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing)
                {
                    var marker = existing.GetComponent<IllustratedPortfolioCard>();
                    var rect = existing.GetComponent<RectTransform>();
                    if (!rect || !marker || !marker.HasAuthoredUi || marker.State != states[i]
                        || marker.ArtworkImage.sprite != sprites[i])
                        throw new InvalidOperationException("Existing illustrated prefab is invalid; preserving user work: " + path);
                    prefabs[i] = rect;
                }
                else if (File.Exists(path))
                    throw new InvalidOperationException("Existing prefab cannot be loaded; preserving: " + path);
            }
            EnsureFolder(PrefabFolder);
            for (int i = 0; i < states.Length; i++)
                if (!prefabs[i]) prefabs[i] = CreateCard(role, states[i], sprites[i], font);
                else prefabs[i] = CorrectCanvasBounds(role, states[i], sprites[i], prefabs[i]);

            // Copy entries so no existing entry object is modified while preparing the update.
            var entries = new List<PortfolioSpriteCatalog.Entry>();
            foreach (var entry in catalog.Entries)
                entries.Add(entry == null ? null : new PortfolioSpriteCatalog.Entry
                {
                    Role = entry.Role, State = entry.State, Sprite = entry.Sprite, CardPrefab = entry.CardPrefab
                });
            for (int i = 0; i < states.Length; i++)
            {
                bool found = false;
                foreach (var entry in entries)
                {
                    if (entry == null || entry.Role != role || entry.State != states[i]) continue;
                    if (found) throw new InvalidOperationException("Duplicate " + role + " catalog state: " + states[i]);
                    entry.Sprite = sprites[i];
                    entry.CardPrefab = prefabs[i];
                    found = true;
                }
                if (!found) entries.Add(new PortfolioSpriteCatalog.Entry
                {
                    Role = role, State = states[i], Sprite = sprites[i], CardPrefab = prefabs[i]
                });
            }
            catalog.ConfigureEditor(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

        static string CardPath(JobRole role, PortfolioDisplayState state) => PrefabFolder + "/" + role + state + ".prefab";

        static Sprite LoadNativeSprite(string artFolder, string name)
        {
            string path = artFolder + name + ".aseprite";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            Sprite result = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var sprite = asset as Sprite;
                // Native Aseprite sprites may include atlas padding in rect.x/rect.y.
                if (!sprite || sprite.name != name) continue;
                if (result) throw new InvalidOperationException("Ambiguous native sprite: " + path);
                result = sprite;
            }
            if (!result) throw new InvalidOperationException("Expected one named native sprite: " + path);
            return result;
        }

        static RectTransform CreateCard(JobRole role, PortfolioDisplayState state, Sprite sprite, Font font)
        {
            var root = new GameObject(role.ToString() + state, typeof(RectTransform), typeof(Image),
                typeof(IllustratedPortfolioCard));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                UiKit.Stretch(rect, 0f);
                var background = root.GetComponent<Image>();
                background.color = Color.clear;
                background.raycastTarget = false;
                var frame = (RectTransform)new GameObject("ArtworkFrame", typeof(RectTransform),
                    typeof(RectMask2D), typeof(AspectRatioFitter)).transform;
                frame.SetParent(rect, false);
                var fitter = frame.GetComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                var canvasSize = NativeCanvasSize(sprite);
                fitter.aspectRatio = canvasSize.x / canvasSize.y;
                var artwork = CreateCanvasArtwork(frame, sprite, canvasSize);
                var memo = UiKit.Text(frame, "QualityMemo", 25, FontStyle.Normal, TextAnchor.UpperLeft);
                memo.font = font;
                memo.color = new Color32(34, 29, 25, 255);
                memo.raycastTarget = false;
                UiKit.Place(memo.rectTransform, .69f, .08f, .95f, .80f);
                root.GetComponent<IllustratedPortfolioCard>().ConfigureEditor(artwork, memo, state);
                bool saved;
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, CardPath(role, state), out saved);
                if (!saved || !prefab)
                    throw new InvalidOperationException("Illustrated prefab save failed: " + CardPath(role, state));
                return prefab.GetComponent<RectTransform>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static Vector2 NativeCanvasSize(Sprite sprite)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as AsepriteImporter;
            return importer && importer.canvasSize.x > 0 && importer.canvasSize.y > 0
                ? (Vector2)importer.canvasSize : sprite.rect.size;
        }

        static Image CreateCanvasArtwork(RectTransform frame, Sprite sprite, Vector2 canvasSize)
        {
            var artwork = UiKit.Image(frame, "Artwork", Color.white);
            artwork.sprite = sprite;
            artwork.type = Image.Type.Simple;
            artwork.preserveAspect = false;
            artwork.raycastTarget = false;
            PlaceCanvasArtwork(artwork, canvasSize);
            return artwork;
        }

        static void PlaceCanvasArtwork(Image artwork, Vector2 canvasSize)
        {
            var sprite = artwork.sprite;
            var rect = artwork.rectTransform;
            // Native assets use a Canvas/BottomCenter pivot, including cels outside the canvas.
            rect.anchorMin = new Vector2(.5f - sprite.pivot.x / canvasSize.x, 0f);
            rect.anchorMax = new Vector2(.5f + (sprite.rect.width - sprite.pivot.x) / canvasSize.x, 0f);
            rect.pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            var fitter = artwork.GetComponent<AspectRatioFitter>();
            if (!fitter) fitter = artwork.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        static RectTransform CorrectCanvasBounds(JobRole role, PortfolioDisplayState state, Sprite sprite, RectTransform prefab)
        {
            var marker = prefab.GetComponent<IllustratedPortfolioCard>();
            var size = NativeCanvasSize(sprite);
            var frame = prefab.Find("ArtworkFrame") as RectTransform;
            var fitter = frame ? frame.GetComponent<AspectRatioFitter>() : null;
            bool outside = !Mathf.Approximately(size.x, sprite.rect.width)
                || !Mathf.Approximately(size.y, sprite.rect.height);
            if (!outside && fitter && Mathf.Approximately(fitter.aspectRatio, size.x / size.y)) return prefab;
            var path = CardPath(role, state);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                marker = root.GetComponent<IllustratedPortfolioCard>();
                frame = root.transform.Find("ArtworkFrame") as RectTransform;
                if (!frame) throw new InvalidOperationException("Missing artwork frame: " + path);
                fitter = frame.GetComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = size.x / size.y;
                if (!frame.GetComponent<RectMask2D>()) frame.gameObject.AddComponent<RectMask2D>();
                var artwork = marker.ArtworkImage;
                if (artwork.transform == frame)
                {
                    var old = artwork;
                    artwork = CreateCanvasArtwork(frame, sprite, size);
                    artwork.transform.SetAsFirstSibling();
                    marker.ConfigureEditor(artwork, marker.MemoText, state);
                    UnityEngine.Object.DestroyImmediate(old);
                }
                else PlaceCanvasArtwork(artwork, size);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (!saved) throw new InvalidOperationException("Canvas correction failed: " + path);
                return saved.GetComponent<RectTransform>();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            if (slash < 0 || !path.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid illustrated prefab folder: " + path);
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            if (Directory.Exists(path))
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            else AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
            if (!AssetDatabase.IsValidFolder(path))
                throw new InvalidOperationException("Could not register illustrated prefab folder: " + path);
        }
    }
}
#endif
