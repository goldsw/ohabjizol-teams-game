#if UNITY_EDITOR
using System;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEngine;

namespace Ohabjizol.EditorTools
{
    /// <summary>Scoped stats migration; does not invoke any full-screen/document rebuild.</summary>
    public static class InterviewStatsBake
    {
        /// <summary>Applies HUD stars and patches its referenced PDF prefab separately.</summary>
        public static void Apply(string prefabPath)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stats bake requires EditMode.");
            if (string.IsNullOrWhiteSpace(prefabPath) || !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("An existing prefab path is required.", nameof(prefabPath));
            Font font = AssetDatabase.LoadAssetAtPath<Font>(DocumentsUiBake.FontPath);
            if (!font) throw new InvalidOperationException("Persistent UI font missing: " + DocumentsUiBake.FontPath);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var screen = root.GetComponentInChildren<InterviewScreen>(true);
                var viewer = root.GetComponentInChildren<PdfDocumentViewer>(true);
                if (!screen && !viewer) throw new InvalidOperationException("InterviewScreen or PdfDocumentViewer required.");
                if (screen)
                {
                    var serialized = new SerializedObject(screen);
                    var template = serialized.FindProperty("_viewerPrefab").objectReferenceValue as PdfDocumentViewer;
                    if (!template) throw new InvalidOperationException("PDF viewer template reference missing.");
                    string viewerPath = AssetDatabase.GetAssetPath(template);
                    if (string.IsNullOrEmpty(viewerPath) || viewerPath == prefabPath)
                        throw new InvalidOperationException("A separate persistent PDF viewer prefab is required.");
                    screen.BakeApplicantStatsUi(font);
                    ApplyViewer(viewerPath, font);
                }
                if (viewer) viewer.BakeApplicantStatsUi(font);
                if (!PrefabUtility.SaveAsPrefabAsset(root, prefabPath))
                    throw new InvalidOperationException("Could not save stats prefab: " + prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void ApplyViewer(string prefabPath, Font font)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var viewer = root.GetComponentInChildren<PdfDocumentViewer>(true);
                if (!viewer) throw new InvalidOperationException("PDF viewer missing: " + prefabPath);
                viewer.BakeApplicantStatsUi(font);
                if (!PrefabUtility.SaveAsPrefabAsset(root, prefabPath))
                    throw new InvalidOperationException("Could not save PDF stats prefab: " + prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
#endif