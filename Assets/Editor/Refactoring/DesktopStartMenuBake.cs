#if UNITY_EDITOR
using System;
using Ohabjizol.Presentation;
using UnityEditor;
using UnityEngine;

namespace Ohabjizol.Editor.Refactoring
{
    public static class DesktopStartMenuBake
    {
        public const string DefaultPrefabPath = "Assets/Prefabs/InterviewRefresh/Desktop.prefab";
        public static void Apply() { Apply(DefaultPrefabPath); }
        public static void Apply(string prefabPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Start menu bake requires Edit Mode.");
            if (string.IsNullOrWhiteSpace(prefabPath)) throw new ArgumentException("Prefab path is required.", nameof(prefabPath));
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                var desktop = contents.GetComponentInChildren<DesktopView>(true);
                if (desktop == null) throw new InvalidOperationException("DesktopView is missing from " + prefabPath);
                desktop.AuthorStartMenu();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath, out bool success);
                if (!success) throw new InvalidOperationException("Could not save Start menu to " + prefabPath);
            }
            finally
            {
                if (contents != null) PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
#endif
