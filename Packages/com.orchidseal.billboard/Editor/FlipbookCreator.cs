using System.IO;
using System.Linq;
using OrchidSeal.Billboard.Gif;
using UnityEditor;
using UnityEngine;
using Graphics = UnityEngine.Graphics;

namespace OrchidSeal.Billboard.Editor
{
    public class FlipbookCreator
    {
        [MenuItem("Assets/Orchid Seal/Billboard/GIF to Texture 2D Array")]
        private static void ConvertGifToTexture2DArray()
        {
            foreach (var guid in Selection.assetGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var texture2DArray = CreateTexture2DArray(path);
                AssetDatabase.CreateAsset(texture2DArray, Path.ChangeExtension(path, ".asset"));
            }
            
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Assets/Orchid Seal/Billboard/GIF to Texture 2D Array", true)]
        private static bool ValidateGifToTexture2DArray()
        {
            if (Selection.assetGUIDs == null) return false;
            return Selection.assetGUIDs.All(guid => Path.GetExtension(AssetDatabase.GUIDToAssetPath(guid)).ToLowerInvariant() == ".gif");
        }

        private static Texture2DArray CreateTexture2DArray(string path)
        {
            using var image = GifReader.FromFile(path);
            var textureFormat = image.HasAlpha ? TextureFormat.DXT5 : TextureFormat.DXT1;
            var frameCount = image.FrameCount;
            
            var width = Mathf.NextPowerOfTwo(image.Width);
            var height = Mathf.NextPowerOfTwo(image.Height);
            var texture2DArray = new Texture2DArray(width, height, frameCount, textureFormat, true);

            var sourceWidth = image.Width;
            var sourceHeight = image.Height;
            var texture = new Texture2D(sourceWidth, sourceHeight);

            for (var i = 0; i < frameCount; i++)
            {
                var scaledTexture = new Texture2D(width, height)
                {
                    filterMode = FilterMode.Trilinear
                };
                
                texture.SetPixelData(image.GetNextFrame(), 0);
                texture.Apply();
                
                ScaleTexture(texture, scaledTexture, width, height);
                EditorUtility.CompressTexture(scaledTexture, textureFormat, TextureCompressionQuality.Normal);
                Graphics.CopyTexture(scaledTexture, 0, texture2DArray, i);
                Object.DestroyImmediate(scaledTexture);
            }
            
            Object.DestroyImmediate(texture);

            return texture2DArray;
        }

        private static void ScaleTexture(Texture2D source, Texture2D destination, int width, int height)
        {
            var renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, renderTexture);
            RenderTexture.active = renderTexture;
            destination.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }
}
