using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ResultPresentationBake
{
    const string Folder = "Assets/Scenes/";
    static readonly EndingKind[] Kinds = { EndingKind.Good, EndingKind.Normal, EndingKind.Bad };

    [MenuItem("Tools/Interview/Apply Result Presentation")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before result migration.");
        var paths = new List<string> { Folder + "ResultScene.unity", Folder + "EndingScene.unity" };
        foreach (var kind in Kinds) paths.Add(Folder + EndingSceneRoute.For(kind) + ".unity");
        foreach (string path in paths)
        {
            var scene = SceneManager.GetSceneByPath(path);
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                throw new InvalidOperationException("Result migration refuses an open dirty target scene: " + path);
        }
        if (!File.Exists(paths[0]) || !File.Exists(paths[1]))
            throw new InvalidOperationException("ResultScene and legacy EndingScene must exist before migration.");
        BakeResultPrefab();
        var active = SceneManager.GetActiveScene();
        var opened = new List<Scene>();
        try
        {
            var resultScene = Open(paths[0], opened);
            var result = Find<ResultBoard>(resultScene);
            if (result == null) throw new InvalidOperationException("ResultScene has no ResultBoard.");
            result.BakeInspectionUi();
            Save(resultScene, paths[0]);
            var legacyScene = Open(paths[1], opened);
            var legacy = Find<EndingBoard>(legacyScene);
            if (legacy == null) throw new InvalidOperationException("EndingScene has no EndingBoard template.");
            legacy.BakeAuthoredUi();
            Configure(legacy, false, EndingKind.Bad);
            if (!legacy.HasAuthoredUi) throw new InvalidOperationException("Legacy EndingBoard template is incomplete.");
            Save(legacyScene, paths[1]);
            foreach (var kind in Kinds)
            {
                string path = Folder + EndingSceneRoute.For(kind) + ".unity";
                Scene scene;
                if (File.Exists(path)) scene = Open(path, opened);
                else
                {
                    scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    opened.Add(scene);
                    SceneManager.SetActiveScene(scene);
                    foreach (var root in legacyScene.GetRootGameObjects())
                    {
                        var copy = UnityEngine.Object.Instantiate(root);
                        copy.name = root.name;
                        if (copy.scene != scene) SceneManager.MoveGameObjectToScene(copy, scene);
                    }
                }
                var board = Find<EndingBoard>(scene);
                if (board == null) throw new InvalidOperationException("Ending scene has no EndingBoard: " + path);
                board.BakeAuthoredUi();
                Configure(board, true, kind);
                if (!board.HasAuthoredUi) throw new InvalidOperationException("Incomplete ending UI: " + path);
                Save(scene, path);
            }
            var build = EditorBuildSettings.scenes.ToList();
            foreach (var kind in Kinds)
            {
                string path = Folder + EndingSceneRoute.For(kind) + ".unity";
                if (!build.Any(entry => entry.path == path)) build.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = build.ToArray();
        }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            for (int i = opened.Count - 1; i >= 0; i--)
                if (opened[i].IsValid() && opened[i].isLoaded) EditorSceneManager.CloseScene(opened[i], true);
        }
    }

    static void BakeResultPrefab()
    {
        const string path = "Assets/Prefabs/InterviewRefresh/ResultBoard.prefab";
        if (!File.Exists(path)) return;
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var boards = root.GetComponentsInChildren<ResultBoard>(true);
            if (boards.Length != 1) throw new InvalidOperationException("Expected one ResultBoard in prefab.");
            boards[0].BakeInspectionUi();
            EditorUtility.SetDirty(boards[0]);
            if (!PrefabUtility.SaveAsPrefabAsset(root, path))
                throw new InvalidOperationException("Failed to save result inspection prefab.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static Scene Open(string path, List<Scene> opened)
    {
        var scene = SceneManager.GetSceneByPath(path);
        if (scene.IsValid() && scene.isLoaded) return scene;
        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        opened.Add(scene);
        return scene;
    }

    static T Find<T>(Scene scene) where T : Component
    {
        T found = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<T>(true))
            {
                if (found != null) throw new InvalidOperationException("Multiple " + typeof(T).Name + " in " + scene.path);
                found = candidate;
            }
        return found;
    }

    static void Configure(EndingBoard board, bool useFixedKind, EndingKind kind)
    {
        var serialized = new SerializedObject(board);
        serialized.Update();
        serialized.FindProperty("goodHalfStarThreshold").intValue = 36;
        serialized.FindProperty("normalHalfStarThreshold").intValue = 28;
        serialized.FindProperty("useFixedKind").boolValue = useFixedKind;
        serialized.FindProperty("fixedKind").intValue = (int)kind;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(board);
    }

    static void Save(Scene scene, string path)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, path))
            throw new InvalidOperationException("Result presentation scene save failed: " + path);
    }
}
