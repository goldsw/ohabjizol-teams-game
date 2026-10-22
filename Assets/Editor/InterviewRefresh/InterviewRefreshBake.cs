using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ohabjizol.Interview;
using Ohabjizol.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InterviewRefreshBake
{
    const string Prefabs = "Assets/Prefabs/InterviewRefresh";
    const string FontPath = Ohabjizol.UI.UiKit.FontAssetPath;

    public static void ApplyAndSave()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before UI migration.");
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isDirty && string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Save the unnamed scene before migration.");
        }
        if (!EditorSceneManager.SaveOpenScenes()) throw new InvalidOperationException("Could not save open scenes.");
        Directory.CreateDirectory("Library/InterviewRefreshBackup");
        foreach (string name in new[] { "MainMenuScene", "InterviewScene", "ResultScene" })
        {
            string path = "Assets/Scenes/" + name + ".unity";
            string backup = "Library/InterviewRefreshBackup/" + name + ".unity";
            if (File.Exists(path) && !File.Exists(backup)) File.Copy(path, backup);
        }
        Directory.CreateDirectory(Prefabs);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        try
        {
            InvokeStatic("PortfolioUiBake", "BuildAssets");
            InvokeStatic("Ohabjizol.Interview.DocumentsUiBake", "BuildAssets");
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null || !EditorUtility.IsPersistent(font)) throw new InvalidOperationException("Persistent Korean font missing: " + FontPath);
            EnsureQuestionCatalog();
            PortraitImport();
            BakeExisting<MainMenuController>("MainMenuScene", "Desktop.prefab", font, controller =>
            {
                var scene = controller.gameObject.scene;
                return Find<DesktopView>(scene).gameObject;
            });
            BakeExisting<InterviewScreen>("InterviewScene", "InterviewScreen.prefab", font, controller => controller.gameObject);
            BakeExisting<ResultBoard>("ResultScene", "ResultBoard.prefab", font, controller => controller.gameObject);
            BakeEnding(font);
            ResultPresentationBake.Apply();
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (string name in new[] { "MainMenuScene", "LoadingScene", "InterviewScene", "ResultScene", "EndingScene" })
            {
                string path = "Assets/Scenes/" + name + ".unity";
                if (File.Exists(path) && !scenes.Any(entry => entry.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            ResultPresentationBake.EnsureResultBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[InterviewRefreshBake] PASS: authored scenes/prefabs saved.");
        }
        finally
        {
            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    static void EnsureQuestionCatalog()
    {
        const string path = "Assets/Art/InterviewRefresh/QuestionCatalog.asset";
        if (AssetDatabase.LoadAssetAtPath<AuthoredQuestionCatalog>(path) != null) return;
        var catalog = ScriptableObject.CreateInstance<AuthoredQuestionCatalog>();
        catalog.SeedDefaults();
        AssetDatabase.CreateAsset(catalog, path);
        AssetDatabase.SaveAssets();
    }
    static void BakeExisting<T>(string sceneName, string prefabName, Font font, Func<T, GameObject> prefabRoot) where T : MonoBehaviour
    {
        string path = "Assets/Scenes/" + sceneName + ".unity";
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            T controller = Find<T>(scene);
            bool authored = HasAuthored(controller);
            if (!authored) Invoke(controller, "BakeAuthoredUi");
            if (!HasAuthored(controller)) throw new InvalidOperationException("Incomplete authored UI: " + typeof(T).Name);
            EnsureSceneInput(scene);
            NormalizeScene(scene, font);
            AssignCallPortraits(scene);
            GameObject root = prefabRoot(controller);
            string prefabPath = Prefabs + "/" + prefabName;
            if (!File.Exists(prefabPath))
            {
                if (PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction) == null)
                    throw new InvalidOperationException("Prefab save failed: " + prefabPath);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Scene save failed: " + path);
        }
        finally { if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    static void BakeEnding(Font font)
    {
        const string path = "Assets/Scenes/EndingScene.unity";
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = File.Exists(path) ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            Type boardType = TypeByName("Ohabjizol.Interview.EndingBoard");
            MonoBehaviour board = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component != null && component.GetType() == boardType) board = component;
            if (board == null)
            {
                var host = new GameObject("EndingBoard");
                SceneManager.MoveGameObjectToScene(host, scene);
                board = (MonoBehaviour)host.AddComponent(boardType);
            }
            if (!HasAuthored(board)) Invoke(board, "BakeAuthoredUi");
            if (!HasAuthored(board)) throw new InvalidOperationException("Incomplete EndingBoard UI.");
            EnsureSceneInput(scene);
            NormalizeScene(scene, font);
            AssignCallPortraits(scene);
            if (!File.Exists(Prefabs + "/EndingBoard.prefab")) PrefabUtility.SaveAsPrefabAssetAndConnect(board.gameObject, Prefabs + "/EndingBoard.prefab", InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Ending scene save failed.");
        }
        finally { if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    static T Find<T>(Scene scene) where T : Component
    {
        T result = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (T candidate in root.GetComponentsInChildren<T>(true))
            {
                if (result != null) throw new InvalidOperationException("Multiple controllers: " + typeof(T).Name);
                result = candidate;
            }
        if (result == null) throw new InvalidOperationException("Missing controller: " + typeof(T).Name + " in " + scene.path);
        return result;
    }

    static bool HasAuthored(object target)
    {
        var property = target.GetType().GetProperty("HasAuthoredUi", BindingFlags.Public | BindingFlags.Instance);
        if (property == null) throw new InvalidOperationException("HasAuthoredUi contract missing: " + target.GetType().Name);
        return (bool)property.GetValue(target);
    }

    static void EnsureSceneInput(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) != null) return;
        var host = new GameObject("EventSystem");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<UnityEngine.EventSystems.EventSystem>();
        Type inputType = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            inputType = assembly.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (inputType != null) break;
        }
        if (inputType != null)
        {
            var module = host.AddComponent(inputType);
            inputType.GetMethod("AssignDefaultActions", BindingFlags.Instance | BindingFlags.Public)?.Invoke(module, null);
            EditorUtility.SetDirty(module);
        }
        else host.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
    static void NormalizeScene(Scene scene, Font font)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text.font == null || !EditorUtility.IsPersistent(text.font)) text.font = font;
                EditorUtility.SetDirty(text);
            }
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image.sprite != null && !EditorUtility.IsPersistent(image.sprite)) { image.sprite = null; EditorUtility.SetDirty(image); }
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true)) if (component != null) EditorUtility.SetDirty(component);
        }
    }

    static void PortraitImport()
    {
        const string path = "Assets/Resources/Interview/Portraits/girl.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Required portrait sheet missing: " + path);
        if (importer.spriteImportMode != SpriteImportMode.Multiple || importer.textureType != TextureImporterType.Sprite)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null || texture.width < 2) throw new InvalidOperationException("Portrait sheet could not load.");
            int half = texture.width / 2;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable 618
            importer.spritesheet = new[]
            {
                new SpriteMetaData { name="GirlIdle", rect=new Rect(0,0,half,texture.height), pivot=new Vector2(.5f,.5f), alignment=(int)SpriteAlignment.Center },
                new SpriteMetaData { name="GirlTalk", rect=new Rect(half,0,texture.width-half,texture.height), pivot=new Vector2(.5f,.5f), alignment=(int)SpriteAlignment.Center }
            };
