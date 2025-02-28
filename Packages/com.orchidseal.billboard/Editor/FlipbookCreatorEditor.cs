using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OrchidSeal.Billboard.Editor
{
    public class FlipbookCreatorEditor : EditorWindow
    {
        private FilterMode filterMode = FilterMode.Trilinear;
        private string[] gifPaths;
        private bool isOutputFolderFoldoutOpen;
        private string outputFolder;
        private SerializedObject serializedObject;
        private ResizeMode resizeMode = ResizeMode.Stretch;
        [SerializeField] private Texture[] sourceTextures = new Texture[1];
        private SerializedProperty sourceTexturesProp;
        private TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        
        [MenuItem ("Window/Orchid Seal/Billboard - Create Flipbook")]
        public static void ShowWindow()
        {
            GetWindow(typeof(FlipbookCreatorEditor));
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Create Flipbook");
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
            EditorGUILayout.PropertyField(sourceTexturesProp, new GUIContent("Source Textures"));
            
            EditorGUILayout.BeginVertical();
            filterMode = (FilterMode) EditorGUILayout.EnumPopup("Filter Mode", filterMode);
            wrapMode = (TextureWrapMode) EditorGUILayout.EnumPopup("Wrap Mode", wrapMode);
            resizeMode = (ResizeMode) EditorGUILayout.EnumPopup("Resize Mode", resizeMode);
            if (resizeMode == ResizeMode.None)
            {
                EditorGUILayout.HelpBox("Resize Mode \"None\" may make larger file sizes! Compression usually requires certain image dimensions. Use sides divisible by 4, or powers of two if possible.", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();

            isOutputFolderFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup(isOutputFolderFoldoutOpen, "Output Folder");
            if (isOutputFolderFoldoutOpen)
            {
                EditorGUILayout.LabelField("Files are output to the same folder as the source textures if no folder is specified.", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Choose Folder", GUILayout.ExpandWidth(false)))
                {
                    var fullPath = EditorUtility.SaveFolderPanel("Choose Folder", "Assets", "");
                    outputFolder = GetPathRelativeToAssets(fullPath);
                }
                EditorGUILayout.BeginHorizontal();
                if (!string.IsNullOrEmpty(outputFolder) && GUILayout.Button("Remove", GUILayout.ExpandWidth(false)))
                {
                    outputFolder = null;
                }
                EditorGUILayout.LabelField(outputFolder, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();    
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create Texture Arrays"))
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
