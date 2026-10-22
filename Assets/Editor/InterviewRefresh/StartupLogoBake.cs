#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ohabjizol.Tutorial;

namespace Ohabjizol.Editor.InterviewRefresh
{
    public static class StartupLogoBake
    {
        private const string ScenePath = "Assets/Scenes/MainMenuScene.unity";

        [MenuItem("Tools/Interview Refresh/Bake Startup Logos")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Startup logos require an idle editor in Edit mode.");

            var active = SceneManager.GetActiveScene();
            var target = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !target.IsValid() || !target.isLoaded;
            string backup = Path.Combine("Logs/StartupLogos/Before", DateTime.Now.ToString("yyyyMMdd_HHmmss_fffffff"));
            Directory.CreateDirectory(backup);
            File.Copy(ScenePath, Path.Combine(backup, "MainMenuScene.unity"));
            File.Copy("ProjectSettings/ProjectSettings.asset", Path.Combine(backup, "ProjectSettings.asset"));
            if (!opened && target.isDirty && !EditorSceneManager.SaveScene(target, Path.Combine(backup, "MainMenuScene.loaded-dirty.unity"), true))
                throw new IOException("Could not back up the dirty loaded MainMenu scene.");

            bool succeeded = false;
            try
            {
                if (opened) target = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                var behaviours = target.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                var titles = behaviours.Where(x => x && x.GetType().Name == "MainMenuTitleOverlay").ToArray();
                if (titles.Length != 1) throw new InvalidOperationException("Expected exactly one MainMenuTitleOverlay.");
                var title = titles[0];
                var titleData = new SerializedObject(title);
                var promptProperty = titleData.FindProperty("prompt");
                var prompt = promptProperty != null ? promptProperty.objectReferenceValue as Text : null;
                if (!prompt) throw new InvalidOperationException("MainMenuTitleOverlay.prompt must reference a UI Text.");
                var sequences = behaviours.OfType<StartupLogoSequence>().ToArray();
                if (sequences.Length > 1) throw new InvalidOperationException("Multiple StartupLogoSequence components found.");
                var team = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Logo/TeamLogo.png");
                var game = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Logo/GameTitle.png");
                if (!team || !game) throw new InvalidOperationException("Logo Texture2D assets are missing.");
                var sequence = sequences.Length == 1 ? sequences[0] : null;
                Canvas canvas = null;
                Image shield = null;
                CanvasGroup[] pages = null;
                CanvasGroup legacyUnityPage = null;
                if (sequence)
                {
                    canvas = sequence.GetComponent<Canvas>();
                    var saved = new SerializedObject(sequence).FindProperty("pages");
                    if (!canvas || saved == null || (saved.arraySize != 2 && saved.arraySize != 3))
                        throw new InvalidOperationException("Expected two startup pages or the previous three-page layout.");
                    int offset = saved.arraySize == 3 ? 1 : 0;
                    if (offset == 1) legacyUnityPage = saved.GetArrayElementAtIndex(0).objectReferenceValue as CanvasGroup;
                    pages = new[] {
                        saved.GetArrayElementAtIndex(offset).objectReferenceValue as CanvasGroup,
                        saved.GetArrayElementAtIndex(offset + 1).objectReferenceValue as CanvasGroup
                    };
                    if (pages.Any(x => !x) || !pages[0].GetComponentsInChildren<RawImage>(true).Any(x => x.texture == team)
                        || !pages[1].GetComponentsInChildren<RawImage>(true).Any(x => x.texture == game))
                        throw new InvalidOperationException("Team and game logo pages must retain their texture references.");
                    if (legacyUnityPage && !legacyUnityPage.GetComponentsInChildren<Text>(true).Any(x => x.name == "MadeWithUnity"))
                        throw new InvalidOperationException("Legacy Unity page no longer matches the known layout; preserve it for manual review.");
                    shield = sequence.GetComponentsInChildren<Image>(true)
                        .FirstOrDefault(x => x.name == "Shield" && !x.GetComponent<CanvasGroup>());
                    if (!shield) throw new InvalidOperationException("Authored startup UI is missing Shield Image.");
                }
                else
                {
                    var root = new GameObject("StartupLogoSequence", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    SceneManager.MoveGameObjectToScene(root, target);
                    root.transform.SetParent(title.transform.parent, false);
                    canvas = root.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 2100;
                    var scaler = root.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = .5f;
                    shield = MakeImage("Shield", root.transform, Color.black);
                    shield.raycastTarget = true;
                    pages = new CanvasGroup[2];
                    Color[] colors = { Color.black, Color.white };
                    for (int i = 0; i < 2; i++)
                    {
                        var background = MakeImage(i == 0 ? "TeamLogoPage" : "GameTitlePage", shield.transform, colors[i]);
                        pages[i] = background.gameObject.AddComponent<CanvasGroup>();
                        pages[i].alpha = 0;
                        pages[i].interactable = false;
                        pages[i].blocksRaycasts = false;
                        background.gameObject.SetActive(i == 0);
                    }
                    MakeLogo(pages[0].transform, team, new Vector2(.25f, .20f), new Vector2(.75f, .80f));
                    MakeLogo(pages[1].transform, game, new Vector2(.12f, .12f), new Vector2(.88f, .88f));
                    sequence = root.AddComponent<StartupLogoSequence>();
                }

                sequence.ConfigureEditor(canvas, shield, pages);
                if (legacyUnityPage) Undo.DestroyObjectImmediate(legacyUnityPage.gameObject);
                var sequenceData = new SerializedObject(sequence);
                var duration = sequenceData.FindProperty("partDuration");
                var fade = sequenceData.FindProperty("fadeDuration");
                if (duration == null || fade == null) throw new InvalidOperationException("Runtime duration fields are missing.");
                duration.floatValue = 1.5f;
                fade.floatValue = .3f;
                sequenceData.ApplyModifiedPropertiesWithoutUndo();
                var promptData = new SerializedObject(prompt);
                var text = promptData.FindProperty("m_Text");
                if (text == null) throw new InvalidOperationException("Prompt text serialization is unavailable.");
                text.stringValue = "화면 터치하여 시작";
                promptData.ApplyModifiedPropertiesWithoutUndo();
                title.gameObject.SetActive(true);
                EditorUtility.SetDirty(title.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(title.gameObject);
                EditorUtility.SetDirty(prompt);
                EditorUtility.SetDirty(sequence);
                PrefabUtility.RecordPrefabInstancePropertyModifications(prompt);
                PrefabUtility.RecordPrefabInstancePropertyModifications(sequence);
                EditorSceneManager.MarkSceneDirty(target);
                if (!EditorSceneManager.SaveScene(target, ScenePath)) throw new IOException("Could not save MainMenu scene.");
                SaveNativeSplashSetting();
                succeeded = true;
                Debug.Log("Startup logos saved. Backups: " + Path.GetFullPath(backup));
            }
            finally
            {
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                // Leave a failed, dirty scene open so manual recovery remains possible.
                if (opened && target.IsValid() && target.isLoaded && (succeeded || !target.isDirty))
                    EditorSceneManager.CloseScene(target, true);
            }
        }

        public static void SaveNativeSplashSetting()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Splash settings require idle Edit mode.");
            // This editor's native setter updates memory but does not persist the flag.
            // Change the single serialized field while preserving the rest of the file.
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/ProjectSettings.asset"));
            byte[] bytes = File.ReadAllBytes(path);
            string text = File.ReadAllText(path);
            var flag = new Regex(@"(?m)^(  m_ShowUnitySplashScreen: )[01](?=\r?$)");
            if (flag.Matches(text).Count != 1)
                throw new InvalidOperationException("Expected exactly one serialized native splash flag.");
            PlayerSettings.SplashScreen.show = false;
            string updated = flag.Replace(text, match => match.Groups[1].Value + "0");
            if (updated != text)
            {
                bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                File.WriteAllText(path, updated, new UTF8Encoding(bom));
            }
            if (!flag.Match(File.ReadAllText(path)).Value.EndsWith("0", StringComparison.Ordinal))
                throw new IOException("Native splash flag was not saved.");
        }

        private static Image MakeImage(string name, Transform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            Stretch(image.rectTransform, Vector2.zero, Vector2.one);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void MakeLogo(Transform parent, Texture2D texture, Vector2 min, Vector2 max)
        {
            var bounds = new GameObject(texture.name + "Bounds", typeof(RectTransform)).GetComponent<RectTransform>();
            bounds.SetParent(parent, false);
            Stretch(bounds, min, max);
            var image = new GameObject(texture.name, typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter)).GetComponent<RawImage>();
            image.transform.SetParent(bounds, false);
            Stretch(image.rectTransform, Vector2.zero, Vector2.one);
            image.texture = texture;
            image.raycastTarget = false;
            var fitter = image.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)texture.width / texture.height;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

    }
}
#endif
