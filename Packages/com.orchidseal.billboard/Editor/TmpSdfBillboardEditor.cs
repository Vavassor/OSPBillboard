using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrchidSeal.Billboard.Editor
{
    public class TmpSdfBillboardEditor : ShaderGUI
    {
        private static class Styles
        {
            public static GUIStyle sectionVerticalLayout = new()
            {
                margin = new RectOffset(0, 0, 0, 12),
            };

            public static GUIStyle sectionHeading = new(EditorStyles.boldLabel);
        }
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var targetMaterial = materialEditor.target as Material;
            
            EditorGUI.BeginChangeCheck();

            FaceOptions(materialEditor, properties);
            BillboardOptions(materialEditor, properties, targetMaterial);
            OutlineOptions(materialEditor, properties);
            UnderlayOptions(materialEditor, properties, targetMaterial);
            LightingOptions(materialEditor, properties, targetMaterial);
            GlowOptions(materialEditor, properties, targetMaterial);
            DistanceFadeOptions(materialEditor, properties, targetMaterial);
            SilhouetteOptions(materialEditor, properties, targetMaterial);
            DebugOptions(materialEditor, properties, targetMaterial);
            
            // TextMeshPro modifies actual vertices depending on the outline thickness and other properties!
            // So we have to notify the text mesh pro component about changes.
            if (EditorGUI.EndChangeCheck())
            {
                TMPro.TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, targetMaterial);
            }
        }
        
        private static bool MaterialKeywordFoldout(string title, ref bool isCollapsed, Material material, string[] keywords, string defaultKeyword)
        {
            var validKeywords = keywords.Where(keyword => !string.IsNullOrEmpty(keyword)).ToArray();
            var shaderKeywords = material.shaderKeywords;
            var isAnyEnabled = validKeywords.Any(keyword => shaderKeywords.Contains(keyword));
            EditorGUI.BeginChangeCheck();
            ShaderGuiUtility.FoldoutHeader(title, ref isCollapsed, ref isAnyEnabled);
            if (!EditorGUI.EndChangeCheck()) return isCollapsed;
            foreach (var t in validKeywords)
            {
                material.SetKeyword(new LocalKeyword(material.shader, t), isAnyEnabled && t == defaultKeyword);
            }
            EditorUtility.SetDirty(material);
            return isCollapsed;
        }
        
        private static bool KeywordToggle(Material material, string keyword, GUIContent label, bool isReversed = false)
        {
            var isToggleOn = material.IsKeywordEnabled(keyword);
            if (isReversed) isToggleOn = !isToggleOn;
            EditorGUI.BeginChangeCheck();
            isToggleOn = EditorGUILayout.Toggle(label, isToggleOn);
            if (!EditorGUI.EndChangeCheck()) return isToggleOn;
            var localKeyword = new LocalKeyword(material.shader, keyword);
            if (!localKeyword.isValid) return isToggleOn;
            material.SetKeyword(localKeyword, isToggleOn);
            EditorUtility.SetDirty(material);
            return isToggleOn;
        }

        private static void EnumKeywordProperty(Material material, GUIContent label, string[] keywords, GUIContent[] displayedOptions)
        {
            var shaderKeywords = material.shaderKeywords;
            var type = keywords.Select((keyword, index) => (keyword, index))
                .FirstOrDefault(x => shaderKeywords.Contains(x.keyword)).index;
            EditorGUI.BeginChangeCheck();
            type = EditorGUILayout.Popup(label, type, displayedOptions);
            if (!EditorGUI.EndChangeCheck()) return;
            for (var i = 0; i < keywords.Length; i++)
            {
                if (string.IsNullOrEmpty(keywords[i])) continue;
                material.SetKeyword(new LocalKeyword(material.shader, keywords[i]), i == type);
            }
            EditorUtility.SetDirty(material);
        }

        // Face Options............................................................................

        private bool showFaceOptions = true;
        private const string faceFoldoutLabel = "Face";
        private readonly GUIContent faceColorLabel = new GUIContent("Color");
        private readonly GUIContent faceSoftnessLabel = new GUIContent("Softness");
        private readonly GUIContent faceDilationLabel = new GUIContent("Dilation");

        private void FaceOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (ShaderGuiUtility.FoldoutHeader(faceFoldoutLabel, ref showFaceOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                materialEditor.ShaderProperty(FindProperty("_FaceColor", properties), faceColorLabel);
                ShaderGuiUtility.OptionalProperty(materialEditor, properties, "_FaceSoftness", faceSoftnessLabel);
                materialEditor.ShaderProperty(FindProperty("_FaceDilate", properties), faceDilationLabel);
                
                EditorGUILayout.EndVertical();
            }
        }

        // Outline Options.........................................................................

        private bool showOutlineOptions = true;
        private const string outlineFoldoutLabel = "Outline";
        private readonly GUIContent outlineTextureLabel = new GUIContent("Texture");
        private readonly GUIContent outlineUvSpeedXLabel = new GUIContent("UV Speed X");
        private readonly GUIContent outlineUvSpeedYLabel = new GUIContent("UV Speed Y");
        private readonly GUIContent outlineColorLabel = new GUIContent("Color");
        private readonly GUIContent outlineWidthLabel = new GUIContent("Thickness");

        private void OutlineOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (ShaderGuiUtility.FoldoutHeader(outlineFoldoutLabel, ref showOutlineOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                materialEditor.ShaderProperty(FindProperty("_OutlineTex", properties), outlineTextureLabel);
                materialEditor.ShaderProperty(FindProperty("_OutlineUVSpeedX", properties), outlineUvSpeedXLabel);
                materialEditor.ShaderProperty(FindProperty("_OutlineUVSpeedY", properties), outlineUvSpeedYLabel);
                materialEditor.ShaderProperty(FindProperty("_OutlineColor", properties), outlineColorLabel);
                materialEditor.ShaderProperty(FindProperty("_OutlineWidth", properties), outlineWidthLabel);
                
                EditorGUILayout.EndVertical();
            }
        }

        // Underlay Options........................................................................

        private bool showUnderlayOptions;
        private const string underlayFoldoutLabel = "Underlay";
        private readonly GUIContent underlayTypeLabel = new GUIContent("Type");
        private readonly GUIContent underlayColorLabel = new GUIContent("Color");
        private readonly GUIContent underlayOffsetXLabel = new GUIContent("Offset X");
        private readonly GUIContent underlayOffsetYLabel = new GUIContent("Offset Y");
        private readonly GUIContent underlayDilationLabel = new GUIContent("Dilation");
        private readonly GUIContent underlaySoftnessLabel = new GUIContent("Softness");

        private readonly string[] underlayTypeKeywords = { null, "UNDERLAY_ON", "UNDERLAY_INNER" };
        private readonly GUIContent[] underlayTypeLabels = {
            new GUIContent("None"),
            new GUIContent("Normal"),
            new GUIContent("Inner"),
        };

        private void UnderlayOptions(MaterialEditor materialEditor, MaterialProperty[] properties, Material material)
        {
            if (MaterialKeywordFoldout(underlayFoldoutLabel, ref showUnderlayOptions, material, underlayTypeKeywords, underlayTypeKeywords[1]))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                EditorGUI.BeginDisabledGroup(!(material.IsKeywordEnabled("UNDERLAY_ON") || material.IsKeywordEnabled("UNDERLAY_INNER")));
                EnumKeywordProperty(material, underlayTypeLabel, underlayTypeKeywords, underlayTypeLabels);
                materialEditor.ShaderProperty(FindProperty("_UnderlayColor", properties), underlayColorLabel);
                materialEditor.ShaderProperty(FindProperty("_UnderlayOffsetX", properties), underlayOffsetXLabel);
                materialEditor.ShaderProperty(FindProperty("_UnderlayOffsetY", properties), underlayOffsetYLabel);
                materialEditor.ShaderProperty(FindProperty("_UnderlayDilate", properties), underlayDilationLabel);
                materialEditor.ShaderProperty(FindProperty("_UnderlaySoftness", properties), underlaySoftnessLabel);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
            }
        }

        // Lighting Options........................................................................

        private bool showLightingOptions;
        private const string lightingFoldoutLabel = "Lighting";

        private void LightingOptions(MaterialEditor materialEditor, MaterialProperty[] properties, Material material)
        {
            var bevelOnKeyword = new LocalKeyword(material.shader, "BEVEL_ON");
            
            if (ShaderGuiUtility.MaterialKeywordFoldout(lightingFoldoutLabel, ref showLightingOptions, material, bevelOnKeyword))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(10, false);
                EditorGUILayout.BeginVertical();
                var isDisabled = !material.IsKeywordEnabled(bevelOnKeyword);
                BevelOptions(materialEditor, properties, isDisabled);
                LocalLightingOptions(materialEditor, properties, isDisabled);
                BumpMapOptions(materialEditor, properties, isDisabled);
                ReflectionOptions(materialEditor, properties, isDisabled);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
        }

        // Bevel Options...........................................................................
        
        private readonly GUIContent bevelSectionHeading = new GUIContent("Bevel");
        private readonly GUIContent bevelTypeLabel = new GUIContent("Type");
        private readonly GUIContent bevelAmountLabel = new GUIContent("Amount");
        private readonly GUIContent bevelOffsetLabel = new GUIContent("Offset");
        private readonly GUIContent bevelWidthLabel = new GUIContent("Width");
        private readonly GUIContent bevelClampLabel = new GUIContent("Clamp");
        private readonly GUIContent bevelRoundnessLabel = new GUIContent("Roundness");

        private void BevelOptions(MaterialEditor materialEditor, MaterialProperty[] properties, bool isDisabled)
        {
            EditorGUILayout.LabelField(bevelSectionHeading, Styles.sectionHeading);

            EditorGUI.BeginDisabledGroup(isDisabled);
            materialEditor.ShaderProperty(FindProperty("_ShaderFlags", properties), bevelTypeLabel);
            materialEditor.ShaderProperty(FindProperty("_Bevel", properties), bevelAmountLabel);
            materialEditor.ShaderProperty(FindProperty("_BevelOffset", properties), bevelOffsetLabel);
            materialEditor.ShaderProperty(FindProperty("_BevelWidth", properties), bevelWidthLabel);
            materialEditor.ShaderProperty(FindProperty("_BevelClamp", properties), bevelClampLabel);
            materialEditor.ShaderProperty(FindProperty("_BevelRoundness", properties), bevelRoundnessLabel);
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(20);
        }

        // Local Lighting Options..................................................................
        
        private readonly GUIContent localLightingSectionHeading = new GUIContent("Local Lighting");
        private readonly GUIContent localLightingLightAngleLabel = new GUIContent("Light Angle");
        private readonly GUIContent localLightingSpecularColorLabel = new GUIContent("Specular");
        private readonly GUIContent localLightingSpecularPowerLabel = new GUIContent("Specular Power");
        private readonly GUIContent localLightingReflectivityLabel = new GUIContent("Reflectivity");
        private readonly GUIContent localLightingDiffuseLabel = new GUIContent("Diffuse");
        private readonly GUIContent localLightingAmbientLabel = new GUIContent("Ambient");

        private void LocalLightingOptions(MaterialEditor materialEditor, MaterialProperty[] properties, bool isDisabled)
        {
            EditorGUILayout.LabelField(localLightingSectionHeading, Styles.sectionHeading);
            
            EditorGUI.BeginDisabledGroup(isDisabled);
            materialEditor.ShaderProperty(FindProperty("_LightAngle", properties), localLightingLightAngleLabel);
            materialEditor.ShaderProperty(FindProperty("_SpecularColor", properties), localLightingSpecularColorLabel);
            materialEditor.ShaderProperty(FindProperty("_SpecularPower", properties), localLightingSpecularPowerLabel);
            materialEditor.ShaderProperty(FindProperty("_Reflectivity", properties), localLightingReflectivityLabel);
            materialEditor.ShaderProperty(FindProperty("_Diffuse", properties), localLightingDiffuseLabel);
            materialEditor.ShaderProperty(FindProperty("_Ambient", properties), localLightingAmbientLabel);
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(20);
        }

        // Bump Map Options........................................................................
        
        private readonly GUIContent bumpMapSectionHeading = new GUIContent("Normal Map");
        private readonly GUIContent bumpMapLabel = new GUIContent("Texture");
        private readonly GUIContent bumpMapFaceLabel = new GUIContent("Face");
        private readonly GUIContent bumpMapOutlineLabel = new GUIContent("Outline");

        private void BumpMapOptions(MaterialEditor materialEditor, MaterialProperty[] properties, bool isDisabled)
        {
            EditorGUILayout.LabelField(bumpMapSectionHeading, Styles.sectionHeading);
            
            EditorGUI.BeginDisabledGroup(isDisabled);
            materialEditor.TexturePropertySingleLine(bumpMapLabel, FindProperty("_BumpMap", properties));
            materialEditor.ShaderProperty(FindProperty("_BumpFace", properties), bumpMapFaceLabel);
            materialEditor.ShaderProperty(FindProperty("_BumpOutline", properties), bumpMapOutlineLabel);
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(20);
        }

        // Reflection Options......................................................................
        
        private readonly GUIContent reflectionSectionLabel = new GUIContent("Environment Map");
        private readonly GUIContent reflectionMapLabel = new GUIContent("Texture");
        private readonly GUIContent reflectionFaceLabel = new GUIContent("Face");
        private readonly GUIContent reflectionOutlineLabel = new GUIContent("Outline");
        private readonly GUIContent reflectionRotationLabel = new GUIContent("Rotation");

        private void ReflectionOptions(MaterialEditor materialEditor, MaterialProperty[] properties, bool isDisabled)
        {
            EditorGUILayout.LabelField(reflectionSectionLabel, Styles.sectionHeading);
            
            EditorGUI.BeginDisabledGroup(isDisabled);
            materialEditor.ShaderProperty(FindProperty("_ReflectFaceColor", properties), reflectionFaceLabel);
            materialEditor.ShaderProperty(FindProperty("_ReflectOutlineColor", properties), reflectionOutlineLabel);
            materialEditor.TexturePropertySingleLine(reflectionMapLabel, FindProperty("_Cube", properties));
            ShaderGuiUtility.Vector3Property(FindProperty("_EnvMatrixRotation", properties), reflectionRotationLabel);
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(20);
        }

        // Glow Options............................................................................

        private bool showGlowOptions;
        private const string glowFoldoutLabel = "Glow";
        private readonly GUIContent glowColorLabel = new GUIContent("Color");
        private readonly GUIContent glowOffsetLabel = new GUIContent("Offset");
        private readonly GUIContent glowInnerLabel = new GUIContent("Inner");
        private readonly GUIContent glowOuterLabel = new GUIContent("Outer");
        private readonly GUIContent glowPowerLabel = new GUIContent("Power");

        private void GlowOptions(MaterialEditor materialEditor, MaterialProperty[] properties, Material material)
        {
            var glowOnKeyword = new LocalKeyword(material.shader, "GLOW_ON");
            
            if (ShaderGuiUtility.MaterialKeywordFoldout(glowFoldoutLabel, ref showGlowOptions, material, glowOnKeyword))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                EditorGUI.BeginDisabledGroup(!material.IsKeywordEnabled(glowOnKeyword));
                materialEditor.ShaderProperty(FindProperty("_GlowColor", properties), glowColorLabel);
                materialEditor.ShaderProperty(FindProperty("_GlowOffset", properties), glowOffsetLabel);
                materialEditor.ShaderProperty(FindProperty("_GlowInner", properties), glowInnerLabel);
                materialEditor.ShaderProperty(FindProperty("_GlowOuter", properties), glowOuterLabel);
                materialEditor.ShaderProperty(FindProperty("_GlowPower", properties), glowPowerLabel);

                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
            }
        }

        // Billboard Options.......................................................................

        private bool showBillboardOptions = true;
        private const string billboardFoldoutLabel = "Billboard";
        private readonly GUIContent billboardModeLabel = new GUIContent("Mode", "None:\nNo billboarding.\n\nAuto:\n\"Vertical\" mode when in VR, and \"View\" mode otherwise.\n\nView:\nFace the camera and match its tilt.\n\nVertical:\nStays upright. Use for objects that appear grounded like trees or candle flame.");
        private readonly GUIContent keepConstantScaleLabel = new GUIContent("Keep Constant Scale", "Do not shrink when further away.");
        private readonly GUIContent constantScaleLabel = new GUIContent("Constant Scale");

        private void BillboardOptions(MaterialEditor materialEditor, MaterialProperty[] properties, Material material)
        {
            if (ShaderGuiUtility.FoldoutHeader(billboardFoldoutLabel, ref showBillboardOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                materialEditor.ShaderProperty(FindProperty("_Billboard_Mode", properties), billboardModeLabel);
                materialEditor.ShaderProperty(FindProperty("_KeepConstantScaling", properties), keepConstantScaleLabel);
                EditorGUI.BeginDisabledGroup(material.GetFloat("_KeepConstantScaling") == 0.0f);
                materialEditor.ShaderProperty(FindProperty("_ConstantScale", properties), constantScaleLabel);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
            }
        }

        // Distance Fade Options...................................................................

        private bool showDistanceFadeOptions;
        private const string distanceFadeFoldoutLabel = "Distance Fade";
        private readonly GUIContent distanceFadeMinAlphaLabel = new GUIContent("Min Alpha");
        private readonly GUIContent distanceFadeMaxAlphaLabel = new GUIContent("Max Alpha");
        private readonly GUIContent distanceFadeMinLabel = new GUIContent("Min");
        private readonly GUIContent distanceFadeMaxLabel = new GUIContent("Max");

        private void DistanceFadeOptions(MaterialEditor materialEditor, MaterialProperty[] properties, Material material)
        {
            var distanceFadeOnKeyword = new LocalKeyword(material.shader, "DISTANCE_FADE_ON");
            
            if (ShaderGuiUtility.MaterialKeywordFoldout(distanceFadeFoldoutLabel, ref showDistanceFadeOptions, material, distanceFadeOnKeyword))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                EditorGUI.BeginDisabledGroup(!material.IsKeywordEnabled(distanceFadeOnKeyword));
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMinAlpha", properties), distanceFadeMinAlphaLabel);
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMaxAlpha", properties), distanceFadeMaxAlphaLabel);
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMin", properties), distanceFadeMinLabel);
                materialEditor.ShaderProperty(FindProperty("_DistanceFadeMax", properties), distanceFadeMaxLabel);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
            }
        }

        // Silhouette Options......................................................................

        private bool showSilhouetteOptions;
        private const string silhouetteFoldoutLabel = "Silhouette";
        private readonly GUIContent silhouetteFadeAlphaLabel = new GUIContent("Fade Alpha");

        private void SilhouetteOptions(MaterialEditor materialEditor, MaterialProperty[] properties, Material material)
        {
            var silhouetteFadingOnKeyword = new LocalKeyword(material.shader, "SILHOUETTE_FADING_ON");
            var wasOn = material.IsKeywordEnabled(silhouetteFadingOnKeyword);
            
            if (ShaderGuiUtility.MaterialKeywordFoldout(silhouetteFoldoutLabel, ref showSilhouetteOptions, material, silhouetteFadingOnKeyword))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                var isOn = material.IsKeywordEnabled(silhouetteFadingOnKeyword);
                if (wasOn != isOn)
                {
                    material.SetShaderPassEnabled("ForwardBase", isOn);
                }

                EditorGUI.BeginDisabledGroup(!isOn);
                materialEditor.ShaderProperty(FindProperty("_SilhouetteFadeAlpha", properties), silhouetteFadeAlphaLabel);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
            }
        }

        // Debug Options...........................................................................

        private bool showDebugOptions;
        private const string debugFoldoutLabel = "Debug";
        private readonly GUIContent fontAtlasLabel = new GUIContent("Font Atlas");
        private readonly GUIContent textureWidthLabel = new GUIContent("Texture Width");
        private readonly GUIContent textureHeightLabel = new GUIContent("Texture Height");
        private readonly GUIContent gradientScaleLabel = new GUIContent("Gradient Scale");
        private readonly GUIContent scaleXLabel = new GUIContent("Scale X");
        private readonly GUIContent scaleYLabel = new GUIContent("Scale Y");
        private readonly GUIContent perspectiveFilterLabel = new GUIContent("Perspective Filter");
        private readonly GUIContent sharpnessLabel = new GUIContent("Sharpness");
        private readonly GUIContent offsetXLabel = new GUIContent("Offset X");
        private readonly GUIContent offsetYLabel = new GUIContent("Offset Y");
        private readonly string[] maskKeywords = { null, "MASK_HARD", "MASK_SOFT" };
        private readonly GUIContent[] maskValueLabels = {
            new GUIContent("Off"),
            new GUIContent("Hard"),
            new GUIContent("Soft"),
        };
        private readonly GUIContent maskLabel = new GUIContent("Mask");
        private readonly GUIContent clipRectLabel = new GUIContent("Clip Rect");
        private readonly string[] clipRectComponentLabels = { "L", "B", "R", "T" };
        private readonly GUIContent depthTestLabel = new GUIContent("Depth Test");
        private readonly GUIContent stencilIdLabel = new GUIContent("Stencil ID");
        private readonly GUIContent stencilComparisonLabel = new GUIContent("Stencil Comparison");
        private readonly GUIContent ratiosEnabledLabel = new GUIContent("Use Ratios");
        private readonly GUIContent cullModeLabel = new GUIContent("Cull Mode");
        private readonly GUIContent scaleRatioALabel = new GUIContent("Scale Ratio A");
        private readonly GUIContent scaleRatioBLabel = new GUIContent("Scale Ratio B");
        private readonly GUIContent scaleRatioCLabel = new GUIContent("Scale Ratio C");

        private void DebugOptions(MaterialEditor materialEditor, MaterialProperty[] properties, Material material)
        {
            if (ShaderGuiUtility.FoldoutHeader(debugFoldoutLabel, ref showDebugOptions))
            {
                EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
                
                materialEditor.ShaderProperty(FindProperty("_MainTex", properties), fontAtlasLabel);
                materialEditor.ShaderProperty(FindProperty("_TextureWidth", properties), textureWidthLabel);
                materialEditor.ShaderProperty(FindProperty("_TextureHeight", properties), textureHeightLabel);
                materialEditor.ShaderProperty(FindProperty("_GradientScale", properties), gradientScaleLabel);
                EditorGUILayout.Space();

                materialEditor.ShaderProperty(FindProperty("_ScaleX", properties), scaleXLabel);
                materialEditor.ShaderProperty(FindProperty("_ScaleY", properties), scaleYLabel);
                materialEditor.ShaderProperty(FindProperty("_PerspectiveFilter", properties), perspectiveFilterLabel);
                materialEditor.ShaderProperty(FindProperty("_Sharpness", properties), sharpnessLabel);
                EditorGUILayout.Space();

                materialEditor.ShaderProperty(FindProperty("_VertexOffsetX", properties), offsetXLabel);
                materialEditor.ShaderProperty(FindProperty("_VertexOffsetY", properties), offsetYLabel);
                if (material.HasProperty("_MaskCoord"))
                {
                    EnumKeywordProperty(material, maskLabel, maskKeywords, maskValueLabels);
                    EditorGUILayout.Space();
                }
                EditorGUIUtility.labelWidth = 150;
                ShaderGuiUtility.Vector4Property(FindProperty("_ClipRect", properties), clipRectLabel, clipRectComponentLabels);
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.Space();

                materialEditor.ShaderProperty(FindProperty("_ZTest", properties), depthTestLabel);
                materialEditor.ShaderProperty(FindProperty("_Stencil", properties), stencilIdLabel);
                materialEditor.ShaderProperty(FindProperty("_StencilComp", properties), stencilComparisonLabel);
                EditorGUILayout.Space();

                KeywordToggle(material, "RATIOS_OFF", ratiosEnabledLabel, true);
                EditorGUILayout.Space();

                materialEditor.ShaderProperty(FindProperty("_CullMode", properties), cullModeLabel);
                EditorGUILayout.Space();

                EditorGUI.BeginDisabledGroup(true);
                materialEditor.ShaderProperty(FindProperty("_ScaleRatioA", properties), scaleRatioALabel);
                materialEditor.ShaderProperty(FindProperty("_ScaleRatioB", properties), scaleRatioBLabel);
                materialEditor.ShaderProperty(FindProperty("_ScaleRatioC", properties), scaleRatioCLabel);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
            }
        }
    }
}
