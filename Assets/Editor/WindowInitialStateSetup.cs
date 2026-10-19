using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persist new field defaults once, without assigning or replacing user settings.</summary>
public static class WindowInitialStateSetup
{
    const string MenuScene = "Assets/Scenes/MainMenuScene.unity";
    const string InterviewScene = "Assets/Scenes/InterviewScene.unity";

    public static void ApplyAndSave()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Initial state migration requires Edit Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save dirty scenes before initial state migration.");
        RequireScene(MenuScene);
        RequireScene(InterviewScene);
        // Exceptions intentionally propagate to the CLI caller.
        Migrate(MenuScene, "MainMenuController", "initialWindows",
            new[] { "codeOpen", "settingsOpen", "tutorialOpen" }, false);
        Migrate(InterviewScene, "InterviewScreen", "initialDocuments",
            new[] { "applicationOpen", "portfolioOpen", "scheduleOpen", "interviewRecordOpen" }, true);
    }

    static void RequireScene(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            throw new FileNotFoundException("Required project scene is missing.", path);
    }

    static bool ValidateSavedPrefabHosts(string path, string typeName, string stateName, string[] fields, bool discord, string yaml)
    {
        Scene preview = EditorSceneManager.OpenPreviewScene(path);
        try
        {
            int targets = 0;
            bool hasSceneOwnedHost = false;
            foreach (GameObject root in preview.GetRootGameObjects())
            foreach (MonoBehaviour host in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (host == null || host.GetType().Name != typeName) continue;
                targets++;
                if (!PrefabUtility.IsPartOfPrefabInstance(host))
                {
                    hasSceneOwnedHost = true;
                    continue;
                }
                var serialized = new SerializedObject(host);
                serialized.Update();
                var saved = WindowInitialStateValidation.ReadSavedHost(host, yaml);
                bool skipDocuments = typeName == "InterviewScreen" && stateName == "initialDocuments"
                    && serialized.FindProperty(stateName) == null;
                if (!skipDocuments)
                    foreach (string field in fields ?? Array.Empty<string>())
                        RequireSavedPrefabBool(serialized, saved, stateName + "." + field);
                if (discord) RequireSavedPrefabBool(serialized, saved, "discordVisible");
            }
            if (targets == 0)
                throw new InvalidOperationException("No existing " + typeName + " found in " + path);
            return hasSceneOwnedHost;
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    static void RequireSavedPrefabBool(SerializedObject serialized, WindowInitialStateValidation.SavedHost saved, string path)
    {
        SerializedProperty property = serialized.FindProperty(path);
        if (property == null || property.propertyType != SerializedPropertyType.Boolean)
            throw new InvalidOperationException("Missing serialized prefab bool: " + path);
        bool value;
        if (!saved.TryReadBool(path, out value))
            throw new InvalidOperationException("prefab authoring 저장 필요: " + path);
        if (property.boolValue != value)
            throw new InvalidOperationException("Loaded/saved prefab mismatch: " + path);
    }

    static void Migrate(string path, string typeName, string stateName, string[] fields, bool discord)
    {
        string source = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
        if (!ValidateSavedPrefabHosts(path, typeName, stateName, fields, discord, source))
        {
            Debug.Log("WindowInitialStateSetup: " + path + " already migrated; preserved.");
            return;
        }
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            int targets = 0;
            bool changed = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().Name != typeName)
                    continue;
                targets++;
                // Prefab hosts were inspected only in the freshly opened preview scene.
                if (PrefabUtility.IsPartOfPrefabInstance(component)) continue;
                var serialized = new SerializedObject(component);
                serialized.Update();
                string currentStateName = stateName;
                string[] currentFields = fields ?? Array.Empty<string>();
                SerializedProperty state = currentStateName == null ? null : serialized.FindProperty(currentStateName);
                // Download-driven hosts have no authored initial document windows.
                if (typeName == "InterviewScreen" && currentStateName == "initialDocuments" && state == null)
                {
                    currentStateName = null;
                    currentFields = Array.Empty<string>();
                }
                if (currentStateName != null && state == null)
                    throw new InvalidOperationException(typeName + "." + stateName + " is missing. Integrate B/C first.");
                if (state == null && currentFields.Length > 0)
                    throw new InvalidOperationException(typeName + " requires a state name for initial fields.");
                foreach (string field in currentFields)
                {
                    SerializedProperty value = state.FindPropertyRelative(field);
                    if (value == null || value.propertyType != SerializedPropertyType.Boolean)
                        throw new InvalidOperationException(typeName + "." + stateName + "." + field + " must be a serialized bool.");
                }
                if (discord)
                {
                    SerializedProperty value = serialized.FindProperty("discordVisible");
                    if (value == null || value.propertyType != SerializedPropertyType.Boolean)
                        throw new InvalidOperationException("InterviewScreen.discordVisible must be a serialized bool.");
                }

                ulong id = GlobalObjectId.GetGlobalObjectIdSlow(component).targetObjectId;
                Match block = Regex.Match(source, @"(?ms)^--- !u!114 &" + id + @"\r?\n.*?(?=^--- !u!|\z)");
                if (!block.Success)
                    throw new InvalidOperationException("Cannot inspect saved component " + id + " in " + path + ". Expected a scene-owned component in text serialization.");
                string stateRegex = currentStateName == null ? null
                    : @"(?m)^  " + Regex.Escape(currentStateName) + @":\r?\n(?<body>(?:^    [^\r\n]*\r?\n)*)";
                Match stateBlock = stateRegex == null ? null : Regex.Match(block.Value, stateRegex);
                bool missing = stateBlock != null && !stateBlock.Success;
                foreach (string field in currentFields)
                    missing |= stateBlock == null || !Regex.IsMatch(stateBlock.Groups["body"].Value, @"(?m)^    " + Regex.Escape(field) + @":");
                if (discord)
                    missing |= !Regex.IsMatch(block.Value, @"(?m)^  discordVisible:");
                if (!missing)
                    continue;

                // Unity retains existing serialized values and emits missing fields from
                // C# initializers. Never inject bool constants here.
                EditorUtility.SetDirty(component);
                changed = true;
            }
            if (targets == 0)
                throw new InvalidOperationException("No existing " + typeName + " found in " + path);
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new IOException("Failed to save " + path);
            }
            Debug.Log("WindowInitialStateSetup: " + path + (changed ? " serialized missing initial fields." : " already migrated; preserved."));
        }
        finally
        {
            if (opened && !EditorSceneManager.CloseScene(scene, true))
                throw new IOException("Failed to close migration scene " + path);
        }
    }
}
