#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public static class TeamWindowBake
    {
        public static void Apply(string prefabPath)
        {
            if (Application.isPlaying || string.IsNullOrWhiteSpace(prefabPath)
                || !prefabPath.StartsWith("Assets/Prefabs/InterviewRefresh/", StringComparison.Ordinal)
                || !prefabPath.EndsWith(".prefab", StringComparison.Ordinal)
                || prefabPath.Contains("..") || prefabPath.Contains("\\"))
                throw new ArgumentException("Expected InterviewRefresh prefab asset path in EditMode.", nameof(prefabPath));
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath))
                throw new ArgumentException("Prefab missing: " + prefabPath, nameof(prefabPath));
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                InterviewScreen screen = root.GetComponentInChildren<InterviewScreen>(true);
                if (!screen) throw new InvalidOperationException("InterviewScreen missing: " + prefabPath);
                screen.BakeTeamWindow();
                var font = AssetDatabase.LoadAssetAtPath<Font>(Ohabjizol.UI.UiKit.FontAssetPath);
                if (!font || !EditorUtility.IsPersistent(font))
                    throw new InvalidOperationException("Persistent Korean UI font required.");
                var bindings = new SerializedObject(screen);
                var window = bindings.FindProperty("_teamWindow").objectReferenceValue as Component;
                var launcher = bindings.FindProperty("_teamLauncher").objectReferenceValue as Component;
                if (!window || !launcher) throw new InvalidOperationException("Team window bindings missing.");
                Normalize(window.gameObject, font);
                Normalize(launcher.gameObject, font);
                if (!PrefabUtility.SaveAsPrefabAsset(root, prefabPath))
                    throw new InvalidOperationException("Failed to save team window prefab: " + prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void Normalize(GameObject root, Font font)
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true)) text.font = font;
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                if (image.sprite && !EditorUtility.IsPersistent(image.sprite)) image.sprite = null;
        }
    }
}
#endif
