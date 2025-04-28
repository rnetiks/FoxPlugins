using System;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Autumn
{
    /// <summary>
    /// Factory class for creating and loading textures.
    /// </summary>
    public static class TextureFactory
    {
        /// <summary>
        /// Creates a new TextureElement with the specified dimensions.
        /// </summary>
        public static TextureElement Create(int width, int height, TextureFormat format = TextureFormat.RGBA32)
        {
            return new TextureElement(width, height, format);
        }

        /// <summary>
        /// Wraps an existing Texture2D in a TextureElement.
        /// </summary>
        public static TextureElement From(Texture2D texture)
        {
            return new TextureElement(texture);
        }

        /// <summary>
        /// Loads a texture from file or creates it with the provided callback if not found.
        /// </summary>
        public static TextureElement Load(string filepath, Func<string, Texture2D> onNotFound)
        {
            if (!File.Exists(filepath))
            {
                var texture2D = onNotFound(filepath);
                return new TextureElement(texture2D);
            }

            var data = new Texture2D(1, 1);
            data.LoadImage(File.ReadAllBytes(filepath));
            data.Apply();
            return new TextureElement(data);
        }

        public static TextureElement Load(string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException(nameof(filepath), filepath, null);
            }

            var data = new Texture2D(1, 1);
            data.LoadImage(File.ReadAllBytes(filepath));
            data.Apply();
            return new TextureElement(data);
        }

        /// <summary>
        /// Creates a new TextureElement with a vertical gradient.
        /// </summary>
        public static TextureElement Gradient(int width, int height, Color topColor, Color bottomColor)
        {
            var element = Create(width, height);
            return element.BackgroundGradient(topColor, bottomColor, 90);
        }

        /// <summary>
        /// Creates a new TextureElement with a solid color.
        /// </summary>
        public static TextureElement SolidColor(int width, int height, Color32 color)
        {
            var element = Create(width, height);
            return element.BackgroundColor(color.r, color.g, color.b, color.a);
        }

        /// <summary>
        /// Creates a new TextureElement with a checkerboard pattern.
        /// </summary>
        public static TextureElement Checkerboard(int width, int height, Color color1, Color color2, int tileSize = 8)
        {
            var element = Create(width, height);
            return element.AddOperation(tex =>
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        bool isColor1 = ((x / tileSize) + (y / tileSize)) % 2 == 0;
                        tex.SetPixel(x, y, isColor1 ? color1 : color2);
                    }
                }
            });
        }

        /// <summary>
        /// Creates a copy of the given Texture2D using Blit.
        /// </summary>
        /// <param name="source">The source Texture2D to copy.</param>
        /// <returns>A new Texture2D object that is an identical copy of the source texture.</returns>
        public static Texture2D CopyTextureBlit(Texture2D source)
        {
            Texture2D result = new Texture2D(
                source.width,
                source.height,
                source.format,
                source.mipmapCount > 1
            )
            {
                filterMode = source.filterMode,
                wrapMode = source.wrapMode
            };

            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear
            );

            Graphics.Blit(source, renderTex);
            RenderTexture.active = renderTex;

            result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            result.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            return result;
        }

        /// <summary>
        /// Creates a copy of the given Texture2D.
        /// </summary>
        /// <param name="source">The source Texture2D to copy.</param>
        /// <returns>A new Texture2D object that is an identical copy of the source texture.</returns>
        public static unsafe Texture2D CopyTextureDirect(Texture2D source)
        {
            Texture2D result = new Texture2D(
                source.width,
                source.height,
                source.format,
                source.mipmapCount > 1
            )
            {
                filterMode = source.filterMode,
                wrapMode = source.wrapMode
            };
            
            var getHandler = TextureFormatHandler.GetHandler(source.format);
            var setHandler = TextureFormatHandler.GetHandler(result.format);
            var getTextureData = source.GetRawTextureData<byte>();
            var setTextureData = result.GetRawTextureData<byte>();
            var pGet = (byte*)getTextureData.GetUnsafePtr();
            var pSet = (byte*)setTextureData.GetUnsafePtr();

            for (int idx = 0; idx < result.width * result.height; idx++)
            {
                getHandler.GetPixel(pGet, idx, out byte r, out byte g, out byte b, out byte a);
                setHandler.SetPixel(pSet, idx, r, g, b, a);
            }
            
            result.Apply();

            return result;
        }

        /// <summary>
        /// Creates a new TextureElement with a polka dot pattern.
        /// </summary>
        public static TextureElement PolkaDots(int width, int height, Color backgroundColor, Color dotColor,
            int dotRadius = 5, int spacing = 20)
        {
            var element = Create(width, height);
            return element.AddOperation(tex =>
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        tex.SetPixel(x, y, backgroundColor);
                    }
                }

                for (int centerX = dotRadius + spacing / 2; centerX < width; centerX += spacing)
                {
                    for (int centerY = dotRadius + spacing / 2; centerY < height; centerY += spacing)
                    {
                        for (int x = centerX - dotRadius; x <= centerX + dotRadius; x++)
                        {
                            for (int y = centerY - dotRadius; y <= centerY + dotRadius; y++)
                            {
                                if (x >= 0 && x < width && y >= 0 && y < height)
                                {
                                    float dist = Vector2.Distance(new Vector2(centerX, centerY), new Vector2(x, y));
                                    if (dist <= dotRadius)
                                    {
                                        tex.SetPixel(x, y, dotColor);
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Creates a new TextureElement with a striped pattern.
        /// </summary>
        public static TextureElement Stripes(int width, int height, Color color1, Color color2, float angle = 45,
            int stripeWidth = 10)
        {
            var element = Create(width, height);
            return element.AddOperation(tex =>
            {
                float radians = angle * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float projection = x * direction.x + y * direction.y;
                        bool isColor1 = (int)(projection / stripeWidth) % 2 == 0;
                        tex.SetPixel(x, y, isColor1 ? color1 : color2);
                    }
                }
            });
        }
    }
}