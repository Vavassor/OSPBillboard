using System.IO;
using System.Linq;
using OrchidSeal.Billboard.Gif;
using UnityEditor;
using UnityEngine;
using Graphics = UnityEngine.Graphics;
using Object = UnityEngine.Object;

namespace OrchidSeal.Billboard.Editor
{
    public enum ResizeMode
    {
        /// <summary>Don't res</summary>
        None,
        ///<summary>Center the image and don't scale it, to keep sharpness.</summary>
        Center,
        ///<summary>Fit the image touching the bounds, to maintain aspect ratio.</summary>
        Contain,
        /// <summary>Stretch the image to fit, to fill the bounds.</summary>
        Stretch,
    }
    
    public struct FlipbookSpec
    {
        public FilterMode filterMode;
        public ResizeMode resizeMode;
        public TextureWrapMode wrapMode;
    }
    
    public class FlipbookCreator
    {
        [MenuItem("Assets/Orchid Seal/Billboard - GIF to Texture Array")]
        private static void ConvertGifToTexture2DArray()
        {
            var guids = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var spec = new FlipbookSpec()
            {
                filterMode = FilterMode.Trilinear,
                resizeMode = ResizeMode.Stretch,
                wrapMode = TextureWrapMode.Clamp,
            };
            CreateTextureArraysFromGifPaths(guids, spec);
        }

        [MenuItem("Assets/Orchid Seal/Billboard - GIF to Texture Array", true)]
        private static bool ValidateGifToTexture2DArray()
        {
            if (Selection.assetGUIDs == null) return false;
            return Selection.assetGUIDs.All(guid => Path.GetExtension(AssetDatabase.GUIDToAssetPath(guid)).ToLowerInvariant() == ".gif");
        }

        public static void CreateTextureArraysFromGifPaths(string[] paths, FlipbookSpec spec)
        {
            foreach (var path in paths)
            {
                var texture2DArray = CreateTextureArrayFromGif(path, spec);
                AssetDatabase.CreateAsset(texture2DArray, Path.ChangeExtension(path, ".asset"));
            }
            
            AssetDatabase.SaveAssets();
        }

        private static TextureFormat ChooseFormat(bool hasAlpha, int width, int height)
        {
            if (width % 4 == 0 && height % 4 == 0)
            {
                return hasAlpha ? TextureFormat.DXT5 : TextureFormat.DXT1;
            }

            return hasAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24;
        }

        private static Texture2DArray CreateTextureArrayFromGif(string path, FlipbookSpec spec)
        {
            using var image = GifReader.FromFile(path);
            var frameCount = image.FrameCount;

            switch (spec.resizeMode)
            {
                default:
                case ResizeMode.None:
                {
                    var textureFormat = ChooseFormat(image.HasAlpha, image.Width, image.Height);
                    var texture2dArray = new Texture2DArray(image.Width, image.Height, frameCount, textureFormat, true)
                    {
                        filterMode = spec.filterMode,
                        wrapMode = spec.wrapMode
                    };
                    
                    for (var i = 0; i < frameCount; i++)
                    {
                        texture2dArray.SetPixelData(image.GetNextFrame(), 0, i);
                        texture2dArray.Apply(updateMipmaps: false);
                    }
                
                    texture2dArray.Apply();
                    return texture2dArray;
                }
                case ResizeMode.Center:
                {
                    var blitMaterial = new Material(Shader.Find("Orchid Seal/OSP Billboard/Center Blit"));
                    var width = Mathf.NextPowerOfTwo(image.Width);
                    var height = Mathf.NextPowerOfTwo(image.Height);
                    Debug.Log($"w {width} h {height} iw {image.Width} ih {image.Height}");
                    blitMaterial.SetVector("_TargetTexelSize", new Vector4(1.0f / width, 1.0f / height, width, height));
                    return CreateScaledFrames(image, spec, blitMaterial, true);
                }
                case ResizeMode.Contain:
                {
                    var blitMaterial = new Material(Shader.Find("Orchid Seal/OSP Billboard/Contain Blit"));
                    blitMaterial.SetFloat("_Aspect", image.Width / (float) image.Height);
                    return CreateScaledFrames(image, spec, blitMaterial, true);
                }
                case ResizeMode.Stretch:
                {
                    return CreateScaledFrames(image, spec, null, false);
                }
            }
        }

        private static Texture2DArray CreateScaledFrames(GifReader image, FlipbookSpec spec, Material blitMaterial, bool shouldClear)
        {
            var width = Mathf.NextPowerOfTwo(image.Width);
            var height = Mathf.NextPowerOfTwo(image.Height);
            var frameCount = image.FrameCount;
            var textureFormat = image.HasAlpha ? TextureFormat.DXT5 : TextureFormat.DXT1;
            var texture2dArray = new Texture2DArray(width, height, frameCount, textureFormat, true)
            {
                filterMode = spec.filterMode,
                wrapMode = spec.wrapMode
            };

            var clearData = shouldClear ? Enumerable.Repeat((byte)0x00, 4 * width * height).ToArray() : null;
            var texture = new Texture2D(image.Width, image.Height);

            for (var i = 0; i < frameCount; i++)
            {
                // Load the next frame and upload it to the GPU.
                texture.SetPixelData(image.GetNextFrame(), 0);
                texture.Apply();

                // Scale the frame.
                var scaledTexture = new Texture2D(width, height);
                if (clearData != null)
                {
                    scaledTexture.SetPixelData(clearData, 0);
                    scaledTexture.Apply();
                }
                
                var renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                if (blitMaterial)
                {
                    Graphics.Blit(texture, renderTexture, blitMaterial);
                }
                else
                {
                    Graphics.Blit(texture, renderTexture);
                }
                RenderTexture.active = renderTexture;
                scaledTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(renderTexture);
                
                // Compress and add the frame to the array.
                EditorUtility.CompressTexture(scaledTexture, textureFormat, TextureCompressionQuality.Normal);
                // TODO: Are mipmaps copied here? 
                Graphics.CopyTexture(scaledTexture, 0, texture2dArray, i);
                Object.DestroyImmediate(scaledTexture);
            }
            
            Object.DestroyImmediate(texture);

            return texture2dArray;
        }
    }
}
