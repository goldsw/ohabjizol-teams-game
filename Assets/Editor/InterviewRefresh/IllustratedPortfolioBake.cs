#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Ohabjizol.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    /// <summary>Explicit authoring only; never opens or saves a scene.</summary>
    public static class IllustratedPortfolioBake
    {
        public const string CatalogPath = "Assets/Art/InterviewRefresh/PortfolioCatalog.asset";
        public const string PrefabFolder = "Assets/Prefabs/InterviewRefresh/Portfolio/Illustrated";
        const string ArtFolder = "Assets/Art/Portfolio/Director/";

        [MenuItem("Tools/Interview Refresh/Apply Illustrated Planning Portfolio")]
        public static void Apply()
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
                sprites[i] = LoadNativeSprite(states[i]);
                string path = CardPath(states[i]);
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
                if (!prefabs[i]) prefabs[i] = CreateCard(states[i], sprites[i], font);

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
                    if (entry == null || entry.Role != JobRole.Planning || entry.State != states[i]) continue;
                    if (found) throw new InvalidOperationException("Duplicate Planning catalog state: " + states[i]);
                    entry.Sprite = sprites[i];
                    entry.CardPrefab = prefabs[i];
                    found = true;
                }
                if (!found) entries.Add(new PortfolioSpriteCatalog.Entry
                {
                    Role = JobRole.Planning, State = states[i], Sprite = sprites[i], CardPrefab = prefabs[i]
                });
            }
            catalog.ConfigureEditor(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

        static string CardPath(PortfolioDisplayState state) => PrefabFolder + "/Planning" + state + ".prefab";

        static Sprite LoadNativeSprite(PortfolioDisplayState state)
        {
            string name = "Direc_" + state;
            string path = ArtFolder + name + ".aseprite";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            Sprite result = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var sprite = asset as Sprite;
                // Native Aseprite sprites may include atlas padding in rect.x/rect.y.
                if (!sprite || sprite.name != name || !Mathf.Approximately(sprite.rect.width, 198f)
                    || !Mathf.Approximately(sprite.rect.height, 140f)) continue;
                if (result) throw new InvalidOperationException("Ambiguous native 198x140 sprite: " + path);
                result = sprite;
            }
            if (!result) throw new InvalidOperationException("Expected one named native 198x140 sprite: " + path);
            return result;
        }

        static RectTransform CreateCard(PortfolioDisplayState state, Sprite sprite, Font font)
        {
            var root = new GameObject("Planning" + state, typeof(RectTransform), typeof(Image),
                typeof(IllustratedPortfolioCard));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                UiKit.Stretch(rect, 0f);
                var background = root.GetComponent<Image>();
                background.color = Color.clear;
                background.raycastTarget = false;
                var frame = UiKit.Image(rect, "ArtworkFrame", Color.white);
                frame.sprite = sprite;
                frame.type = Image.Type.Simple;
                frame.preserveAspect = true;
                frame.raycastTarget = false;
                var fitter = frame.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = 198f / 140f;
                var memo = UiKit.Text(frame.transform, "QualityMemo", 25, FontStyle.Normal, TextAnchor.UpperLeft);
                memo.font = font;
                memo.color = new Color32(34, 29, 25, 255);
                memo.raycastTarget = false;
                UiKit.Place(memo.rectTransform, .69f, .08f, .95f, .80f);
                root.GetComponent<IllustratedPortfolioCard>().ConfigureEditor(frame, memo, state);
                bool saved;
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, CardPath(state), out saved);
                if (!saved || !prefab)
                    throw new InvalidOperationException("Illustrated prefab save failed: " + CardPath(state));
                return prefab.GetComponent<RectTransform>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
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
