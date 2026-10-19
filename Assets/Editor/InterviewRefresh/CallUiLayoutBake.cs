#if UNITY_EDITOR
using System;
using System.IO;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ohabjizol.EditorTools
{
    public static class CallUiLayoutBake
    {
        [Serializable]
        sealed class Report
        {
            public bool success;
            public string status;
            public int screens;
            public int teamRows;
            public int callRows;
            public string error;
        }
        [MenuItem("Ohabjizol/Interview/Apply Call UI Layout")]
        public static void ApplyToCurrentScene()
        {
            var report = new Report { status = "failed" };
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Call UI Layout");
            try
            {
                if (Application.isPlaying) throw new InvalidOperationException("EditMode required.");
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                InterviewScreen target = null;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var screen in root.GetComponentsInChildren<InterviewScreen>(true))
                    {
                        if (target) throw new InvalidOperationException("Multiple InterviewScreen components in active scene.");
                        target = screen;
                    }
                if (!target) throw new InvalidOperationException("InterviewScreen required in active scene.");
                target.BakeCallUiLayoutEditor();
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                report.success = true; report.status = "applied_unsaved";
                report.screens = 1; report.teamRows = 6; report.callRows = 1;
            }
            catch (Exception ex)
            {
                Undo.RevertAllDownToGroup(group);
                report.error = ex.ToString();
                Debug.LogException(ex);
            }
            finally
            {
                var directory = Path.GetFullPath("Logs/CallImeEnding");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "ui-apply.json"), JsonUtility.ToJson(report, true));
            }
        }
    }
}
#endif