#pragma warning restore 618
            importer.isReadable = true; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    static void AssignCallPortraits(Scene scene)
    {
        const string path = "Assets/Resources/Interview/Portraits/girl.png";
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        var idle = sprites.FirstOrDefault(sprite => sprite.name == "GirlIdle");
        var talk = sprites.FirstOrDefault(sprite => sprite.name == "GirlTalk");
        if (idle == null || talk == null) throw new InvalidOperationException("Portrait slicing has not produced idle/talk assets.");
        foreach (var root in scene.GetRootGameObjects())
            foreach (var view in root.GetComponentsInChildren<InterviewCallView>(true))
            {
                var serialized = new SerializedObject(view);
                serialized.Update();
                if (serialized.FindProperty("_idleSprite").objectReferenceValue == null) serialized.FindProperty("_idleSprite").objectReferenceValue = idle;
                if (serialized.FindProperty("_talkSprite").objectReferenceValue == null) serialized.FindProperty("_talkSprite").objectReferenceValue = talk;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var image = serialized.FindProperty("_portrait").objectReferenceValue as Image;
                if (image != null && image.sprite == null) { image.sprite = idle; image.color = Color.white; image.preserveAspect = true; EditorUtility.SetDirty(image); }
            }
    }
    static Type TypeByName(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(name);
            if (type != null) return type;
        }
        throw new InvalidOperationException("Missing compiled type: " + name);
    }
    static void InvokeStatic(string type, string method) => Call(TypeByName(type).GetMethod(method, BindingFlags.Public | BindingFlags.Static), null);
    static void Invoke(object target, string method) => Call(target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance), target);
    static void Call(MethodInfo method, object target)
    {
        if (method == null) throw new InvalidOperationException("Bake method missing.");
        try { method.Invoke(target, null); }
        catch (TargetInvocationException error) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw(); throw; }
    }
}