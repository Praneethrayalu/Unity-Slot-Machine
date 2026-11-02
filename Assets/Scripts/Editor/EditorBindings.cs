using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlotGame.Editor
{
    /// <summary>
    /// Writes private [SerializeField] values from editor tooling, the same way the Inspector does.
    /// Every setter fails loudly on a misspelt property name so a broken wiring is caught at build
    /// time instead of as a NullReferenceException at runtime.
    /// </summary>
    internal static class EditorBindings
    {
        public static void Set(Object target, string propertyName, Object value)
        {
            Modify(target, propertyName, p => p.objectReferenceValue = value);
        }

        public static void Set(Object target, string propertyName, float value)
        {
            Modify(target, propertyName, p => p.floatValue = value);
        }

        public static void Set(Object target, string propertyName, int value)
        {
            Modify(target, propertyName, p => p.intValue = value);
        }

        public static void Set(Object target, string propertyName, bool value)
        {
            Modify(target, propertyName, p => p.boolValue = value);
        }

        public static void Set(Object target, string propertyName, string value)
        {
            Modify(target, propertyName, p => p.stringValue = value);
        }

        public static void Set(Object target, string propertyName, Vector2 value)
        {
            Modify(target, propertyName, p => p.vector2Value = value);
        }

        public static void SetArray(Object target, string propertyName, params Object[] values)
        {
            Modify(target, propertyName, p =>
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            });
        }

        private static void Modify(Object target, string propertyName, Action<SerializedProperty> apply)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new ArgumentException($"{target.GetType().Name} has no serialized property '{propertyName}'.");
            }

            apply(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
