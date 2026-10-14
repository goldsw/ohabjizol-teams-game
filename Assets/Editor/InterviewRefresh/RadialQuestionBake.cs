using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public static class RadialQuestionBake
    {
        public static void Apply(string prefabPath)
        {
            if (string.IsNullOrWhiteSpace(prefabPath)) throw new ArgumentException("Prefab path required.", nameof(prefabPath));
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var screen = prefab.GetComponentInChildren<InterviewScreen>(true);
                if (!screen) throw new InvalidOperationException("InterviewScreen missing.");
                var serialized = new SerializedObject(screen);
                var call = serialized.FindProperty("_call").objectReferenceValue as InterviewCallView;
                var source = serialized.FindProperty("_questions");
                if (!call || !call.PortraitRect || source.arraySize != 5)
                    throw new InvalidOperationException("Authored portrait and five questions required.");
                var buttons = new Button[source.arraySize];
                for (int i = 0; i < buttons.Length; i++)
                {
                    var view = source.GetArrayElementAtIndex(i).objectReferenceValue as InterviewQuestionView;
                    if (!view || !view.Button) throw new InvalidOperationException("Question button missing at index " + i);
                    buttons[i] = view.Button;
                }
                var portraitCanvas = call.GetComponentInParent<Canvas>();
                var rootCanvas = portraitCanvas ? portraitCanvas.rootCanvas : null;
                Transform uiParent = rootCanvas &&
                    (rootCanvas.transform == prefab.transform || rootCanvas.transform.IsChildOf(prefab.transform))
                    ? rootCanvas.transform : prefab.transform as RectTransform;
                if (!uiParent)
                    throw new InvalidOperationException("Prefab-owned root Canvas or root RectTransform required.");

                var radial = serialized.FindProperty("_radialQuestions").objectReferenceValue as RadialQuestionView;
                RectTransform host;
                RectTransform options;
                Button launcher;
                if (radial)
                {
                    host = radial.transform as RectTransform;
                    var binding = new SerializedObject(radial);
                    options = binding.FindProperty("_optionsRoot").objectReferenceValue as RectTransform;
                    launcher = binding.FindProperty("_launcher").objectReferenceValue as Button;
                    if (!host || !options || !launcher) throw new InvalidOperationException("Existing radial bindings incomplete.");
                    if (host.parent != uiParent)
                    {
                        Vector3 launcherPosition = launcher.transform.position;
                        host.SetParent(uiParent, true);
                        host.localScale = Vector3.one;
                        host.localRotation = Quaternion.identity;
                        Stretch(host);
                        launcher.transform.position = launcherPosition;
                    }
                }
                else
                {
                    host = Rect("RadialQuestions", uiParent);
                    Stretch(host);
                    options = Rect("OptionsRoot", host);
                    Stretch(options);
                    var launcherRect = Rect("QuestionLauncher", host);
                    launcherRect.anchorMin = launcherRect.anchorMax = launcherRect.pivot = new Vector2(.5f, .5f);
                    launcherRect.sizeDelta = new Vector2(154f, 48f);
                    Vector3 center = host.InverseTransformPoint(call.PortraitRect.TransformPoint(call.PortraitRect.rect.center));
                    launcherRect.localPosition = center + new Vector3(0f, -250f, 0f);
                    var image = launcherRect.gameObject.AddComponent<Image>();
                    image.color = new Color(.18f, .27f, .36f, 1f);
                    image.raycastTarget = true;
                    launcher = launcherRect.gameObject.AddComponent<Button>();
                    launcher.targetGraphic = image;
                    var navigation = launcher.navigation;
                    navigation.mode = Navigation.Mode.None;
                    launcher.navigation = navigation;
                    var labelRect = Rect("Label", launcherRect);
                    Stretch(labelRect);
                    var label = labelRect.gameObject.AddComponent<Text>();
                    var originalLabel = buttons[0].GetComponentInChildren<Text>(true);
                    label.font = originalLabel && originalLabel.font ? originalLabel.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    label.fontSize = 20;
                    label.alignment = TextAnchor.MiddleCenter;
                    label.color = Color.white;
                    label.raycastTarget = false;
                    label.text = "질문";
                    radial = host.gameObject.AddComponent<RadialQuestionView>();
                }
                for (int i = 0; i < buttons.Length; i++)
                {
                    var rect = buttons[i].transform as RectTransform;
                    rect.SetParent(options, false);
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                    rect.sizeDelta = new Vector2(154f, 48f);
                    rect.localScale = Vector3.one;
                    rect.localRotation = Quaternion.identity;
                    var layout = rect.GetComponent<LayoutElement>();
                    if (layout) layout.ignoreLayout = true;
                }
                radial.ConfigureEditor(launcher, options, call.PortraitRect, buttons);
                serialized.FindProperty("_radialQuestions").objectReferenceValue = radial;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(screen);
                EditorUtility.SetDirty(radial);
                if (!PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath))
                    throw new InvalidOperationException("Failed to save radial question prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}