#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Ohabjizol.Presentation;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Explicit editor commands only. No scene is opened or saved by this tool.</summary>
public sealed class MainMenuEnvironmentAuthoring : ScriptableObject
{
    public MainMenuEnvironmentSettings settings = new MainMenuEnvironmentSettings();
    public const string SettingsPath = "Assets/Editor/MainMenuEnvironmentSettings.asset";
    const string DeskPath = "Assets/Art/Background/Models/Desk.fbx";
    const string RoomPath = "Assets/Art/Background/Textures/Desk/갸오방.png";
    const string WallpaperPath = "Assets/Art/Background/Textures/Misc/Background.png";
    const string SettingPath = "Assets/Art/Background/UI/Lobby/Setting.png";
    const string BinPath = "Assets/Art/Background/UI/Lobby/Bin.png";

    [MenuItem("Ohabjizol/Main Menu/Apply Desk Environment")]
    public static void ApplyDeskEnvironment()
    {
        RequireEditMode();
        const string menuPath = "Assets/Scenes/MainMenuScene.unity";
        const string loadingPath = "Assets/Scenes/LoadingScene.unity";
        // Asset/geometry validation precedes opening scenes, importer changes and UI mutation.
        var modelImporter = AssetImporter.GetAtPath(DeskPath) as ModelImporter;
        Require(modelImporter != null, "Desk.fbx requires a ModelImporter.");
        if (!modelImporter.isReadable)
        {
            modelImporter.isReadable = true;
            modelImporter.SaveAndReimport();
        }
        var model = Load<GameObject>(DeskPath);
        Load<Texture2D>(RoomPath);
        Load<Texture2D>(WallpaperPath);
        Load<Texture2D>(SettingPath);
        Load<Texture2D>(BinPath);
        Load<SceneAsset>(menuPath);
        Load<SceneAsset>(loadingPath);
        var measured = MeasureDesk(model);
        Require(Shader.Find("Universal Render Pipeline/Unlit") != null && Shader.Find("Universal Render Pipeline/Lit") != null,
            "Required URP shaders are unavailable.");
        Require(UiKit.Font != null, "UiKit Korean font must be available before applying scene UI.");
        var previousActive = SceneManager.GetActiveScene();
        var menuScene = SceneManager.GetSceneByPath(menuPath);
        bool openedMenu = !menuScene.isLoaded;
        if (openedMenu) menuScene = EditorSceneManager.OpenScene(menuPath, OpenSceneMode.Additive);
        var loadingScene = SceneManager.GetSceneByPath(loadingPath);
        bool openedLoading = !loadingScene.isLoaded;
        // Never save pre-existing user edits as part of the one-click operation.
        Require(!menuScene.isDirty, "MainMenuScene has existing unsaved edits; save them explicitly before applying.");
        Require(openedLoading || !loadingScene.isDirty, "LoadingScene has existing unsaved edits; save them explicitly before migration.");
        try
        {
            if (openedLoading) loadingScene = EditorSceneManager.OpenScene(loadingPath, OpenSceneMode.Additive);
            var menu = Unique<MainMenuController>(menuScene);
            var legacyScreen = Unique<ComputerScreen>(loadingScene);
            var legacyTransition = Unique<ComputerTransitionController>(loadingScene);
            var legacyCamera = SceneCamera(loadingScene);
            var menuData = new SerializedObject(menu);
            var owner = menuData.FindProperty("cameraController").objectReferenceValue as MainMenuCameraController;
            Require(owner != null && owner.GetComponent<Camera>() != null, "Main menu requires its existing authored camera.");
            // Resolve the existing wallpaper through authored Image references, never a global scene lookup.
            var desktop = menuData.FindProperty("_desktop").objectReferenceValue as DesktopView;
            Require(desktop != null, "Main menu requires its existing Desktop.");
            var settings = measured.settings;
            settings.wallpaperRelativePath = FindWallpaperPath(desktop);
            ConfigureDesktopPose(settings, measured, owner.GetComponent<Camera>().aspect);
            ValidateLegacyFrame(legacyScreen);
            EnsureSprite(WallpaperPath);
            EnsureSprite(SettingPath);
            EnsureSprite(BinPath);
            var asset = CreateSettings();
            Undo.RecordObject(asset, "Store measured Desk environment settings");
            asset.settings = settings;
            EditorUtility.SetDirty(asset);
            SceneManager.SetActiveScene(menuScene);
            Setup(menu, settings);
            SetupLegacyLoadingScene(legacyScreen, legacyTransition, legacyCamera);
            Require(EditorSceneManager.SaveScene(menuScene, menuPath), "Could not save authored MainMenuScene.");
            Require(EditorSceneManager.SaveScene(loadingScene, loadingPath), "Could not save migrated LoadingScene.");
            AssetDatabase.SaveAssetIfDirty(asset);
            SaveGeneratedMaterials(menuScene);
            Debug.Log("Desk environment applied and both target scenes saved. MainMenuScene remains active.", menu);
        }
        catch
        {
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            throw;
        }
        finally
        {
            // Only the additive LoadingScene opened by this command may be closed.
            // Retain dirty failures for inspection; never discard them.
            if (openedLoading && loadingScene.IsValid() && loadingScene.isLoaded && !loadingScene.isDirty)
                EditorSceneManager.CloseScene(loadingScene, true);
        }
    }

