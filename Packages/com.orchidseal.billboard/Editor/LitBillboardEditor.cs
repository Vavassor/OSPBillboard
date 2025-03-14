using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrchidSeal.Billboard.Editor
{
    public class LitBillboardEditor : ShaderGUI
    {
        private enum RenderMode
        {
            Opaque,
            Cutout,
            Transparent,
            Premultiply,
            Additive,
            Custom
        }
        
        private static class Styles
        {
            public static GUIStyle sectionVerticalLayout = new()
            {
                margin = new RectOffset(0, 0, 0, 12),
            };
            
            // Blending option labels
            public const string blendingFoldoutLabel = "Blending";
            public static readonly GUIContent renderModeLabel = new("Render Mode", "Opaque:\nCannot be seen through.\n\nCutout:\nCut holes in geometry by discarding any pixel whose combined alpha is below a cutoff threshold.\n\nTransparent:\nSmoothly blended transparency that uses the combined alpha values of pixels.\n\nPremultiply:\nTransparency using colors that are premultiplied with the alpha value, which can improve the appearance of soft edges.\n\nAdditive:\nGlowing transparency. Add the numbers for the two colors together.\n\nCustom:\nControl all blending settings separately.");
            public static readonly GUIContent useAlphaTestLabel = new GUIContent("Use Alpha Test");
            public static readonly GUIContent alphaCutoffLabel = new GUIContent("Alpha Cutoff");
            public static readonly GUIContent alphaToMaskLabel = new GUIContent("Alpha To Mask");
            public static readonly GUIContent sourceBlendLabel = new GUIContent("Source Blend");
            public static readonly GUIContent destinationBlendLabel = new GUIContent("Destination Blend");
            public static readonly GUIContent blendOperationLabel = new GUIContent("Blend Operation");
            public static readonly GUIContent zTestLabel = new GUIContent("Depth Test");
            public static readonly GUIContent zWriteLabel = new GUIContent("Depth Write");
            
            // Base option labels
            public const string baseFoldoutLabel = "Base";
            public static readonly GUIContent baseTextureLabel = new("Base Color");
            public static readonly GUIContent normalMapLabel = new("Normal");
            public static readonly GUIContent glossinessLabel = new("Glossiness");
            public static readonly GUIContent metallicLabel = new("Metallic");
            
            // Emission option labels
            public const string emissionFoldoutLabel = "Emission";
            public static readonly GUIContent emissionMapLabel = new("Emission");
            
            // Transformation option labels
            public const string transformationFoldoutLabel = "Transformation";
            public static readonly GUIContent billboardModeLabel = new("Billboard Mode", "None:\nNo billboarding.\n\nSpherical:\nFace the camera from any direction.\n\nCylindrical World:\nFace the camera, turning around the world Y axis. Use to appear grounded or upright, like for trees or fire.\n\nCylindrical Local\nFace the camera, turning around the object Y axis.");
        }
        
        private static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
        private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

        private bool showBaseOptions = true;
        private bool showBlendingOptions = true;
        private bool showEmissionOptions;
        private bool showTransformationOptions = true;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var targetMaterial = materialEditor.target as Material;
            
            BlendingOptions(materialEditor, properties, targetMaterial);
            BaseOptions(materialEditor, properties, targetMaterial);
            EmissionOptions(materialEditor, properties, targetMaterial);
            TransformationOptions(materialEditor, properties);
            
            materialEditor.EnableInstancingField();
        }

        private void BlendingOptions(MaterialEditor materialEditor, MaterialProperty[] properties, Material targetMaterial)
        {
            if (ShaderGuiUtility.FoldoutHeader(Styles.blendingFoldoutLabel, ref showBlendingOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                var renderModeProperty = FindProperty("_RenderMode", properties);
                var useAlphaTestProperty = FindProperty("_UseAlphaTest", properties);

                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(renderModeProperty, Styles.renderModeLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    SetBlendMode(targetMaterial);
                }

                var blendMode = (RenderMode)renderModeProperty.floatValue;
                
                switch (blendMode)
                {
                    case RenderMode.Custom:
                        EditorGUILayout.Space(8);
                        materialEditor.ShaderProperty(FindProperty("_BlendSrc", properties), Styles.sourceBlendLabel);
                        materialEditor.ShaderProperty(FindProperty("_BlendDst", properties), Styles.destinationBlendLabel);
                        materialEditor.ShaderProperty(FindProperty("_BlendOp", properties), Styles.blendOperationLabel);
                        
                        EditorGUILayout.Space(8);
                        materialEditor.ShaderProperty(FindProperty("_ZTest", properties), Styles.zTestLabel);
                        materialEditor.ShaderProperty(FindProperty("_ZWrite", properties), Styles.zWriteLabel);
                        
                        EditorGUILayout.Space(8);
                        materialEditor.ShaderProperty(useAlphaTestProperty, Styles.useAlphaTestLabel);
                        EditorGUI.BeginDisabledGroup(useAlphaTestProperty.floatValue == 0.0f);
                        materialEditor.ShaderProperty(FindProperty("_AlphaCutoff", properties), Styles.alphaCutoffLabel);
                        materialEditor.ShaderProperty(FindProperty("_AlphaToMask", properties), Styles.alphaToMaskLabel);
                        EditorGUI.EndDisabledGroup();
                        break;
                    case RenderMode.Cutout:
                        EditorGUILayout.Space(8);
                        materialEditor.ShaderProperty(FindProperty("_AlphaCutoff", properties), Styles.alphaCutoffLabel);
                        materialEditor.ShaderProperty(FindProperty("_AlphaToMask", properties), Styles.alphaToMaskLabel);
                        break;
                }
                
                EditorGUILayout.Space(8);
                materialEditor.RenderQueueField();
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private static void SetBlendMode(Material material)
        {
            var renderMode = (RenderMode) material.GetInt("_RenderMode");

            switch (renderMode)
            {
                case RenderMode.Opaque:
                {
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.SetFloat("_BlendSrc", (float)BlendMode.One);
                    material.SetFloat("_BlendDst", (float)BlendMode.Zero);
                    material.SetFloat("_BlendOp", (float)BlendOp.Add);
                    material.SetFloat("_ZTest", 4.0f);
                    material.SetFloat("_ZWrite", 1.0f);
                    material.SetFloat("_UseAlphaTest", 0.0f);
                    material.DisableKeyword("USE_ALPHA_TEST");
                    material.SetFloat("_AlphaToMask", 0.0f);
                    material.DisableKeyword("CAST_TRANSPARENT_SHADOWS");
                    material.renderQueue = (int)RenderQueue.Geometry;
                    break;
                }

                case RenderMode.Cutout:
                {
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetFloat("_BlendSrc", (float)BlendMode.One);
                    material.SetFloat("_BlendDst", (float)BlendMode.Zero);
                    material.SetFloat("_BlendOp", (float)BlendOp.Add);
                    material.SetFloat("_ZTest", 4.0f);
                    material.SetFloat("_ZWrite", 1.0f);
                    material.SetFloat("_UseAlphaTest", 1.0f);
                    material.EnableKeyword("USE_ALPHA_TEST");
                    material.SetFloat("_AlphaToMask", 1.0f);
                    material.DisableKeyword("CAST_TRANSPARENT_SHADOWS");
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    break;
                }

                case RenderMode.Transparent:
                {
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetFloat("_BlendSrc", (float)BlendMode.SrcAlpha);
                    material.SetFloat("_BlendDst", (float)BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_BlendOp", (float)BlendOp.Add);
                    material.SetFloat("_ZTest", 4.0f);
                    material.SetFloat("_ZWrite", 0.0f);
                    material.SetFloat("_UseAlphaTest", 0.0f);
                    material.DisableKeyword("USE_ALPHA_TEST");
                    material.SetFloat("_AlphaToMask", 0.0f);
                    material.EnableKeyword("CAST_TRANSPARENT_SHADOWS");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
                }
                
                case RenderMode.Premultiply:
                {
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetFloat("_BlendSrc", (float)BlendMode.One);
                    material.SetFloat("_BlendDst", (float)BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_BlendOp", (float)BlendOp.Add);
                    material.SetFloat("_ZTest", 4.0f);
                    material.SetFloat("_ZWrite", 0.0f);
                    material.SetFloat("_UseAlphaTest", 0.0f);
                    material.DisableKeyword("USE_ALPHA_TEST");
                    material.SetFloat("_AlphaToMask", 0.0f);
                    material.EnableKeyword("CAST_TRANSPARENT_SHADOWS");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
                }
                
                case RenderMode.Additive:
                {
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetFloat("_BlendSrc", (float)BlendMode.One);
                    material.SetFloat("_BlendDst", (float)BlendMode.One);
                    material.SetFloat("_BlendOp", (float)BlendOp.Add);
                    material.SetFloat("_ZTest", 4.0f);
                    material.SetFloat("_ZWrite", 0.0f);
                    material.SetFloat("_UseAlphaTest", 0.0f);
                    material.DisableKeyword("USE_ALPHA_TEST");
                    material.SetFloat("_AlphaToMask", 0.0f);
                    material.DisableKeyword("CAST_TRANSPARENT_SHADOWS");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
                }

                case RenderMode.Custom:
                {
                    material.SetOverrideTag("RenderType", "Opaque");
                    break;
                }
            }
        }

        private static void SetKeyword(Material material, string keyword, bool isOn)
        {
            if (isOn)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        private void BaseOptions(MaterialEditor materialEditor, MaterialProperty[] properties, Material targetMaterial)
        {
            if (ShaderGuiUtility.FoldoutHeader(Styles.baseFoldoutLabel, ref showBaseOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                var baseTextureProperty = FindProperty("_MainTex", properties);
                materialEditor.TexturePropertySingleLine(Styles.baseTextureLabel, baseTextureProperty, FindProperty("_Color", properties));
                materialEditor.ShaderProperty(FindProperty("_Glossiness", properties), Styles.glossinessLabel);
                materialEditor.ShaderProperty(FindProperty("_Metallic", properties), Styles.metallicLabel);
                
                EditorGUI.BeginChangeCheck();
                materialEditor.TexturePropertySingleLine(Styles.normalMapLabel, FindProperty("_BumpMap", properties), FindProperty("_BumpScale", properties));
                if (EditorGUI.EndChangeCheck())
                {
                    SetKeyword(targetMaterial, "USE_NORMAL_MAP", targetMaterial.GetTexture(BumpMap));
                }
                
                materialEditor.TextureScaleOffsetProperty(baseTextureProperty);
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void EmissionOptions(MaterialEditor materialEditor, MaterialProperty[] properties, Material targetMaterial)
        {
            if (ShaderGuiUtility.FoldoutHeader(Styles.emissionFoldoutLabel, ref showEmissionOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                EditorGUI.BeginChangeCheck();
                materialEditor.TexturePropertySingleLine(Styles.emissionMapLabel, FindProperty("_EmissionMap", properties), FindProperty("_EmissionColor", properties));
                if (EditorGUI.EndChangeCheck())
                {
                    SetKeyword(targetMaterial, "USE_EMISSION_MAP", targetMaterial.GetTexture(EmissionMap));
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void TransformationOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (ShaderGuiUtility.FoldoutHeader(Styles.transformationFoldoutLabel, ref showTransformationOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                materialEditor.ShaderProperty(FindProperty("_Billboard_Mode", properties), Styles.billboardModeLabel);
                // ShaderGuiUtility.Vector2Property(FindProperty("_Position", properties), positionLabel);
                // materialEditor.ShaderProperty(FindProperty("_RotationRoll", properties), rotationRollLabel);
                // ShaderGuiUtility.Vector2Property(FindProperty("_Scale", properties), scaleLabel);
                
                EditorGUILayout.EndVertical();
            }
        }
    }
}
