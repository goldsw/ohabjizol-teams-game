#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewScreen
    {
        public int ConfigureNotepadImeEditor()
        {
            int changed = 0;
            var windows = new List<InterviewNotepadWindow>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
                windows.AddRange(root.GetComponentsInChildren<InterviewNotepadWindow>(true));
            if (windows.Count == 0) throw new InvalidOperationException("Authored notepad window missing.");
            foreach (var window in windows)
            {
                var original = window.AuthoredInputEditor;
                if (!original) throw new InvalidOperationException("Notepad InputField reference missing.");
                if (original is InterviewImeInputField) continue;
                var owner = original.gameObject;
                if (owner.GetComponents<InputField>().Length != 1)
                    throw new InvalidOperationException("Expected exactly one notepad InputField.");
                string serialized = EditorJsonUtility.ToJson(original);
                // Keep the replacement's script identity while restoring inherited serialized fields.
                serialized = Regex.Replace(serialized, "\"m_Script\"\\s*:\\s*\\{[^}]*\\}\\s*,", string.Empty);
                var selfReferences = new List<string>();
                var originalSerialized = new SerializedObject(original);
                var iterator = originalSerialized.GetIterator();
                while (iterator.Next(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == original)
                        selfReferences.Add(iterator.propertyPath);
                var validator = original.onValidateInput;
                var textGraphic = original.textComponent;
                var placeholderGraphic = original.placeholder;
                var target = original.targetGraphic;
                bool targetsSelf = original.navigation.selectOnUp == original || original.navigation.selectOnDown == original
                    || original.navigation.selectOnLeft == original || original.navigation.selectOnRight == original;
                var navigation = original.navigation;
                bool up = navigation.selectOnUp == original, down = navigation.selectOnDown == original;
                bool left = navigation.selectOnLeft == original, right = navigation.selectOnRight == original;
                Undo.RecordObject(window, "Replace notepad InputField");
                Undo.DestroyObjectImmediate(original);
                var replacement = Undo.AddComponent<InterviewImeInputField>(owner);
                Undo.RecordObject(replacement, "Restore notepad input settings");
                EditorJsonUtility.FromJsonOverwrite(serialized, replacement);
                var restored = new SerializedObject(replacement);
                foreach (string path in selfReferences)
                {
                    var property = restored.FindProperty(path);
                    if (property == null) throw new InvalidOperationException("Cannot restore input self reference: " + path);
                    property.objectReferenceValue = replacement;
                }
                restored.ApplyModifiedProperties();
                replacement.textComponent = textGraphic;
                replacement.placeholder = placeholderGraphic;
                replacement.targetGraphic = target;
                replacement.onValidateInput = validator;
                if (targetsSelf)
                {
                    if (up) navigation.selectOnUp = replacement;
                    if (down) navigation.selectOnDown = replacement;
                    if (left) navigation.selectOnLeft = replacement;
                    if (right) navigation.selectOnRight = replacement;
                    replacement.navigation = navigation;
                }
                window.ConfigureEditor(replacement);
                EditorUtility.SetDirty(replacement);
                EditorUtility.SetDirty(window);
                PrefabUtility.RecordPrefabInstancePropertyModifications(window);
                PrefabUtility.RecordPrefabInstancePropertyModifications(replacement);
                changed++;
            }
            return changed;
        }
    }
}
#endif
