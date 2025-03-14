using UnityEditor;
using UnityEngine;

namespace OrchidSeal.Billboard.Editor
{
    public static class ShaderGuiUtility
    {
        private static readonly int s_ToggleHash = nameof(s_ToggleHash).GetHashCode();
        
        private static class Styles
        {
            public static int headerHeight = 22;

            public static GUIStyle foldoutCollapseToggle = new (EditorStyles.foldout);
            public static Color foldoutHeaderBackground = new (0.34f, 0.34f, 0.34f);
            public static Color foldoutHeaderBackgroundHover = new(0.29f, 0.29f, 0.29f);
            
            public static GUIStyle foldoutHeaderLabel = new(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };
            
            public static GUIStyle foldoutHeaderLabelFocus = new(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                hover =
                {
                    textColor = new Color(0.45f, 0.62f, 0.86f),
                },
                normal =
                {
                    textColor = new Color(0.45f, 0.62f, 0.86f),
                }
            };
        }
        
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

        public static bool MaterialPropertyFoldout(string title, ref bool isCollapsed, MaterialProperty property)
        {
            var boolValue = property.floatValue != 0.0f;
            FoldoutHeader(title, ref isCollapsed, ref boolValue);
            property.floatValue = boolValue ? 1.0f : 0.0f;
            return isCollapsed;
        }

        public static bool FoldoutHeader(string title, ref bool isShown)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, Styles.headerHeight);
            backgroundRect.xMin = 0;
            backgroundRect.xMax = Screen.width;
            
            var current = Event.current;
            var isHover = backgroundRect.Contains(current.mousePosition);
            
            var collapseRect = new Rect(backgroundRect);
            collapseRect.xMin += 8;
            collapseRect.width = 20;
            var collapseToggleId = GUIUtility.GetControlID(s_ToggleHash, FocusType.Keyboard, collapseRect);
            var collapseHasKeyboardFocus = GUIUtility.keyboardControl == collapseToggleId;
            
            var labelRect = new Rect(backgroundRect);
            labelRect.xMin = collapseRect.xMax;
            
            EditorGUI.DrawRect(backgroundRect, isHover ? Styles.foldoutHeaderBackgroundHover : Styles.foldoutHeaderBackground);
            isShown = GUI.Toggle(collapseRect, collapseToggleId, isShown, GUIContent.none, Styles.foldoutCollapseToggle);
            GUI.Label(labelRect, title, collapseHasKeyboardFocus ? Styles.foldoutHeaderLabelFocus : Styles.foldoutHeaderLabel);

            if (current.type == EventType.MouseDown && isHover && current.button == 0)
            {
                isShown = !isShown;
                GUIUtility.hotControl = collapseToggleId;
                GUIUtility.keyboardControl = collapseToggleId;
                current.Use();
            }
            
            GUILayout.Space(isShown ? 6 : 2);

            return isShown;
        }

        public static bool FoldoutHeader(string title, ref bool isShown, ref bool toggle)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, Styles.headerHeight);
            backgroundRect.xMin = 0;
            backgroundRect.xMax = Screen.width;
            
            var current = Event.current;
            var isHover = backgroundRect.Contains(current.mousePosition);
            
            var collapseRect = new Rect(backgroundRect);
            collapseRect.xMin += 8;
            collapseRect.width = 20;
            var collapseToggleId = GUIUtility.GetControlID(s_ToggleHash, FocusType.Keyboard, collapseRect);
            var collapseHasKeyboardFocus = GUIUtility.keyboardControl == collapseToggleId;
            
            var toggleRect = new Rect(backgroundRect);
            toggleRect.xMin = collapseRect.xMax;
            toggleRect.width = 20;
            var toggleId = GUIUtility.GetControlID(s_ToggleHash, FocusType.Keyboard, toggleRect);
            
            var labelRect = new Rect(backgroundRect);
            labelRect.xMin = toggleRect.xMax;
            
            EditorGUI.DrawRect(backgroundRect, isHover ? Styles.foldoutHeaderBackgroundHover : Styles.foldoutHeaderBackground);
            isShown = GUI.Toggle(collapseRect, collapseToggleId, isShown, GUIContent.none, Styles.foldoutCollapseToggle);
            toggle = GUI.Toggle(toggleRect, toggleId, toggle, GUIContent.none, EditorStyles.toggle);
            EditorGUI.LabelField(labelRect, title, collapseHasKeyboardFocus ? Styles.foldoutHeaderLabelFocus : Styles.foldoutHeaderLabel);

            if (current.type == EventType.MouseDown && isHover && current.button == 0)
            {
                isShown = !isShown;
                GUIUtility.hotControl = collapseToggleId;
                GUIUtility.keyboardControl = collapseToggleId;
                current.Use();
            }
            
            GUILayout.Space(isShown ? 6 : 2);

            return isShown;
        }
    }
}
