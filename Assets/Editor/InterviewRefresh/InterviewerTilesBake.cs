using System;
using System.IO;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Interview.Editor
{
    public static class InterviewerTilesBake
    {
        const string Prefab = "Assets/Prefabs/InterviewRefresh/InterviewScreen.prefab";
        static readonly string[] Scenes = { "Assets/Scenes/InterviewScene.unity", "Assets/Scenes/TutorialScene.unity" };

        [MenuItem("Tools/Interview Refresh/Apply Interviewer Tiles")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Interviewer authoring requires idle Edit Mode.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Close Prefab Mode before authoring shared interviewer tiles.");
            string backup = "Logs/InterviewerTiles/Before/" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff");
            foreach (string path in new[] { Prefab, Scenes[0], Scenes[1] })
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
            var active = SceneManager.GetActiveScene();
            try
            {
                var root = PrefabUtility.LoadPrefabContents(Prefab);
                try
                {
                    Bake(root.GetComponentInChildren<InterviewScreen>(true));
                    PrefabUtility.SaveAsPrefabAsset(root, Prefab, out bool success);
                    if (!success) throw new IOException("Screen prefab save failed.");
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                foreach (var path in Scenes)
                {
                    var scene = SceneManager.GetSceneByPath(path);
                    bool opened = !scene.IsValid() || !scene.isLoaded;
                    if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    try
                    {
                        int count = 0;
                        foreach (var sceneRoot in scene.GetRootGameObjects())
                            foreach (var screen in sceneRoot.GetComponentsInChildren<InterviewScreen>(true)) { Bake(screen); count++; }
                        if (count != 1) throw new InvalidOperationException("One InterviewScreen required: " + path);
                        EditorSceneManager.MarkSceneDirty(scene);
                        if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Scene save failed: " + path);
                    }
                    finally { if (opened && scene.IsValid() && scene.isLoaded && !scene.isDirty) EditorSceneManager.CloseScene(scene, true); }
                }
            }
            finally { if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
        }

        static void Bake(InterviewScreen screen)
        {
            if (!screen) throw new InvalidOperationException("Screen required.");
            var view = screen.transform.Find("InterviewAuthoredUi/DiscordViewRoot");
            var left = view ? view.Find("FixedParticipant0") : null;
            var right = view ? view.Find("FixedParticipant2") : null;
            var source = view ? view.Find("CallChannelAnchor/CallParticipants/SlotGroup0/Profile") : null;
            if (!left || !right || !source) throw new InvalidOperationException("Existing interviewer tiles and profile required.");
            var fade = left.GetComponent<CanvasGroup>();
            if (!fade) fade = left.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 1f;
            fade.interactable = false;
            fade.blocksRaycasts = false;
            Save(fade);
            var profile = left.Find("Profile");
            if (!profile)
            {
                profile = UnityEngine.Object.Instantiate(source.gameObject, left, false).transform;
                profile.name = "Profile";
                var rect = (RectTransform)profile;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .6f);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(88, 88);
            }
            profile.gameObject.SetActive(true);
            var icon = left.Find("Icon");
            if (icon) { icon.gameObject.SetActive(false); Save(icon.gameObject); }
            var portrait = profile.Find("ProfileMask/Portrait")?.GetComponent<Image>();
            if (!portrait) throw new InvalidOperationException("Copied profile portrait required.");
            portrait.gameObject.SetActive(true);
            foreach (var image in profile.GetComponentsInChildren<Image>(true)) { image.raycastTarget = false; Save(image); }
            var serialized = new SerializedObject(screen);
            var call = serialized.FindProperty("_call").objectReferenceValue as InterviewCallView;
            if (!call) throw new InvalidOperationException("Call view required.");
            call.ConfigureInterviewerProfileEditor(portrait);
            Save(call);

            var text = serialized.FindProperty("_notification").objectReferenceValue as Text;
            if (!text) throw new InvalidOperationException("Notification text required.");
            var background = right.Find("NewApplicantNotification")?.GetComponent<Image>();
            if (!background)
            {
                background = UiKit.Image(right, "NewApplicantNotification", new Color(1f, .94f, .84f, .98f));
                var rect = background.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
                rect.pivot = new Vector2(.5f, 0f);
                rect.anchoredPosition = new Vector2(0, 12);
                rect.sizeDelta = new Vector2(400, 64);
                var tail = UiKit.Image(rect, "Tail", background.color);
                tail.rectTransform.anchorMin = tail.rectTransform.anchorMax = new Vector2(.5f, 0);
                tail.rectTransform.anchoredPosition = Vector2.zero;
                tail.rectTransform.sizeDelta = new Vector2(14, 14);
                tail.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                tail.raycastTarget = false;
                text.transform.SetParent(rect, false);
                UiKit.Stretch(text.rectTransform, 8);
                text.font = UiKit.Font;
                text.fontSize = 24;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(.28f, .22f, .18f);
                text.raycastTarget = false;
                text.text = string.Empty;
            }
            background.raycastTarget = false;
            var bubble = text.GetComponent<InterviewNotificationBubble>();
            if (!bubble) bubble = text.gameObject.AddComponent<InterviewNotificationBubble>();
            bubble.ConfigureEditor(background);
            Save(background);
            Save(background.gameObject);
            Save(text);
            Save(text.rectTransform);
            Save(bubble);
        }

        static void Save(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
