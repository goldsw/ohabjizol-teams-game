#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Ohabjizol.Interview;
using Ohabjizol.Presentation;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ohabjizol.Interview.Editor
{
    /// <summary>Explicit editor bake; never runs automatically on import.</summary>
    public static class InGameSettingsBake
    {
        private const string Desktop = "Assets/Prefabs/InterviewRefresh/Desktop.prefab";
        private const string Launcher = "Assets/Prefabs/InterviewRefresh/InGameSettings.prefab";
        private const string Screen = "Assets/Prefabs/InterviewRefresh/InterviewScreen.prefab";

        [MenuItem("Tools/Interview Refresh/Bake In Game Settings")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Apply requires idle Edit Mode.");

            string[] scenes = { ScenePath("InterviewScene"), ScenePath("TutorialScene") };
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
                throw new InvalidOperationException("Close Prefab Mode before baking dependent prefabs and scenes.");
            RequireAsset(Desktop);
            RequireAsset(Screen);
            var active = SceneManager.GetActiveScene();
            // Back up all disk targets and every loaded dirty target before the first mutation.
            Backup(scenes);
            try
            {
                EnsureLauncher();
                UpdateScreen();
                foreach (string path in scenes) UpdateScene(path);
                Debug.Log("InGameSettingsBake.Apply completed. Backups: Logs/InGameSettings/BeforeApply");
            }
            finally
            {
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            }
        }

        private static string ScenePath(string name)
        {
            string path = "Assets/Scenes/" + name + ".unity";
            if (!File.Exists(path)) throw new FileNotFoundException("Target scene missing.", path);
            return path;
        }

        private static void RequireAsset(string path)
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(path))
                throw new InvalidOperationException("Missing prefab: " + path);
        }

        private static void Backup(string[] scenes)
        {
            string directory = Path.Combine("Logs/InGameSettings/BeforeApply",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            foreach (string path in new[] { Desktop, Launcher, Screen }.Concat(scenes))
            {
                string destination = Path.Combine(directory, "Disk", path);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                if (File.Exists(path)) File.Copy(path, destination, false);
                else File.WriteAllText(destination + ".absent", "Asset did not exist before Apply.");
                if (File.Exists(path + ".meta")) File.Copy(path + ".meta", destination + ".meta", false);
            }
            foreach (string path in scenes)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded || !scene.isDirty) continue;
                string destination = Path.Combine(directory, "LoadedDirty", path).Replace('\\', '/');
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                if (!EditorSceneManager.SaveScene(scene, destination, true))
                    throw new IOException("Could not back up loaded dirty scene: " + path);
            }
        }

        private static void EnsureLauncher()
        {
            if (File.Exists(Launcher))
            {
                RequireAsset(Launcher);
                var existing = PrefabUtility.LoadPrefabContents(Launcher);
                try
                {
                    UpdateSettingsContent(existing);
                    ValidateLauncher(existing);
                    PrefabUtility.SaveAsPrefabAsset(existing, Launcher, out bool saved);
                    if (!saved) throw new IOException("Could not save " + Launcher);
                }
                finally { PrefabUtility.UnloadPrefabContents(existing); }
                return;
            }

            GameObject source = PrefabUtility.LoadPrefabContents(Desktop);
            GameObject root = null;
            Scene previousActive = SceneManager.GetActiveScene();
            Scene staging = default;
            try
            {
                var windows = source.GetComponentsInChildren<SettingsWindow>(true);
                if (windows.Length != 1 || !windows[0].HasAuthoredContent)
                    throw new InvalidOperationException("Desktop must contain exactly one authored SettingsWindow.");
                staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(staging);
                root = new GameObject("InGameSettings", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(InGameSettingsLauncher));
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().sortingOrder = 500;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;

                var buttonObject = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(root.transform, false);
                var buttonRect = (RectTransform)buttonObject.transform;
                buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = Vector2.zero;
                buttonRect.anchoredPosition = new Vector2(24, 80);
                buttonRect.sizeDelta = new Vector2(112, 48);
                var button = buttonObject.GetComponent<Button>();
                button.targetGraphic = buttonObject.GetComponent<Image>();
                AddLabel(buttonObject.transform);

                var clone = Object.Instantiate(windows[0].gameObject, root.transform, false);
                clone.name = windows[0].gameObject.name;
                var rect = clone.transform as RectTransform;
                if (!rect) throw new InvalidOperationException("SettingsWindow root must be a RectTransform.");
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(960, 680);
                var window = clone.GetComponent<SettingsWindow>();
                ValidateLocalReferences(clone);
                clone.SetActive(false);
                TrackButton(button, clone);
                root.GetComponent<InGameSettingsLauncher>().ConfigureEditor(button, window);
                UpdateSettingsContent(root);
                ValidateLauncher(root);
                PrefabUtility.SaveAsPrefabAsset(root, Launcher, out bool success);
                if (!success) throw new IOException("Could not save " + Launcher);
            }
            finally
            {
                if (root) Object.DestroyImmediate(root);
                PrefabUtility.UnloadPrefabContents(source);
                if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
                if (staging.IsValid() && staging.isLoaded) EditorSceneManager.CloseScene(staging, true);
            }
        }

        private static void UpdateSettingsContent(GameObject root)
        {
            var windows = root.GetComponentsInChildren<SettingsWindow>(true);
            if (windows.Length != 1 || !windows[0].HasAuthoredContent)
                throw new InvalidOperationException("Exactly one authored SettingsWindow is required.");
            var window = windows[0];
            var transforms = window.GetComponentsInChildren<Transform>(true);
            var reset = transforms.SingleOrDefault(item => item.name == "ResetProgress");
            var shutdown = transforms.SingleOrDefault(item => item.name == "ShutDown");
            if (!reset || !shutdown)
                throw new InvalidOperationException("Settings content requires ResetProgress and ShutDown.");

            var restartTransform = transforms.SingleOrDefault(item => item.name == "RestartGame");
            Button restart;
            if (restartTransform)
            {
                restart = restartTransform.GetComponent<Button>();
                if (!restart) throw new InvalidOperationException("RestartGame requires a Button.");
            }
            else
            {
                // Clone the authored UiKit button to retain its prefab styling and font assets.
                var template = reset.GetComponent<Button>();
                if (!template) throw new InvalidOperationException("ResetProgress requires a Button template.");
                restart = Object.Instantiate(template, reset.parent, false);
                restart.name = "RestartGame";
                restart.onClick = new Button.ButtonClickedEvent();
                var rect = restart.transform as RectTransform;
                if (!rect) throw new InvalidOperationException("RestartGame requires a RectTransform.");
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(182, -364);
                rect.sizeDelta = new Vector2(308, 52);
                var labels = restart.GetComponentsInChildren<Text>(true);
                if (labels.Length == 0) throw new InvalidOperationException("RestartGame requires an authored label.");
                foreach (var label in labels) label.text = "다시 시작";
                restart.interactable = true;
                restart.gameObject.SetActive(true);
            }

            var returnTransform = transforms.SingleOrDefault(item => item.name == "ReturnToTitle");
            Button returnToTitle;
            if (returnTransform)
            {
                returnToTitle = returnTransform.GetComponent<Button>();
                if (!returnToTitle) throw new InvalidOperationException("ReturnToTitle requires a Button.");
            }
            else
            {
                returnToTitle = Object.Instantiate(restart, restart.transform.parent, false);
                returnToTitle.name = "ReturnToTitle";
                returnToTitle.onClick = new Button.ButtonClickedEvent();
                var rect = (RectTransform)returnToTitle.transform;
                rect.anchoredPosition = new Vector2(182, -428);
                foreach (var label in returnToTitle.GetComponentsInChildren<Text>(true)) label.text = "타이틀로 돌아가기";
                returnToTitle.interactable = true;
                returnToTitle.gameObject.SetActive(true);
            }

            if (!transforms.Any(item => item.name == "ReturnToTitleWarning"))
            {
                var status = transforms.SingleOrDefault(item => item.name == "ApplyStatus")?.GetComponent<Text>();
                if (!status) throw new InvalidOperationException("ApplyStatus text is required as the warning style template.");
                var warning = Object.Instantiate(status, returnToTitle.transform.parent, false);
                warning.name = "ReturnToTitleWarning";
                var rect = warning.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(182, -488);
                rect.sizeDelta = new Vector2(600, 40);
                warning.text = "※ 타이틀로 돌아가면 현재 진행사항은 저장되지 않습니다.";
                warning.color = new Color32(200, 40, 40, 255);
                warning.alignment = TextAnchor.MiddleLeft;
                warning.raycastTarget = false;
                warning.gameObject.SetActive(true);
            }

            // Keep the six authored button references intact for SettingsWindow validation.
            reset.gameObject.SetActive(false);
            shutdown.gameObject.SetActive(false);
            foreach (var label in window.GetComponentsInChildren<Text>(true))
            {
                if (label.gameObject.name == "Label 전원" || label.text.Trim() == "전원") label.gameObject.SetActive(false);
                else if (label.gameObject.name == "Label 진행" || label.text.Trim() == "진행") label.text = "게임";
            }
            var launcher = root.GetComponent<InGameSettingsLauncher>();
            if (!launcher) throw new InvalidOperationException("InGameSettingsLauncher is required.");
            launcher.ConfigureRestartEditor(restart);
            launcher.ConfigureReturnToTitleEditor(returnToTitle);
        }

        private static void AddLabel(Transform parent)
        {
            var text = UiKit.Text(parent, "Label", 24, FontStyle.Normal, TextAnchor.MiddleCenter);
            text.text = "설정";
            text.color = Color.black;
            UiKit.Stretch(text.rectTransform, 0);
        }

        private static void TrackButton(Button button, GameObject window)
        {
            ButtonActiveBackground.Ensure(button).TrackVisibleContent(window);
        }

        private static void ValidateLocalReferences(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (!component) throw new InvalidOperationException("Missing script in " + root.name);
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (component.transform == root.transform && property.propertyPath == "m_Father") continue;
                    if (property.propertyPath == "m_CorrespondingSourceObject" || property.propertyPath == "m_PrefabInstance" || property.propertyPath == "m_PrefabAsset") continue;
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    Object reference = property.objectReferenceValue;
                    if (!reference)
                    {
                        if (property.objectReferenceInstanceIDValue != 0)
                            throw new InvalidOperationException("Broken reference: " + property.propertyPath);
                        continue;
                    }
                    var target = reference as GameObject;
                    if (reference is Component targetComponent) target = targetComponent.gameObject;
                    if (target)
                    {
                        if (target != root && !target.transform.IsChildOf(root.transform))
                            throw new InvalidOperationException("Nonlocal reference: " + component.name + "." + property.propertyPath);
                    }
                    else if (!EditorUtility.IsPersistent(reference))
                        throw new InvalidOperationException("External scene reference: " + property.propertyPath);
                }
            }
        }

        private static void ValidateLauncher(GameObject root)
        {
            var launcher = root.GetComponent<InGameSettingsLauncher>();
            var windows = root.GetComponentsInChildren<SettingsWindow>(true);
            var canvas = root.GetComponent<Canvas>();
            if (!launcher || !launcher.HasAuthoredUi || windows.Length != 1 || !windows[0].HasAuthoredContent ||
                windows[0].gameObject.activeSelf || root.GetComponent<Image>() || !canvas ||
                canvas.renderMode != RenderMode.ScreenSpaceOverlay || canvas.sortingOrder != 500 ||
                !root.GetComponent<CanvasScaler>() || !root.GetComponent<GraphicRaycaster>())
                throw new InvalidOperationException("Existing InGameSettings requires valid authored UI; it will not be overwritten.");
            ValidateLocalReferences(windows[0].gameObject);
            ValidateLocalReferences(root);
        }

        private static bool HasLauncher(GameObject target)
        {
            var existing = target.GetComponentsInChildren<InGameSettingsLauncher>(true);
            foreach (var launcher in existing) ValidateLauncher(launcher.gameObject);
            return existing.Length != 0;
        }

        private static void Attach(GameObject target)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Launcher);
            var instance = PrefabUtility.InstantiatePrefab(asset, target.transform) as GameObject;
            if (!instance) throw new InvalidOperationException("Could not instantiate " + Launcher);
            instance.name = "InGameSettings";
        }

        private static void UpdateScreen()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(Screen);
            try
            {
                if (HasLauncher(root)) return;
                Attach(root);
                PrefabUtility.SaveAsPrefabAsset(root, Screen, out bool success);
                if (!success) throw new IOException("Could not save " + Screen);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void UpdateScene(string path)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool owned = !scene.IsValid() || !scene.isLoaded;
            if (owned) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var screens = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<InterviewScreen>(true)).ToArray();
                if (screens.Length == 0) throw new InvalidOperationException("InterviewScreen missing in " + path);
                bool changed = false;
                foreach (var screen in screens)
                {
                    if (HasLauncher(screen.gameObject)) continue;
                    Attach(screen.gameObject);
                    changed = true;
                }
                if (!changed) return;
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + path);
            }
            finally
            {
                // Preserve loaded scenes and any unsaved changes after a failure.
                if (owned && scene.IsValid() && scene.isLoaded && !scene.isDirty)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
#endif
