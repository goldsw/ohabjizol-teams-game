using System;
using UnityEditor;
using UnityEngine;
namespace Ohabjizol.Interview
{
    public static class DownloadFeedbackBake
    {
        public static void Apply(string prefabPath)
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires EditMode.");
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var panels = root.GetComponentsInChildren<DownloadPanelView>(true);
                if (panels.Length == 0) throw new InvalidOperationException("DownloadPanelView missing: " + prefabPath);
                foreach (var panel in panels) panel.PreflightItemsEditor();
                foreach (var panel in panels) panel.EnsureItemsEditor();
                if (!PrefabUtility.SaveAsPrefabAsset(root, prefabPath)) throw new InvalidOperationException("Could not save: " + prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
