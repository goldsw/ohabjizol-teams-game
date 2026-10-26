using System;
using System.IO;
using Ohabjizol.Presentation;
using Ohabjizol.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ohabjizol.Editor.InterviewRefresh
{
    public static class CreditsDesktopBake
    {
        const string MenuPath = "Assets/Scenes/MainMenuScene.unity";

        [MenuItem("Tools/Interview/Add Credits Desktop Button")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Credits desktop authoring requires Edit Mode.");
            Scene originalActive = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(MenuPath);
            bool closeAfter = !scene.IsValid() || !scene.isLoaded;
            if (closeAfter) scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Additive);
            try
            {
                DesktopView desktop = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var candidate = root.GetComponentInChildren<DesktopView>(true);
                    if (!candidate) continue;
                    if (desktop) throw new InvalidOperationException("Main menu contains multiple desktops.");
                    desktop = candidate;
                }
                if (!desktop) throw new InvalidOperationException("Main menu desktop is missing.");
                var data = new SerializedObject(desktop);
                var reference = data.FindProperty("_codeIcon").objectReferenceValue as DesktopIcon;
                if (!reference) throw new InvalidOperationException("Existing desktop code icon is required.");
                var icons = reference.transform.parent as RectTransform;
                var existing = icons.Find("CreditsIcon");
                if (existing)
                {
                    var shortcut = existing.GetComponent<DesktopCreditsButton>();
                    if (!shortcut || !shortcut.HasAuthoredUi)
                        throw new InvalidOperationException("Existing CreditsIcon requires complete button references.");
                    return;
                }

                // Persist the user's open scene edits before taking the authoring snapshot.
                if (scene.isDirty && !EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not preserve the edited main menu scene.");
                string backup = "Logs/Credits/Before/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff");
                Directory.CreateDirectory(backup);
                File.Copy(MenuPath, Path.Combine(backup, "MainMenuScene.unity"));
                Author(desktop, icons, reference);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save the credits desktop shortcut.");
            }
            finally
            {
                if (originalActive.IsValid() && originalActive.isLoaded)
                    SceneManager.SetActiveScene(originalActive);
                if (closeAfter && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void Author(DesktopView desktop, RectTransform icons, DesktopIcon reference)
        {
            var source = (RectTransform)reference.transform;
            var sourceTile = reference.transform.Find("Tile") as RectTransform;
            if (!sourceTile) sourceTile = reference.transform.Find("Icon") as RectTransform;
            var sourceGlyph = sourceTile ? sourceTile.Find("Glyph")?.GetComponent<Text>() : null;
            var sourceLabel = reference.transform.Find("Label")?.GetComponent<Text>();
            if (!sourceTile || !sourceLabel)
                throw new InvalidOperationException("Existing desktop icon artwork references are incomplete.");
            Vector2 position = NextPosition(icons, source);
            var button = UiKit.Button(icons, "CreditsIcon", "크레딧", Color.clear, sourceLabel.fontSize);
            var rect = (RectTransform)button.transform;
            CopyRect(source, rect);
            rect.anchoredPosition = position;
            var label = button.GetComponentInChildren<Text>(true);
            CopyRect(sourceLabel.rectTransform, label.rectTransform);
            label.font = UiKit.Font;
            label.fontStyle = FontStyle.Normal;
            label.alignment = sourceLabel.alignment;
            label.color = sourceLabel.color;
            label.raycastTarget = false;
            label.gameObject.name = "Label";
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .6f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            var visual = (RectTransform)new GameObject("Icon", typeof(RectTransform)).transform;
            visual.SetParent(rect, false);
            CopyRect(sourceTile, visual);
            var sourceFrame = sourceTile.Find("background")?.GetComponent<Image>();
            var tile = UiKit.Image(visual, "Background", sourceFrame ? sourceFrame.color : new Color32(112, 89, 180, 255));
            if (sourceFrame)
            {
                CopyRect(sourceFrame.rectTransform, tile.rectTransform);
                tile.sprite = sourceFrame.sprite;
                tile.type = sourceFrame.type;
                tile.preserveAspect = sourceFrame.preserveAspect;
            }
            else UiKit.Stretch(tile.rectTransform, 0f);
            var glyph = UiKit.Text(visual, "Glyph", sourceGlyph ? sourceGlyph.fontSize : 64,
                FontStyle.Normal, TextAnchor.MiddleCenter);
            glyph.text = "크";
            glyph.color = Color.white;
            glyph.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Stretch(glyph.rectTransform, 0f);
            foreach (var image in button.GetComponentsInChildren<Image>(true))
                if (image.sprite && !EditorUtility.IsPersistent(image.sprite)) image.sprite = null;
            button.gameObject.AddComponent<DesktopCreditsButton>().ConfigureEditor(desktop, button);
        }

        static Vector2 NextPosition(RectTransform parent, RectTransform reference)
        {
            float bottom = -reference.anchoredPosition.y + reference.rect.height;
            float rightColumn = reference.anchoredPosition.x;
            foreach (Transform child in parent)
            {
                var rect = child as RectTransform;
                if (!rect) continue;
                bottom = Mathf.Max(bottom, -rect.anchoredPosition.y + rect.rect.height);
                rightColumn = Mathf.Max(rightColumn, rect.anchoredPosition.x);
            }
            float nextTop = bottom + 14f;
            if (nextTop + reference.rect.height <= parent.rect.height - 64f)
                return new Vector2(reference.anchoredPosition.x, -nextTop);
            return new Vector2(rightColumn + reference.rect.width + 32f, reference.anchoredPosition.y);
        }

        static void CopyRect(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta;
            target.anchoredPosition = source.anchoredPosition;
            target.localScale = source.localScale;
            target.localRotation = source.localRotation;
        }
    }
}
