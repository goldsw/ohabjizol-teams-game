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
    static readonly string[] ResultNames =
    {
        ResultSceneRoute.GoodSceneName, ResultSceneRoute.NormalSceneName, ResultSceneRoute.BadSceneName
    };
    static readonly Color[] ResultColors =
    {
        new Color(.08f, .24f, .14f, 1f),
        new Color(.29f, .25f, .06f, 1f),
        new Color(.30f, .09f, .10f, 1f)
    };
    static readonly EndingKind[] Kinds = { EndingKind.Good, EndingKind.Normal, EndingKind.Bad };

    [MenuItem("Tools/Interview/Apply Result Presentation")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before result migration.");
        var paths = new List<string> { Folder + "ResultScene.unity", Folder + "EndingScene.unity" };
        foreach (var kind in Kinds) paths.Add(Folder + EndingSceneRoute.For(kind) + ".unity");
        foreach (string name in ResultNames) paths.Add(Folder + name + ".unity");
        foreach (string path in paths)
        {
            var scene = SceneManager.GetSceneByPath(path);
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                throw new InvalidOperationException("Result migration refuses an open dirty target scene: " + path);
        }
        if (!File.Exists(paths[0]) || !File.Exists(paths[1]))
            throw new InvalidOperationException("ResultScene and legacy EndingScene must exist before migration.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isDirty && string.IsNullOrEmpty(scene.path))
                throw new InvalidOperationException("Save the unnamed scene before result migration.");
        }
        BakeResultPrefab();
        var active = SceneManager.GetActiveScene();
        var opened = new List<Scene>();
        try
        {
            var resultScene = Open(paths[0], opened);
            var result = Find<ResultBoard>(resultScene);
            if (result == null) throw new InvalidOperationException("ResultScene has no ResultBoard.");
            result.BakeInspectionUi();
            result.FitScheduleLayout(6);
            NormalizeAuthored(result);
            foreach (var legacyRoot in resultScene.GetRootGameObjects())
            {
                if (legacyRoot.name == "ResultOverlay" &&
                    legacyRoot.GetComponent<Canvas>() != null &&
                    legacyRoot.GetComponentInChildren<ResultBoard>(true) == null)
                    legacyRoot.SetActive(false);
            }
            Save(resultScene, paths[0]);
            for (int i = 0; i < ResultNames.Length; i++)
            {
                string path = Folder + ResultNames[i] + ".unity";
                var existing = SceneManager.GetSceneByPath(path);
                if (existing.IsValid() && existing.isLoaded &&
                    !EditorSceneManager.CloseScene(existing, true))
                    throw new InvalidOperationException("Could not close result variant: " + path);
                // Copy the entire scene together to preserve references between its roots.
                if (!EditorSceneManager.SaveScene(resultScene, path, true))
                    throw new InvalidOperationException("Could not clone ResultScene: " + path);
                var variantScene = Open(path, opened);
                var variant = Find<ResultBoard>(variantScene);
                if (variant == null) throw new InvalidOperationException("Result variant has no ResultBoard: " + path);
                variant.BakeInspectionUi();
                variant.FitScheduleLayout(6);
                NormalizeAuthored(variant);
                ApplyResultTheme(variant, ResultColors[i]);
                Save(variantScene, path);
            }
            var legacyScene = Open(paths[1], opened);
            var legacy = Find<EndingBoard>(legacyScene);
            if (legacy == null) throw new InvalidOperationException("EndingScene has no EndingBoard template.");
            legacy.BakeAuthoredUi();
            Configure(legacy, false, EndingKind.Bad);
            NormalizeAuthored(legacy);
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
                NormalizeAuthored(board);
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
            EnsureResultBuildScenes();
        }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            for (int i = opened.Count - 1; i >= 0; i--)
                if (opened[i].IsValid() && opened[i].isLoaded) EditorSceneManager.CloseScene(opened[i], true);
            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    public static void EnsureResultBuildScenes()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        foreach (string name in ResultNames)
        {
            string path = Folder + name + ".unity";
            if (!File.Exists(path)) throw new InvalidOperationException("Missing result variant: " + path);
            int index = scenes.FindIndex(entry => entry.path == path);
            if (index < 0) scenes.Add(new EditorBuildSettingsScene(path, true));
            else scenes[index] = new EditorBuildSettingsScene(path, true);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void ApplyResultTheme(ResultBoard board, Color background)
    {
        var serialized = new SerializedObject(board);
        serialized.Update();
        var rootProperty = serialized.FindProperty("authoredRoot");
        var root = rootProperty == null ? null : rootProperty.objectReferenceValue as Component;
        var image = root == null ? null : root.GetComponent<UnityEngine.UI.Image>();
        if (image == null && root != null)
        {
            var backgroundRoot = root.transform.Find("OrangeBackground");
            image = backgroundRoot == null ? null : backgroundRoot.GetComponent<UnityEngine.UI.Image>();
        }
        if (image == null || root == null || image.gameObject.scene != board.gameObject.scene ||
            root.gameObject.scene != board.gameObject.scene)
            throw new InvalidOperationException("Result theme requires a scene-local authoredRoot and background Image.");
        image.color = background;
        EditorUtility.SetDirty(image);
        foreach (var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            var color = text.color;
            float minimum = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            float maximum = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            bool darkNeutral = maximum <= .4f && maximum - minimum <= .06f;
            bool defaultBrown = Mathf.Abs(color.r - .15f) <= .01f &&
                Mathf.Abs(color.g - .10f) <= .01f && Mathf.Abs(color.b - .06f) <= .01f;
            if (text.GetComponentInParent<UnityEngine.UI.Button>() == null &&
                (darkNeutral || defaultBrown))
            {
                text.color = new Color(.94f, .94f, .94f, color.a);
                EditorUtility.SetDirty(text);
            }
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
            boards[0].FitScheduleLayout(6);
            NormalizeAuthored(boards[0]);
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

    static void NormalizeAuthored(Component board)
    {
        const string fontPath = "Assets/Art/InterviewRefresh/Fonts/NotoSansKR.ttf";
        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null || !EditorUtility.IsPersistent(font) ||
            !AssetDatabase.Contains(font) || AssetDatabase.GetAssetPath(font) != fontPath)
            throw new InvalidOperationException("Persistent authored UI font required: " + fontPath);

        var serialized = new SerializedObject(board);
        serialized.Update();
        var property = serialized.FindProperty("authoredRoot");
        var root = property == null ? null : property.objectReferenceValue as Component;
        if (root == null)
            throw new InvalidOperationException("Missing authoredRoot on " + board.name);
        var scheduleProperty = serialized.FindProperty("scheduleRoot");
        if (scheduleProperty != null && scheduleProperty.propertyType == SerializedPropertyType.ObjectReference &&
            scheduleProperty.objectReferenceValue is Transform bound && bound.parent == root.transform)
        {
            foreach (Transform child in root.transform)
            {
                if (child.name != "ResultSchedule" || child == bound || !child.gameObject.activeSelf) continue;
                child.gameObject.SetActive(false);
                EditorUtility.SetDirty(child.gameObject);
                if (PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
            }
        }
        var authoredCanvas = root.GetComponent<Canvas>();
        if (authoredCanvas == null)
            throw new InvalidOperationException("Missing authored Canvas on " + board.name);

        foreach (var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            text.font = font;
            EditorUtility.SetDirty(text);
        }
        foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            if (image.sprite != null && !EditorUtility.IsPersistent(image.sprite))
            {
                image.sprite = null;
                EditorUtility.SetDirty(image);
            }
        }
        authoredCanvas.enabled = true;
        authoredCanvas.sortingOrder = 100;
        root.gameObject.SetActive(true);
        EditorUtility.SetDirty(authoredCanvas);
        EditorUtility.SetDirty(root.gameObject);
        EditorUtility.SetDirty(board);
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
