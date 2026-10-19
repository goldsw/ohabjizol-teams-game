#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ohabjizol.EditorTools
{
    /// <summary>Reversible default PNG background and native Aseprite control Sprite bindings.</summary>
    public static class InterviewDiscordSkinAuthoring
    {
        const string ScenePath = "Assets/Scenes/InterviewScene.unity";
        const string ReportPath = "Logs/DiscordCallUI/skin-apply.json";
        const string Art = "Assets/Art/Discord/Source/Aseprite/";
        const string UndoName = "Apply Aseprite Call UI";

        [Serializable] sealed class Source
        {
            public string path, name, guid;
            public long fileID;
            [NonSerialized] public Sprite sprite;
        }
        [Serializable] sealed class Report
        {
            public string targetScene = ScenePath;
            public string utc, status, error;
            public bool requestedSave, saved, dirty;
            public List<Source> nativeSprites = new List<Source>();
            public List<string> targets = new List<string>();
            public List<string> created = new List<string>();
            public List<string> skipped = new List<string>();
            public List<string> restored = new List<string>();
            public List<string> disabled = new List<string>();
        }
        sealed class Binding
        {
            public Image image;
            public Source source;
            public bool white;
        }
        sealed class Decoration
        {
            public Transform parent;
            public string name;
            public Source source;
            public Vector2 min, max;
            public Text placeholder;
            public bool group;
        }

        [MenuItem("Ohabjizol/Interview/Apply Aseprite Call UI")]
        public static void Apply() { Run(false); }

        [MenuItem("Ohabjizol/Interview/Apply Aseprite Call UI And Save")]
        public static void ApplyAndSave() { Run(true); }

        static void Run(bool save)
        {
            var report = new Report { utc = DateTime.UtcNow.ToString("O"), requestedSave = save };
            Scene scene = default(Scene);
            int undo = -1;
            bool wasDirty = false;
            bool saveAttempted = false;
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
                    !EditorApplication.isUpdating, "Requires idle EditMode.");
                var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                    .Where(s => s.isLoaded && s.path == ScenePath).ToArray();
                Require(scenes.Length == 1, "Exactly one already loaded InterviewScene is required.");
                scene = scenes[0];
                wasDirty = scene.isDirty;
                Require(!save || !wasDirty, "Scene contains unsaved edits; save explicitly before ApplyAndSave.");
                var screens = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<InterviewScreen>(true)).ToArray();
                Require(screens.Length == 1, "Expected one InterviewScreen in target scene.");
                var serialized = new SerializedObject(screens[0]);
                var root = Ref<RectTransform>(serialized, "_discordViewRoot", scene);
                var call = Ref<InterviewCallView>(serialized, "_call", scene);
                var panel = Ref<DownloadPanelView>(serialized, "_downloadPanel", scene);
                var chat = Ref<Button>(serialized, "_chatButton", scene);
                Ref<Component>(serialized, "_teamWindow", scene);
                Ref<Button>(serialized, "_acceptButton", scene);
                Ref<Button>(serialized, "_rejectButton", scene);
                Ref<GameObject>(serialized, "_unreadMark", scene);
                Require(call.transform.IsChildOf(root) && panel.transform.IsChildOf(root) && chat.transform.IsChildOf(root),
                    "Call, download panel and chat must belong to DiscordViewRoot.");
                var questionProperty = serialized.FindProperty("_questions");
                Require(questionProperty != null && questionProperty.isArray && questionProperty.arraySize == 5,
                    "Expected five serialized question references.");
                var questions = new List<Button>();
                for (int i = 0; i < 5; i++)
                {
                    var question = questionProperty.GetArrayElementAtIndex(i).objectReferenceValue as InterviewQuestionView;
                    Require(question && question.gameObject.scene == scene, "Invalid question reference.");
                    var button = question.GetComponent<Button>();
                    Require(button && question.Button == button, "Question Button reference mismatch.");
                    Require(!questions.Contains(button), "Duplicate question reference.");
                    questions.Add(button);
                }
                panel.PreflightItemsEditor();
                Require(panel.HasAuthoredUi, "Apply Download Items before applying the skin.");
                var downloads = panel.Items.Select(item => item.Button).ToArray();
                Require(downloads[0] != downloads[1] && downloads.All(b => b.transform.IsChildOf(panel.transform)), "Invalid download button hierarchy.");

                // Resolve every required source before touching any scene object.
                var background = LoadFromPath("Assets/Resources/Interview/Discord_total.png", "Discord_total", report);
                var questionSprite = Load("디코 통화방 UI", "Question", report);
                var file = Load("디코 통화방 UI", "File", report);
                var download = Load("디코 통화방 UI", "download", report);
                var chatSprite = Load("오합지졸-디코", "chat", report);
                var profileBg = Load("오합지졸-디코", "profile_bg", report);
                var profile = Load("오합지졸-디코", "profile", report);
                var toolbar = Load("오합지졸-디코", "sdf", report);
                var controls = Load("오합지졸-디코", "profile_left_under", report);
                var server = Load("오합지졸-디코", "server", report);
                var channel = Load("오합지졸-디코", "shap", report);
                var sound = Load("오합지졸-디코", "soudn", report);
                var live = Load("오합지졸-디코", "LIVE_UI", report);
                var bindings = new List<Binding>();
                var decorations = new List<Decoration>();
                var labels = new List<Text>();
                var disableGraphics = new List<Image>();
                var restoreIcons = new List<Text>();
                var restoreTiles = new Dictionary<Image, Color>();
                var participantBackupColors = new[]
                {
                    new Color(.19f, .23f, .30f, 1f),
                    new Color(.229999989f, .23f, .30f, 1f),
                    new Color(.269999981f, .23f, .30f, 1f)
                };
                var bg = Child(root, "Background").GetComponent<Image>();
                Require(bg, "Background Image missing.");
                bindings.Add(new Binding { image = bg, source = background, white = true });
                foreach (var button in questions) PlanButton(button, questionSprite, bindings, report);
                foreach (var button in downloads)
                {
                    if (!PlanButton(button, file, bindings, report)) continue;
                    var texts = button.GetComponentsInChildren<Text>(true);
                    Require(texts.Length == 1, "Expected one existing download label: " + PathOf(button.transform));
                    labels.Add(texts[0]);
                    decorations.Add(Decor(button.transform, "SkinDownloadIcon", download, .89f, .36f, .96f, .64f));
                }
                Require(chat.GetComponent<Image>() && chat.targetGraphic, "Chat graphics missing.");
                decorations.Add(Decor(chat.transform, "SkinChatIcon", chatSprite, .02f, .22f, .18f, .78f));
                for (int i = 0; i < 3; i++)
                {
                    var tile = Child(root, "FixedParticipant" + i);
                    var image = tile.GetComponent<Image>();
                    Require(image, "Participant Image missing.");
                    if (UsesSource(image, profileBg)) restoreTiles.Add(image, participantBackupColors[i]);
                    var icon = Child(tile, "Icon").GetComponent<Text>();
                    Require(icon, "Participant Icon Text missing.");
                    if (icon.text == "나" || icon.text == "면접관")
                    {
                        restoreIcons.Add(icon);
                    }
                    else report.skipped.Add(PathOf(icon.transform) + ": custom placeholder text preserved");
                    PlanDisable(icon.transform, "SkinVisual", profile, disableGraphics, report);
                }
                var group = FindChild(root, "DiscordSkinDecorations");
                if (group) Require(group is RectTransform && !group.GetComponent<Graphic>(), "Invalid decoration group.");
                decorations.Add(Decor(root, "DiscordSkinDecorations", null, 0, 0, 1, 1));
                PlanDisable(group, "CallToolbar", toolbar, disableGraphics, report);
                PlanDisable(group, "LeftProfileControls", controls, disableGraphics, report);
                PlanDisable(group, "ServerGlyph", server, disableGraphics, report);
                PlanDisable(group, "ChannelGlyph", channel, disableGraphics, report);
                PlanDisable(group, "SoundGlyph", sound, disableGraphics, report);
                PlanDisable(call.transform, "SkinLiveBadge", live, disableGraphics, report);
                // Existing named children must be compatible; never replace custom objects.
                foreach (var d in decorations)
                {
                    var existing = FindChild(d.group ? group : d.parent, d.name);
                    if (!existing) continue;
                    Require(existing is RectTransform && (d.source == null || existing.GetComponent<Image>()), "Incompatible skin object: " + d.name);
                    Require(existing.GetComponents<Component>().All(c => c is RectTransform || c is CanvasRenderer || c is Image),
                        "Skin object contains non-decoration components: " + d.name);
                }
                report.skipped.Add("Team_List: deferred; embedded toolbar/row alignment with existing window not verified.");
                report.skipped.Add("DownloadPanel frame, Accept/Reject, applicant Portrait and existing UI rects preserved.");
                report.skipped.Add("Applied button Image base tint set to white; ColorBlock and transitions preserved. ColorTint state colors still apply. Runtime outside reviewed views not verified.");

                Undo.IncrementCurrentGroup();
                undo = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(UndoName);
                foreach (var graphic in disableGraphics)
                {
                    Change(graphic, () => { graphic.enabled = false; graphic.raycastTarget = false; });
                    report.disabled.Add(PathOf(graphic.transform));
                }
                foreach (var icon in restoreIcons)
                {
                    Change(icon, () => icon.enabled = true);
                    report.restored.Add(PathOf(icon.transform) + ": original placeholder Text enabled");
                }
                foreach (var restoration in restoreTiles)
                {
                    var tile = restoration.Key;
                    Change(tile, () =>
                    {
                        tile.sprite = null;
                        tile.color = restoration.Value;
                    });
                    report.restored.Add(PathOf(tile.transform) + ": native profile_bg cleared; original backup tint restored");
                }
                foreach (var binding in bindings)
                {
                    Change(binding.image, () =>
                    {
                        binding.image.sprite = binding.source.sprite;
                        if (binding.white) binding.image.color = Color.white;
                    });
                    report.targets.Add(PathOf(binding.image.transform) + " -> " + binding.source.name);
                }
                report.restored.Add(PathOf(bg.transform) + " -> " + background.path);
                foreach (var label in labels)
                    Change(label, () => { label.resizeTextForBestFit = true; label.resizeTextMinSize = Mathf.Min(10, label.fontSize); label.resizeTextMaxSize = label.fontSize; });
                // Label inset only; button and all original layout rects otherwise remain untouched.
                foreach (var label in labels)
                    Change(label.rectTransform, () =>
                    {
                        label.rectTransform.anchorMin = new Vector2(.22f, .08f);
                        label.rectTransform.anchorMax = new Vector2(.86f, .92f);
                        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                    });
                foreach (var d in decorations)
                {
                    Transform parent = d.group ? group : d.parent;
                    var child = FindChild(parent, d.name);
                    bool created = !child;
                    if (created)
                    {
                        var go = new GameObject(d.name, typeof(RectTransform));
                        Undo.RegisterCreatedObjectUndo(go, UndoName);
                        Undo.SetTransformParent(go.transform, parent, UndoName);
                        child = go.transform;
                        report.created.Add(PathOf(child));
                    }
                    var rect = (RectTransform)child;
                    if (created) Change(rect, () =>
                    {
                        rect.localScale = Vector3.one;
                        rect.localRotation = Quaternion.identity;
                        rect.anchorMin = d.min; rect.anchorMax = d.max;
                        rect.offsetMin = rect.offsetMax = Vector2.zero;
                    });
                    if (d.source == null)
                    {
                        group = child;
                        Change(rect, () => rect.SetSiblingIndex(bg.transform.GetSiblingIndex() + (rect.GetSiblingIndex() < bg.transform.GetSiblingIndex() ? 0 : 1)));
                        continue;
                    }
                    var image = child.GetComponent<Image>();
                    if (!image) image = Undo.AddComponent<Image>(child.gameObject);
                    Change(image, () => { image.sprite = d.source.sprite; image.color = Color.white; image.raycastTarget = false; image.preserveAspect = true; });
                    if (d.placeholder) Change(d.placeholder, () => d.placeholder.enabled = false);
                    report.targets.Add(PathOf(child) + " -> " + d.source.name);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undo);
                if (save)
                {
                    saveAttempted = true;
                    Require(EditorSceneManager.SaveScene(scene, ScenePath), "Target scene save returned false; applied edits remain available for inspection/Undo.");
                    report.saved = true;
                }
                report.status = save ? "saved" : "applied-dirty";
            }
            catch (Exception exception)
            {
                report.error = exception.ToString();
                report.status = saveAttempted ? "save-failed" : "failed";
                if (undo >= 0 && !saveAttempted)
                {
                    Undo.RevertAllDownToGroup(undo);
                    report.status = "rolled-back";
                    if (!wasDirty && scene.IsValid() && scene.isDirty)
                        report.skipped.Add("Contents rolled back through Undo; scene may remain dirty after the exception.");
                }
                throw;
            }
            finally
            {
                report.dirty = scene.IsValid() && scene.isDirty;
                try
                {
                    string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportPath));
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, JsonUtility.ToJson(report, true));
                }
                catch (Exception e) { Debug.LogError("Skin report write failed: " + e.Message); }
            }
        }

        static Source Load(string folder, string name, Report report)
        {
            return LoadFromPath(Art + folder + "/" + name + ".aseprite", name, report);
        }

        static Source LoadFromPath(string path, string name, Report report)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Where(s => s.name == name).ToArray();
            Require(sprites.Length == 1, "Expected exactly one native Sprite named " + name + " at " + path);
            string guid; long id;
            Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprites[0], out guid, out id), "Sprite identity unavailable: " + path);
            var source = new Source { path = path, name = name, guid = guid, fileID = id, sprite = sprites[0] };
            report.nativeSprites.Add(source);
            return source;
        }

        static bool UsesSource(Image image, Source source)
        {
            return image && image.sprite && source != null &&
                AssetDatabase.GetAssetPath(image.sprite) == source.path;
        }

        static void PlanDisable(Transform parent, string name, Source source, List<Image> graphics, Report report)
        {
            var child = FindChild(parent, name);
            if (!child) return;
            var image = child.GetComponent<Image>();
            if (UsesSource(image, source)) graphics.Add(image);
            else report.skipped.Add(PathOf(child) + ": custom or missing native sprite preserved");
        }

        static bool PlanButton(Button button, Source source, List<Binding> bindings, Report report)
        {
            var image = button.GetComponent<Image>();
            Require(image && button.targetGraphic == image, "Button targetGraphic is not its existing Image: " + PathOf(button.transform));
            var state = button.spriteState;
            if (image.overrideSprite != image.sprite || (button.transition == Selectable.Transition.SpriteSwap &&
                (state.highlightedSprite || state.pressedSprite || state.selectedSprite || state.disabledSprite)) ||
                button.transition == Selectable.Transition.Animation || button.GetComponent<Animator>())
            {
                report.skipped.Add(PathOf(button.transform) + ": SpriteSwap/override/animation may replace sprite; preserved");
                return false;
            }
            bindings.Add(new Binding { image = image, source = source, white = true });
            return true;
        }

        static T Ref<T>(SerializedObject owner, string name, Scene scene) where T : Object
        {
            var property = owner.FindProperty(name);
            var value = property == null ? null : property.objectReferenceValue as T;
            Require(value, "Missing serialized reference: " + name);
            var component = value as Component;
            var go = value as GameObject;
            Require((component && component.gameObject.scene == scene) || (go && go.scene == scene), "Reference outside target scene: " + name);
            return value;
        }
        static Transform FindChild(Transform parent, string name)
        {
            if (!parent) return null;
            Transform found = null;
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name)
                { Require(!found, "Duplicate child: " + name); found = parent.GetChild(i); }
            return found;
        }
        static Transform Child(Transform parent, string name)
        {
            var child = FindChild(parent, name);
            Require(child, "Missing child: " + name);
            return child;
        }
        static Decoration Decor(Transform parent, string name, Source source, float x, float y, float right, float top)
        { return new Decoration { parent = parent, name = name, source = source, min = new Vector2(x, y), max = new Vector2(right, top) }; }
        static void AddGroup(List<Decoration> list, string name, Source source, float x, float y, float right, float top)
        { var d = Decor(null, name, source, x, y, right, top); d.group = true; list.Add(d); }
        static void Change(Object target, Action action)
        {
            Undo.RecordObject(target, UndoName);
            action();
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        static string PathOf(Transform transform)
        { return transform.parent ? PathOf(transform.parent) + "/" + transform.name : transform.name; }
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
