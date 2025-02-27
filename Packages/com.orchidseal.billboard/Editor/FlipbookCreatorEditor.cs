using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OrchidSeal.Billboard.Editor
{
    public class FlipbookCreatorEditor : EditorWindow
    {
        private FilterMode filterMode = FilterMode.Trilinear;
        private SerializedObject serializedObject;
        private ResizeMode resizeMode = ResizeMode.Stretch;
        [SerializeField] private Texture[] sourceTextures = new Texture[1];
        private SerializedProperty sourceTexturesProp;
        private TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        
        [MenuItem ("Window/Orchid Seal/Billboard - Create Flipbook")]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(FlipbookCreatorEditor));
            window.titleContent = new GUIContent("Create Flipbook");
        }

        private void CreateGUI()
        {
            serializedObject = new SerializedObject(this);
            sourceTexturesProp = serializedObject.FindProperty(nameof(sourceTextures));
        }

        private void OnGUI()
        {
            EditorGUILayout.PropertyField(sourceTexturesProp, new GUIContent("Source Textures"));
            filterMode = (FilterMode) EditorGUILayout.EnumPopup("Filter Mode", filterMode);
            wrapMode = (TextureWrapMode) EditorGUILayout.EnumPopup("Wrap Mode", wrapMode);
            resizeMode = (ResizeMode) EditorGUILayout.EnumPopup("Resize Mode", resizeMode);
            if (resizeMode == ResizeMode.None)
            {
                EditorGUILayout.HelpBox("Resize Mode \"None\" may make larger file sizes! Compression usually requires certain image dimensions. Use sides divisible by 4, or powers of two if possible.", MessageType.Warning);
            }
            
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create Texture Arrays"))
            {
                if (sourceTextures != null && sourceTextures.Length > 0)
                {
                    var paths = sourceTextures
                        .Select(AssetDatabase.GetAssetPath)
                        .Where(path => Path.GetExtension(path).ToLowerInvariant() == ".gif")
                        .ToArray();
                    var spec = new FlipbookSpec()
                    {
                        filterMode = filterMode,
                        resizeMode = resizeMode,
                        wrapMode = wrapMode,
                    };
                    FlipbookCreator.CreateTextureArraysFromGifPaths(paths, spec);
                }
            }
        }
    }
}
