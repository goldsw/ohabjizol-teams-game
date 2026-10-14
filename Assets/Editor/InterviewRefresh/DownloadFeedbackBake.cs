using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public static class DownloadFeedbackBake
    {
        public static void Apply(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var panels = root.GetComponentsInChildren<DownloadPanelView>(true);
                if (panels.Length == 0)
                    throw new InvalidOperationException("DownloadPanelView missing from prefab: " + prefabPath);
                foreach (var panel in panels)
                {
                    var serialized = new SerializedObject(panel);
                    var application = serialized.FindProperty("_applicationButton").objectReferenceValue as Button;
                    var portfolio = serialized.FindProperty("_portfolioButton").objectReferenceValue as Button;
                    if (!application || !portfolio)
                        throw new InvalidOperationException("DownloadPanelView download button references missing.");
                    panel.ConfigureProgressEditor(EnsureFill(application), EnsureFill(portfolio));
                    EditorUtility.SetDirty(panel);
                }
                if (!PrefabUtility.SaveAsPrefabAsset(root, prefabPath))
                    throw new InvalidOperationException("Could not save download feedback prefab: " + prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Image EnsureFill(Button button)
        {
            Transform child = button.transform.Find("ProgressFill");
            if (!child)
            {
                var created = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                created.transform.SetParent(button.transform, false);
                child = created.transform;
            }
            var fill = child.GetComponent<Image>();
            if (!fill) fill = child.gameObject.AddComponent<Image>();
            fill.sprite = null;
            fill.type = Image.Type.Simple;
            fill.color = new Color(.2f, .75f, .35f, 1f);
            fill.raycastTarget = false;
            fill.enabled = true;
            child.gameObject.SetActive(true);
            var rect = fill.rectTransform;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(0);
            return fill;
        }
    }
}