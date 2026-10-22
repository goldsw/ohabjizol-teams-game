using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Interview.Editor
{
    public static class TwoQuestionMenuBake
    {
        const string Prefab = "Assets/Prefabs/InterviewRefresh/InterviewScreen.prefab";
        static readonly string[] Scenes = { "Assets/Scenes/InterviewScene.unity", "Assets/Scenes/TutorialScene.unity" };
        public static string LastSummary { get; private set; }

        [MenuItem("Tools/Interview Refresh/Apply Two Question Menu")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Question menu authoring requires idle Edit Mode.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Close Prefab Mode before updating the shared question menu.");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponentInChildren<InterviewScreen>(true);
            var catalog = new SerializedObject(source).FindProperty("_questionCatalog").objectReferenceValue as AuthoredQuestionCatalog;
            if (!catalog) throw new InvalidOperationException("Existing authored question catalog required.");
            var targets = new List<string> { Prefab, Scenes[0], Scenes[1], AssetDatabase.GetAssetPath(catalog) };
            string backup = "Logs/TwoQuestionMenu/BeforeApply/" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff");
            foreach (string path in targets)
            {
                string destination = backup + "/Disk/" + path;
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(path, destination);
            }
            foreach (string path in Scenes)
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded || !scene.isDirty) continue;
                string destination = backup + "/Dirty/" + Path.GetFileName(path);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                if (!EditorSceneManager.SaveScene(scene, destination, true)) throw new IOException("Dirty scene backup failed.");
            }
            catalog.PruneQuestionsEditor();
            AssetDatabase.SaveAssetIfDirty(catalog);
            var active = SceneManager.GetActiveScene();
            try
            {
                var root = PrefabUtility.LoadPrefabContents(Prefab);
                try
                {
                    Bake(root.GetComponentInChildren<InterviewScreen>(true));
                    PrefabUtility.SaveAsPrefabAsset(root, Prefab, out bool success);
                    if (!success) throw new IOException("Question menu prefab save failed.");
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                foreach (string path in Scenes)
                {
                    var scene = SceneManager.GetSceneByPath(path);
                    bool own = !scene.IsValid() || !scene.isLoaded;
                    if (own) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    try
                    {
                        int count = 0;
                        foreach (var sceneRoot in scene.GetRootGameObjects())
                            foreach (var screen in sceneRoot.GetComponentsInChildren<InterviewScreen>(true)) { Bake(screen); count++; }
                        if (count != 1) throw new InvalidOperationException("Expected one InterviewScreen: " + path);
                        EditorSceneManager.MarkSceneDirty(scene);
                        if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Question menu scene save failed: " + path);
                    }
                    finally { if (own && scene.IsValid() && scene.isLoaded && !scene.isDirty) EditorSceneManager.CloseScene(scene, true); }
                }
                LastSummary = "Two question menu saved; catalog questions=" + catalog.QuestionCount + ", responses=" + catalog.ResponseCount;
            }
            finally { if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
        }

        static void Bake(InterviewScreen screen)
        {
            if (!screen) throw new InvalidOperationException("InterviewScreen required.");
            var serialized = new SerializedObject(screen);
            var source = serialized.FindProperty("_questions");
            var call = serialized.FindProperty("_call").objectReferenceValue as InterviewCallView;
            var menu = serialized.FindProperty("_radialQuestions").objectReferenceValue as RadialQuestionView;
            if (!call || !call.PortraitRect || !menu || source.arraySize < InterviewQuestionCatalog.QuestionCount)
                throw new InvalidOperationException("Existing portrait, question menu and two question views required.");
            var buttons = new Button[InterviewQuestionCatalog.QuestionCount];
            for (int i = 0; i < source.arraySize; i++)
            {
                var view = source.GetArrayElementAtIndex(i).objectReferenceValue as InterviewQuestionView;
                if (i < buttons.Length)
                {
                    if (!view || !view.Button) throw new InvalidOperationException("Question view missing: " + i);
                    buttons[i] = view.Button;
                    view.EnsureUsedOverlayEditor();
                    view.gameObject.SetActive(true);
                    view.Bind(i == 0 ? "포트폴리오 프로젝트" : "일정");
                    Save(view.gameObject);
                    foreach (var label in view.GetComponentsInChildren<Text>(true)) Save(label);
                }
                else if (view) { view.gameObject.SetActive(false); Save(view.gameObject); }
            }
            source.arraySize = buttons.Length;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var binding = new SerializedObject(menu);
            var options = binding.FindProperty("_optionsRoot").objectReferenceValue as RectTransform;
            var launcher = binding.FindProperty("_launcher").objectReferenceValue as Button;
            if (!options) throw new InvalidOperationException("Existing options root required.");
            foreach (var view in options.GetComponentsInChildren<InterviewQuestionView>(true))
            {
                if (Array.IndexOf(buttons, view.Button) >= 0) continue;
                view.gameObject.SetActive(false);
                Save(view.gameObject);
            }
            menu.ConfigureEditor(launcher, options, call.PortraitRect, buttons);
            foreach (var button in buttons)
            {
                var rect = (RectTransform)button.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(1f, .5f);
                rect.sizeDelta = new Vector2(300, 56);
                Save(rect);
            }
            Save(menu);
            Save(options.gameObject);
            Save(screen);
            if (serialized.FindProperty("_tutorialTargets").objectReferenceValue)
                screen.ConfigureTutorialTargetsEditor();
            if (!screen.HasAuthoredUi) throw new InvalidOperationException("Screen references invalid after reducing questions.");
        }

        static void Save(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
