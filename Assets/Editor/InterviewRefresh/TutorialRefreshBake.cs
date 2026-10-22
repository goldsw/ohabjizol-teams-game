#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Ohabjizol.Interview;
using Ohabjizol.Presentation;
using Ohabjizol.Tutorial;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TutorialRefreshBake
{
    const string InterviewPath = "Assets/Scenes/InterviewScene.unity";
    const string MainPath = "Assets/Scenes/MainMenuScene.unity";
    const string TutorialPath = "Assets/Scenes/TutorialScene.unity";
    const string ScreenPath = "Assets/Prefabs/InterviewRefresh/InterviewScreen.prefab";
    const string ProfilePath = "Assets/Data/Interview/Profiles/character-01.asset";
    const string VisualsPath = "Assets/Art/Applicants/ApplicantVisualCatalog.asset";

    // Run only after the parent has applied the shared Screen prefab changes.
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Tutorial authoring requires EditMode.");
        Scene active = SceneManager.GetActiveScene();
        var opened = new List<Scene>();
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath);
            var profile = AssetDatabase.LoadAssetAtPath<ApplicantProfileAsset>(ProfilePath);
            var catalog = AssetDatabase.LoadAssetAtPath<ApplicantVisualCatalog>(VisualsPath);
            if (!prefab || !profile || !catalog || !catalog.TryGet("character-12", out var visual) || !visual.Idle)
                throw new InvalidOperationException("Updated Screen prefab, demo profile and supervisor Idle are required.");
            Scene interview = Open(InterviewPath, opened);
            Scene tutorial = SceneManager.GetSceneByPath(TutorialPath);
            if (!tutorial.IsValid() || !tutorial.isLoaded)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TutorialPath)) tutorial = Open(TutorialPath, opened);
                else
                {
                    tutorial = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    opened.Add(tutorial);
                }
            }
            SceneManager.SetActiveScene(tutorial);
            var bootstrap = SingleInScene<TutorialSceneBootstrap>(tutorial, false);
            if (!bootstrap)
                AuthorTutorial(tutorial, interview, prefab, profile, visual.Idle);
            else
            {
                AuthorSpotlight(SingleInScene<TutorialOverlay>(tutorial, true));
                bootstrap.RefreshEditor(profile, visual.Idle);
                EnsureButtons(tutorial);
            }
            AuthorSpeech(SingleInScene<TutorialOverlay>(tutorial, true));
            EditorSceneManager.MarkSceneDirty(tutorial);
            if (!EditorSceneManager.SaveScene(tutorial, TutorialPath))
                throw new InvalidOperationException("TutorialScene save failed.");

            Scene main = Open(MainPath, opened);
            SceneManager.SetActiveScene(main);
            var prompt = SingleInScene<TutorialWindow>(main, true);
            prompt.BakePromptEditor();
            var controller = SingleInScene<MainMenuController>(main, true);
            var title = SingleInScene<MainMenuTitleOverlay>(main, false);
            MainMenuTitleOverlay.CreateEditor(title ? title.transform.parent : controller.transform);
            EnsureButtons(main);
            EditorSceneManager.MarkSceneDirty(main);
            if (!EditorSceneManager.SaveScene(main, MainPath))
                throw new InvalidOperationException("MainMenuScene save failed.");
            UpdateBuildSettings();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainPath);
        }
        finally
        {
            // Never close a scene that was already open when Apply began.
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            for (int i = opened.Count - 1; i >= 0; --i)
                if (opened[i].IsValid() && opened[i].isLoaded) EditorSceneManager.CloseScene(opened[i], true);
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        }
    }

    [MenuItem("Tools/Interview Refresh/Apply Tutorial Speech")]
    public static void ApplySpeech()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Tutorial authoring requires EditMode.");
        Scene active = SceneManager.GetActiveScene();
        var opened = new List<Scene>();
        try
        {
            Scene tutorial = Open(TutorialPath, opened);
            string backupDirectory = System.IO.Path.Combine("Library", "TutorialSpeechBackups",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
            System.IO.Directory.CreateDirectory(backupDirectory);
            if (System.IO.File.Exists(TutorialPath))
                System.IO.File.Copy(TutorialPath, System.IO.Path.Combine(backupDirectory, "TutorialScene.disk.unity"));
            if (tutorial.isDirty)
            {
                string copy = System.IO.Path.Combine(backupDirectory, "TutorialScene.dirty.unity").Replace('\\', '/');
                if (!EditorSceneManager.SaveScene(tutorial, copy, true))
                    throw new InvalidOperationException("Dirty TutorialScene backup failed.");
            }
            AuthorSpeech(SingleInScene<TutorialOverlay>(tutorial, true));
            EditorSceneManager.MarkSceneDirty(tutorial);
            if (!EditorSceneManager.SaveScene(tutorial, TutorialPath))
                throw new InvalidOperationException("TutorialScene save failed.");
            Debug.Log("Tutorial speech backup: " + System.IO.Path.GetFullPath(backupDirectory));
        }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            for (int i = opened.Count - 1; i >= 0; --i)
                if (opened[i].IsValid() && opened[i].isLoaded) EditorSceneManager.CloseScene(opened[i], true);
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        }
    }

    static void AuthorSpeech(TutorialOverlay overlay)
    {
        var idle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Applicants/character-12/idle.png");
        var talk = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Applicants/character-12/talk.png");
        var panel = overlay.transform.Find("SupervisorCover");
        var nameTransform = panel ? panel.Find("SupervisorName") : null;
        var name = nameTransform ? nameTransform.GetComponent<Text>() : null;
        if (!idle || !talk || !name)
            throw new InvalidOperationException("Tutorial supervisor Idle, Talk and name are required.");
        overlay.SetSupervisorSpeechEditor(idle, talk);
        name.text = "선배";
        EditorUtility.SetDirty(name);
    }

    [MenuItem("Tools/Interview Refresh/Apply Tutorial Spotlight")]
    public static void ApplySpotlight()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Tutorial authoring requires EditMode.");
        Scene active = SceneManager.GetActiveScene();
        var opened = new List<Scene>();
        try
        {
            Scene tutorial = Open(TutorialPath, opened);
            string backupDirectory = System.IO.Path.Combine("Library", "TutorialSpotlightBackups",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
            System.IO.Directory.CreateDirectory(backupDirectory);
            if (System.IO.File.Exists(TutorialPath))
                System.IO.File.Copy(TutorialPath, System.IO.Path.Combine(backupDirectory, "TutorialScene.disk.unity"));
            if (tutorial.isDirty)
            {
                string copy = System.IO.Path.Combine(backupDirectory, "TutorialScene.dirty.unity").Replace('\\', '/');
                if (!EditorSceneManager.SaveScene(tutorial, copy, true))
                    throw new InvalidOperationException("Dirty TutorialScene backup failed.");
                Debug.Log("Dirty TutorialScene backup: " + System.IO.Path.GetFullPath(copy));
            }
            AuthorSpotlight(SingleInScene<TutorialOverlay>(tutorial, true));
            EditorSceneManager.MarkSceneDirty(tutorial);
            if (!EditorSceneManager.SaveScene(tutorial, TutorialPath))
                throw new InvalidOperationException("TutorialScene save failed.");
        }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            for (int i = opened.Count - 1; i >= 0; --i)
                if (opened[i].IsValid() && opened[i].isLoaded) EditorSceneManager.CloseScene(opened[i], true);
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        }
    }

    static Scene Open(string path, List<Scene> opened)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        if (scene.IsValid() && scene.isLoaded) return scene;
        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        opened.Add(scene);
        return scene;
    }

    static T SingleInScene<T>(Scene scene, bool required) where T : Component
    {
        T found = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<T>(true))
            {
                if (found) throw new InvalidOperationException("Multiple " + typeof(T).Name + " objects in " + scene.path);
                found = component;
            }
        if (required && !found) throw new InvalidOperationException("Missing " + typeof(T).Name + " in " + scene.path);
        return found;
    }

    static void AuthorTutorial(Scene scene, Scene interview, GameObject prefab, ApplicantProfileAsset profile, Sprite idle)
    {
        var host = new GameObject("TutorialHost");
        SceneManager.MoveGameObjectToScene(host, scene);
        try
        {
        var wrapper = new GameObject("InactiveInterviewWrapper");
        wrapper.transform.SetParent(host.transform, false);
        wrapper.SetActive(false);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wrapper.transform);
        var screens = instance.GetComponentsInChildren<InterviewScreen>(true);
        if (screens.Length != 1) throw new InvalidOperationException("Shared prefab must contain exactly one InterviewScreen.");
        var screen = screens[0];
        screen.gameObject.SetActive(true);
        PrefabUtility.RecordPrefabInstancePropertyModifications(screen.gameObject);
        screen.ConfigureTutorialTargetsEditor();
        var cameraObject = new GameObject("TutorialUiCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.transform.SetParent(host.transform, false);
        cameraObject.transform.localPosition = new Vector3(0, 0, -10);
        var uiCamera = cameraObject.GetComponent<Camera>();
        uiCamera.clearFlags = CameraClearFlags.SolidColor;
        uiCamera.backgroundColor = new Color(.08f, .09f, .11f);
        uiCamera.orthographic = true;
        foreach (var canvas in instance.GetComponentsInChildren<Canvas>(true))
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.worldCamera) continue;
            canvas.worldCamera = uiCamera;
            EditorUtility.SetDirty(canvas);
            PrefabUtility.RecordPrefabInstancePropertyModifications(canvas);
        }
        var sourceEvents = SingleInScene<EventSystem>(interview, true);
        if (sourceEvents.transform.parent || !sourceEvents.GetComponent<InputSystemUIInputModule>())
            throw new InvalidOperationException("InterviewScene requires its authored root InputSystem EventSystem.");
        var events = Object.Instantiate(sourceEvents.gameObject);
        events.name = "TutorialEventSystem";
        SceneManager.MoveGameObjectToScene(events, scene);
        events.transform.SetParent(host.transform, false);
        events.SetActive(true);
        var font = AssetDatabase.LoadAssetAtPath<Font>(Ohabjizol.UI.UiKit.FontAssetPath);
        if (!font) throw new InvalidOperationException("Tutorial requires the persistent PFStardustBold font.");
        var overlay = AuthorOverlay(host.transform, font, idle);
        var controller = host.AddComponent<TutorialController>();
        var bootstrap = host.AddComponent<TutorialSceneBootstrap>();
        bootstrap.ConfigureEditor(wrapper, screen, profile, controller, overlay);
        EditorUtility.SetDirty(bootstrap);
        EnsureButtons(scene);
        }
        catch
        {
            Object.DestroyImmediate(host);
            throw;
        }
    }

    static TutorialOverlay AuthorOverlay(Transform parent, Font font, Sprite idle)
    {
        var root = new GameObject("TutorialOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(Image));
        root.transform.SetParent(parent, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32760;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        var mask = root.GetComponent<Image>();
        mask.color = new Color(0, 0, 0, .42f);
        mask.raycastTarget = true;
        var overlay = root.AddComponent<TutorialOverlay>();
        var highlight = Rect(root.transform, "TargetOutline");
        foreach (var edge in new[] { "Top", "Bottom", "Left", "Right" })
        {
            var border = Image(highlight, edge, new Color(1, .85f, .25f));
            var rect = border.rectTransform;
            switch (edge)
            {
                case "Top": Place(rect, 0, 1, 1, 1); rect.sizeDelta = new Vector2(8, 4); break;
                case "Bottom": Place(rect, 0, 0, 1, 0); rect.sizeDelta = new Vector2(8, 4); break;
                case "Left": Place(rect, 0, 0, 0, 1); rect.sizeDelta = new Vector2(4, 8); break;
                default: Place(rect, 1, 0, 1, 1); rect.sizeDelta = new Vector2(4, 8); break;
            }
        }
        var supervisor = Image(root.transform, "SupervisorCover", new Color(.11f, .13f, .18f));
        var fade = supervisor.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0; fade.blocksRaycasts = false; fade.interactable = false;
        var portrait = Image(supervisor.transform, "SupervisorPortrait", Color.white);
        portrait.sprite = idle; portrait.preserveAspect = true;
        Place(portrait.rectTransform, .05f, .12f, .95f, .97f);
        var name = Label(supervisor.transform, "SupervisorName", "선배", font, 28);
        Place(name.rectTransform, .05f, .01f, .95f, .14f);
        var captionBack = Image(root.transform, "CaptionBackground", new Color(.06f, .07f, .1f, .97f));
        Place(captionBack.rectTransform, .12f, .035f, .88f, .18f);
        var caption = Label(captionBack.transform, "Dialogue", string.Empty, font, 26);
        Place(caption.rectTransform, .025f, .15f, .8f, .85f);
        overlay.ConfigureEditor(highlight, caption, null, supervisor.rectTransform, portrait, fade, idle);
        AuthorSpotlight(overlay);
        EditorUtility.SetDirty(overlay);
        return overlay;
    }

    static void AuthorSpotlight(TutorialOverlay overlay)
    {
        var root = overlay.transform;
        var rootImage = root.GetComponent<Image>();
        var captionBack = root.Find("CaptionBackground");
        var dialogue = captionBack ? captionBack.Find("Dialogue") : null;
        var caption = dialogue ? dialogue.GetComponent<Text>() : null;
        if (!rootImage || !caption)
            throw new InvalidOperationException("TutorialOverlay requires its authored root Image and CaptionBackground/Dialogue.");
        rootImage.color = Color.clear;
        rootImage.raycastTarget = true;
        rootImage.canvasRenderer.cullTransparentMesh = false;
        var next = captionBack.Find("Next");
        if (next) next.gameObject.SetActive(false);

        var maskTransform = root.Find("MaskRoot");
        var maskRect = maskTransform ? (RectTransform)maskTransform : Rect(root, "MaskRoot");
        maskRect.SetAsFirstSibling();
        Place(maskRect, 0, 0, 1, 1);
        var writer = maskRect.GetComponent<Image>();
        if (!writer) writer = maskRect.gameObject.AddComponent<Image>();
        writer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        writer.color = Color.white;
        writer.raycastTarget = false;
        var cutout = maskRect.GetComponent<Mask>();
        if (!cutout) cutout = maskRect.gameObject.AddComponent<Mask>();
        cutout.showMaskGraphic = false;
        var dimmerTransform = maskRect.Find("Dimmer");
        var dimmerRect = dimmerTransform ? (RectTransform)dimmerTransform : Rect(maskRect, "Dimmer");
        var dimmer = dimmerRect.GetComponent<Image>();
        if (!dimmer) dimmer = dimmerRect.gameObject.AddComponent<Image>();
        Place(dimmerRect, 0, 0, 1, 1);
        dimmer.maskable = false;
        dimmer.raycastTarget = false;
        dimmer.color = new Color(0, 0, 0, .55f);
        dimmer.material = SpotlightMaterial();

        var hintTransform = captionBack.Find("ContinueHint");
        var hintRect = hintTransform ? (RectTransform)hintTransform : Rect(captionBack, "ContinueHint");
        var hint = hintRect.GetComponent<Text>();
        if (!hint) hint = hintRect.gameObject.AddComponent<Text>();
        hint.font = caption.font;
        hint.fontSize = 22;
        hint.fontStyle = caption.fontStyle;
        hint.color = Color.white;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.horizontalOverflow = HorizontalWrapMode.Wrap;
        hint.verticalOverflow = VerticalWrapMode.Truncate;
        hint.raycastTarget = false;
        hint.text = "아무 곳이나 눌러 계속 . . .";
        Place(caption.rectTransform, .025f, .36f, .975f, .94f);
        Place(hintRect, .025f, .04f, .975f, .34f);
        overlay.ConfigureSpotlightEditor(cutout, dimmer, hint);
        EditorUtility.SetDirty(overlay);
    }

    static Material SpotlightMaterial()
    {
        const string folder = "Assets/Art/Tutorial";
        const string path = folder + "/TutorialInverseMask.mat";
        var shader = Shader.Find("UI/Default");
        if (!shader) throw new InvalidOperationException("UI/Default shader is required.");
        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Art", "Tutorial");
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(shader) { name = "TutorialInverseMask" };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetInt("_StencilComp", (int)UnityEngine.Rendering.CompareFunction.NotEqual);
        material.SetInt("_Stencil", 1);
        material.SetInt("_StencilOp", (int)UnityEngine.Rendering.StencilOp.Keep);
        material.SetInt("_StencilReadMask", 1);
        material.SetInt("_StencilWriteMask", 0);
        material.SetInt("_ColorMask", (int)UnityEngine.Rendering.ColorWriteMask.All);
        material.SetFloat("_UseUIAlphaClip", 0);
        material.DisableKeyword("UNITY_UI_ALPHACLIP");
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    static RectTransform Rect(Transform parent, string name)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    static Image Image(Transform parent, string name, Color color)
    {
        var image = Rect(parent, name).gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false; return image;
    }
    static Text Label(Transform parent, string name, string value, Font font, int size)
    {
        var label = Rect(parent, name).gameObject.AddComponent<Text>();
        label.font = font; label.fontSize = size; label.text = value;
        label.color = Color.white; label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false; return label;
    }
    static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    static void EnsureButtons(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var button in root.GetComponentsInChildren<Button>(true)) ButtonActiveBackground.Ensure(button);
    }
    static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MainPath, true) };
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.path != MainPath && scene.path != TutorialPath) scenes.Add(scene);
        scenes.Add(new EditorBuildSettingsScene(TutorialPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
