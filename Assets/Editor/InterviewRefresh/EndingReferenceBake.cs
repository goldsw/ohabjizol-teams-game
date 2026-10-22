#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ohabjizol.Editor.InterviewRefresh
{
    public static class EndingReferenceBake
    {
        public static void Apply()
        {
            Apply(false);
        }

        public static void ApplyStarVisibility()
        {
            Apply(true);
        }

        static void Apply(bool starsOnly)
        {
            RequireIdle();
            string[] names = starsOnly ? new[] { "NormalEndingScene", "BadEndingScene" }
                : new[] { "GoodEndingScene", "NormalEndingScene", "BadEndingScene" };
            EndingKind[] kinds = starsOnly ? new[] { EndingKind.Normal, EndingKind.Bad }
                : new[] { EndingKind.Good, EndingKind.Normal, EndingKind.Bad };
            var paths = new string[names.Length];
            for (int i = 0; i < names.Length; i++) paths[i] = ResolveScene(names[i]);
            string project = Directory.GetParent(Application.dataPath).FullName;
            string backup = Path.Combine(project, "Logs", "EndingDesign", "Before", DateTime.Now.ToString("yyyyMMdd_HHmmss_fffffff"));
            Directory.CreateDirectory(backup);
            for (int i = 0; i < paths.Length; i++)
                File.Copy(Path.Combine(project, paths[i]), Path.Combine(backup, names[i] + ".unity"), false);
            // Save copies, retaining dirty state, scene paths and open scene ordering.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || !scene.isDirty) continue;
                string copy = Path.Combine(backup, "LoadedDirty_" + i + "_" + (string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name) + ".unity");
                if (!EditorSceneManager.SaveScene(scene, copy, true))
                    throw new IOException("Cannot back up dirty scene: " + scene.path);
            }
            var active = SceneManager.GetActiveScene();
            var owned = new List<Scene>();
            try
            {
                // Validate every board before changing any scene.
                var scenes = new Scene[paths.Length];
                var boards = new EndingBoard[paths.Length];
                for (int i = 0; i < paths.Length; i++)
                {
                    scenes[i] = SceneManager.GetSceneByPath(paths[i]);
                    if (!scenes[i].IsValid() || !scenes[i].isLoaded)
                    {
                        scenes[i] = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Additive);
                        owned.Add(scenes[i]);
                    }
                    var found = new List<EndingBoard>();
                    foreach (var root in scenes[i].GetRootGameObjects()) found.AddRange(root.GetComponentsInChildren<EndingBoard>(true));
                    if (found.Count != 1) throw new InvalidOperationException(paths[i] + " requires exactly one EndingBoard; found " + found.Count);
                    boards[i] = found[0];
                }
                for (int i = 0; i < paths.Length; i++)
                {
                    RequireIdle();
                    EditorSceneManager.MarkSceneDirty(scenes[i]);
                    if (starsOnly) boards[i].BakeVisibleStarSlotsEditor();
                    else boards[i].BakeReferenceLayoutEditor(kinds[i]);
                    if (!EditorSceneManager.SaveScene(scenes[i])) throw new IOException("Cannot save " + paths[i]);
                }
                Debug.Log("Ending reference layouts saved. Backup: " + backup);
            }
            finally
            {
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                foreach (var scene in owned)
                    if (scene.IsValid() && scene.isLoaded && !scene.isDirty) EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void RequireIdle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Ending reference bake requires idle Edit Mode.");
        }

        static string ResolveScene(string name)
        {
            string match = null;
            foreach (string guid in AssetDatabase.FindAssets(name + " t:Scene", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(Path.GetFileNameWithoutExtension(path), name, StringComparison.Ordinal)) continue;
                if (match != null) throw new InvalidOperationException("Ambiguous ending scene: " + name);
                match = path;
            }
            if (match == null) throw new FileNotFoundException("Ending scene missing: " + name);
            return match;
        }
    }
}
#endif
