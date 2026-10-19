#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Ohabjizol.Interview
{
    public static class DownloadPanelItemBake
    {
        const string ScenePath = "Assets/Scenes/InterviewScene.unity";
        [Serializable] sealed class ItemReport
        {
            public string kind, button, fill, icon, download, check;
            public bool HasAuthoredUi;
        }
        [Serializable] sealed class Report
        {
            public string status, error;
            public bool HasAuthoredUi;
            public int count;
            public List<ItemReport> items = new List<ItemReport>();
        }
        [MenuItem("Ohabjizol/Interview/Apply Download Items")]
        public static void ApplyToCurrentScene()
        {
            var report = new Report(); int undo = -1;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                    throw new InvalidOperationException("Requires idle EditMode.");
                var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s => s.isLoaded && s.path == ScenePath).ToArray();
                if (scenes.Length != 1) throw new InvalidOperationException("Expected already loaded InterviewScene.");
                var scene = scenes[0];
                var screens = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<InterviewScreen>(true)).ToArray();
                if (screens.Length != 1) throw new InvalidOperationException("Expected one InterviewScreen.");
                var panel = new SerializedObject(screens[0]).FindProperty("_downloadPanel").objectReferenceValue as DownloadPanelView;
                if (!panel || panel.gameObject.scene != scene || !panel.transform.IsChildOf(screens[0].transform))
                    throw new InvalidOperationException("Invalid InterviewScreen download panel reference.");
                panel.PreflightItemsEditor();
                Undo.IncrementCurrentGroup(); undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Apply Download Items");
                panel.EnsureItemsEditor();
                panel.PreflightItemsEditor();
                report.HasAuthoredUi = panel.HasAuthoredUi; report.count = panel.Items.Count;
                foreach (var item in panel.Items)
                {
                    var so = new SerializedObject(item);
                    report.items.Add(new ItemReport { kind = item.Kind.ToString(), button = Hierarchy(item.Button),
                        fill = Hierarchy(so.FindProperty("_fill").objectReferenceValue as Component),
                        icon = Hierarchy(so.FindProperty("_statusIcon").objectReferenceValue as Component),
                        download = Source(so.FindProperty("_downloadSprite").objectReferenceValue),
                        check = Source(so.FindProperty("_checkSprite").objectReferenceValue), HasAuthoredUi = item.HasAuthoredUi });
                }
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undo); report.status = "applied-dirty";
            }
            catch (Exception e)
            {
                if (undo >= 0) Undo.RevertAllDownToGroup(undo);
                report.status = undo >= 0 ? "rolled-back" : "preflight-failed"; report.error = e.ToString();
                throw;
            }
            finally
            {
                try
                {
                    var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/DownloadItemRefactor/apply.json"));
                    Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(report, true));
                }
                catch (Exception e) { Debug.LogError("Download item report failed: " + e.Message); }
            }
        }
        static string Source(UnityEngine.Object value)
        {
            if (!value) return null;
            string guid; long id;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out guid, out id);
            return AssetDatabase.GetAssetPath(value) + "#" + value.name + " (" + guid + ":" + id + ")";
        }
        static string Hierarchy(Component value)
        {
            if (!value) return null;
            var path = value.name; for (var t = value.transform.parent; t; t = t.parent) path = t.name + "/" + path;
            return path;
        }
    }
}
#endif
