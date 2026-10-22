#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public static class TeamProfileNotepadBake
    {
        const string ScenePath = "Assets/Scenes/InterviewScene.unity";
        const string ViewerPath = "Assets/Prefabs/InterviewRefresh/Documents/PdfViewer.prefab";
        [Serializable] sealed class Report
        {
            public string status, error, background;
            public int teamSlots, sceneWindows, viewerWindows;
            public bool notepadInput, lowerStatsActive;
        }

        [MenuItem("Ohabjizol/Interview/Apply Team Profiles and Notepad")]
        public static void ApplyToCurrentScene()
        {
            var report = new Report();
            int undo = -1;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                    throw new InvalidOperationException("Requires idle EditMode.");
                var scene = SceneManager.GetSceneByPath(ScenePath);
                if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open InterviewScene before applying.");
                var screens = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<InterviewScreen>(true)).ToArray();
                if (screens.Length != 1) throw new InvalidOperationException("Expected one InterviewScreen.");
                var screen = screens[0];
                if (!InterviewScreen.LoadTeamProfileSpriteEditor() ||
                    !AssetDatabase.LoadAssetAtPath<ApplicantVisualCatalog>("Assets/Art/Applicants/ApplicantVisualCatalog.asset") ||
                    !AssetDatabase.LoadAssetAtPath<Font>(Ohabjizol.UI.UiKit.FontAssetPath) ||
                    !AssetDatabase.LoadAssetAtPath<GameObject>(ViewerPath))
                    throw new InvalidOperationException("Required profile Sprite, visual catalog, canonical UI font or PDF viewer prefab missing.");
                Undo.IncrementCurrentGroup();
                undo = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Apply team profiles and notepad");
                screen.BakeTeamProfilesEditor();
                screen.BakeNotepadWindowEditor();
                var windows = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WindowToolbar>(true)).ToArray();
                foreach (var window in windows) window.EnsureControlsEditor();
                var note = screen.NotepadWindow;
                if (!note || !note.HasAuthoredInput) throw new InvalidOperationException("Notepad input not authored.");
                var slots = screen.TeamWindow.GetComponentsInChildren<InterviewTeamSlotView>(true);
                if (slots.Length != 6 || slots.Any(s => !s.HasAuthoredPortrait))
                    throw new InvalidOperationException("Expected six complete team profile slots.");
                var stats = new SerializedObject(screen).FindProperty("_applicantStats").objectReferenceValue as ApplicantStatsView;
                if (!stats || stats.gameObject.activeSelf) throw new InvalidOperationException("Legacy corner stats must remain referenced and hidden.");
                report.teamSlots = slots.Length;
                report.sceneWindows = windows.Length;
                report.notepadInput = note.HasAuthoredInput;
                report.lowerStatsActive = stats.gameObject.activeSelf;
                var background = screen.GetComponentsInChildren<Image>(true).SingleOrDefault(i => i.name == "Background" &&
                    AssetDatabase.GetAssetPath(i.sprite) == "Assets/Resources/Interview/Discord_total.png");
                if (!background || !background.enabled) throw new InvalidOperationException("Discord background reference changed or disabled.");
                report.background = AssetDatabase.GetAssetPath(background.sprite);
                report.viewerWindows = RepairViewerControls();
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undo);
                report.status = "applied-dirty";
            }
            catch (Exception error)
            {
                if (undo >= 0) Undo.RevertAllDownToGroup(undo);
                report.status = undo < 0 ? "preflight-failed" : "rolled-back";
                report.error = error.ToString();
                throw;
            }
            finally
            {
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/TeamProfileNotepad/apply.json"));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
            }
        }

        static int RepairViewerControls()
        {
            var root = PrefabUtility.LoadPrefabContents(ViewerPath);
            try
            {
                var windows = root.GetComponentsInChildren<WindowToolbar>(true);
                if (windows.Length != 1) throw new InvalidOperationException("Expected one PDF viewer window.");
                foreach (var window in windows) window.EnsureControlsEditor();
                if (!PrefabUtility.SaveAsPrefabAsset(root, ViewerPath)) throw new InvalidOperationException("PDF viewer prefab save failed.");
                return windows.Length;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
#endif
