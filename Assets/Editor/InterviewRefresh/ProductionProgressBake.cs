#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public static class ProductionProgressBake
    {
        private const string ScenePath = "Assets/Scenes/ProductionProgressScene.unity";

        [MenuItem("Tools/Interview Refresh/Apply Production Progress Scene")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Production scene authoring requires idle Edit Mode.");
            Scene previousActive = SceneManager.GetActiveScene();
            Scene target = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = false;
            try
            {
                if (File.Exists(ScenePath))
                {
                    if (!target.IsValid() || !target.isLoaded)
                    {
                        target = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                        openedHere = true;
                    }
                    ValidateAuthoredUi(target);
                }
                else
                {
                    Font font = UiKit.Font;
                    if (font == null)
                        throw new InvalidOperationException("UiKit.Font is required to author production progress UI.");

                    if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                        AssetDatabase.CreateFolder("Assets", "Scenes");

                    target = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    openedHere = true;
                    SceneManager.SetActiveScene(target);
                    Author(target, font);
                    ValidateAuthoredUi(target);
                    if (!EditorSceneManager.SaveScene(target, ScenePath))
                        throw new InvalidOperationException("Could not save " + ScenePath);
                }

                EnableBuildScene();
            }
            finally
            {
                if (previousActive.IsValid() && previousActive.isLoaded)
                    SceneManager.SetActiveScene(previousActive);
                if (openedHere && target.IsValid() && target.isLoaded && !target.isDirty)
                    EditorSceneManager.CloseScene(target, true);
            }
        }

        private static void ValidateAuthoredUi(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ProductionProgressView view in root.GetComponentsInChildren<ProductionProgressView>(true))
                {
                    if (view.HasAuthoredUi)
                        return;
                }
            }
            throw new InvalidOperationException(ScenePath + " requires a ProductionProgressView with authored UI; scene was not rebuilt.");
        }

        private static void Author(Scene scene, Font font)
        {
            var canvasObject = new GameObject("ProductionProgressCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            ImagePanel("Background", canvasObject.transform, Vector2.zero, Vector2.one, new Color(0.035f, 0.045f, 0.065f));
            RectTransform calendar = Rect("Calendar", canvasObject.transform, new Vector2(0.04f, 0.69f), new Vector2(0.19f, 0.94f));
            ImagePanel("CalendarBackground", calendar, Vector2.zero, Vector2.one, new Color(0.12f, 0.14f, 0.19f));
            ImagePanel("HeaderBackground", calendar, new Vector2(0f, 0.72f), Vector2.one, new Color(0.23f, 0.28f, 0.38f));
            Label("Header", calendar, new Vector2(0.03f, 0.72f), new Vector2(0.97f, 1f), "제작 일정", 32, font);
            ImagePanel("DayCellBackground", calendar, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.69f), new Color(0.15f, 0.18f, 0.24f));
            Text day = Label("DayNumber", calendar, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.69f), "1일", 96, font);
            Text caption = Label("ActivityCaption", canvasObject.transform, new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.60f), "컨셉 기획 중", 48, font);
            ProductionProgressView controller = canvasObject.AddComponent<ProductionProgressView>();
            controller.ConfigureEditor(day, caption);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            result.SetParent(parent, false);
            result.anchorMin = min;
            result.anchorMax = max;
            result.offsetMin = Vector2.zero;
            result.offsetMax = Vector2.zero;
            return result;
        }

        private static void ImagePanel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            Image image = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static Text Label(string name, Transform parent, Vector2 min, Vector2 max, string value, int size, Font font)
        {
            Text text = Rect(name, parent, min, max).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void EnableBuildScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            bool changed = false;
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.path != ScenePath)
                    continue;
                found = true;
                if (!scene.enabled)
                {
                    scene.enabled = true;
                    changed = true;
                }
            }
            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                changed = true;
            }
            if (changed)
                EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
