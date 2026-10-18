#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Ohabjizol.Interview;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameJamRosterBake
{
    public static void Apply(string prefabPath)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Roster bake requires EditMode.");
        if (prefabPath != "Assets/Prefabs/InterviewRefresh/InterviewScreen.prefab")
            throw new ArgumentException("Only the InterviewScreen prefab is supported.", nameof(prefabPath));

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var screens = root.GetComponentsInChildren<InterviewScreen>(true);
            if (screens.Length != 1) throw new InvalidOperationException("Expected one InterviewScreen.");
            var serialized = new SerializedObject(screens[0]);
            serialized.Update();
            var slots = serialized.FindProperty("_slotLabels");
            if (slots == null || !slots.isArray || (slots.arraySize != 4 && slots.arraySize != 6))
                throw new InvalidOperationException("Expected four or six serialized roster slots.");

            var labels = new Text[6];
            var unique = new HashSet<Text>();
            Transform parent = null;
            for (int i = 0; i < slots.arraySize; i++)
            {
                var label = slots.GetArrayElementAtIndex(i).objectReferenceValue as Text;
                if (!label || !unique.Add(label))
                    throw new InvalidOperationException("Roster slot references must be present and distinct.");
                if (i == 0) parent = label.transform.parent;
                if (!parent || label.transform.parent != parent || label.name != "Slot" + i)
                    throw new InvalidOperationException("Expected sibling Slot0..SlotN labels.");
                labels[i] = label;
            }
            if (parent.name != "TeamSummary")
                throw new InvalidOperationException("Roster slots must belong to TeamSummary.");
            if (slots.arraySize == 4 && (parent.Find("Slot4") || parent.Find("Slot5")))
                throw new InvalidOperationException("Unregistered Slot4/Slot5 already exist; resolve references before baking.");

            for (int i = slots.arraySize; i < labels.Length; i++)
            {
                var clone = UnityEngine.Object.Instantiate(labels[0].gameObject, parent, false);
                clone.name = "Slot" + i;
                labels[i] = clone.GetComponent<Text>();
                labels[i].text = "빈 자리";
            }

            var layout = parent.GetComponent<LayoutGroup>();
            bool layoutActive = layout && layout.isActiveAndEnabled;
            int firstSibling = labels[0].transform.GetSiblingIndex();
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].transform.SetSiblingIndex(firstSibling + i);
                if (!layoutActive)
                {
                    var rect = labels[i].rectTransform;
                    rect.anchorMin = new Vector2(.06f, .76f - i * .11f);
                    rect.anchorMax = new Vector2(.94f, .86f - i * .11f);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                }
            }

            slots.arraySize = labels.Length;
            for (int i = 0; i < labels.Length; i++)
                slots.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (layoutActive) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)parent);
            EditorUtility.SetDirty(screens[0]);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
            if (!success) throw new InvalidOperationException("Saving roster prefab failed.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
