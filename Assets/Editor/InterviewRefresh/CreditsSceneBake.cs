using System;
using System.Collections.Generic;
using System.IO;
using Ohabjizol.Presentation;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Editor.InterviewRefresh
{
    public static class CreditsSceneBake
    {
        const string ScenePath = "Assets/Scenes/CreditsScene.unity";

        [MenuItem("Tools/Interview/Create Credits Scene")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Credits scene baking requires edit mode.");

            Scene originalActive = SceneManager.GetActiveScene();
            Scene working = default;
            bool closeWorking = false;
            try
            {
                SceneAsset existing = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                if (existing != null)
                {
                    working = SceneManager.GetSceneByPath(ScenePath);
                    if (!working.IsValid() || !working.isLoaded)
                    {
                        working = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                        closeWorking = true;
                    }
                    bool authored = false;
                    foreach (GameObject root in working.GetRootGameObjects())
                    {
                        var screen = root.GetComponentInChildren<CreditsScreen>(true);
                        authored |= screen && screen.HasAuthoredUi;
                    }
                    if (!authored)
                        throw new InvalidOperationException("Existing CreditsScene requires an authored CreditsScreen; it was left unchanged.");
                }
                else
                {
                    if (File.Exists(ScenePath))
                        throw new InvalidOperationException("CreditsScene exists but is not imported as a SceneAsset; refusing to overwrite it.");
                    working = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    closeWorking = true;
                    SceneManager.SetActiveScene(working);
                    Build(working);
                    if (!EditorSceneManager.SaveScene(working, ScenePath))
                        throw new InvalidOperationException("Could not save the new CreditsScene.");
                }
                RegisterScene();
            }
            finally
            {
                if (originalActive.IsValid() && originalActive.isLoaded)
                    SceneManager.SetActiveScene(originalActive);
                if (closeWorking && working.IsValid() && working.isLoaded)
                    EditorSceneManager.CloseScene(working, true);
            }
        }

        static void Build(Scene scene)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;

            var canvasObject = new GameObject("CreditsCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CreditsScreen));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            Image background = backgroundObject.GetComponent<Image>();
            background.color = Color.black;
            background.raycastTarget = true;
            UiKit.Stretch(background.rectTransform, 0f);

            var foregroundObject = new GameObject("Foreground", typeof(RectTransform), typeof(CanvasGroup));
            foregroundObject.transform.SetParent(canvasObject.transform, false);
            UiKit.Stretch((RectTransform)foregroundObject.transform, 0f);
            CanvasGroup foreground = foregroundObject.GetComponent<CanvasGroup>();
            foreground.alpha = 1f;
            Transform parent = foregroundObject.transform;
            AddText(parent, "Title", "너 우리팀 할래?", 82, 120, 100, 620, 120);
            AddText(parent, "Team", "팀: 오합지졸", 32, 125, 275, 580, 50);
            AddText(parent, "LeadHeading", "팀장", 46, 125, 335, 170, 55);
            AddText(parent, "LeadName", "유승민", 32, 125, 390, 170, 50);
            AddText(parent, "ScribeHeading", "서기", 46, 300, 335, 220, 55);
            AddText(parent, "ScribeName", "김현승", 32, 300, 390, 220, 50);
            AddText(parent, "PlannerHeading", "기획자", 46, 125, 475, 580, 55);
            AddText(parent, "PlannerNames", "김현승, 유승민, 이준서", 32, 125, 535, 580, 50);
            AddText(parent, "GraphicsHeading", "그래픽", 46, 125, 645, 580, 55);
            AddText(parent, "GraphicsNames", "배성영, 최가온", 32, 125, 705, 580, 50);
            AddText(parent, "ProgrammerHeading", "프로그래머", 46, 125, 815, 580, 55);
            AddText(parent, "ProgrammerName", "이서우", 32, 125, 875, 580, 50);
            Text thanks = AddText(parent, "ThankYou", "Thank you for playing", 76, 730, 285, 1080, 130);
            while (thanks.fontSize > 60 && thanks.preferredWidth > 1080f)
                thanks.fontSize--;
            thanks.resizeTextForBestFit = true;
            thanks.resizeTextMinSize = 60;
            thanks.resizeTextMaxSize = thanks.fontSize;

            var empty = new GameObject("EmptyResultArea", typeof(RectTransform));
            empty.transform.SetParent(parent, false);
            UiKit.Place((RectTransform)empty.transform, 0.42f, 0.12f, 0.94f, 0.64f);

            Button back = UiKit.Button(parent, "BackToMainMenu", "메인 화면으로 돌아가기",
                new Color(0.18f, 0.18f, 0.18f, 0.8f), 28);
            Position((RectTransform)back.transform, 1430, 970, 390, 68);
            foreach (Image image in back.GetComponentsInChildren<Image>(true))
                if (image.sprite != null && !EditorUtility.IsPersistent(image.sprite))
                    image.sprite = null;
            foreach (Text label in back.GetComponentsInChildren<Text>(true))
                Style(label);
            canvasObject.GetComponent<CreditsScreen>().ConfigureEditor(foreground, back, 3f);

            // Always author a scene-local event system; another open scene may have one.
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(events, scene);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            events.GetComponent<EventSystem>().firstSelectedGameObject = back.gameObject;
        }

        static Text AddText(Transform parent, string name, string caption, int size,
            float x, float y, float width, float height)
        {
            Text text = UiKit.Text(parent, name, size, FontStyle.Normal, TextAnchor.UpperLeft);
            text.text = caption;
            Style(text);
            Position(text.rectTransform, x, y, width, height);
            return text;
        }

        static void Style(Text text)
        {
            text.font = UiKit.Font;
            text.fontStyle = FontStyle.Normal;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        static void Position(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static void RegisterScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            bool changed = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (!string.Equals(scenes[i].path, ScenePath, StringComparison.Ordinal))
                    continue;
                found = true;
                if (!scenes[i].enabled)
                {
                    scenes[i] = new EditorBuildSettingsScene(ScenePath, true);
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
