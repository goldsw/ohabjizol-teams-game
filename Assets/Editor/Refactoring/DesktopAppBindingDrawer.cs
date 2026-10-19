#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Ohabjizol.Presentation;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DesktopAppBinding))]
public sealed class DesktopAppBindingDrawer : PropertyDrawer
{
    static readonly string[] FieldNames = { "icon", "window", "initiallyOpen", "onActivated" };
    static List<Type> _handlerTypes;

    static List<Type> HandlerTypes
    {
        get
        {
            if (_handlerTypes != null) return _handlerTypes;
            _handlerTypes = new List<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<DesktopAppHandler>())
            {
                if (type.IsAbstract || type.ContainsGenericParameters ||
                    type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, Type.EmptyTypes, null) == null) continue;
                _handlerTypes.Add(type);
            }
            _handlerTypes.Sort((left, right) =>
                string.Compare(left.AssemblyQualifiedName, right.AssemblyQualifiedName, StringComparison.Ordinal));
            return _handlerTypes;
        }
    }

    static float Gap => EditorGUIUtility.standardVerticalSpacing;
    static float Line => EditorGUIUtility.singleLineHeight;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = Line;
        if (!property.isExpanded) return height;
        foreach (var fieldName in FieldNames)
            height += Gap + EditorGUI.GetPropertyHeight(property.FindPropertyRelative(fieldName), true);
        height += Gap + Line;
        var activation = property.FindPropertyRelative("activation");
        if (activation.managedReferenceValue != null)
            height += Gap + EditorGUI.GetPropertyHeight(activation, true);
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var row = new Rect(position.x, position.y, position.width, Line);
        property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
        if (property.isExpanded)
        {
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;
            foreach (var fieldName in FieldNames)
            {
                var field = property.FindPropertyRelative(fieldName);
                row.y += row.height + Gap;
                row.height = EditorGUI.GetPropertyHeight(field, true);
                EditorGUI.PropertyField(row, field, true);
            }
            row.y += row.height + Gap;
            row.height = Line;
            var activation = property.FindPropertyRelative("activation");
            DrawActivationType(row, activation);
            if (activation.managedReferenceValue != null)
            {
                row.y += row.height + Gap;
                row.height = EditorGUI.GetPropertyHeight(activation, true);
                EditorGUI.PropertyField(row, activation, new GUIContent("Handler settings"), true);
            }
            EditorGUI.indentLevel = previousIndent;
        }
        EditorGUI.EndProperty();
    }

    static void DrawActivationType(Rect position, SerializedProperty activation)
    {
        var types = HandlerTypes;
        Type currentType = activation.managedReferenceValue?.GetType();
        int current = currentType == null ? 0 : types.IndexOf(currentType) + 1;
        // Preserve an existing handler even when its type cannot be constructed by this drawer.
        bool unavailable = currentType != null && current == 0;
        var labels = new string[types.Count + 1 + (unavailable ? 1 : 0)];
        labels[0] = "Common Open";
        for (int i = 0; i < types.Count; i++)
            labels[i + 1] = types[i].FullName + " (" + types[i].Assembly.GetName().Name + ")";
        if (unavailable)
        {
            current = labels.Length - 1;
            labels[current] = currentType.FullName + " (existing)";
        }
        bool previousMixed = EditorGUI.showMixedValue;
        EditorGUI.showMixedValue = activation.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int selected = EditorGUI.Popup(position, "Activation", current, labels);
        bool changed = EditorGUI.EndChangeCheck();
        EditorGUI.showMixedValue = previousMixed;
        if (!changed || (unavailable && selected == labels.Length - 1)) return;
        try
        {
            activation.managedReferenceValue = selected == 0
                ? null : Activator.CreateInstance(types[selected - 1], true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
#endif