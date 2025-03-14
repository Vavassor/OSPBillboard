using UnityEngine;
using UnityEditor;

namespace OrchidSeal.Billboard.Editor
{
    public class BillboardEditor : ShaderGUI
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
            public static readonly GUIContent renderModeLabel = new ("Render Mode", "Opaque:\nCannot be seen through.\n\nCutout:\nCut holes in geometry by discarding any pixel whose combined alpha is below a cutoff threshold.\n\nTransparent:\nSmoothly blended transparency that uses the combined alpha values of pixels.\n\nPremultiply:\nTransparency using colors that are premultiplied with the alpha value, which can improve the appearance of soft edges.\n\nAdditive:\nGlowing transparency. Add the numbers for the two colors together.\n\nCustom:\nControl all blending settings separately.");
            public static readonly GUIContent useAlphaTestLabel = new ("Use Alpha Test");
            public static readonly GUIContent alphaCutoffLabel = new ("Alpha Cutoff");
            public static readonly GUIContent alphaToMaskLabel = new ("Alpha To Mask");
            public static readonly GUIContent sourceBlendLabel = new ("Source Blend");
            public static readonly GUIContent destinationBlendLabel = new ("Destination Blend");
            public static readonly GUIContent blendOperationLabel = new ("Blend Operation");
            public static readonly GUIContent zTestLabel = new ("Depth Test");
            public static readonly GUIContent zWriteLabel = new ("Depth Write");
            public static readonly GUIContent useGammaSpaceLabel = new ("Use Gamma Space Blending", "Perform shader calculations in gamma space. Blending with the framebuffer will still follow the color space workflow in project settings.");
            
            // Base option labels
            public const string baseFoldoutLabel = "Base";
            public static readonly GUIContent baseTextureLabel = new ("Base Color");
            public static readonly GUIContent tintColorModeLabel = new ("Tint Color Mode");
            public static readonly GUIContent usePixelSharpenLabel = new("Sharp Pixels", "Blocky pixels with smooth edges. Set filtering on textures to Bilinear or Trilinear when using this.");
            
            // Flipbook option labels
            public const string flipbookFoldoutLabel = "Flipbook";
            public static readonly GUIContent flipbookLabel = new ("Flipbook");
            public static readonly GUIContent flipbookEditorButtonLabel = new ("Create Flipbooks");
            public static readonly GUIContent flipbookScrollVelocityLabel = new ("Scroll Velocity");
            public static readonly GUIContent flipbookBlendModeLabel = new ("Blend Mode");
            public static readonly GUIContent flipbookFramesPerSecondLabel = new ("Frames Per Second");
            public static readonly GUIContent useFlipbookSmoothingLabel = new ("Smoothing");
            public static readonly GUIContent flipbookUseManualFrameLabel = new ("Control Frame Manually");
            public static readonly GUIContent flipbookManualFrameLabel = new ("Manual Frame");
            
            // Transformation
            public const string transformationFoldoutLabel = "Transformation";
            public static readonly GUIContent positionLabel = new("Position");
            public static readonly GUIContent rotationRollLabel = new ("Rotation Roll");
            public static readonly GUIContent scaleLabel = new ("Scale");
            public static readonly GUIContent billboardModeLabel = new ("Billboard Mode");
            public static readonly GUIContent useNonUniformScaleLabel = new ("Use Non Uniform Object Scale");
            public static readonly GUIContent keepConstantScalingLabel = new ("Keep Constant Scaling");
            public static readonly GUIContent constantScaleLabel = new ("Constant Scale");
            
            // Distance Fade
            public const string distanceFadeFoldoutLabel = "Distance Fade";
            public static readonly GUIContent distanceFadeMinAlphaLabel = new ("Min Alpha");
            public static readonly GUIContent distanceFadeMaxAlphaLabel = new ("Max Alpha");
            public static readonly GUIContent distanceFadeMinLabel = new ("Min");
            public static readonly GUIContent distanceFadeMaxLabel = new ("Max");
            
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

