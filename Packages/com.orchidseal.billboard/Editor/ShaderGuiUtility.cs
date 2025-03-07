using UnityEditor;
using UnityEngine;

namespace OrchidSeal.Billboard.Editor
{
    public static class ShaderGuiUtility
    {
        public static void Vector2Property(MaterialProperty property, GUIContent name)
        {
            EditorGUI.BeginChangeCheck();
            var vector2 = EditorGUILayout.Vector2Field(name, new Vector2(property.vectorValue.x, property.vectorValue.y), null);
            if (EditorGUI.EndChangeCheck())
            {
                property.vectorValue = new Vector4(vector2.x, vector2.y, property.vectorValue.z, property.vectorValue.w);
            }
        }
        
        public static void Vector3Property(MaterialProperty property, GUIContent name)
        {
            EditorGUI.BeginChangeCheck();
            var vector3 = EditorGUILayout.Vector3Field(name, new Vector3(property.vectorValue.x, property.vectorValue.y, property.vectorValue.z), null);
            if (EditorGUI.EndChangeCheck())
            {
                property.vectorValue = new Vector4(vector3.x, vector3.y, vector3.z, property.vectorValue.w);
            }
        }

        public static void Vector4Property(MaterialProperty property, GUIContent name, string[] labels)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(name);
            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 12;
            var x = EditorGUILayout.FloatField(labels[0], property.vectorValue.x);
            var y = EditorGUILayout.FloatField(labels[1], property.vectorValue.y);
            var z = EditorGUILayout.FloatField(labels[2], property.vectorValue.z);
            var w = EditorGUILayout.FloatField(labels[3], property.vectorValue.w);
            EditorGUIUtility.labelWidth = 0;
            if (EditorGUI.EndChangeCheck())
            {
                property.vectorValue = new Vector4(x, y, z, w);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
