using System;
using System.IO;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Autumn
{
    /// <summary>
    /// TextureElement represents a texture with its associated operations.
    /// You might wanna use a Structure View for this, to find the correct methods.
    /// </summary>
    public class TextureElement
    {
        private Texture2D _texture;
        private bool _isDirty = false;
        private List<Action<Texture2D>> _pendingOperations = new List<Action<Texture2D>>();

        public int Width => _texture.width;
        public int Height => _texture.height;

        public TextureElement(Texture2D texture)
        {
            _texture = texture;
        }

        public TextureElement(int width, int height, TextureFormat format = TextureFormat.RGBA32)
        {
            _texture = new Texture2D(width, height, format, false);
        }

        /// <summary>
        /// Applies all pending operations to the texture.
        /// </summary>
        public TextureElement Apply()
        {
            if (_isDirty)
            {
                foreach (var operation in _pendingOperations)
                {
                    operation(_texture);
                }

                _pendingOperations.Clear();
                _texture.Apply();
                _isDirty = false;
            }

            return this;
        }

        /// <summary>
        /// Gets the underlying Texture2D after applying any pending operations.
        /// </summary>
        public Texture2D GetTexture()
        {
            Apply();
            return _texture;
        }

        /// <summary>
        /// Adds an operation to the pending operations queue.
        /// </summary>
        public TextureElement AddOperation(Action<Texture2D> operation)
        {
            _pendingOperations.Add(operation);
            _isDirty = true;
            return this;
        }

        /// <summary>
        /// Saves the texture to a file.
        /// </summary>
        public TextureElement Save(string filepath)
        {
            Apply();

            if (!Directory.Exists("AutumnTextures"))
                Directory.CreateDirectory("AutumnTextures");

            File.WriteAllBytes(Path.Combine("AutumnTextures", filepath), _texture.EncodeToPNG());
            return this;
        }

        #region Styling Properties

        /// <summary>
        /// Sets the color of all pixels in the texture.
        /// </summary>
        public TextureElement BackgroundColor(Color color)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            });
        }

        /// <summary>
        /// Fast fill with a specific color.
        /// </summary>
        public unsafe TextureElement BackgroundColor(byte r, byte g, byte b, byte a)
        {
            return AddOperation(tex =>
            {
                var data = tex.GetRawTextureData<Color32>();
                var unsafePtr = data.GetUnsafePtr();
                Color32* cr = (Color32*)unsafePtr;

                if (tex.format == TextureFormat.RGBA32)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        cr[i].r = r;
                        cr[i].g = g;
                        cr[i].b = b;
                        cr[i].a = a;
                    }
                }
                else
                {
                    throw new ArgumentException("The provided texture format is not supported yet.");
                }
            });
        }

        /// <summary>
        /// Creates a gradient background.
        /// </summary>
        public TextureElement BackgroundGradient(Color startColor, Color endColor, float angle = 0)
        {
            return AddOperation(tex =>
            {
                float r = angle * Mathf.Deg2Rad;
                float dirX = Mathf.Cos(r);
                float dirY = Mathf.Sin(r);

                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        float t = (dirX * x + dirY * y) / Mathf.Sqrt(tex.width * tex.width + tex.height * tex.height);
                        t = Mathf.Clamp01((t + 1) / 2);
                        Color gradientColor = Color.Lerp(startColor, endColor, t);
                        tex.SetPixel(x, y, gradientColor);
                    }
                }
            });
        }

        /// <summary>
        /// Creates a radial gradient background.
        /// </summary>
        public TextureElement BackgroundRadialGradient(Color centerColor, Color outerColor, Vector2? center = null)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;
                Vector2 centerPoint = center ?? new Vector2(width / 2f, height / 2f);
                float maxDistance = Vector2.Distance(centerPoint, new Vector2(0, 0));
                maxDistance = Mathf.Max(maxDistance, Vector2.Distance(centerPoint, new Vector2(width, 0)));
                maxDistance = Mathf.Max(maxDistance, Vector2.Distance(centerPoint, new Vector2(0, height)));
                maxDistance = Mathf.Max(maxDistance, Vector2.Distance(centerPoint, new Vector2(width, height)));

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float dist = Vector2.Distance(centerPoint, new Vector2(x, y));
                        float t = Mathf.Clamp01(dist / maxDistance);
                        Color gradientColor = Color.Lerp(centerColor, outerColor, t);
                        tex.SetPixel(x, y, gradientColor);
                    }
                }
            });
        }

        /// <summary>
        /// Creates a repeating pattern background.
        /// </summary>
        public TextureElement BackgroundPattern(Texture2D patternTexture,
            BackgroundRepeatMode repeatMode = BackgroundRepeatMode.Repeat)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;
                int patternWidth = patternTexture.width;
                int patternHeight = patternTexture.height;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int patternX = 0;
                        int patternY = 0;

                        switch (repeatMode)
                        {
                            case BackgroundRepeatMode.Repeat:
                                patternX = x % patternWidth;
                                patternY = y % patternHeight;
                                break;

                            case BackgroundRepeatMode.RepeatX:
                                patternX = x % patternWidth;
                                patternY = Mathf.Clamp(y, 0, patternHeight - 1);
                                break;

                            case BackgroundRepeatMode.RepeatY:
                                patternX = Mathf.Clamp(x, 0, patternWidth - 1);
                                patternY = y % patternHeight;
                                break;

                            case BackgroundRepeatMode.NoRepeat:
                                patternX = Mathf.Clamp(x, 0, patternWidth - 1);
                                patternY = Mathf.Clamp(y, 0, patternHeight - 1);
                                break;
                        }

                        Color patternColor = patternTexture.GetPixel(patternX, patternY);
                        if (patternColor.a > 0)
                        {
                            tex.SetPixel(x, y, patternColor);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Adds rounded corners to the texture.
        /// </summary>
        public TextureElement BorderRadius(int radius, BorderType borderType = BorderType.All, int aliasDistance = 0)
        {
            return AddOperation(tex =>
            {
                if ((borderType & BorderType.TopLeft) != 0)
                    ApplyBorderTopLeft(tex, radius, aliasDistance);

                if ((borderType & BorderType.TopRight) != 0)
                    ApplyBorderTopRight(tex, radius, aliasDistance);

                if ((borderType & BorderType.BottomLeft) != 0)
                    ApplyBorderBottomLeft(tex, radius, aliasDistance);

                if ((borderType & BorderType.BottomRight) != 0)
                    ApplyBorderBottomRight(tex, radius, aliasDistance);

                if ((borderType & BorderType.All) == BorderType.All)
                {
                    ApplyBorderTopLeft(tex, radius, aliasDistance);
                    ApplyBorderTopRight(tex, radius, aliasDistance);
                    ApplyBorderBottomLeft(tex, radius, aliasDistance);
                    ApplyBorderBottomRight(tex, radius, aliasDistance);
                }
            });
        }

        /// <summary>
        /// Adds a border to the texture.
        /// </summary>
        public TextureElement Border(int width, Color color, BorderDrawMode drawMode = BorderDrawMode.Outside)
        {
            return AddOperation(tex =>
            {
                int texWidth = tex.width;
                int texHeight = tex.height;

                if (drawMode == BorderDrawMode.Inside)
                {
                    for (int x = 0; x < texWidth; x++)
                    {
                        for (int y = 0; y < texHeight; y++)
                        {
                            if (x < width || y < width || x >= texWidth - width || y >= texHeight - width)
                            {
                                Color origColor = tex.GetPixel(x, y);
                                tex.SetPixel(x, y, Color.Lerp(origColor, color, color.a));
                            }
                        }
                    }
                }
                else if (drawMode == BorderDrawMode.Outside)
                {
                    Texture2D newTex = new Texture2D(texWidth + width * 2, texHeight + width * 2);

                    for (int x = 0; x < newTex.width; x++)
                    {
                        for (int y = 0; y < newTex.height; y++)
                        {
                            newTex.SetPixel(x, y, color);
                        }
                    }

                    for (int x = 0; x < texWidth; x++)
                    {
                        for (int y = 0; y < texHeight; y++)
                        {
                            newTex.SetPixel(x + width, y + width, tex.GetPixel(x, y));
                        }
                    }

                    newTex.Apply();
                    _texture = newTex;
                }
                else
                {
                    Texture2D newTex = new Texture2D(texWidth + width, texHeight + width);

                    for (int x = 0; x < newTex.width; x++)
                    {
                        for (int y = 0; y < newTex.height; y++)
                        {
                            newTex.SetPixel(x, y, color);
                        }
                    }

                    for (int x = 0; x < texWidth; x++)
                    {
                        for (int y = 0; y < texHeight; y++)
                        {
                            newTex.SetPixel(x + width / 2, y + width / 2, tex.GetPixel(x, y));
                        }
                    }

                    newTex.Apply();
                    _texture = newTex;
                }
            });
        }

        /// <summary>
        /// Sets the overall opacity of the texture.
        /// </summary>
        public TextureElement Opacity(float value)
        {
            return AddOperation(tex =>
            {
                value = Mathf.Clamp01(value);
                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color pixelColor = tex.GetPixel(x, y);
                        pixelColor.a *= value;
                        tex.SetPixel(x, y, pixelColor);
                    }
                }
            });
        }

        /// <summary>
        /// Applies a box shadow to the texture.
        /// </summary>
        public TextureElement BoxShadow(int offsetX, int offsetY, int blurRadius, Color shadowColor)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;

                int newWidth = width + Mathf.Abs(offsetX) + blurRadius * 2;
                int newHeight = height + Mathf.Abs(offsetY) + blurRadius * 2;
                Texture2D newTex = new Texture2D(newWidth, newHeight);

                for (int x = 0; x < newWidth; x++)
                {
                    for (int y = 0; y < newHeight; y++)
                    {
                        newTex.SetPixel(x, y, Color.clear);
                    }
                }

                int shadowStartX = blurRadius + (offsetX < 0 ? Mathf.Abs(offsetX) : 0);
                int shadowStartY = blurRadius + (offsetY < 0 ? Mathf.Abs(offsetY) : 0);

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Color origColor = tex.GetPixel(x, y);
                        if (origColor.a > 0)
                        {
                            newTex.SetPixel(shadowStartX + x + offsetX, shadowStartY + y + offsetY,
                                new Color(shadowColor.r, shadowColor.g, shadowColor.b, origColor.a * shadowColor.a));
                        }
                    }
                }

                if (blurRadius > 0)
                {
                    Color[] pixels = newTex.GetPixels();
                    Color[] newPixels = new Color[pixels.Length];

                    for (int x = 0; x < newWidth; x++)
                    {
                        for (int y = 0; y < newHeight; y++)
                        {
                            float r = 0, g = 0, b = 0, a = 0;
                            int count = 0;

                            for (int i = -blurRadius; i <= blurRadius; i++)
                            {
                                for (int j = -blurRadius; j <= blurRadius; j++)
                                {
                                    int px = Mathf.Clamp(x + i, 0, newWidth - 1);
                                    int py = Mathf.Clamp(y + j, 0, newHeight - 1);
                                    int index = py * newWidth + px;

                                    Color pixelColor = pixels[index];
                                    r += pixelColor.r;
                                    g += pixelColor.g;
                                    b += pixelColor.b;
                                    a += pixelColor.a;
                                    count++;
                                }
                            }

                            newPixels[y * newWidth + x] = new Color(r / count, g / count, b / count, a / count);
                        }
                    }

                    newTex.SetPixels(newPixels);
                }

                int origStartX = blurRadius + (offsetX < 0 ? Mathf.Abs(offsetX) : 0);
                int origStartY = blurRadius + (offsetY < 0 ? Mathf.Abs(offsetY) : 0);

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Color pixelColor = tex.GetPixel(x, y);
                        if (pixelColor.a > 0)
                        {
                            newTex.SetPixel(origStartX + x, origStartY + y, pixelColor);
                        }
                    }
                }

                newTex.Apply();
                _texture = newTex;
            });
        }

        public TextureElement Scale(float size, FilterMode filterMode = FilterMode.Bilinear)
        {
            return AddOperation(tex => Scale((int)(tex.width * size), (int)(tex.height * size), filterMode));
        }

        /// <summary>
        /// Scales the texture to the specified dimensions.
        /// </summary>
        public TextureElement Scale(int newWidth, int newHeight, FilterMode filterMode = FilterMode.Bilinear)
        {
            return AddOperation(tex =>
            {
                Texture2D newTex = new Texture2D(newWidth, newHeight);

                FilterMode originalFilterMode = tex.filterMode;
                tex.filterMode = filterMode;

                float scaleX = (float)newWidth / tex.width;
                float scaleY = (float)newHeight / tex.height;

                for (int x = 0; x < newWidth; x++)
                {
                    for (int y = 0; y < newHeight; y++)
                    {
                        float u = x / (float)newWidth;
                        float v = y / (float)newHeight;

                        Color sampledColor = tex.GetPixelBilinear(u, v);
                        newTex.SetPixel(x, y, sampledColor);
                    }
                }

                tex.filterMode = originalFilterMode;

                newTex.Apply();
                _texture = newTex;
            });
        }

        /// <summary>
        /// Rotates the texture by the specified angle in degrees.
        /// </summary>
        public TextureElement Rotate(float angle, bool resizeCanvas = true)
        {
            return AddOperation(tex =>
            {
                angle = angle % 360;
                if (angle < 0) angle += 360;

                float radians = angle * Mathf.Deg2Rad;

                int width = tex.width;
                int height = tex.height;

                int newWidth = width;
                int newHeight = height;

                if (resizeCanvas)
                {
                    float cos = Mathf.Abs(Mathf.Cos(radians));
                    float sin = Mathf.Abs(Mathf.Sin(radians));
                    newWidth = Mathf.CeilToInt(width * cos + height * sin);
                    newHeight = Mathf.CeilToInt(width * sin + height * cos);
                }

                Texture2D newTex = new Texture2D(newWidth, newHeight);

                for (int x = 0; x < newWidth; x++)
                {
                    for (int y = 0; y < newHeight; y++)
                    {
                        newTex.SetPixel(x, y, Color.clear);
                    }
                }

                Vector2 center = new Vector2(width / 2f, height / 2f);
                Vector2 newCenter = new Vector2(newWidth / 2f, newHeight / 2f);

                for (int x = 0; x < newWidth; x++)
                {
                    for (int y = 0; y < newHeight; y++)
                    {
                        Vector2 pos = new Vector2(x - newCenter.x, y - newCenter.y);

                        float rotX = pos.x * Mathf.Cos(-radians) - pos.y * Mathf.Sin(-radians);
                        float rotY = pos.x * Mathf.Sin(-radians) + pos.y * Mathf.Cos(-radians);

                        int origX = Mathf.RoundToInt(rotX + center.x);
                        int origY = Mathf.RoundToInt(rotY + center.y);

                        if (origX >= 0 && origX < width && origY >= 0 && origY < height)
                        {
                            newTex.SetPixel(x, y, tex.GetPixel(origX, origY));
                        }
                    }
                }

                newTex.Apply();
                _texture = newTex;
            });
        }

        /// <summary>
        /// Clips the texture to a specific shape.
        /// </summary>
        public TextureElement ClipPath(ClipShapeType shapeType, params float[] parameters)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;

                switch (shapeType)
                {
                    case ClipShapeType.Circle:

                        float radiusPerc = parameters.Length > 0 ? parameters[0] : 0.5f;
                        Vector2 center = new Vector2(width / 2f, height / 2f);
                        float maxDim = Mathf.Max(width, height);
                        float radius = maxDim * radiusPerc;

                        for (int x = 0; x < width; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                float distance = Vector2.Distance(new Vector2(x, y), center);
                                if (distance > radius)
                                {
                                    tex.SetPixel(x, y, Color.clear);
                                }
                            }
                        }

                        break;

                    case ClipShapeType.Ellipse:

                        float xRadiusPerc = parameters.Length > 0 ? parameters[0] : 0.5f;
                        float yRadiusPerc = parameters.Length > 1 ? parameters[1] : 0.5f;
                        Vector2 ellipseCenter = new Vector2(width / 2f, height / 2f);
                        float xRadius = width * xRadiusPerc;
                        float yRadius = height * yRadiusPerc;

                        for (int x = 0; x < width; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                float normalizedX = (x - ellipseCenter.x) / xRadius;
                                float normalizedY = (y - ellipseCenter.y) / yRadius;
                                if (normalizedX * normalizedX + normalizedY * normalizedY > 1)
                                {
                                    tex.SetPixel(x, y, Color.clear);
                                }
                            }
                        }

                        break;

                    case ClipShapeType.Polygon:

                        if (parameters.Length < 6 || parameters.Length % 2 != 0)
                        {
                            Debug.LogError(
                                "ClipPath Polygon requires at least 3 points (6 values) and an even number of parameters");
                            break;
                        }

                        int vertexCount = parameters.Length / 2;
                        Vector2[] vertices = new Vector2[vertexCount];

                        for (int i = 0; i < vertexCount; i++)
                        {
                            vertices[i] = new Vector2(
                                parameters[i * 2] * width,
                                parameters[i * 2 + 1] * height
                            );
                        }

                        for (int x = 0; x < width; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                if (!IsPointInPolygon(new Vector2(x, y), vertices))
                                {
                                    tex.SetPixel(x, y, Color.clear);
                                }
                            }
                        }

                        break;

                    case ClipShapeType.Inset:

                        float top = parameters.Length > 0 ? parameters[0] * height : 0;
                        float right = parameters.Length > 1 ? parameters[1] * width : 0;
                        float bottom = parameters.Length > 2 ? parameters[2] * height : 0;
                        float left = parameters.Length > 3 ? parameters[3] * width : 0;

                        for (int x = 0; x < width; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                if (x < left || x > width - right || y < bottom || y > height - top)
                                {
                                    tex.SetPixel(x, y, Color.clear);
                                }
                            }
                        }

                        break;
                }
            });
        }

        /// <summary>
        /// Applies a mask texture to this texture (black = transparent, white = opaque).
        /// </summary>
        public TextureElement Mask(Texture2D maskTexture)
        {
            return AddOperation(tex =>
            {
                if (tex.width != maskTexture.width || tex.height != maskTexture.height)
                {
                    Debug.LogError("Mask texture dimensions must match the target texture dimensions");
                    return;
                }

                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color texColor = tex.GetPixel(x, y);
                        float maskAlpha = maskTexture.GetPixel(x, y).grayscale;
                        texColor.a *= maskAlpha;
                        tex.SetPixel(x, y, texColor);
                    }
                }
            });
        }

        #endregion

        #region Filters

        /// <summary>
        /// Adjusts the brightness of the texture.
        /// </summary>
        public TextureElement Brightness(float value)
        {
            return AddOperation(tex =>
            {
                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color pixelColor = tex.GetPixel(x, y);
                        pixelColor.r = Mathf.Clamp01(pixelColor.r + value);
                        pixelColor.g = Mathf.Clamp01(pixelColor.g + value);
                        pixelColor.b = Mathf.Clamp01(pixelColor.b + value);
                        tex.SetPixel(x, y, pixelColor);
                    }
                }
            });
        }

        /// <summary>
        /// Adds noise to the texture, like a film grain effect.
        /// </summary>
        public TextureElement Noise(float intensity, bool monochrome = true)
        {
            return AddOperation(tex =>
            {
                System.Random random = new System.Random();

                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color pixelColor = tex.GetPixel(x, y);

                        if (monochrome)
                        {
                            float noise = (float)random.NextDouble() * 2 - 1;
                            noise *= intensity;

                            pixelColor.r = Mathf.Clamp01(pixelColor.r + noise);
                            pixelColor.g = Mathf.Clamp01(pixelColor.g + noise);
                            pixelColor.b = Mathf.Clamp01(pixelColor.b + noise);
                        }
                        else
                        {
                            pixelColor.r =
                                Mathf.Clamp01(pixelColor.r + ((float)random.NextDouble() * 2 - 1) * intensity);
                            pixelColor.g =
                                Mathf.Clamp01(pixelColor.g + ((float)random.NextDouble() * 2 - 1) * intensity);
                            pixelColor.b =
                                Mathf.Clamp01(pixelColor.b + ((float)random.NextDouble() * 2 - 1) * intensity);
                        }

                        tex.SetPixel(x, y, pixelColor);
                    }
                }
            });
        }

        /// <summary>
        /// Applies a sharpen filter to the texture.
        /// </summary>
        public TextureElement Sharpen(float intensity = 1.0f)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;
                Color[] pixels = tex.GetPixels();
                Color[] newPixels = new Color[pixels.Length];

                float center = 1.0f + 4.0f * intensity;
                float adjacent = -intensity;

                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        int index = y * width + x;

                        Color result = pixels[index] * center +
                                       pixels[index - 1] * adjacent +
                                       pixels[index + 1] * adjacent +
                                       pixels[index - width] * adjacent +
                                       pixels[index + width] * adjacent;

                        newPixels[index] = new Color(
                            Mathf.Clamp01(result.r),
                            Mathf.Clamp01(result.g),
                            Mathf.Clamp01(result.b),
                            pixels[index].a
                        );
                    }
                }

                for (int x = 0; x < width; x++)
                {
                    newPixels[x] = pixels[x];
                    newPixels[x + width * (height - 1)] = pixels[x + width * (height - 1)];
                }

                for (int y = 0; y < height; y++)
                {
                    newPixels[y * width] = pixels[y * width];
                    newPixels[y * width + width - 1] = pixels[y * width + width - 1];
                }

                tex.SetPixels(newPixels);
            });
        }

        /// <summary>
        /// Reduces the number of colors.
        /// </summary>
        public TextureElement Posterize(int levels)
        {
            return AddOperation(tex =>
            {
                levels = Mathf.Max(2, levels);

                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color pixelColor = tex.GetPixel(x, y);
                        pixelColor.r = Mathf.Floor(pixelColor.r * levels) / levels;
                        pixelColor.g = Mathf.Floor(pixelColor.g * levels) / levels;
                        pixelColor.b = Mathf.Floor(pixelColor.b * levels) / levels;
                        tex.SetPixel(x, y, pixelColor);
                    }
                }
            });
        }

        /// <summary>
        /// Applies a threshold filter, converting pixels to black or white based on their brightness.
        /// </summary>
        public TextureElement Threshold(float threshold)
        {
            return AddOperation(tex =>
            {
                threshold = Mathf.Clamp01(threshold);

                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color pixelColor = tex.GetPixel(x, y);
                        float brightness = pixelColor.r * 0.299f + pixelColor.g * 0.587f + pixelColor.b * 0.114f;

                        if (brightness < threshold)
                        {
                            tex.SetPixel(x, y, new Color(0, 0, 0, pixelColor.a));
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color(1, 1, 1, pixelColor.a));
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Applies a Gaussian blur filter to the texture.
        /// </summary>
        public TextureElement GaussianBlur(int radius, float sigma = 1.0f)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;
                Color[] pixels = tex.GetPixels();
                Color[] newPixels = new Color[pixels.Length];

                int kernelSize = radius * 2 + 1;
                float[] kernel = new float[kernelSize];
                float kernelSum = 0;

                for (int i = 0; i < kernelSize; i++)
                {
                    int x = i - radius;
                    kernel[i] = Mathf.Exp(-(x * x) / (2 * sigma * sigma));
                    kernelSum += kernel[i];
                }

                for (int i = 0; i < kernelSize; i++)
                {
                    kernel[i] /= kernelSum;
                }

                Color[] tempPixels = new Color[pixels.Length];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float r = 0, g = 0, b = 0, a = 0;

                        for (int k = -radius; k <= radius; k++)
                        {
                            int px = Mathf.Clamp(x + k, 0, width - 1);
                            int index = y * width + px;
                            float weight = kernel[k + radius];

                            r += pixels[index].r * weight;
                            g += pixels[index].g * weight;
                            b += pixels[index].b * weight;
                            a += pixels[index].a * weight;
                        }

                        tempPixels[y * width + x] = new Color(r, g, b, a);
                    }
                }

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float r = 0, g = 0, b = 0, a = 0;

                        for (int k = -radius; k <= radius; k++)
                        {
                            int py = Mathf.Clamp(y + k, 0, height - 1);
                            int index = py * width + x;
                            float weight = kernel[k + radius];

                            r += tempPixels[index].r * weight;
                            g += tempPixels[index].g * weight;
                            b += tempPixels[index].b * weight;
                            a += tempPixels[index].a * weight;
                        }

                        newPixels[y * width + x] = new Color(r, g, b, a);
                    }
                }

                tex.SetPixels(newPixels);
            });
        }

        public TextureElement Contrast(float value)
        {
            return AddOperation(tex =>
            {
                float factor = (259 * (value + 255)) / (255 * (259 - value));

                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color pixelColor = tex.GetPixel(x, y);
                        pixelColor.r = Mathf.Clamp01(factor * (pixelColor.r - 0.5f) + 0.5f);
                        pixelColor.g = Mathf.Clamp01(factor * (pixelColor.g - 0.5f) + 0.5f);
                        pixelColor.b = Mathf.Clamp01(factor * (pixelColor.b - 0.5f) + 0.5f);
                        tex.SetPixel(x, y, pixelColor);
                    }
                }
            });
        }

        public TextureElement Saturation(float value)
        {
            return AddOperation(tex =>
            {
                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color pixelColor = tex.GetPixel(x, y);
                        float gray = 0.299f * pixelColor.r + 0.587f * pixelColor.g + 0.114f * pixelColor.b;
                        Color adjustedColor = new Color(
                            Mathf.Clamp01(gray + value * (pixelColor.r - gray)),
                            Mathf.Clamp01(gray + value * (pixelColor.g - gray)),
                            Mathf.Clamp01(gray + value * (pixelColor.b - gray)),
                            pixelColor.a
                        );
                        tex.SetPixel(x, y, adjustedColor);
                    }
                }
            });
        }

        public TextureElement Hue(float value)
        {
            return AddOperation(tex =>
            {
                Color[] pixels = tex.GetPixels();

                float cosA = Mathf.Cos(value * Mathf.PI * 2);
                float sinA = Mathf.Sin(value * Mathf.PI * 2);

                double[,] hueMatrix =
                {
                    {
                        0.299d + 0.701d * cosA + 0.168d * sinA, 0.587d - 0.587d * cosA + 0.330d * sinA,
                        0.114d - 0.114d * cosA - 0.497d * sinA
                    },
                    {
                        0.299d - 0.299d * cosA - 0.328d * sinA, 0.587d + 0.413d * cosA + 0.035d * sinA,
                        0.114d - 0.114d * cosA + 0.292d * sinA
                    },
                    {
                        0.299d - 0.3d * cosA + 1.25d * sinA, 0.587d - 0.588d * cosA - 1.05d * sinA,
                        0.114d + 0.886d * cosA - 0.203d * sinA
                    }
                };

                for (int i = 0; i < pixels.Length; i++)
                {
                    Color c = pixels[i];

                    float r = (float)(c.r * hueMatrix[0, 0] + c.g * hueMatrix[0, 1] + c.b * hueMatrix[0, 2]);
                    float g = (float)(c.r * hueMatrix[1, 0] + c.g * hueMatrix[1, 1] + c.b * hueMatrix[1, 2]);
                    float b = (float)(c.r * hueMatrix[2, 0] + c.g * hueMatrix[2, 1] + c.b * hueMatrix[2, 2]);

                    pixels[i] = new Color(r, g, b, c.a);
                }

                tex.SetPixels(pixels);
            });
        }

        public TextureElement Grayscale()
        {
            return AddOperation(tex =>
            {
                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color pixelColor = tex.GetPixel(x, y);
                        float gray = 0.299f * pixelColor.r + 0.587f * pixelColor.g + 0.114f * pixelColor.b;
                        Color grayColor = new Color(gray, gray, gray, pixelColor.a);
                        tex.SetPixel(x, y, grayColor);
                    }
                }
            });
        }

        public TextureElement Invert()
        {
            return AddOperation(tex =>
            {
                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color pixelColor = tex.GetPixel(x, y);
                        Color invertedColor = new Color(1 - pixelColor.r, 1 - pixelColor.g, 1 - pixelColor.b,
                            pixelColor.a);
                        tex.SetPixel(x, y, invertedColor);
                    }
                }
            });
        }

        public TextureElement Sepia()
        {
            return AddOperation(tex =>
            {
                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        var pixelColor = tex.GetPixel(x, y);
                        var r = pixelColor.r * 0.393f + pixelColor.g * 0.769f + pixelColor.b * 0.189f;
                        var g = pixelColor.r * 0.349f + pixelColor.g * 0.686f + pixelColor.b * 0.168f;
                        var b = pixelColor.r * 0.272f + pixelColor.g * 0.534f + pixelColor.b * 0.131f;
                        var sepiaColor = new Color(r, g, b, pixelColor.a);
                        tex.SetPixel(x, y, sepiaColor);
                    }
                }
            });
        }

        public TextureElement Blur(int radius)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;
                Color[] pixels = tex.GetPixels();
                Color[] newPixels = new Color[pixels.Length];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float r = 0, g = 0, b = 0, a = 0;
                        int count = 0;

                        for (int i = -radius; i <= radius; i++)
                        {
                            for (int j = -radius; j <= radius; j++)
                            {
                                int px = Mathf.Clamp(x + i, 0, width - 1);
                                int py = Mathf.Clamp(y + j, 0, height - 1);
                                Color pixelColor = pixels[py * width + px];
                                r += pixelColor.r;
                                g += pixelColor.g;
                                b += pixelColor.b;
                                a += pixelColor.a;
                                count++;
                            }
                        }

                        newPixels[y * width + x] = new Color(r / count, g / count, b / count, a / count);
                    }
                }

                tex.SetPixels(newPixels);
            });
        }

        public TextureElement Pixelate(int pixelSize)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;
                Color[] pixels = tex.GetPixels();
                Color[] newPixels = new Color[pixels.Length];

                for (int x = 0; x < width; x += pixelSize)
                {
                    for (int y = 0; y < height; y += pixelSize)
                    {
                        Color avgColor = Color.black;
                        int count = 0;

                        for (int i = 0; i < pixelSize; i++)
                        {
                            for (int j = 0; j < pixelSize; j++)
                            {
                                int px = Mathf.Clamp(x + i, 0, width - 1);
                                int py = Mathf.Clamp(y + j, 0, height - 1);
                                avgColor += pixels[py * width + px];
                                count++;
                            }
                        }

                        avgColor /= count;

                        for (int i = 0; i < pixelSize; i++)
                        {
                            for (int j = 0; j < pixelSize; j++)
                            {
                                int px = Mathf.Clamp(x + i, 0, width - 1);
                                int py = Mathf.Clamp(y + j, 0, height - 1);
                                newPixels[py * width + px] = avgColor;
                            }
                        }
                    }
                }

                tex.SetPixels(newPixels);
            });
        }

        public TextureElement EdgeDetection()
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;
                Color[] pixels = tex.GetPixels();
                Color[] newPixels = new Color[pixels.Length];

                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        int index = y * width + x;
                        float gx = pixels[index - 1].grayscale - pixels[index + 1].grayscale;
                        float gy = pixels[index - width].grayscale - pixels[index + width].grayscale;
                        float magnitude = Mathf.Sqrt(gx * gx + gy * gy);
                        newPixels[index] = new Color(magnitude, magnitude, magnitude, pixels[index].a);
                    }
                }

                tex.SetPixels(newPixels);
            });
        }

        public TextureElement Vignette(float strength)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;
                float centerX = width / 2.0f;
                float centerY = height / 2.0f;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float dx = x - centerX;
                        float dy = y - centerY;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        float maxDistance = Mathf.Sqrt(centerX * centerX + centerY * centerY);
                        float factor = Mathf.Clamp01(1.0f - distance / maxDistance);
                        factor = Mathf.Pow(factor, strength);

                        Color pixelColor = tex.GetPixel(x, y);
                        tex.SetPixel(x, y,
                            new Color(pixelColor.r * factor, pixelColor.g * factor, pixelColor.b * factor,
                                pixelColor.a));
                    }
                }
            });
        }

        public TextureElement ColorTint(Color tintColor)
        {
            return AddOperation(tex =>
            {
                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color pixelColor = tex.GetPixel(x, y);
                        Color tintedColor = new Color(
                            pixelColor.r * tintColor.r,
                            pixelColor.g * tintColor.g,
                            pixelColor.b * tintColor.b,
                            pixelColor.a
                        );
                        tex.SetPixel(x, y, tintedColor);
                    }
                }
            });
        }

        #endregion

        #region Transformations

        public TextureElement Crop(Rect rect)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;

                if (rect.width + rect.x > width || rect.height + rect.y > height)
                    throw new ArgumentException("The provided rect is out of bounds.");

                Texture2D newTex = new Texture2D((int)rect.width, (int)rect.height);
                for (int x = 0; x < rect.width; x++)
                {
                    for (int y = 0; y < rect.height; y++)
                    {
                        newTex.SetPixel(x, y, tex.GetPixel((int)(x + rect.x), (int)(y + rect.y)));
                    }
                }

                newTex.Apply();
                _texture = newTex;
            });
        }

        public TextureElement Wave(float amplitude, float frequency)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;
                Color[] pixels = tex.GetPixels();
                Color[] newPixels = new Color[pixels.Length];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float offset = amplitude * Mathf.Sin(2 * Mathf.PI * frequency * y / height);
                        int newX = Mathf.Clamp((int)(x + offset), 0, width - 1);
                        newPixels[y * width + x] = pixels[y * width + newX];
                    }
                }

                tex.SetPixels(newPixels);
            });
        }

        public TextureElement Swirl(float strength)
        {
            return AddOperation(tex =>
            {
                int width = tex.width;
                int height = tex.height;
                float centerX = width / 2.0f;
                float centerY = height / 2.0f;
                Color[] pixels = tex.GetPixels();
                Color[] newPixels = new Color[pixels.Length];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float dx = x - centerX;
                        float dy = y - centerY;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        float angle = Mathf.Atan2(dy, dx);
                        angle += strength * distance;
                        float newX = centerX + distance * Mathf.Cos(angle);
                        float newY = centerY + distance * Mathf.Sin(angle);

                        int px = Mathf.Clamp((int)newX, 0, width - 1);
                        int py = Mathf.Clamp((int)newY, 0, height - 1);
                        newPixels[y * width + x] = pixels[py * width + px];
                    }
                }

                tex.SetPixels(newPixels);
            });
        }

        #endregion

        #region Blend Operations

        public TextureElement Blend(TextureElement other, BlendMode blendMode = BlendMode.Normal)
        {
            return AddOperation(tex =>
            {
                Texture2D otherTex = other.GetTexture();

                if (tex.width != otherTex.width || tex.height != otherTex.height)
                    throw new ArgumentException("Both textures must have the same size for blending.");

                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color baseColor = tex.GetPixel(x, y);
                        Color blendColor = otherTex.GetPixel(x, y);
                        Color resultColor;

                        switch (blendMode)
                        {
                            case BlendMode.Add:
                                resultColor = baseColor + blendColor;
                                break;
                            case BlendMode.Subtract:
                                resultColor = baseColor - blendColor;
                                break;
                            case BlendMode.Multiply:
                                resultColor = baseColor * blendColor;
                                break;
                            case BlendMode.Screen:
                                resultColor = new Color(
                                    1 - (1 - baseColor.r) * (1 - blendColor.r),
                                    1 - (1 - baseColor.g) * (1 - blendColor.g),
                                    1 - (1 - baseColor.b) * (1 - blendColor.b),
                                    baseColor.a
                                );
                                break;
                            case BlendMode.Overlay:
                                resultColor = new Color(
                                    (baseColor.r <= 0.5f)
                                        ? (2 * baseColor.r * blendColor.r)
                                        : (1 - 2 * (1 - baseColor.r) * (1 - blendColor.r)),
                                    (baseColor.g <= 0.5f)
                                        ? (2 * baseColor.g * blendColor.g)
                                        : (1 - 2 * (1 - baseColor.g) * (1 - blendColor.g)),
                                    (baseColor.b <= 0.5f)
                                        ? (2 * baseColor.b * blendColor.b)
                                        : (1 - 2 * (1 - baseColor.b) * (1 - blendColor.b)),
                                    baseColor.a
                                );
                                break;
                            case BlendMode.Normal:
                            default:
                                resultColor = Color.Lerp(baseColor, blendColor, blendColor.a);
                                break;
                        }

                        tex.SetPixel(x, y, resultColor);
                    }
                }
            });
        }

        /// <summary>
        /// Applies blend modes between textures.
        /// </summary>
        public TextureElement MixBlendMode(Texture2D overlayTexture, MixBlendModeType blendMode)
        {
            return AddOperation(tex =>
            {
                if (tex.width != overlayTexture.width || tex.height != overlayTexture.height)
                {
                    Debug.LogWarning("Blend textures should have the same dimensions for best results");
                }

                int width = tex.width;
                int height = tex.height;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Color baseColor = tex.GetPixel(x, y);

                        Color overlayColor;
                        if (x < overlayTexture.width && y < overlayTexture.height)
                        {
                            overlayColor = overlayTexture.GetPixel(x, y);
                        }
                        else
                        {
                            overlayColor = Color.clear;
                        }

                        if (overlayColor.a <= 0)
                            continue;

                        Color resultColor = baseColor;

                        switch (blendMode)
                        {
                            case MixBlendModeType.Normal:
                                resultColor = Color.Lerp(baseColor, overlayColor, overlayColor.a);
                                break;

                            case MixBlendModeType.Multiply:
                                resultColor = Color.Lerp(baseColor, baseColor * overlayColor, overlayColor.a);
                                break;

                            case MixBlendModeType.Screen:
                                Color screenColor = new Color(
                                    1 - (1 - baseColor.r) * (1 - overlayColor.r),
                                    1 - (1 - baseColor.g) * (1 - overlayColor.g),
                                    1 - (1 - baseColor.b) * (1 - overlayColor.b),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, screenColor, overlayColor.a);
                                break;

                            case MixBlendModeType.Overlay:
                                Color overlayResult = new Color(
                                    (baseColor.r <= 0.5f)
                                        ? (2 * baseColor.r * overlayColor.r)
                                        : (1 - 2 * (1 - baseColor.r) * (1 - overlayColor.r)),
                                    (baseColor.g <= 0.5f)
                                        ? (2 * baseColor.g * overlayColor.g)
                                        : (1 - 2 * (1 - baseColor.g) * (1 - overlayColor.g)),
                                    (baseColor.b <= 0.5f)
                                        ? (2 * baseColor.b * overlayColor.b)
                                        : (1 - 2 * (1 - baseColor.b) * (1 - overlayColor.b)),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, overlayResult, overlayColor.a);
                                break;

                            case MixBlendModeType.Darken:
                                Color darkenColor = new Color(
                                    Mathf.Min(baseColor.r, overlayColor.r),
                                    Mathf.Min(baseColor.g, overlayColor.g),
                                    Mathf.Min(baseColor.b, overlayColor.b),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, darkenColor, overlayColor.a);
                                break;

                            case MixBlendModeType.Lighten:
                                Color lightenColor = new Color(
                                    Mathf.Max(baseColor.r, overlayColor.r),
                                    Mathf.Max(baseColor.g, overlayColor.g),
                                    Mathf.Max(baseColor.b, overlayColor.b),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, lightenColor, overlayColor.a);
                                break;

                            case MixBlendModeType.ColorDodge:
                                Color dodgeColor = new Color(
                                    (overlayColor.r == 1) ? 1 : Mathf.Min(1, baseColor.r / (1 - overlayColor.r)),
                                    (overlayColor.g == 1) ? 1 : Mathf.Min(1, baseColor.g / (1 - overlayColor.g)),
                                    (overlayColor.b == 1) ? 1 : Mathf.Min(1, baseColor.b / (1 - overlayColor.b)),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, dodgeColor, overlayColor.a);
                                break;

                            case MixBlendModeType.ColorBurn:
                                Color burnColor = new Color(
                                    (overlayColor.r == 0) ? 0 : 1 - Mathf.Min(1, (1 - baseColor.r) / overlayColor.r),
                                    (overlayColor.g == 0) ? 0 : 1 - Mathf.Min(1, (1 - baseColor.g) / overlayColor.g),
                                    (overlayColor.b == 0) ? 0 : 1 - Mathf.Min(1, (1 - baseColor.b) / overlayColor.b),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, burnColor, overlayColor.a);
                                break;

                            case MixBlendModeType.HardLight:
                                Color hardLightColor = new Color(
                                    (overlayColor.r <= 0.5f)
                                        ? (2 * overlayColor.r * baseColor.r)
                                        : (1 - 2 * (1 - overlayColor.r) * (1 - baseColor.r)),
                                    (overlayColor.g <= 0.5f)
                                        ? (2 * overlayColor.g * baseColor.g)
                                        : (1 - 2 * (1 - overlayColor.g) * (1 - baseColor.g)),
                                    (overlayColor.b <= 0.5f)
                                        ? (2 * overlayColor.b * baseColor.b)
                                        : (1 - 2 * (1 - overlayColor.b) * (1 - baseColor.b)),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, hardLightColor, overlayColor.a);
                                break;

                            case MixBlendModeType.Difference:
                                Color diffColor = new Color(
                                    Mathf.Abs(baseColor.r - overlayColor.r),
                                    Mathf.Abs(baseColor.g - overlayColor.g),
                                    Mathf.Abs(baseColor.b - overlayColor.b),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, diffColor, overlayColor.a);
                                break;

                            case MixBlendModeType.Exclusion:
                                Color exclColor = new Color(
                                    baseColor.r + overlayColor.r - 2 * baseColor.r * overlayColor.r,
                                    baseColor.g + overlayColor.g - 2 * baseColor.g * overlayColor.g,
                                    baseColor.b + overlayColor.b - 2 * baseColor.b * overlayColor.b,
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, exclColor, overlayColor.a);
                                break;
                        }

                        tex.SetPixel(x, y, resultColor);
                    }
                }
            });
        }

        /// <summary>
        /// Applies text or icon to a texture.
        /// </summary>
        public TextureElement DrawText(string text, int fontSize, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, Font font = null)
        {
            return AddOperation(tex =>
            {
                RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height);

                GameObject go = new GameObject("TempTextObject");
                go.transform.position = Vector3.zero;

                TextMesh textMesh = go.AddComponent<TextMesh>();
                textMesh.text = text;
                textMesh.fontSize = fontSize;
                textMesh.color = color;
                textMesh.alignment = (TextAlignment)alignment;
                textMesh.anchor = TextAnchor.MiddleCenter;

                if (font != null)
                    textMesh.font = font;

                GameObject cameraObj = new GameObject("TempCamera");
                Camera camera = cameraObj.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = tex.height / 2f;
                camera.transform.position = new Vector3(tex.width / 2f, tex.height / 2f, -10);
                camera.clearFlags = CameraClearFlags.Depth;
                camera.targetTexture = rt;

                go.transform.position = new Vector3(tex.width / 2f, tex.height / 2f, 0);

                camera.Render();

                RenderTexture.active = rt;
                Texture2D tempTex = new Texture2D(tex.width, tex.height);
                tempTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                tempTex.Apply();
                RenderTexture.active = null;

                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        Color textColor = tempTex.GetPixel(x, y);
                        if (textColor.a > 0.01f)
                        {
                            tex.SetPixel(x, y, textColor);
                        }
                    }
                }

                RenderTexture.ReleaseTemporary(rt);
                GameObject.Destroy(go);
                GameObject.Destroy(cameraObj);
            });
        }

        /// <summary>
        /// Checks if a point is inside a polygon defined by vertices.
        /// </summary>
        private bool IsPointInPolygon(Vector2 point, Vector2[] vertices)
        {
            int j = vertices.Length - 1;
            bool inside = false;

            for (int i = 0; i < vertices.Length; j = i++)
            {
                if (((vertices[i].y > point.y) != (vertices[j].y > point.y)) &&
                    (point.x < (vertices[j].x - vertices[i].x) * (point.y - vertices[i].y) /
                        (vertices[j].y - vertices[i].y) + vertices[i].x))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        #endregion

        #region Private Helpers

        private static void ApplyBorderTopRight(Texture2D texture, int distance, int aliasDistance)
        {
            if (distance <= texture.width && distance <= texture.height)
            {
                var point = new Vector2(texture.width - distance, texture.height - distance);
                for (int x = texture.width - distance; x < texture.width; x++)
                {
                    for (int y = texture.height - distance; y < texture.height; y++)
                    {
                        float dist = Vector2.Distance(point, new Vector2(x, y));
                        if (dist >= distance - aliasDistance && dist <= distance + aliasDistance)
                        {
                            float n = Mathf.Clamp01(1 -
                                                    Mathf.Pow((dist - (distance - aliasDistance)) / (2 * aliasDistance),
                                                        2));
                            Color pixelColor = texture.GetPixel(x, y);
                            pixelColor.a *= n;
                            texture.SetPixel(x, y, pixelColor);
                        }
                        else if (dist > distance + aliasDistance)
                        {
                            texture.SetPixel(x, y, Color.clear);
                        }
                    }
                }
            }
        }

        private static void ApplyBorderTopLeft(Texture2D texture, int distance, int aliasDistance)
        {
            if (distance <= texture.width && distance <= texture.height)
            {
                var point = new Vector2(distance, texture.height - distance);
                for (int x = 0; x < distance; x++)
                {
                    for (int y = texture.height - distance; y < texture.height; y++)
                    {
                        float dist = Vector2.Distance(point, new Vector2(x, y));
                        if (dist >= distance - aliasDistance && dist <= distance + aliasDistance)
                        {
                            float alpha =
                                Mathf.Clamp01(1 - Mathf.Pow((dist - (distance - aliasDistance)) / (2 * aliasDistance),
                                    2));
                            Color pixelColor = texture.GetPixel(x, y);
                            pixelColor.a *= alpha;
                            texture.SetPixel(x, y, pixelColor);
                        }
                        else if (dist > distance + aliasDistance)
                        {
                            texture.SetPixel(x, y, Color.clear);
                        }
                    }
                }
            }
        }

        private static void ApplyBorderBottomRight(Texture2D texture, int distance, int aliasDistance)
        {
            if (distance <= texture.width && distance <= texture.height)
            {
                var point = new Vector2(texture.width - distance, distance);
                for (int x = texture.width - distance; x < texture.width; x++)
                {
                    for (int y = 0; y < distance; y++)
                    {
                        float dist = Vector2.Distance(point, new Vector2(x, y));
                        if (dist >= distance - aliasDistance && dist <= distance + aliasDistance)
                        {
                            float alpha =
                                Mathf.Clamp01(1 - Mathf.Pow((dist - (distance - aliasDistance)) / (2 * aliasDistance),
                                    2));
                            Color pixelColor = texture.GetPixel(x, y);
                            pixelColor.a *= alpha;
                            texture.SetPixel(x, y, pixelColor);
                        }
                        else if (dist > distance + aliasDistance)
                        {
                            texture.SetPixel(x, y, Color.clear);
                        }
                    }
                }
            }
        }

        private static void ApplyBorderBottomLeft(Texture2D texture, int distance, int aliasDistance)
        {
            if (distance <= texture.width && distance <= texture.height)
            {
                var point = new Vector2(distance, distance);
                for (int x = 0; x < distance; x++)
                {
                    for (int y = 0; y < distance; y++)
                    {
                        float dist = Vector2.Distance(point, new Vector2(x, y));
                        if (dist >= distance - aliasDistance && dist <= distance + aliasDistance)
                        {
                            float alpha =
                                Mathf.Clamp01(1 - Mathf.Pow((dist - (distance - aliasDistance)) / (2 * aliasDistance),
                                    2));
                            Color pixelColor = texture.GetPixel(x, y);
                            pixelColor.a *= alpha;
                            texture.SetPixel(x, y, pixelColor);
                        }
                        else if (dist > distance + aliasDistance)
                        {
                            texture.SetPixel(x, y, Color.clear);
                        }
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Factory class for creating and loading textures.
    /// </summary>
    public static class TextureFactory
    {
        private static string _combinedPath;

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
            if (!Directory.Exists("AutumnTextures"))
                Directory.CreateDirectory("AutumnTextures");

            _combinedPath = Path.Combine("AutumnTextures", filepath);
            if (!File.Exists(_combinedPath))
            {
                var texture2D = onNotFound(filepath);
                return new TextureElement(texture2D);
            }

            var data = new Texture2D(1, 1);
            data.LoadImage(File.ReadAllBytes(_combinedPath));
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
        public static TextureElement SolidColor(int width, int height, Color color)
        {
            var element = Create(width, height);
            return element.BackgroundColor(color);
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

    [Flags]
    public enum BorderType
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomLeft = 4,
        BottomRight = 8,
        All = 15
    }

    public enum BlendMode
    {
        Normal,
        Add,
        Subtract,
        Multiply,
        Screen,
        Overlay
    }

    public enum BackgroundRepeatMode
    {
        Repeat,
        RepeatX,
        RepeatY,
        NoRepeat
    }

    public enum ClipShapeType
    {
        Circle,
        Ellipse,
        Polygon,
        Inset
    }

    public enum BorderDrawMode
    {
        Inside,
        Outside,
        Center
    }

    public enum MixBlendModeType
    {
        Normal,
        Multiply,
        Screen,
        Overlay,
        Darken,
        Lighten,
        ColorDodge,
        ColorBurn,
        HardLight,
        Difference,
        Exclusion
    }
}