    [MenuItem("Tools/Main Menu/1. Create or Select Authoring Settings")]
    public static void SelectSettings()
    {
        var asset = CreateSettings();
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    [MenuItem("Tools/Main Menu/2. Apply to Open Main Menu Scene")]
    public static void ApplyFromMenu() { Apply(); }

    [MenuItem("Tools/Main Menu/3. Migrate Open Legacy Loading Scene")]
    public static void MigrateLegacyFromMenu() { MigrateLegacyLoadingScene(); }

    public static MainMenuEnvironmentAuthoring CreateSettings()
    {
        RequireEditMode();
        var existing = AssetDatabase.LoadAssetAtPath<MainMenuEnvironmentAuthoring>(SettingsPath);
        if (existing != null) return existing;
        var asset = ScriptableObject.CreateInstance<MainMenuEnvironmentAuthoring>();
        AssetDatabase.CreateAsset(asset, SettingsPath);
        return asset;
    }

    // Suitable for parent CLI invocation after explicitly opening the intended scene.
    public static void Apply()
    {
        RequireEditMode();
        Require(SceneManager.GetActiveScene().name == "MainMenuScene", "Explicitly open and activate MainMenuScene before applying authoring.");
        var asset = AssetDatabase.LoadAssetAtPath<MainMenuEnvironmentAuthoring>(SettingsPath);
        MainMenuController menu = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<MainMenuController>(true))
            {
                Require(menu == null, "Active MainMenuScene must contain exactly one MainMenuController.");
                menu = candidate;
            }
        Setup(menu, asset != null ? asset.settings : null);
    }

