using System;
using System.Collections.Generic;
using System.IO;
using Ohabjizol.Interview;
using Ohabjizol.Presentation;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TutorialPrefabMergeBake
{
    const string ScenePath = "Assets/Scenes/InterviewScene.unity";
    const string ScreenPath = "Assets/Prefabs/InterviewRefresh/InterviewScreen.prefab";
    const string DesktopPath = "Assets/Prefabs/InterviewRefresh/Desktop.prefab";

    [MenuItem("Ohabjizol/Tutorial/Merge Current Gameplay UI")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Wait for idle Edit mode before merging UI.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("Open InterviewScene before merging its current edits.");
        var original = SceneManager.GetActiveScene();
        var rows = new List<string>();
        Directory.CreateDirectory("Logs/TutorialRefresh/Before");
        if (!EditorSceneManager.SaveScene(scene, "Logs/TutorialRefresh/Before/InterviewScene.before-merge.unity", true))
            throw new IOException("Could not preserve current scene edits.");
        try
        {
            SceneManager.SetActiveScene(scene);
            var screens = new List<InterviewScreen>();
            foreach (var root in scene.GetRootGameObjects())
                screens.AddRange(root.GetComponentsInChildren<InterviewScreen>(true));
            if (screens.Count != 1 || !screens[0].HasAuthoredUi)
                throw new InvalidOperationException("Expected one complete authored InterviewScreen.");
            var screen = screens[0];
            var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(screen.gameObject);
            if (!prefabRoot || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot) != ScreenPath)
                throw new InvalidOperationException("The current UI must be connected to the shared InterviewScreen prefab.");
            Backup(ScreenPath);
            Backup(DesktopPath);
            screen.ConfigureTutorialTargetsEditor();
            int buttons = Decorate(prefabRoot);
            CheckSceneReferences(prefabRoot);
            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the merged InterviewScene.");
            rows.Add("screenMerged=" + ScreenPath + ";buttons=" + buttons + ";tutorialTargets=valid");

            var queue = new Queue<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            queue.Enqueue(ScreenPath);
            queue.Enqueue(DesktopPath);
            while (queue.Count > 0)
            {
                string path = queue.Dequeue();
                if (!seen.Add(path)) continue;
                if (seen.Count > 32) throw new InvalidOperationException("Unexpected UI prefab dependency expansion.");
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var component in contents.GetComponentsInChildren<Component>(true))
                    {
                        if (!component) continue;
                        var serialized = new SerializedObject(component);
                        var property = serialized.GetIterator();
                        while (property.NextVisible(true))
                        {
                            if (property.propertyType != SerializedPropertyType.ObjectReference || !property.objectReferenceValue) continue;
                            string dependency = AssetDatabase.GetAssetPath(property.objectReferenceValue);
                            if (dependency.StartsWith("Assets/Prefabs/InterviewRefresh/", StringComparison.Ordinal)
                                && dependency.EndsWith(".prefab", StringComparison.Ordinal) && !seen.Contains(dependency))
                                queue.Enqueue(dependency);
                        }
                    }
                    var tutorialWindows = contents.GetComponentsInChildren<TutorialWindow>(true);
                    foreach (var window in tutorialWindows) window.BakePromptEditor();
                    int count = Decorate(contents);
                    if (count == 0 && tutorialWindows.Length == 0) continue;
                    Backup(path);
                    PrefabUtility.SaveAsPrefabAsset(contents, path, out bool saved);
                    if (!saved) throw new IOException("Could not save UI prefab " + path);
                    rows.Add("template=" + path + ";buttons=" + count);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            File.WriteAllLines("Logs/TutorialRefresh/prefab-merge.txt", rows);
        }
    }

    static int Decorate(GameObject root)
    {
        var buttons = root.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            ButtonActiveBackground.Ensure(button);
            foreach (var component in button.GetComponentsInChildren<Component>(true))
            {
                if (!component) continue;
                EditorUtility.SetDirty(component);
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
        return buttons.Length;
    }

    static void CheckSceneReferences(GameObject root)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (!component) continue;
            var property = new SerializedObject(component).GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                var value = property.objectReferenceValue;
                if (!value || EditorUtility.IsPersistent(value)) continue;
                var target = value is Component c ? c.transform : value is GameObject go ? go.transform : null;
                if (target && target != root.transform && !target.IsChildOf(root.transform))
                    throw new InvalidOperationException("External scene reference prevents prefab merge: " + component.name + "." + property.propertyPath);
            }
        }
    }

    static void Backup(string path)
    {
        string destination = "Logs/TutorialRefresh/Before/" + path.Replace('/', '_');
        if (File.Exists(path) && !File.Exists(destination)) File.Copy(path, destination);
    }
}
