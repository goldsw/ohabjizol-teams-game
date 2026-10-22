using System;
using System.Collections.Generic;
using System.IO;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Updates each existing ending independently, preserving its artwork and theme.</summary>
public static class EndingSummaryBake
{
    const string PrefabPath = "Assets/Prefabs/InterviewRefresh/EndingBoard.prefab";
    static readonly string[] ScenePaths = {
        "Assets/Scenes/EndingScene.unity", "Assets/Scenes/GoodEndingScene.unity",
        "Assets/Scenes/NormalEndingScene.unity", "Assets/Scenes/BadEndingScene.unity"
    };

    public static string LastSummary { get; private set; }
    public static string LastBackupDirectory { get; private set; }

    [MenuItem("Tools/Interview/Apply Ending Summary")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Ending summary authoring requires idle Edit Mode.");
        LastSummary = "Not applied";
        var targets = new List<string> { PrefabPath };
        targets.AddRange(ScenePaths);
        foreach (var path in targets)
            if (!File.Exists(path)) throw new FileNotFoundException("Existing ending target required.", path);
        LastBackupDirectory = Path.Combine("Logs/EndingCalendarRefresh/Before",
            DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(LastBackupDirectory);
        foreach (var path in targets)
        {
            BackupDisk(path);
            if (File.Exists(path + ".meta")) BackupDisk(path + ".meta");
        }
        bool dirtyTarget = false;
        foreach (var path in ScenePaths)
        {
            var scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded || !scene.isDirty) continue;
            BackupDirtyScene(scene, path);
            dirtyTarget = true;
        }
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.scene.isDirty)
        {
            BackupDirtyScene(stage.scene, "PrefabStage/" + Path.GetFileName(stage.assetPath));
            dirtyTarget = true;
        }
        if (dirtyTarget || (stage != null && stage.assetPath == PrefabPath))
        {
            LastSummary = "Stopped before changes; save/close target or dirty Prefab Stage. Backup: " + LastBackupDirectory;
            throw new InvalidOperationException(LastSummary);
        }
        var active = SceneManager.GetActiveScene();
        var opened = new List<Scene>();
        int saved = 0;
        try
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var board = RequireSingleBoard(root.GetComponentsInChildren<EndingBoard>(true), PrefabPath);
                board.BakeSummaryUi();
                if (!PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success) || !success)
                    throw new IOException("Could not save ending prefab.");
                saved++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            foreach (var path in ScenePaths)
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    opened.Add(scene);
                }
                SceneManager.SetActiveScene(scene);
                var boards = new List<EndingBoard>();
                foreach (var sceneRoot in scene.GetRootGameObjects())
                    boards.AddRange(sceneRoot.GetComponentsInChildren<EndingBoard>(true));
                RequireSingleBoard(boards.ToArray(), path).BakeSummaryUi();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save " + path);
                saved++;
            }
            LastSummary = "Applied ending summary to " + saved + " targets. Backup: " + LastBackupDirectory;
        }
        catch (Exception error)
        {
            LastSummary = "Stopped after " + saved + " saved targets: " + error.Message + " Backup: " + LastBackupDirectory;
            throw;
        }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            for (int i = opened.Count - 1; i >= 0; i--)
                if (opened[i].IsValid() && opened[i].isLoaded && !opened[i].isDirty)
                    EditorSceneManager.CloseScene(opened[i], true);
        }
    }

    static EndingBoard RequireSingleBoard(EndingBoard[] boards, string path)
    {
        if (boards.Length != 1) throw new InvalidOperationException("Expected one existing EndingBoard: " + path);
        return boards[0];
    }

    static void BackupDisk(string path)
    {
        var destination = Path.Combine(LastBackupDirectory, "Disk", path);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.Copy(path, destination, false);
    }

    static void BackupDirtyScene(Scene scene, string path)
    {
        var destination = Path.Combine(LastBackupDirectory, "Dirty", path + ".unity");
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        if (!EditorSceneManager.SaveScene(scene, destination, true))
            throw new IOException("Could not back up dirty scene: " + path);
    }
}