    public static void Setup(MainMenuController menu, MainMenuEnvironmentSettings settings)
    {
        RequireEditMode();
        Require(menu != null && settings != null, "Provide the open MainMenu controller and authored settings asset.");
        Require(settings.geometryConfirmed, "Desk geometry, screen pose, intro/desktop poses and room/glow placements require parent confirmation.");
        Require(settings.sourceModel == "Assets/Art/Background/Source~/Models/Desk.blend", "Only the user-selected Desk.blend export is authorized.");
        var deskAsset = Load<GameObject>(DeskPath);
        Require(!string.IsNullOrEmpty(settings.monitorMeshPath) && deskAsset.transform.Find(settings.monitorMeshPath) != null,
            "Confirmed monitorMeshPath must exist in Desk.fbx.");
        var room = Load<Texture2D>(RoomPath);
        var wallpaper = Load<Sprite>(WallpaperPath);
        var setting = Load<Sprite>(SettingPath);
        var bin = Load<Sprite>(BinPath);
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        Require(shader != null, "URP Unlit shader is required.");
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        Require(litShader != null, "URP Lit shader is required for imported Standard materials.");
        Require(settings.displaySize.x > 0f && settings.displaySize.y > 0f && settings.canvasSize.x > 0f && settings.canvasSize.y > 0f,
            "Confirmed positive display and Canvas dimensions are required.");
        Require(Positive(settings.deskScale) && Positive(settings.roomScale) && Positive(settings.glowScale),
            "Desk, room and glow scales must be explicitly authored and positive.");
        var serialized = new SerializedObject(menu);
        var desktop = serialized.FindProperty("_desktop").objectReferenceValue as DesktopView;
        var screenRoot = serialized.FindProperty("viewRoot").objectReferenceValue as Transform;
        var cameraOwner = serialized.FindProperty("cameraController").objectReferenceValue as MainMenuCameraController;
        var screen = screenRoot != null ? screenRoot.GetComponentInChildren<ComputerScreen>(true) : null;
        var canvas = screen != null ? screen.GetComponentInChildren<Canvas>(true) : null;
        Require(desktop != null && screen != null && canvas != null && cameraOwner != null,
            "Reuse requires existing authored Desktop, ComputerScreen, WorldCanvas and camera references.");
        var camera = cameraOwner.GetComponent<Camera>();
        Require(camera != null && camera.CompareTag("MainCamera"), "Controller must be on the camera tagged MainCamera.");
        Require(canvas.transform is RectTransform, "Existing screen Canvas must use a RectTransform.");
        var priorEnvironment = menu.transform.Find("AuthoredDeskEnvironment");
        var priorDesk = priorEnvironment != null ? priorEnvironment.Find("Desk") : null;
        Require(priorDesk == null || priorDesk.Find(settings.monitorMeshPath) != null,
            "Existing Desk instance does not contain the confirmed monitor path; reconcile the instance before applying.");
        var wallpaperImage = desktop.transform.Find(settings.wallpaperRelativePath)?.GetComponent<Image>();
        Require(wallpaperImage != null, "Set wallpaperRelativePath to the existing Desktop wallpaper Image.");
        var desktopData = new SerializedObject(desktop);
        var bindings = desktopData.FindProperty("_appBindings");
        Require(bindings != null && bindings.isArray, "Existing Desktop _appBindings schema is required.");
        DesktopIcon settingIcon = null, tutorialIcon = null;
        for (int i = 0; i < bindings.arraySize; i++)
        {
            var binding = bindings.GetArrayElementAtIndex(i);
            var handler = binding.FindPropertyRelative("activation").managedReferenceValue;
            var icon = binding.FindPropertyRelative("icon").objectReferenceValue as DesktopIcon;
            if (handler is SettingsAppHandler) settingIcon = icon;
            if (handler is TutorialAppHandler) tutorialIcon = icon;
        }
        Require(settingIcon != null && tutorialIcon != null, "Existing Settings and Tutorial bindings must remain valid.");
        var settingTile = settingIcon.transform.Find("Tile")?.GetComponent<Image>();
        Require(settingTile != null, "Settings icon requires its existing Tile Image.");
        var tutorialTile = tutorialIcon.transform.Find("Tile") as RectTransform;
        var wallpaperDesktopRoot = desktop.transform as RectTransform;
        Require(tutorialTile != null && wallpaperDesktopRoot != null,
            "Tutorial icon requires its existing Tile RectTransform and Desktop RectTransform.");
        // All asset/geometry/schema gates above precede any scene mutation.
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply MainMenu environment");
        try
        {
            var environment = Child(menu.transform, "AuthoredDeskEnvironment");
            var desk = environment.Find("Desk");
            if (desk == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(deskAsset, environment);
                Undo.RegisterCreatedObjectUndo(instance, "Create Desk prefab instance");
                instance.name = "Desk";
                desk = instance.transform;
            }
            SetLocal(desk, settings.deskPosition, settings.deskEuler, settings.deskScale);
            ConfigureImportedModel(desk, litShader);
            var monitor = desk.Find(settings.monitorMeshPath);
            Require(monitor != null, "Existing Desk instance does not match confirmed monitor mesh path.");
            Undo.SetTransformParent(screen.transform, monitor, "Reparent existing ComputerScreen");
            SetLocal(screen.transform, settings.screenLocalPosition, settings.screenLocalEuler,
                new Vector3(settings.displaySize.x, settings.displaySize.y, 1f));
            var screenData = new SerializedObject(screen);
            screenData.FindProperty("viewRoot").objectReferenceValue = environment;
            screenData.FindProperty("applyDefaultScreenColor").boolValue = false;
            screenData.FindProperty("worldCanvasSize").vector2Value = settings.canvasSize;
            screenData.ApplyModifiedProperties();
            var oldScreenRenderer = screen.GetComponent<Renderer>();
            if (oldScreenRenderer != null)
            {
                Undo.RecordObject(oldScreenRenderer, "Use FBX monitor surface instead of placeholder");
                oldScreenRenderer.enabled = false;
            }
            // PlaceWorldCanvas is an editor invocation; no runtime placement is introduced.
            Undo.RecordObject(canvas.transform, "Fit existing monitor Canvas");
            screen.PlaceWorldCanvas((RectTransform)canvas.transform, camera);
            if (canvas.GetComponent<RectMask2D>() == null) Undo.AddComponent<RectMask2D>(canvas.gameObject);
            var collider = screen.GetComponent<BoxCollider>();
            if (collider == null) collider = Undo.AddComponent<BoxCollider>(screen.gameObject);
            Undo.RecordObject(collider, "Configure monitor picking");
            collider.center = Vector3.zero;
            collider.size = new Vector3(1f, 1f, 0.01f);
            var rig = Child(environment, "CameraPoseRoot");
            var pivot = Child(rig, "MouseLookPivot");
            SetLocal(rig, Vector3.zero, Vector3.zero, Vector3.one);
            SetLocal(pivot, Vector3.zero, Vector3.zero, Vector3.one);
            Undo.SetTransformParent(camera.transform, pivot, "Parent menu camera to authored rig");
            SetLocal(camera.transform, Vector3.zero, Vector3.zero, Vector3.one);
            Undo.RecordObject(camera, "Use artist camera field of view");
            camera.fieldOfView = settings.cameraFov;
            SetLocal(rig, settings.introPosition, settings.introEuler, Vector3.one);
            var cameraData = new SerializedObject(cameraOwner);
            cameraData.FindProperty("cameraPoseRoot").objectReferenceValue = rig;
            cameraData.FindProperty("mouseLookPivot").objectReferenceValue = pivot;
            cameraData.FindProperty("computerScreenFrame").objectReferenceValue = canvas.transform;
            cameraData.ApplyModifiedProperties();
            var transition = menu.GetComponent<ComputerTransitionController>();
            if (transition == null) transition = Undo.AddComponent<ComputerTransitionController>(menu.gameObject);
            SetReference(transition, "screenFrame", canvas.transform);
            SetReference(transition, "targetComputerScreen", screen.transform);
            var intro = Child(environment, "IntroStart");
            var desktopPose = Child(environment, "DesktopPose");
            SetLocal(intro, settings.introPosition, settings.introEuler, Vector3.one);
            SetLocal(desktopPose, settings.desktopPosition, settings.desktopEuler, Vector3.one);
            var loading = SetupLoadingLayer(canvas);
            serialized.Update();
            serialized.FindProperty("viewRoot").objectReferenceValue = environment;
            serialized.FindProperty("transition").objectReferenceValue = transition;
            serialized.FindProperty("loadingPresenter").objectReferenceValue = loading;
            serialized.FindProperty("introStart").objectReferenceValue = intro;
            serialized.FindProperty("zoomTarget").objectReferenceValue = desktopPose;
            serialized.FindProperty("connectDelay").floatValue = 0f;
            serialized.ApplyModifiedProperties();
            Undo.RecordObject(wallpaperImage, "Set Desktop wallpaper");
            wallpaperImage.sprite = wallpaper;
            wallpaperImage.color = Color.white;
            wallpaperImage.type = UnityEngine.UI.Image.Type.Simple;
            wallpaperImage.preserveAspect = true;
            if (wallpaperImage.transform.parent.name != "AuthoredWallpaperClip")
            {
                int index = wallpaperImage.transform.GetSiblingIndex();
                var clip = Child(wallpaperImage.transform.parent, "AuthoredWallpaperClip", true) as RectTransform;
                UiKit.Stretch(clip, 0f);
                if (clip.GetComponent<RectMask2D>() == null) Undo.AddComponent<RectMask2D>(clip.gameObject);
                Undo.SetTransformParent(wallpaperImage.transform, clip, "Keep wallpaper cover inside monitor");
                clip.SetSiblingIndex(index);
                UiKit.Stretch(wallpaperImage.rectTransform, 0f);
                settings.wallpaperRelativePath = RelativePath(desktop.transform, wallpaperImage.transform);
            }
            var wallpaperFit = wallpaperImage.GetComponent<AspectRatioFitter>();
            if (wallpaperFit == null) wallpaperFit = Undo.AddComponent<AspectRatioFitter>(wallpaperImage.gameObject);
            Undo.RecordObject(wallpaperFit, "Cover the measured monitor with preserved wallpaper aspect");
            wallpaperFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            wallpaperFit.aspectRatio = wallpaper.rect.width / wallpaper.rect.height;
            Undo.RecordObject(settingTile, "Set Settings icon");
            settingTile.sprite = setting;
            settingTile.color = Color.white;
            settingTile.preserveAspect = true;
            var glyph = settingTile.transform.Find("Glyph");
            if (glyph != null) { Undo.RecordObject(glyph.gameObject, "Hide replaced Settings glyph"); glyph.gameObject.SetActive(false); }
            Canvas.ForceUpdateCanvases();
            var tileCorners = new Vector3[4];
            tutorialTile.GetWorldCorners(tileCorners);
            var localTopLeft = wallpaperDesktopRoot.InverseTransformPoint(tileCorners[1]);
            var localBottomLeft = wallpaperDesktopRoot.InverseTransformPoint(tileCorners[0]);
            var localTopRight = wallpaperDesktopRoot.InverseTransformPoint(tileCorners[2]);
            settings.binPosition = new Vector2(localTopLeft.x - wallpaperDesktopRoot.rect.xMin,
                localTopLeft.y - wallpaperDesktopRoot.rect.yMax);
            settings.binSize = new Vector2((localTopRight - localTopLeft).magnitude,
                (localTopLeft - localBottomLeft).magnitude);
            Undo.RecordObject(tutorialIcon.gameObject, "Hide Tutorial shortcut only");
            tutorialIcon.gameObject.SetActive(false);
            SetupBin(desktop.transform, bin, settings);
            SetupRoom(environment, room, shader, settings);
            if (!string.IsNullOrEmpty(settings.sourceRoomMeshPath))
            {
                var sourceRoom = desk.Find(settings.sourceRoomMeshPath)?.GetComponent<Renderer>();
                Require(sourceRoom != null, "Measured source wall renderer is missing from Desk instance.");
                Undo.RecordObject(sourceRoom, "Replace original wall with authored Unlit wall");
                sourceRoom.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(sourceRoom);
            }
            menu.BakeAuthoredUi();
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
        finally { Undo.CollapseUndoOperations(group); }
    }

    // Parent opens the legacy scene explicitly, calls this, then saves it explicitly.
    // Reuses the saved unit-quad ComputerScreen and Canvas; no FBX/pose inference needed.
    public static void MigrateLegacyLoadingScene()
    {
        RequireEditMode();
        var scene = SceneManager.GetActiveScene();
        Require(scene.name == "LoadingScene", "Open and activate the legacy LoadingScene explicitly before migration.");
        ComputerScreen screen = null;
        ComputerTransitionController transition = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var item in root.GetComponentsInChildren<ComputerScreen>(true))
            { Require(screen == null, "Legacy scene has multiple screens; use the explicit-reference migration overload."); screen = item; }
            foreach (var item in root.GetComponentsInChildren<ComputerTransitionController>(true))
            { Require(transition == null, "Legacy scene has multiple transitions; use the explicit-reference migration overload."); transition = item; }
        }
        SetupLegacyLoadingScene(screen, transition, Camera.main);
    }

    public static void SetupLegacyLoadingScene(ComputerScreen screen, ComputerTransitionController transition, Camera camera)
    {
        RequireEditMode();
        Require(screen != null && transition != null && camera != null, "Legacy migration requires explicit screen, transition and camera.");
        Require(screen.gameObject.scene == transition.gameObject.scene && screen.gameObject.scene == camera.gameObject.scene,
            "Legacy references must belong to the same explicitly opened scene.");
        var canvas = screen.GetComponentInChildren<Canvas>(true);
        Require(canvas == null || canvas.renderMode == RenderMode.WorldSpace, "Legacy display Canvas must be WorldSpace.");
        RectTransform frame;
        if (canvas != null) frame = canvas.transform as RectTransform;
        else
        {
            // ComputerScreen's explicit display contract is its unit local quad.
            // A saved marker is sufficient when legacy UI is supplied by its adapter.
            frame = Child(screen.transform, "LegacyDisplayFrame", true) as RectTransform;
            SetLocal(frame, Vector3.zero, new Vector3(0f, 180f, 0f), Vector3.one);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = Vector2.one;
        }
        Require(MainMenuCameraController.TryGetFrame(frame, out _, out _, out _, out _, out _), "Legacy display Canvas must have a valid saved rectangular transform.");
        var owner = camera.GetComponent<MainMenuCameraController>();
        if (owner == null) owner = Undo.AddComponent<MainMenuCameraController>(camera.gameObject);
        SetReference(owner, "computerScreenFrame", frame);
        SetReference(transition, "screenFrame", frame);
        EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
    }

    static LoadingUiPresenter SetupLoadingLayer(Canvas canvas)
    {
        var layer = Child(canvas.transform, "AuthoredLoadingLayer", true) as RectTransform;
        UiKit.Stretch(layer, 0f);
        var background = layer.GetComponent<Image>();
        if (background == null) background = Undo.AddComponent<Image>(layer.gameObject);
        background.color = new Color(0.02f, 0.06f, 0.12f, 1f);
        background.raycastTarget = true;
        var presenter = layer.GetComponent<LoadingUiPresenter>();
        if (presenter == null) presenter = Undo.AddComponent<LoadingUiPresenter>(layer.gameObject);
        var boot = Label(layer, "BootLabel", 42, new Vector2(0.1f, 0.60f), new Vector2(0.9f, 0.8f));
        var wait = Label(layer, "WaitLabel", 26, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.6f));
        var percent = Label(layer, "PercentLabel", 28, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.3f));
        var fill = Image(layer, "ProgressFill", new Vector2(0.15f, 0.33f), new Vector2(0.85f, 0.37f));
        fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fill.type = UnityEngine.UI.Image.Type.Filled;
        fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        var scan = Image(layer, "Scan", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f));
        scan.rectTransform.sizeDelta = new Vector2(0f, 2f);
        scan.color = new Color(0.4f, 0.8f, 1f, 0.15f);
        SetReference(presenter, "bootLabel", boot);
        SetReference(presenter, "waitLabel", wait);
        SetReference(presenter, "percentLabel", percent);
        SetReference(presenter, "progressFill", fill);
        SetReference(presenter, "scan", scan.rectTransform);
        layer.SetAsLastSibling();
        layer.gameObject.SetActive(false);
        return presenter;
    }

    static void SetupBin(Transform desktop, Sprite sprite, MainMenuEnvironmentSettings settings)
    {
        var image = Image(desktop, "BinVisualOnly", Vector2.zero, Vector2.zero);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0f, 1f);
        image.rectTransform.pivot = new Vector2(0f, 1f);
        image.rectTransform.anchoredPosition = settings.binPosition;
        image.rectTransform.sizeDelta = settings.binSize;
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        // No DesktopIcon, app binding, event or destructive action is registered.
    }

    static void SetupRoom(Transform parent, Texture2D texture, Shader shader, MainMenuEnvironmentSettings settings)
    {
        var roomMaterial = MaterialAsset("RoomBackground", shader, texture, Color.white);
        var glowMaterial = MaterialAsset("MonitorGlow", shader, null, settings.glowColor);
        ConfigureSoftGlow(glowMaterial);
        Quad(parent, "RoomBackground", roomMaterial, settings.roomPosition, settings.roomEuler, settings.roomScale);
        Quad(parent, "MonitorGlow", glowMaterial, settings.glowPosition, settings.glowEuler, settings.glowScale);
        var lightRoot = Child(parent, "MonitorHaloLight");
        SetLocal(lightRoot, settings.lightPosition, Vector3.zero, Vector3.one);
        var light = lightRoot.GetComponent<Light>();
        if (light == null) light = Undo.AddComponent<Light>(lightRoot.gameObject);
        Undo.RecordObject(light, "Configure monitor halo");
        light.type = LightType.Point;
        light.color = settings.glowColor;
        light.range = settings.lightRange;
        light.intensity = settings.lightIntensity;
    }

    static Material MaterialAsset(string name, Shader shader, Texture texture, Color color)
    {
        const string folder = "Assets/Art/Background/MainMenuAuthored";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Art/Background", "MainMenuAuthored");
        string path = folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        Undo.RecordObject(material, "Configure authored Unlit material");
        material.shader = shader;
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void ConfigureSoftGlow(Material material)
    {
        const int size = 128;
        const string path = "Assets/Art/Background/MainMenuAuthored/MonitorGlowRadial.asset";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
                { name = "MonitorGlowRadial", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float fade = Mathf.Clamp01(1f - dx * dx - dy * dy);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, fade * fade);
                }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            AssetDatabase.CreateAsset(texture, path);
        }
        material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
    }

    sealed class MeasuredDesk
    {
        public MainMenuEnvironmentSettings settings;
        public Vector3 center, normal;
        public float width, height;
    }
    struct MeasuredQuad
    {
        public Vector3 center, normal, up, right;
        public float width, height;
    }
    static MeasuredDesk MeasureDesk(GameObject model)
    {
        Camera sourceCamera = null;
        MeshFilter panel = null, wall = null;
        foreach (var camera in model.GetComponentsInChildren<Camera>(true))
            if (camera.name == "Camera") { Require(sourceCamera == null, "Multiple artist Cameras found."); sourceCamera = camera; }
        foreach (var mesh in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.name == "Cube.005") { Require(panel == null, "Multiple Cube.005 panels found."); panel = mesh; }
            if (mesh.name == "Plane") { Require(wall == null, "Multiple source Plane walls found."); wall = mesh; }
        }
        Require(sourceCamera != null && !sourceCamera.orthographic && panel != null && wall != null,
            "Desk requires the original perspective Camera, Cube.005 monitor panel and Plane wall.");
        var display = MeasureQuad(panel, MaterialSlot(panel, "Material006"), sourceCamera.transform.position);
        var room = MeasureQuad(wall, MaterialSlot(wall, "Material005"), sourceCamera.transform.position);
        var parent = panel.transform;
        Vector3 localNormal = parent.InverseTransformVector(display.normal).normalized;
        Vector3 localUp = parent.InverseTransformVector(display.up).normalized;
        Vector3 localRight = parent.InverseTransformVector(display.right).normalized;
        Require(Mathf.Abs(Vector3.Dot(localRight, localUp)) < 0.001f && Mathf.Abs(Vector3.Dot(localNormal, localUp)) < 0.001f,
            "Imported monitor transform contains shear; cannot author a rectangular display safely.");
        var settings = new MainMenuEnvironmentSettings
        {
            sourceModel = "Assets/Art/Background/Source~/Models/Desk.blend",
            monitorMeshPath = RelativePath(model.transform, parent),
            sourceRoomMeshPath = RelativePath(model.transform, wall.transform),
            deskPosition = model.transform.localPosition,
            deskEuler = model.transform.localEulerAngles,
            deskScale = model.transform.localScale,
            screenLocalPosition = parent.InverseTransformPoint(display.center + display.normal * Mathf.Max(0.001f, display.width * 0.0005f)),
            screenLocalEuler = Quaternion.LookRotation(localNormal, localUp).eulerAngles,
            displaySize = new Vector2(parent.InverseTransformVector(display.right * display.width).magnitude,
                parent.InverseTransformVector(display.up * display.height).magnitude),
            canvasSize = new Vector2(1600f, 1600f * display.height / display.width),
            cameraFov = sourceCamera.fieldOfView,
            introPosition = sourceCamera.transform.position,
            introEuler = sourceCamera.transform.rotation.eulerAngles,
            roomPosition = room.center,
            roomEuler = Quaternion.LookRotation(-room.normal, room.up).eulerAngles,
            roomScale = new Vector3(room.width, room.height, 1f),
            glowPosition = display.center - display.normal * 0.05f,
            glowEuler = Quaternion.LookRotation(-display.normal, display.up).eulerAngles,
            glowScale = new Vector3(display.width * 1.6f, display.height * 1.8f, 1f),
            lightPosition = display.center - display.normal * 0.05f,
            lightRange = Mathf.Max(display.width, display.height) * 2f,
            lightIntensity = 1.5f,
            binPosition = Vector2.zero,
            geometryConfirmed = true
        };
        return new MeasuredDesk { settings = settings, center = display.center, normal = display.normal, width = display.width, height = display.height };
    }
    static void ConfigureDesktopPose(MainMenuEnvironmentSettings settings, MeasuredDesk measured, float aspect)
    {
        Require(aspect > 0f && settings.cameraFov > 1f && settings.cameraFov < 179f, "Artist FOV and menu camera aspect must be valid.");
        float tangent = Mathf.Tan(settings.cameraFov * Mathf.Deg2Rad * 0.5f);
        float distance = Mathf.Max(measured.height * 0.5f / tangent, measured.width * 0.5f / (tangent * aspect)) * 1.18f;
        settings.desktopPosition = measured.center + measured.normal * distance;
        settings.desktopEuler = Quaternion.LookRotation(-measured.normal, Vector3.up).eulerAngles;
    }
    static int MaterialSlot(MeshFilter filter, string expected)
    {
        var renderer = filter.GetComponent<Renderer>();
        Require(renderer != null && filter.sharedMesh != null, "Measured mesh requires a Renderer and imported mesh: " + filter.name);
        var materials = renderer.sharedMaterials;
        int slot = -1;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;
            string normalized = materials[i].name.Replace(".", "").Replace("_", "").Replace(" ", "");
            if (!normalized.StartsWith(expected, StringComparison.OrdinalIgnoreCase)) continue;
            Require(slot < 0, "Ambiguous measured material slot: " + expected);
            slot = i;
        }
        Require(slot >= 0 && slot < filter.sharedMesh.subMeshCount, "Missing measured material slot: " + expected);
        return slot;
    }
    static MeasuredQuad MeasureQuad(MeshFilter filter, int slot, Vector3 cameraPosition)
    {
        var mesh = filter.sharedMesh;
        var vertices = mesh.vertices;
        var indices = mesh.GetTriangles(slot);
        Require(indices.Length >= 6, "Measured material must contain a triangulated display quad.");
        float largest = 0f;
        Vector3 planeOrigin = default, normal = default;
        for (int i = 0; i < indices.Length; i += 3)
        {
            var a = filter.transform.TransformPoint(vertices[indices[i]]);
            var b = filter.transform.TransformPoint(vertices[indices[i + 1]]);
            var c = filter.transform.TransformPoint(vertices[indices[i + 2]]);
            var cross = Vector3.Cross(b - a, c - a);
            if (cross.magnitude <= largest) continue;
            largest = cross.magnitude; planeOrigin = a; normal = cross.normalized;
        }
        Require(largest > 0.000001f, "Measured plane is degenerate.");
        if (Vector3.Dot(normal, cameraPosition - planeOrigin) < 0f) normal = -normal;
        var up = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
        Require(up.sqrMagnitude > 0.9f, "Measured plane has no stable vertical direction.");
        var right = Vector3.Cross(up, normal).normalized;
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity, area = 0f;
        float tolerance = Mathf.Sqrt(largest) * 0.0001f;
        int triangleCount = 0;
        for (int i = 0; i < indices.Length; i += 3)
        {
            var a = filter.transform.TransformPoint(vertices[indices[i]]);
            var b = filter.transform.TransformPoint(vertices[indices[i + 1]]);
            var c = filter.transform.TransformPoint(vertices[indices[i + 2]]);
            var cross = Vector3.Cross(b - a, c - a);
            if (cross.magnitude < largest * 0.05f || Mathf.Abs(Vector3.Dot(cross.normalized, normal)) < 0.9999f ||
                Mathf.Abs(Vector3.Dot(a - planeOrigin, normal)) > tolerance ||
                Mathf.Abs(Vector3.Dot(b - planeOrigin, normal)) > tolerance ||
                Mathf.Abs(Vector3.Dot(c - planeOrigin, normal)) > tolerance) continue;
            area += cross.magnitude * 0.5f;
            triangleCount++;
            foreach (var point in new[] { a, b, c })
            {
                var offset = point - planeOrigin;
                float x = Vector3.Dot(offset, right), y = Vector3.Dot(offset, up);
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
        }
        float width = maxX - minX, height = maxY - minY;
        Require(triangleCount >= 2 && width > 0f && height > 0f && Mathf.Abs(area / (width * height) - 1f) < 0.02f,
            "Largest coplanar material face is not a complete rectangular quad: " + filter.name);
        return new MeasuredQuad { center = planeOrigin + right * ((minX + maxX) * 0.5f) + up * ((minY + maxY) * 0.5f),
            normal = normal, up = up, right = right, width = width, height = height };
    }
    static T Unique<T>(Scene scene) where T : Component
    {
        T found = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var item in root.GetComponentsInChildren<T>(true))
            { Require(found == null, "Scene has multiple " + typeof(T).Name + ": " + scene.path); found = item; }
        Require(found != null, "Scene requires authored " + typeof(T).Name + ": " + scene.path);
        return found;
    }
    static Camera SceneCamera(Scene scene)
    {
        Camera found = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                if (camera.enabled && camera.CompareTag("MainCamera"))
                { Require(found == null, "Legacy scene has multiple enabled MainCameras."); found = camera; }
        Require(found != null, "Legacy scene requires its authored active MainCamera.");
        return found;
    }
    static string FindWallpaperPath(DesktopView desktop)
    {
        var image = new SerializedObject(desktop).FindProperty("wallpaperImage").objectReferenceValue as Image;
        Require(image != null && image.transform.IsChildOf(desktop.transform), "Desktop requires its existing serialized wallpaperImage.");
        return RelativePath(desktop.transform, image.transform);
    }
    static string RelativePath(Transform root, Transform child)
    {
        var names = new List<string>();
        while (child != root)
        { Require(child != null, "Object is outside the specified root."); names.Add(child.name); child = child.parent; }
        names.Reverse();
        return string.Join("/", names);
    }
    static void ValidateLegacyFrame(ComputerScreen screen)
    {
        var canvas = screen.GetComponentInChildren<Canvas>(true);
        if (canvas != null)
            Require(canvas.renderMode == RenderMode.WorldSpace && MainMenuCameraController.TryGetFrame(canvas.transform as RectTransform,
                out _, out _, out _, out _, out _), "Legacy authored Canvas must have a valid saved rectangular display frame.");
        else Require(Positive(screen.transform.lossyScale), "Legacy unit-quad screen requires a positive saved scale.");
    }
    static void EnsureSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Require(importer != null, "Required PNG importer missing: " + path);
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        Load<Sprite>(path);
    }
    static void SaveGeneratedMaterials(Scene scene)
    {
        var saved = new HashSet<Material>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (material != null && AssetDatabase.GetAssetPath(material).StartsWith("Assets/Art/Background/MainMenuAuthored/", StringComparison.Ordinal) && saved.Add(material))
                        AssetDatabase.SaveAssetIfDirty(material);
    }

    static void ConfigureImportedModel(Transform desk, Shader litShader)
    {
        foreach (var importedCamera in desk.GetComponentsInChildren<Camera>(true))
        {
            Undo.RecordObject(importedCamera, "Disable reference model camera");
            importedCamera.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(importedCamera);
            // Exported poses remain available for artist reference; no second MainCamera tag/listener.
            if (importedCamera.CompareTag("MainCamera"))
            {
                Undo.RecordObject(importedCamera.gameObject, "Remove imported MainCamera tag");
                importedCamera.gameObject.tag = "Untagged";
                PrefabUtility.RecordPrefabInstancePropertyModifications(importedCamera.gameObject);
            }
        }
        foreach (var listener in desk.GetComponentsInChildren<AudioListener>(true))
        {
            Undo.RecordObject(listener, "Disable reference model audio listener");
            listener.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(listener);
        }
        const string folder = "Assets/Art/Background/MainMenuAuthored";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Art/Background", "MainMenuAuthored");
        foreach (var renderer in desk.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var source = materials[i];
                if (source == null || source.shader == null ||
                    (source.shader.name != "Standard" && source.shader.name != "Standard (Specular setup)")) continue;
                Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId),
                    "Imported Standard material requires a stable asset identity: " + source.name);
                string path = folder + "/Desk_" + guid + "_" + localId + ".mat";
                var converted = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (converted == null)
                {
                    converted = new Material(litShader) { name = source.name + " (MainMenu URP)" };
                    AssetDatabase.CreateAsset(converted, path);
                }
                Undo.RecordObject(converted, "Preserve imported texture and tint in URP material");
                converted.shader = litShader;
                converted.SetTexture("_BaseMap", source.mainTexture);
                converted.SetTextureScale("_BaseMap", source.mainTextureScale);
                converted.SetTextureOffset("_BaseMap", source.mainTextureOffset);
                converted.SetColor("_BaseColor", source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
                if (source.HasProperty("_Metallic")) converted.SetFloat("_Metallic", source.GetFloat("_Metallic"));
                if (source.HasProperty("_Glossiness")) converted.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
                EditorUtility.SetDirty(converted);
                materials[i] = converted;
                changed = true;
            }
            if (!changed) continue;
            Undo.RecordObject(renderer, "Assign separate URP materials to scene instance");
            renderer.sharedMaterials = materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }

    static void Quad(Transform parent, string name, Material material, Vector3 position, Vector3 euler, Vector3 scale)
    {
        var target = parent.Find(name);
        if (target == null)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Undo.RegisterCreatedObjectUndo(quad, "Create authored room quad");
            quad.name = name;
            target = quad.transform;
            target.SetParent(parent, false);
            var collider = quad.GetComponent<Collider>();
            if (collider != null) Undo.DestroyObjectImmediate(collider);
        }
        SetLocal(target, position, euler, scale);
        Undo.RecordObject(target.GetComponent<Renderer>(), "Set room material");
        target.GetComponent<Renderer>().sharedMaterial = material;
    }

    static Transform Child(Transform parent, string name, bool rect = false)
    {
        var child = parent.Find(name);
        if (child != null) return child;
        var go = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create authored menu object");
        go.transform.SetParent(parent, false);
        return go.transform;
    }
    static Text Label(Transform parent, string name, int size, Vector2 min, Vector2 max)
    {
        var target = Child(parent, name, true);
        var text = target.GetComponent<Text>();
        if (text == null)
        {
            text = Undo.AddComponent<Text>(target.gameObject);
        }
        text.font = UiKit.Font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        Rect(text.rectTransform, min, max);
        return text;
    }
    static Image Image(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var target = Child(parent, name, true);
        var image = target.GetComponent<Image>();
        if (image == null) image = Undo.AddComponent<Image>(target.gameObject);
        image.raycastTarget = false;
        Rect(image.rectTransform, min, max);
        return image;
    }
    static void Rect(RectTransform rect, Vector2 min, Vector2 max)
    {
        Undo.RecordObject(rect, "Set authored UI rect");
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    static void SetLocal(Transform target, Vector3 position, Vector3 euler, Vector3 scale)
    {
        Undo.RecordObject(target, "Set authored transform");
        target.localPosition = position;
        target.localRotation = Quaternion.Euler(euler);
        target.localScale = scale;
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
    static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        var field = serialized.FindProperty(property);
        Require(field != null, target.GetType().Name + " is missing serialized field " + property);
        field.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }
    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Require(asset != null, "Required authored asset missing or wrong importer type: " + path);
        return asset;
    }
    static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
    static bool Positive(Vector3 value) => value.x > 0f && value.y > 0f && value.z > 0f;
    static void RequireEditMode() { Require(!Application.isPlaying, "MainMenu authoring requires Edit Mode."); }
}

/// <summary>Artist-editable saved schema; geometryConfirmed is set only after parent supplies actual mesh/poses.</summary>
[Serializable]
public sealed class MainMenuEnvironmentSettings
{
    public string sourceModel = "Assets/Art/Background/Source~/Models/Desk.blend";
    public bool geometryConfirmed;
    public string monitorMeshPath;
    public string wallpaperRelativePath;
    public Vector3 deskPosition, deskEuler;
    public Vector3 deskScale = Vector3.one;
    public Vector3 screenLocalPosition, screenLocalEuler;
    public Vector2 displaySize;
    public Vector2 canvasSize = new Vector2(1600f, 900f);
    public Vector3 introPosition, introEuler, desktopPosition, desktopEuler;
    public float cameraFov = 60f;
    public string sourceRoomMeshPath;
    public Vector3 roomPosition, roomEuler, roomScale;
    public Vector3 glowPosition, glowEuler, glowScale, lightPosition;
    public Color glowColor = new Color(0.15f, 0.5f, 1f, 1f);
    public float lightRange, lightIntensity;
    public Vector2 binPosition, binSize = new Vector2(144f, 144f);
}
#endif