        private bool showBlendingOptions = true;
        private bool showBaseOptions = true;
        private bool showFlipbookOptions = true;
        private bool showTransformationOptions = true;
        private bool showDistanceFadeOptions;
        private bool showStencilOptions;
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // Intentionally don't call the base class so that the normal settings aren't rendered.
            // base.OnGUI(materialEditor, properties);

            var targetMaterial = materialEditor.target as Material;

            BlendingOptions(materialEditor, properties, targetMaterial);
            BaseOptions(materialEditor, properties);
            TransformationOptions(materialEditor, properties);
            FlipbookOptions(materialEditor, properties);
            DistanceFadeOptions(materialEditor, properties);
            StencilOptions(materialEditor, properties);

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
                materialEditor.ShaderProperty(FindProperty("_UseGammaSpace", properties), Styles.useGammaSpaceLabel);
                materialEditor.ShaderProperty(FindProperty("_UsePixelSharpen", properties), Styles.usePixelSharpenLabel);

                EditorGUILayout.EndVertical();
            }
        }

        private void SetBlendMode(Material material)
        {
            RenderMode renderMode = (RenderMode) material.GetInt("_RenderMode");

            switch (renderMode)
            {
                case RenderMode.Opaque:
                {
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
                    material.SetFloat("_ZTest", 4.0f);
                    material.SetFloat("_ZWrite", 1.0f);
                    material.SetFloat("_UseAlphaTest", 0.0f);
                    material.DisableKeyword("USE_ALPHA_TEST");
                    material.SetFloat("_AlphaToMask", 0.0f);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    break;
                }

                case RenderMode.Cutout:
                {
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
                    material.SetFloat("_ZTest", 4.0f);
                    material.SetFloat("_ZWrite", 1.0f);
                    material.SetFloat("_UseAlphaTest", 1.0f);
                    material.EnableKeyword("USE_ALPHA_TEST");
                    material.SetFloat("_AlphaToMask", 1.0f);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    break;
                }

                case RenderMode.Transparent:
                {
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
                    material.SetFloat("_ZTest", 4.0f);
                    material.SetFloat("_ZWrite", 0.0f);
                    material.SetFloat("_UseAlphaTest", 0.0f);
                    material.DisableKeyword("USE_ALPHA_TEST");
                    material.SetFloat("_AlphaToMask", 0.0f);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
                }
                
                case RenderMode.Premultiply:
                {
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
                    material.SetFloat("_ZTest", 4.0f);
                    material.SetFloat("_ZWrite", 0.0f);
                    material.SetFloat("_UseAlphaTest", 0.0f);
                    material.DisableKeyword("USE_ALPHA_TEST");
                    material.SetFloat("_AlphaToMask", 0.0f);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
                }
                
                case RenderMode.Additive:
                {
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
                    material.SetFloat("_ZTest", 4.0f);
                    material.SetFloat("_ZWrite", 0.0f);
                    material.SetFloat("_UseAlphaTest", 0.0f);
                    material.DisableKeyword("USE_ALPHA_TEST");
                    material.SetFloat("_AlphaToMask", 0.0f);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
                }

                case RenderMode.Custom:
                {
                    material.SetOverrideTag("RenderType", "Opaque");
                    break;
                }
            }
        }
        
        // Base....................................................................................
        
        private void BaseOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (ShaderGuiUtility.FoldoutHeader(Styles.baseFoldoutLabel, ref showBaseOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                var baseTextureProperty = FindProperty("_MainTex", properties);
                materialEditor.TexturePropertySingleLine(Styles.baseTextureLabel, baseTextureProperty, FindProperty("_Color", properties));
                materialEditor.ShaderProperty(FindProperty("_ColorMode", properties), Styles.tintColorModeLabel);
                materialEditor.TextureScaleOffsetProperty(baseTextureProperty);
                EditorGUILayout.EndVertical();
            }
        }
        
        // Flipbook................................................................................
        
        private void FlipbookOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var isFlipbookEnabledProperty = FindProperty("_UseFlipbook", properties);
            
            if (ShaderGuiUtility.MaterialPropertyFoldout(Styles.flipbookFoldoutLabel, ref showFlipbookOptions, isFlipbookEnabledProperty))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                var flipbookProperty = FindProperty("_FlipbookTexArray", properties);
                var flipbookUseManualFrameProperty = FindProperty("_FlipbookUseManualFrame", properties);
                var flipbookManualFrameProperty = FindProperty("_FlipbookManualFrame", properties);

                EditorGUILayout.Space();
                if (GUILayout.Button(Styles.flipbookEditorButtonLabel, GUILayout.Width(120)))
                {
                    FlipbookCreatorEditor.ShowWindow();
                }

                EditorGUI.BeginDisabledGroup(isFlipbookEnabledProperty.floatValue == 0.0f);
                materialEditor.TexturePropertySingleLine(Styles.flipbookLabel, flipbookProperty, FindProperty("_FlipbookTint", properties));
                materialEditor.TextureScaleOffsetProperty(flipbookProperty);
                ShaderGuiUtility.Vector2Property(FindProperty("_FlipbookScrollVelocity", properties), Styles.flipbookScrollVelocityLabel);
                materialEditor.ShaderProperty(FindProperty("_FlipbookBlendMode", properties), Styles.flipbookBlendModeLabel);
                materialEditor.ShaderProperty(FindProperty("_FlipbookFramesPerSecond", properties), Styles.flipbookFramesPerSecondLabel);
                materialEditor.ShaderProperty(FindProperty("_UseFlipbookSmoothing", properties), Styles.useFlipbookSmoothingLabel);

                materialEditor.ShaderProperty(flipbookUseManualFrameProperty, Styles.flipbookUseManualFrameLabel);
                if (flipbookUseManualFrameProperty.floatValue > 0.0f)
                {
                    materialEditor.ShaderProperty(flipbookManualFrameProperty, Styles.flipbookManualFrameLabel);
                }

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndVertical();
            }
        }

        // Transformation..........................................................................

        private void TransformationOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (ShaderGuiUtility.FoldoutHeader(Styles.transformationFoldoutLabel, ref showTransformationOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                var keepConstantScalingProp = FindProperty("_KeepConstantScaling", properties);

                materialEditor.ShaderProperty(FindProperty("_Billboard_Mode", properties), Styles.billboardModeLabel);
                ShaderGuiUtility.Vector2Property(FindProperty("_Position", properties), Styles.positionLabel);
                materialEditor.ShaderProperty(FindProperty("_RotationRoll", properties), Styles.rotationRollLabel);
                ShaderGuiUtility.Vector2Property(FindProperty("_Scale", properties), Styles.scaleLabel);
                materialEditor.ShaderProperty(FindProperty("_UseNonUniformScale", properties), Styles.useNonUniformScaleLabel);
                materialEditor.ShaderProperty(keepConstantScalingProp, Styles.keepConstantScalingLabel);
                EditorGUI.BeginDisabledGroup(keepConstantScalingProp.floatValue == 0.0f);
                materialEditor.ShaderProperty(FindProperty("_ConstantScale", properties), Styles.constantScaleLabel);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
            }
        }

        // Distance Fade...........................................................................

        private void DistanceFadeOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var useDistanceFadeProp = FindProperty("_UseDistanceFade", properties);

            if (ShaderGuiUtility.MaterialPropertyFoldout(Styles.distanceFadeFoldoutLabel, ref showDistanceFadeOptions, useDistanceFadeProp))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                EditorGUI.BeginDisabledGroup(useDistanceFadeProp.floatValue == 0.0f);
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMinAlpha", properties), Styles.distanceFadeMinAlphaLabel);
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMaxAlpha", properties), Styles.distanceFadeMaxAlphaLabel);
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMin", properties), Styles.distanceFadeMinLabel);
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMax", properties), Styles.distanceFadeMaxLabel);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
            }
        }

        // Stencil.................................................................................

        private void StencilOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
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
