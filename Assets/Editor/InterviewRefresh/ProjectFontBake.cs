#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ohabjizol.Interview
{
    /// <summary>Explicit, font-reference-only project migration. Never run automatically.</summary>
    public static class ProjectFontBake
    {
        [Serializable]
        public sealed class Summary
        {
            public int prefabs, scenes, assets, textrefs, fontrefs, changedfiles;
            public string backupDirectory;
            public string error;
            public List<string> files = new List<string>();
        }

        public static Summary Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                || EditorApplication.isUpdating)
                throw new InvalidOperationException("ProjectFontBake requires idle Edit Mode.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Close Prefab Mode before migrating project fonts.");
            AssetDatabase.ImportAsset(UiKit.FontAssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Font font = AssetDatabase.LoadAssetAtPath<Font>(UiKit.FontAssetPath);
            if (!font) throw new InvalidOperationException("Canonical font missing: " + UiKit.FontAssetPath);
            var summary = new Summary
            {
                backupDirectory = "Logs/ResultUiRefresh/Before/Fonts/" +
                    DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")
            };
            Directory.CreateDirectory(summary.backupDirectory);
            Scene active = SceneManager.GetActiveScene();
            var diskBackups = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                // Snapshot unsaved state before prefab saves can propagate into open scenes.
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded || !scene.isDirty || !Eligible(scene.path, ".unity")) continue;
                    BackupDisk(scene.path, summary, diskBackups);
                    string snapshot = Path.Combine(summary.backupDirectory, "DirtyScenes", scene.path);
                    Directory.CreateDirectory(Path.GetDirectoryName(snapshot));
                    if (!EditorSceneManager.SaveScene(scene, snapshot, true))
                        throw new IOException("Cannot back up dirty scene: " + scene.path);
                }
                var paths = new SortedSet<string>(StringComparer.Ordinal);
                foreach (string guid in AssetDatabase.FindAssets("", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Eligible(path, ".prefab") || Eligible(path, ".unity") || Eligible(path, ".asset"))
                        paths.Add(path);
                }
                foreach (string path in paths)
                {
                    if (Eligible(path, ".prefab")) ApplyPrefab(path, font, summary, diskBackups);
                    else if (Eligible(path, ".unity")) ApplyScene(path, font, summary, diskBackups);
                    else ApplyAsset(path, font, summary, diskBackups);
                }
                return summary;
            }
            catch (Exception ex)
            {
                summary.error = ex.ToString();
                throw;
            }
            finally
            {
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                summary.changedfiles = summary.files.Count;
                File.WriteAllText("Logs/ResultUiRefresh/font-apply-summary.json", JsonUtility.ToJson(summary, true));
            }
        }

        static bool Eligible(string path, string extension)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)
                || !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return false;
            foreach (string part in path.Split('/'))
                if (part.Equals("Tests", StringComparison.OrdinalIgnoreCase)
                    || part.Equals("_Recovery", StringComparison.OrdinalIgnoreCase)
                    || part.Equals("Packages", StringComparison.OrdinalIgnoreCase)
                    || part.Equals("Library", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        static void BackupDisk(string path, Summary summary, HashSet<string> backups)
        {
            if (backups.Contains(path)) return;
            string destination = Path.Combine(summary.backupDirectory, "Disk", path);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(path, destination, false);
            if (File.Exists(path + ".meta")) File.Copy(path + ".meta", destination + ".meta", false);
            backups.Add(path);
        }

        static void ApplyPrefab(string path, Font font, Summary summary, HashSet<string> backups)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = ApplyHierarchy(root, font, summary, () => BackupDisk(path, summary, backups));
                if (!changed) return;
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
                if (!saved) throw new IOException("Cannot save prefab: " + path);
                summary.prefabs++;
                summary.files.Add(path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void ApplyScene(string path, Font font, Summary summary, HashSet<string> backups)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                    changed |= ApplyHierarchy(root, font, summary, () => BackupDisk(path, summary, backups));
                if (!changed) return;
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save scene: " + path);
                summary.scenes++;
                summary.files.Add(path);
            }
            finally
            {
                // A failed save leaves the scene open for recovery instead of discarding edits.
                if (opened && !scene.isDirty) EditorSceneManager.CloseScene(scene, true);
            }
        }

        static bool ApplyHierarchy(GameObject root, Font font, Summary summary, Action beforeChange)
        {
            bool changed = false;
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
                if (component) changed |= ApplyReferences(component, font, summary, beforeChange);
            return changed;
        }

        static void ApplyAsset(string path, Font font, Summary summary, HashSet<string> backups)
        {
            bool changed = false;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                // Imported images, meshes and generated objects are outside this migration.
                if (!(asset is ScriptableObject)) continue;
                if (!ApplyReferences(asset, font, summary, () => BackupDisk(path, summary, backups))) continue;
                AssetDatabase.SaveAssetIfDirty(asset);
                changed = true;
            }
            if (!changed) return;
            summary.assets++;
            summary.files.Add(path);
        }

        static bool ApplyReferences(Object target, Font font, Summary summary, Action beforeChange)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.GetIterator();
            bool changed = false;
            int fontrefs = 0, textrefs = 0;
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                Object current = property.objectReferenceValue;
                bool textFont = target is Text && property.propertyPath == "m_FontData.m_Font";
                bool fontProperty = current is Font || property.type == "PPtr<Font>"
                    || property.type == "PPtr<$Font>" || textFont;
                if (!fontProperty || current == font) continue;
                if (!changed) beforeChange();
                property.objectReferenceValue = font;
                changed = true;
                fontrefs++;
                if (textFont) textrefs++;
            }
            if (!changed) return false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
            summary.fontrefs += fontrefs;
            summary.textrefs += textrefs;
            return true;
        }
    }
}
#endif
