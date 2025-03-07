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
            public static GUIStyle sectionVerticalLayout = new(EditorStyles.helpBox)
            {
                margin = new RectOffset(0, 0, 0, 20),
                padding = new RectOffset(8, 8, 8, 8),
            };
        }

        private bool showBlendingOptions = true;
        private bool showBaseOptions = true;
        private bool showFlipbookOptions = true;

        // Blending option labels
        private const string blendingFoldoutLabel = "Blending";
        private readonly GUIContent renderModeLabel = new GUIContent("Render Mode", "Opaque:\nCannot be seen through.\n\nCutout:\nCut holes in geometry by discarding any pixel whose combined alpha is below a cutoff threshold.\n\nTransparent:\nSmoothly blended transparency that uses the combined alpha values of pixels.\n\nPremultiply:\nTransparency using colors that are premultiplied with the alpha value, which can improve the appearance of soft edges.\n\nAdditive:\nGlowing transparency. Add the numbers for the two colors together.\n\nCustom:\nControl all blending settings separately.");
        private readonly GUIContent useAlphaTestLabel = new GUIContent("Use Alpha Test");
        private readonly GUIContent alphaCutoffLabel = new GUIContent("Alpha Cutoff");
        private readonly GUIContent alphaToMaskLabel = new GUIContent("Alpha To Mask");
        private readonly GUIContent sourceBlendLabel = new GUIContent("Source Blend");
        private readonly GUIContent destinationBlendLabel = new GUIContent("Destination Blend");
        private readonly GUIContent blendOperationLabel = new GUIContent("Blend Operation");
        private readonly GUIContent zTestLabel = new GUIContent("Depth Test");
        private readonly GUIContent zWriteLabel = new GUIContent("Depth Write");
        private readonly GUIContent useGammaSpaceLabel = new GUIContent("Use Gamma Space Blending", "Perform shader calculations in gamma space. Blending with the framebuffer will still follow the color space workflow in project settings.");

        // Base option labels
        private const string baseFoldoutLabel = "Base";
        private readonly GUIContent baseTextureLabel = new GUIContent("Base Color");
        private readonly GUIContent tintColorModeLabel = new GUIContent("Tint Color Mode");

        // Flipbook option labels
        private const string flipbookFoldoutLabel = "Flipbook";
        private readonly GUIContent isFlipbookEnabledLabel = new GUIContent("Enabled");
        private readonly GUIContent flipbookLabel = new GUIContent("Flipbook");
        private readonly GUIContent flipbookScrollVelocityLabel = new GUIContent("Scroll Velocity");
        private readonly GUIContent flipbookBlendModeLabel = new GUIContent("Blend Mode");
        private readonly GUIContent flipbookFramesPerSecondLabel = new GUIContent("Frames Per Second");
        private readonly GUIContent useFlipbookSmoothingLabel = new GUIContent("Smoothing");
        private readonly GUIContent flipbookUseManualFrameLabel = new GUIContent("Control Frame Manually");
        private readonly GUIContent flipbookManualFrameLabel = new GUIContent("Manual Frame");

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
            showBlendingOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showBlendingOptions, blendingFoldoutLabel);

            if (showBlendingOptions)
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                var renderModeProperty = FindProperty("_RenderMode", properties);
                var useAlphaTestProperty = FindProperty("_UseAlphaTest", properties);

                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(renderModeProperty, renderModeLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    SetBlendMode(targetMaterial);
                }

                var blendMode = (RenderMode)renderModeProperty.floatValue;
                
                switch (blendMode)
                {
                    case RenderMode.Custom:
                        EditorGUILayout.Space(8);
                        materialEditor.ShaderProperty(FindProperty("_BlendSrc", properties), sourceBlendLabel);
                        materialEditor.ShaderProperty(FindProperty("_BlendDst", properties), destinationBlendLabel);
                        materialEditor.ShaderProperty(FindProperty("_BlendOp", properties), blendOperationLabel);
                        
                        EditorGUILayout.Space(8);
                        materialEditor.ShaderProperty(FindProperty("_ZTest", properties), zTestLabel);
                        materialEditor.ShaderProperty(FindProperty("_ZWrite", properties), zWriteLabel);
                        
                        EditorGUILayout.Space(8);
                        materialEditor.ShaderProperty(useAlphaTestProperty, useAlphaTestLabel);
                        EditorGUI.BeginDisabledGroup(useAlphaTestProperty.floatValue == 0.0f);
                        materialEditor.ShaderProperty(FindProperty("_AlphaCutoff", properties), alphaCutoffLabel);
                        materialEditor.ShaderProperty(FindProperty("_AlphaToMask", properties), alphaToMaskLabel);
                        EditorGUI.EndDisabledGroup();
                        break;
                    case RenderMode.Cutout:
                        EditorGUILayout.Space(8);
                        materialEditor.ShaderProperty(FindProperty("_AlphaCutoff", properties), alphaCutoffLabel);
                        materialEditor.ShaderProperty(FindProperty("_AlphaToMask", properties), alphaToMaskLabel);
                        break;
                }

                EditorGUILayout.Space(8);
                materialEditor.RenderQueueField();
                materialEditor.ShaderProperty(FindProperty("_UseGammaSpace", properties), useGammaSpaceLabel);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void BaseOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            showBaseOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showBaseOptions, baseFoldoutLabel);

            if (showBaseOptions)
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                var baseTextureProperty = FindProperty("_MainTex", properties);
                materialEditor.TexturePropertySingleLine(baseTextureLabel, baseTextureProperty, FindProperty("_Color", properties));
                materialEditor.ShaderProperty(FindProperty("_ColorMode", properties), tintColorModeLabel);
                materialEditor.TextureScaleOffsetProperty(baseTextureProperty);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void FlipbookOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            showFlipbookOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showFlipbookOptions, flipbookFoldoutLabel);

            if (showFlipbookOptions)
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                var isFlipbookEnabledProperty = FindProperty("_UseFlipbook", properties);
                var flipbookProperty = FindProperty("_FlipbookTexArray", properties);
                var flipbookUseManualFrameProperty = FindProperty("_FlipbookUseManualFrame", properties);
                var flipbookManualFrameProperty = FindProperty("_FlipbookManualFrame", properties);

                materialEditor.ShaderProperty(isFlipbookEnabledProperty, isFlipbookEnabledLabel);
                
                EditorGUILayout.Space();
                if (GUILayout.Button("Create Flipbooks", GUILayout.Width(120)))
                {
                    FlipbookCreatorEditor.ShowWindow();
                }

                EditorGUI.BeginDisabledGroup(isFlipbookEnabledProperty.floatValue == 0.0f);
                materialEditor.TexturePropertySingleLine(flipbookLabel, flipbookProperty, FindProperty("_FlipbookTint", properties));
                materialEditor.TextureScaleOffsetProperty(flipbookProperty);
                ShaderGuiUtility.Vector2Property(FindProperty("_FlipbookScrollVelocity", properties), flipbookScrollVelocityLabel);
                materialEditor.ShaderProperty(FindProperty("_FlipbookBlendMode", properties), flipbookBlendModeLabel);
                materialEditor.ShaderProperty(FindProperty("_FlipbookFramesPerSecond", properties), flipbookFramesPerSecondLabel);
                materialEditor.ShaderProperty(FindProperty("_UseFlipbookSmoothing", properties), useFlipbookSmoothingLabel);

                materialEditor.ShaderProperty(flipbookUseManualFrameProperty, flipbookUseManualFrameLabel);
                if (flipbookUseManualFrameProperty.floatValue > 0.0f)
                {
                    materialEditor.ShaderProperty(flipbookManualFrameProperty, flipbookManualFrameLabel);
                }

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
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

        // Transformation..........................................................................

        private bool showTransformationOptions = true;
        private const string transformationFoldoutLabel = "Transformation";
        private readonly GUIContent positionLabel = new("Position");
        private readonly GUIContent rotationRollLabel = new GUIContent("Rotation Roll");
        private readonly GUIContent scaleLabel = new GUIContent("Scale");
        private readonly GUIContent billboardModeLabel = new GUIContent("Billboard Mode");
        private readonly GUIContent useNonUniformScaleLabel = new GUIContent("Use Non Uniform Object Scale");
        private readonly GUIContent keepConstantScalingLabel = new GUIContent("Keep Constant Scaling");
        private readonly GUIContent constantScaleLabel = new GUIContent("Constant Scale");

        private void TransformationOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            showTransformationOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showTransformationOptions, transformationFoldoutLabel);

            if (showTransformationOptions)
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                var keepConstantScalingProp = FindProperty("_KeepConstantScaling", properties);

                materialEditor.ShaderProperty(FindProperty("_Billboard_Mode", properties), billboardModeLabel);
                ShaderGuiUtility.Vector2Property(FindProperty("_Position", properties), positionLabel);
                materialEditor.ShaderProperty(FindProperty("_RotationRoll", properties), rotationRollLabel);
                ShaderGuiUtility.Vector2Property(FindProperty("_Scale", properties), scaleLabel);
                materialEditor.ShaderProperty(FindProperty("_UseNonUniformScale", properties), useNonUniformScaleLabel);
                materialEditor.ShaderProperty(keepConstantScalingProp, keepConstantScalingLabel);
                EditorGUI.BeginDisabledGroup(keepConstantScalingProp.floatValue == 0.0f);
                materialEditor.ShaderProperty(FindProperty("_ConstantScale", properties), constantScaleLabel);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // Distance Fade...........................................................................

        private bool showDistanceFadeOptions;
        private readonly GUIContent distanceFadeFoldoutLabel = new GUIContent("Distance Fade");
        private readonly GUIContent distanceFadeEnabledLabel = new GUIContent("Enabled");
        private readonly GUIContent distanceFadeMinAlphaLabel = new GUIContent("Min Alpha");
        private readonly GUIContent distanceFadeMaxAlphaLabel = new GUIContent("Max Alpha");
        private readonly GUIContent distanceFadeMinLabel = new GUIContent("Min");
        private readonly GUIContent distanceFadeMaxLabel = new GUIContent("Max");

        private void DistanceFadeOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            showDistanceFadeOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showDistanceFadeOptions, distanceFadeFoldoutLabel);

            if (showDistanceFadeOptions)
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                var useDistanceFadeProp = FindProperty("_UseDistanceFade", properties);
                materialEditor.ShaderProperty(useDistanceFadeProp, distanceFadeEnabledLabel);
                EditorGUI.BeginDisabledGroup(useDistanceFadeProp.floatValue == 0.0f);
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMinAlpha", properties), distanceFadeMinAlphaLabel);
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMaxAlpha", properties), distanceFadeMaxAlphaLabel);
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMin", properties), distanceFadeMinLabel);
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMax", properties), distanceFadeMaxLabel);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // Stencil.................................................................................

        private bool showStencilOptions;
        private readonly GUIContent stencilFoldoutLabel = new GUIContent("Stencil");
        private readonly GUIContent stencilReferenceLabel = new GUIContent("Reference");
        private readonly GUIContent stencilReadMaskLabel = new GUIContent("Read Mask");
        private readonly GUIContent stencilWriteMaskLabel = new GUIContent("Write Mask");
        private readonly GUIContent stencilComparisonLabel = new GUIContent("Comparison");
        private readonly GUIContent stencilPassLabel = new GUIContent("Pass");
        private readonly GUIContent stencilFailLabel = new GUIContent("Fail");
        private readonly GUIContent stencilZFailLabel = new GUIContent("ZFail");

        private void StencilOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            showStencilOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showStencilOptions, stencilFoldoutLabel);

            if (showStencilOptions)
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                materialEditor.ShaderProperty(FindProperty("_StencilRef", properties), stencilReferenceLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilReadMask", properties), stencilReadMaskLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilWriteMask", properties), stencilWriteMaskLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilComp", properties), stencilComparisonLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilPass", properties), stencilPassLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilFail", properties), stencilFailLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilZFail", properties), stencilZFailLabel);
                
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
