using UnityEditor;
using UnityEngine;

namespace OrchidSeal.Billboard.Editor
{
    public class BaseBillboardEditor : ShaderGUI
    {
        protected bool showStencilOptions;
        private bool showTransformationOptions = true;
        
        private static class Styles
        {
            public static GUIStyle sectionVerticalLayout = new()
            {
                margin = new RectOffset(0, 0, 0, 12),
            };
            
            // Transformation
            public const string transformationFoldoutLabel = "Transformation";
            public static readonly GUIContent positionLabel = new("Position");
            public static readonly GUIContent rotationRollLabel = new ("Rotation Roll");
            public static readonly GUIContent scaleLabel = new ("Scale");
            public static readonly GUIContent billboardModeLabel = new ("Billboard Mode");
            public static readonly GUIContent useNonUniformScaleLabel = new ("Use Non Uniform Object Scale");
            public static readonly GUIContent keepConstantScalingLabel = new ("Keep Constant Scaling");
            public static readonly GUIContent constantScaleLabel = new ("Constant Scale");
            
            // Stencil
            public const string stencilFoldoutLabel = "Stencil";
            public static readonly GUIContent stencilReferenceLabel = new ("Reference");
            public static readonly GUIContent stencilReadMaskLabel = new ("Read Mask");
            public static readonly GUIContent stencilWriteMaskLabel = new ("Write Mask");
            public static readonly GUIContent stencilComparisonLabel = new ("Comparison");
            public static readonly GUIContent stencilPassLabel = new ("Pass");
            public static readonly GUIContent stencilFailLabel = new ("Fail");
            public static readonly GUIContent stencilZFailLabel = new ("ZFail");
        }
        
        protected void TransformationOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (ShaderGuiUtility.FoldoutHeader(Styles.transformationFoldoutLabel, ref showTransformationOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);

                materialEditor.ShaderProperty(FindProperty("_Billboard_Mode", properties), Styles.billboardModeLabel);
                ShaderGuiUtility.Vector2Property(FindProperty("_Position", properties), Styles.positionLabel);
                materialEditor.ShaderProperty(FindProperty("_RotationRoll", properties), Styles.rotationRollLabel);
                ShaderGuiUtility.Vector2Property(FindProperty("_Scale", properties), Styles.scaleLabel);
                ShaderGuiUtility.OptionalProperty(materialEditor, properties, "_UseNonUniformScale", Styles.useNonUniformScaleLabel);
                
                var keepConstantScalingProp = FindProperty("_KeepConstantScaling", properties, false);
                if (keepConstantScalingProp != null)
                {
                    materialEditor.ShaderProperty(keepConstantScalingProp, Styles.keepConstantScalingLabel);
                    EditorGUI.BeginDisabledGroup(keepConstantScalingProp.floatValue == 0.0f);
                    materialEditor.ShaderProperty(FindProperty("_ConstantScale", properties), Styles.constantScaleLabel);
                    EditorGUI.EndDisabledGroup();    
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        protected void StencilOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (ShaderGuiUtility.FoldoutHeader(Styles.stencilFoldoutLabel, ref showStencilOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                materialEditor.ShaderProperty(FindProperty("_StencilRef", properties), Styles.stencilReferenceLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilReadMask", properties), Styles.stencilReadMaskLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilWriteMask", properties), Styles.stencilWriteMaskLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilComp", properties), Styles.stencilComparisonLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilPass", properties), Styles.stencilPassLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilFail", properties), Styles.stencilFailLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilZFail", properties), Styles.stencilZFailLabel);
                
                EditorGUILayout.EndVertical();
            }
        }
    }
}
