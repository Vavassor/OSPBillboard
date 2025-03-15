using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OrchidSeal.Billboard.Editor
{
    public class FlipbookCreatorEditor : EditorWindow
    {
        private FilterMode filterMode = FilterMode.Bilinear;
        private string[] gifPaths;
        private bool isOutputFolderFoldoutOpen;
        private string outputFolder;
        private SerializedObject serializedObject;
        private ResizeMode resizeMode = ResizeMode.Stretch;
        [SerializeField] private Texture[] sourceTextures = new Texture[1];
        private SerializedProperty sourceTexturesProp;
        private TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        private static class Styles
        {
            public static GUIStyle sectionVerticalLayout = new()
            {
                margin = new RectOffset(0, 0, 0, 12),
            };
            
            public static readonly GUIContent chooseOutputFolderButtonLabel = new ("Choose Folder");
            public const string chooseOutputFolderMenuTitle = "Choose Folder";
            public static readonly GUIContent createTextureArraysButtonLabel = new ("Create Texture Arrays");
            public static readonly GUIContent deselectOutputFolderButtonLabel = new ("Remove");
            public static readonly GUIContent filterModeFieldLabel = new ("Filter Mode");
            public const string filterModePointTip = "Pixel art may look better using the \"Sharp pixels\" option in the Unlit Billboard shader and setting this filter mode to Bilinear/Trilinear.";
            public static readonly GUIContent outputFolderUnspecifiedMessage = new ("Files are output to the same folder as the source textures if no folder is specified.");
            public static readonly GUIContent outputFolderFoldoutHeader = new ("Output Folder");
            public static readonly GUIContent pixelArtPresetButtonLabel = new ("Pixel Art");
            public static readonly GUIContent presetHeading = new ("Presets");
            public static readonly GUIContent  resizeModeFieldLabel = new ("Resize Mode");
            public const string resizeModeNonePerformanceWarning = "Resize Mode \"None\" may make larger file sizes! Compression usually requires certain image dimensions. Use sides divisible by 4, or powers of two if possible.";
            public static readonly GUIContent sourceTexturesFieldLabel = new ("Source Textures");
            public static readonly GUIContent videoPresetButtonLabel = new ("Video");
            public static readonly GUIContent windowTitle = new ("Create Flipbook");
            public static readonly GUIContent wrapModeFieldLabel = new ("Wrap Mode");
        }
        
        [MenuItem ("Window/Orchid Seal/Billboard - Create Flipbook")]
        public static void ShowWindow()
        {
            GetWindow(typeof(FlipbookCreatorEditor));
        }

        private void OnEnable()
        {
            titleContent = Styles.windowTitle;
        }

        private void CreateGUI()
        {
            serializedObject = new SerializedObject(this);
            sourceTexturesProp = serializedObject.FindProperty(nameof(sourceTextures));
        }

        private static string GetPathRelativeToAssets(string fullPath)
        {
            if (fullPath == null) return null;
            var relativePath = Path.GetRelativePath(Path.Combine(Application.dataPath, ".."), fullPath);
            return relativePath.Equals(fullPath) ? null : relativePath;
        }

        private void OnGUI()
        {
            EditorGUILayout.PropertyField(sourceTexturesProp, Styles.sourceTexturesFieldLabel);
            
            EditorGUILayout.BeginVertical(Styles.sectionVerticalLayout);
            GUILayout.Label(Styles.presetHeading, EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(Styles.pixelArtPresetButtonLabel))
            {
                filterMode = FilterMode.Bilinear;
                resizeMode = ResizeMode.Center;
            }
            if (GUILayout.Button(Styles.videoPresetButtonLabel))
            {
                filterMode = FilterMode.Bilinear;
                resizeMode = ResizeMode.Stretch;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical();
            filterMode = (FilterMode) EditorGUILayout.EnumPopup(Styles.filterModeFieldLabel, filterMode);
            if (filterMode == FilterMode.Point)
            {
                EditorGUILayout.HelpBox(Styles.filterModePointTip, MessageType.Info);
            }
            wrapMode = (TextureWrapMode) EditorGUILayout.EnumPopup(Styles.wrapModeFieldLabel, wrapMode);
            resizeMode = (ResizeMode) EditorGUILayout.EnumPopup(Styles.resizeModeFieldLabel, resizeMode);
            if (resizeMode == ResizeMode.None)
            {
                EditorGUILayout.HelpBox(Styles.resizeModeNonePerformanceWarning, MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();

            isOutputFolderFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup(isOutputFolderFoldoutOpen, Styles.outputFolderFoldoutHeader);
            if (isOutputFolderFoldoutOpen)
            {
                EditorGUILayout.LabelField(Styles.outputFolderUnspecifiedMessage, EditorStyles.wordWrappedLabel);
                if (GUILayout.Button(Styles.chooseOutputFolderButtonLabel, GUILayout.ExpandWidth(false)))
                {
                    var fullPath = EditorUtility.SaveFolderPanel(Styles.chooseOutputFolderMenuTitle, "Assets", "");
                    outputFolder = GetPathRelativeToAssets(fullPath);
                }
                EditorGUILayout.BeginHorizontal();
                if (!string.IsNullOrEmpty(outputFolder) && GUILayout.Button(Styles.deselectOutputFolderButtonLabel, GUILayout.ExpandWidth(false)))
                {
                    outputFolder = null;
                }
                EditorGUILayout.LabelField(outputFolder, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button(Styles.createTextureArraysButtonLabel))
            {
                if (sourceTextures != null)
                {
                    var paths = sourceTextures
                        .Where(texture => texture)
                        .Select(AssetDatabase.GetAssetPath)
                        .Where(path => Path.GetExtension(path).ToLowerInvariant() == ".gif")
                        .Distinct()
                        .ToArray();
                    if (paths.Length > 0)
                    {
                        var spec = new FlipbookSpec()
                        {
                            filterMode = filterMode,
                            resizeMode = resizeMode,
                            wrapMode = wrapMode,
                        };
                        FlipbookCreator.CreateTextureArraysFromGifPaths(paths, spec, outputFolder);
                    }
                }
            }
        }
    }
}
