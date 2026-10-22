using System;
using System.Collections.Generic;
using System.IO;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ResultUiRefreshBake
{
    const string BoardPath = "Assets/Prefabs/InterviewRefresh/ResultBoard.prefab";
    const string TemplatePath = "Assets/Prefabs/InterviewRefresh/ResultProfileTemplate.prefab";

    [MenuItem("Tools/Interview/Apply Result UI Refresh")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Result UI refresh requires idle Edit mode.");
        var paths = new List<string>
        {
            "Assets/Scenes/ResultScene.unity",
            "Assets/Scenes/" + ResultSceneRoute.GoodSceneName + ".unity",
            "Assets/Scenes/" + ResultSceneRoute.NormalSceneName + ".unity",
            "Assets/Scenes/" + ResultSceneRoute.BadSceneName + ".unity"
        };
        foreach (string path in paths)
            if (!File.Exists(path)) throw new InvalidOperationException("Required result scene missing: " + path);
        if (!File.Exists(BoardPath)) throw new InvalidOperationException("ResultBoard prefab missing.");
        var endings = new List<string> { "Assets/Scenes/EndingScene.unity" };
        foreach (EndingKind kind in new[] { EndingKind.Good, EndingKind.Normal, EndingKind.Bad })
            endings.Add("Assets/Scenes/" + EndingSceneRoute.For(kind) + ".unity");
        foreach (var path in endings) if (File.Exists(path) && !paths.Contains(path)) paths.Add(path);

        string backup = "Logs/ResultUiRefresh/Before/Results/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(backup);
        BackupFile(BoardPath, backup);
        if (File.Exists(TemplatePath)) BackupFile(TemplatePath, backup);
        // Preserve both on-disk files and unsaved in-memory target scenes before writing any target.
        foreach (var path in paths)
        {
            BackupFile(path, backup);
            var loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded && loaded.isDirty)
            {
                string dirtyPath = backup + "/" + Path.GetFileNameWithoutExtension(path) + ".dirty.unity";
                if (!EditorSceneManager.SaveScene(loaded, dirtyPath, true))
                    throw new InvalidOperationException("Could not back up dirty scene: " + path);
            }
        }
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && (stage.assetPath == BoardPath || stage.assetPath == TemplatePath) && stage.scene.isDirty)
        {
            if (!EditorSceneManager.SaveScene(stage.scene, backup + "/DirtyPrefabStage.unity", true))
                throw new InvalidOperationException("Could not back up dirty prefab stage.");
            throw new InvalidOperationException("Dirty result prefab stage backed up to " + backup + "; save the stage before Apply.");
        }

        var active = SceneManager.GetActiveScene();
        var opened = new List<Scene>();
        try
        {
            var prefab = PrefabUtility.LoadPrefabContents(BoardPath);
            try
            {
                var boards = prefab.GetComponentsInChildren<ResultBoard>(true);
                if (boards.Length != 1) throw new InvalidOperationException("Expected exactly one ResultBoard in prefab.");
                boards[0].BakeInspectionUi();
                MarkHierarchy(boards[0]);
                if (PrefabUtility.SaveAsPrefabAsset(prefab, BoardPath) == null)
                    throw new InvalidOperationException("Could not save ResultBoard prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            foreach (string path in paths)
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    opened.Add(scene);
                }
                bool resultScene = !endings.Contains(path);
                int count = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (resultScene)
                    {
                        foreach (var board in root.GetComponentsInChildren<ResultBoard>(true))
                        { board.BakeInspectionUi(); MarkHierarchy(board); count++; }
                    }
                    else
                    {
                        foreach (var board in root.GetComponentsInChildren<EndingBoard>(true))
                        { MoveEndingButton(board); count++; }
                    }
                }
                if (count != 1) throw new InvalidOperationException("Expected one board in " + path + "; found " + count);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Could not save " + path);
            }
            Debug.Log("Result UI refresh applied. Backups: " + backup);
        }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            for (int i = opened.Count - 1; i >= 0; i--)
                if (opened[i].IsValid() && opened[i].isLoaded) EditorSceneManager.CloseScene(opened[i], true);
        }
    }

    static void MoveEndingButton(EndingBoard board)
    {
        var data = new SerializedObject(board);
        var property = data.FindProperty("newRunButton");
        var button = property == null ? null : property.objectReferenceValue as UnityEngine.UI.Button;
        if (button == null) throw new InvalidOperationException("Ending main button missing.");
        var rect = (RectTransform)button.transform;
        rect.anchorMin = new Vector2(.75f, .025f);
        rect.anchorMax = new Vector2(.975f, board.HasSummaryUi ? .085f : .105f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rect);
        if (PrefabUtility.IsPartOfPrefabInstance(rect))
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
    }

    static void MarkHierarchy(Component board)
    {
        foreach (var component in board.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorUtility.SetDirty(component.gameObject);
            if (PrefabUtility.IsPartOfPrefabInstance(component.gameObject))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component.gameObject);
        }
    }

    static void BackupFile(string path, string folder)
    {
        File.Copy(path, folder + "/" + Path.GetFileName(path), false);
        if (File.Exists(path + ".meta")) File.Copy(path + ".meta", folder + "/" + Path.GetFileName(path) + ".meta", false);
    }
}
