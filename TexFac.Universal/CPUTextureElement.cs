using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Collections;

namespace TexFac.Universal
{
    /// <summary>
    /// TextureElement represents a texture with its associated operations.
    /// You might wanna use a Structure View for this, to find the correct methods.
    /// </summary>
    public class CPUTextureElement : ITextureElement
    {
        internal Texture2D _texture;
        private bool _isDirty = false;
        private List<Action<Texture2D>> _pendingOperations = new List<Action<Texture2D>>();

        public static implicit operator Texture2D(CPUTextureElement r)
        {
            return r.GetTexture();
        }

        public int Width => _texture.width;
        public int Height => _texture.height;

        public CPUTextureElement(Texture2D texture)
        {
            _texture = texture;
        }

        public CPUTextureElement(int width, int height, TextureFormat format = TextureFormat.RGBA32)
        {
            _texture = new Texture2D(width, height, format, false);
            
        }

        /// <summary>
        /// Applies all pending operations to the texture.
        /// </summary>
        public ITextureElement Apply()
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

        ITextureElement ITextureElement.BackgroundColor(byte r, byte g, byte b, byte a)
        {
            return BackgroundColor(r, g, b, a);
        }
        ITextureElement ITextureElement.AddOperation(Action<Texture2D> operation)
        {
            return AddOperation(operation);
        }
        /// <summary>
        /// Adds an operation to the pending operations queue.
        /// </summary>
        public CPUTextureElement AddOperation(Action<Texture2D> operation)
        {
            _pendingOperations.Add(operation);
            _isDirty = true;
            return this;
        }

        /// <summary>
        /// Saves the texture to a file.
        /// </summary>
        public ITextureElement Save(string filepath, ImageType imageType = ImageType.PNG)
        {
            Apply();

            File.WriteAllBytes(filepath, GetBytes(imageType));

            return this;
        }
        
        public byte[] GetBytes(ImageType imageType = ImageType.PNG)
        {
            Apply();

            switch (imageType)
            {
                case ImageType.PNG:
                    return _texture.EncodeToPNG();
                case ImageType.JPEG:
                    return _texture.EncodeToJPG();
                case ImageType.EXR:
                    return _texture.EncodeToEXR();
                default:
                    throw new ArgumentOutOfRangeException(nameof(imageType), imageType, null);
            }
        }
        
        public Texture2D LoadScreen()
        {
            RenderTexture currentActiveRT = RenderTexture.active;
        
            RenderTexture renderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
        
            Camera.main.targetTexture = renderTexture;
        
            Camera.main.Render();
        
            RenderTexture.active = renderTexture;
        
            Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
            screenshot.Apply();
        
            Camera.main.targetTexture = null;
            RenderTexture.active = currentActiveRT;
        
            RenderTexture.ReleaseTemporary(renderTexture);

            _texture = screenshot;
        
            return this;
        }

        #region Styling Properties

        public ITextureElement BackgroundGradient(Color32 color1, Color32 color2, float angle)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Fast fill with a specific color.
        /// </summary>
        public unsafe ITextureElement BackgroundColor(byte r, byte g, byte b, byte a)
        {
            return AddOperation(tex =>
            {
                if (!TextureFormatHandler.IsFormatSupported(tex.format)) return;
                var handler = TextureFormatHandler.GetHandler(tex.format);
                var data = tex.GetRawTextureData();
                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var ptr = handle.AddrOfPinnedObject();
                var pData = (byte*)ptr.ToPointer();
                int i;
                for (i = 0; i < tex.width * tex.height - 8; i += 8)
                {
                    handler.SetPixel(pData, i, r, g, b, a);
                    handler.SetPixel(pData, i + 1, r, g, b, a);
                    handler.SetPixel(pData, i + 2, r, g, b, a);
                    handler.SetPixel(pData, i + 3, r, g, b, a);
                    handler.SetPixel(pData, i + 4, r, g, b, a);
                    handler.SetPixel(pData, i + 5, r, g, b, a);
                    handler.SetPixel(pData, i + 6, r, g, b, a);
                    handler.SetPixel(pData, i + 7, r, g, b, a);
                }
                
                for (; i < tex.width * tex.height; i++)
                {
                    handler.SetPixel(pData, i, r, g, b, a);
                }

                handle.Free();
            });
        }

        /// <summary>
        /// Creates a gradient background.
        /// </summary>
        public unsafe ITextureElement BackgroundGradient(Color startColor, Color endColor, float angle = 0)
        {
            return AddOperation(tex =>
            {
                if (!TextureFormatHandler.IsFormatSupported(tex.format)) return;

                var handler = TextureFormatHandler.GetHandler(tex.format);
                var data = tex.GetRawTextureData();
                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var ptr = handle.AddrOfPinnedObject();
                var pData = (byte*)ptr.ToPointer();

                float rad = angle * Mathf.Deg2Rad;
                float dirX = Mathf.Cos(rad);
                float dirY = Mathf.Sin(rad);

                int width = tex.width;
                int height = tex.height;

                float normFactor = 0.5f / Mathf.Sqrt(width * width + height * height);
                float startR = startColor.r, startG = startColor.g, startB = startColor.b, startA = startColor.a;
                float diffR = endColor.r - startR;
                float diffG = endColor.g - startG;
                float diffB = endColor.b - startB;
                float diffA = endColor.a - startA;

                for (int yIndex = 0; yIndex < height; yIndex++)
                {
                    float yContrib = dirY * yIndex;

                    for (int xIndex = 0; xIndex < width; xIndex++)
                    {
                        float t = (dirX * xIndex + yContrib) * normFactor + 0.5f;
                        t = t < 0 ? 0 : t > 1 ? 1 : t;
                        byte r = (byte)(startR + diffR * t);
                        byte g = (byte)(startG + diffG * t);
                        byte b = (byte)(startB + diffB * t);
                        byte a = (byte)(startA + diffA * t);
                        handler.SetPixel(pData, xIndex, r, g, b, a);
                    }
                }
                handle.Free();
            });
        }

        /// <summary>
        /// Creates a radial gradient background.
        /// </summary>
        public ITextureElement BackgroundRadialGradient(Color centerColor, Color outerColor, Vector2? center = null)
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
        public ITextureElement BackgroundPattern(Texture2D patternTexture,
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
        public ITextureElement BorderRadius(int radius, BorderType borderType = BorderType.All, int aliasDistance = 0)
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
        public ITextureElement Border(int width, Color color, BorderDrawMode drawMode = BorderDrawMode.Outside)
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
        public ITextureElement Opacity(float value)
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
        public ITextureElement BoxShadow(int offsetX, int offsetY, int blurRadius, Color shadowColor)
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

        public ITextureElement Scale(float size, FilterMode filterMode = FilterMode.Bilinear)
        {
            return Scale((int)(Width * size), (int)(Height * size), filterMode);
        }

        /// <summary>
        /// Scales the texture to the specified dimensions.
        /// </summary>
        public ITextureElement Scale(int newWidth, int newHeight, FilterMode filterMode = FilterMode.Bilinear)
        {
            return AddOperation(tex =>
            {
                if (newWidth == -1 && newHeight == -1)
                    return;
                
                float aspect = tex.width / tex.height;
                if (newWidth == -1)
                {
                    newWidth = (int)(newHeight * aspect);
                }

                if (newHeight == -1)
                {
                    newHeight = (int)(newWidth / aspect);
                }
                
                Texture2D newTex = new Texture2D(newWidth, newHeight);

                FilterMode originalFilterMode = tex.filterMode;
                tex.filterMode = filterMode;
                
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

        public unsafe ITextureElement RotateUnsafe(float angle)
        {
            return AddOperation(tex =>
            {
                // Normalize angle
                angle %= 360;
                if (angle < 0) angle += 360;
                float radians = angle * Mathf.Deg2Rad;

                int width = tex.width;
                int height = tex.height;
                int newWidth = width;
                int newHeight = height;


                Texture2D newTex = new Texture2D(newWidth, newHeight, tex.format, false);

                var srcData = tex.GetRawTextureData();
                var dstData = newTex.GetRawTextureData();
                GCHandle handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned);
                GCHandle handleDst = GCHandle.Alloc(dstData, GCHandleType.Pinned);
                var srcptr = handleSrc.AddrOfPinnedObject();
                var dstptr = handleDst.AddrOfPinnedObject();
                byte* srcPtr = (byte*)srcptr.ToPointer();
                byte* dstPtr = (byte*)dstptr.ToPointer();


                uint clearColor = 0;

                Vector2 center = new Vector2(width / 2f, height / 2f);
                float cosAngle = Mathf.Cos(-radians);
                float sinAngle = Mathf.Sin(-radians);

                for (int y = 0; y < newHeight; y++)
                {
                    float yOffset = y - center.y;

                    for (int x = 0; x < newWidth; x++)
                    {
                        float xOffset = x - center.x;

                        float rotX = xOffset * cosAngle - yOffset * sinAngle;
                        float rotY = xOffset * sinAngle + yOffset * cosAngle;

                        int origX = Mathf.RoundToInt(rotX + center.x);
                        int origY = Mathf.RoundToInt(rotY + center.y);

                        if (origX >= 0 && origX < width && origY >= 0 && origY < height)
                        {
                            dstPtr[y * newWidth + x] = srcPtr[origY * width + origX];
                        }
                    }
                }

                newTex.Apply();
                handleDst.Free();
                handleSrc.Free();
                _texture = newTex;
            });
        }

        /// <summary>
        /// Rotates the texture by the specified angle in degrees.
        /// </summary>
        public ITextureElement Rotate(float angle, bool resizeCanvas = true)
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
        public ITextureElement ClipPath(ClipShapeType shapeType, params float[] parameters)
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
        public ITextureElement Mask(Texture2D maskTexture)
        {
            return AddOperation(tex =>
            {
                if (tex.width != maskTexture.width || tex.height != maskTexture.height)
                {
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
        public ITextureElement Brightness(float value)
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
        public unsafe ITextureElement Noise(float intensity, bool monochrome = true)
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
        public ITextureElement Sharpen(float intensity = 1.0f)
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
        public ITextureElement Posterize(int levels)
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
        /// Reduces the number of colors.
        /// </summary>
        public unsafe ITextureElement PosterizeUnsafe(int levels)
        {
            return AddOperation(tex =>
            {
                if (TextureFormatHandler.IsFormatSupported(tex.format))
                {
                    levels = Mathf.Max(2, levels);
                    var handler = TextureFormatHandler.GetHandler(tex.format);
                    var srcData = tex.GetRawTextureData();
                    GCHandle handle = GCHandle.Alloc(srcData, GCHandleType.Pinned);
                    var ptr = handle.AddrOfPinnedObject();
                    var pData = (byte*)ptr.ToPointer();

                    float scaleFactor = 255f / (levels - 1);
                    for (int x = 0; x < tex.width * tex.height; x += 4)
                    {
                        handler.GetPixel(pData, x, out byte r, out byte g, out byte b, out byte a);
                        r = (byte)(Mathf.Round(r / scaleFactor) * scaleFactor);
                        g = (byte)(Mathf.Round(g / scaleFactor) * scaleFactor);
                        b = (byte)(Mathf.Round(b / scaleFactor) * scaleFactor);
                        handler.SetPixel(pData, x, r, g, b, a);

                        handler.GetPixel(pData, x + 1, out r, out g, out b, out a);
                        r = (byte)(Mathf.Round(r / scaleFactor) * scaleFactor);
                        g = (byte)(Mathf.Round(g / scaleFactor) * scaleFactor);
                        b = (byte)(Mathf.Round(b / scaleFactor) * scaleFactor);
                        handler.SetPixel(pData, x + 1, r, g, b, a);

                        handler.GetPixel(pData, x + 2, out r, out g, out b, out a);
                        r = (byte)(Mathf.Round(r / scaleFactor) * scaleFactor);
                        g = (byte)(Mathf.Round(g / scaleFactor) * scaleFactor);
                        b = (byte)(Mathf.Round(b / scaleFactor) * scaleFactor);
                        handler.SetPixel(pData, x + 2, r, g, b, a);

                        handler.GetPixel(pData, x + 3, out r, out g, out b, out a);
                        r = (byte)(Mathf.Round(r / scaleFactor) * scaleFactor);
                        g = (byte)(Mathf.Round(g / scaleFactor) * scaleFactor);
                        b = (byte)(Mathf.Round(b / scaleFactor) * scaleFactor);
                        handler.SetPixel(pData, x + 3, r, g, b, a);
                    }
                    handle.Free();
                }
                else
                {
                    Posterize(levels);
                }
            });
        }


        /// <summary>
        /// Applies a threshold filter, converting pixels to black or white based on their brightness.
        /// </summary>
        public ITextureElement Threshold(float threshold)
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
        public ITextureElement GaussianBlur(int radius, float sigma = 1.0f)
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

        public ITextureElement Contrast(float value)
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

        public ITextureElement Saturation(float value)
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

        public ITextureElement Hue(float value)
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

        public ITextureElement Grayscale()
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

        public ITextureElement Invert()
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

        public ITextureElement Sepia()
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

        public ITextureElement Blur(int radius)
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

        public ITextureElement Pixelate(int pixelSize)
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

        public ITextureElement EdgeDetection()
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

        public ITextureElement Vignette(float strength)
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

        public ITextureElement ColorTint(Color tintColor)
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

        public ITextureElement Crop(Rect rect)
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

        public ITextureElement Translate(Vector2 offset, bool expand = false)
        {
            int width = _texture.width;
            int height = _texture.height;
            if (expand)
            {
                width = (int)Mathf.Max(width, offset.x + width);
                height = (int)Mathf.Max(height, offset.y + height);

                Texture2D newTex = new Texture2D(width, height);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        newTex.SetPixel(x, y, _texture.GetPixel((int)(x + offset.x), (int)(y + offset.y)));
                    }
                }

                newTex.Apply();
                _texture = newTex;
                return this;
            }

            return AddOperation(tex =>
            {
                for (int x = 0; x < _texture.width; x++)
                {
                    for (int y = 0; y < _texture.height; y++)
                    {
                        if (x + offset.x > 0 && x + offset.x < width && y + offset.y > 0 && y + offset.y < height)
                            _texture.SetPixel(x, y, _texture.GetPixel((int)(x + offset.x), (int)(y + offset.y)));
                        else
                            tex.SetPixel(x, y, Color.clear);
                    }
                }
            });
        }

        public ITextureElement Wave(float amplitude, float frequency)
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

        public ITextureElement Swirl(float strength)
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

        public ITextureElement Blend(ITextureElement other, BlendMode blendMode = BlendMode.Normal)
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
        public ITextureElement MixBlendMode(Texture2D overlayTexture, MixBlendModeType blendMode)
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
        public ITextureElement DrawText(string text, int fontSize, Color color,
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
    /// BorderType represents different styles or types of borders
    /// that can be used for visual rendering or styling purposes.
    /// </summary>
    [Flags]
    public enum BorderType
    {
        /// <summary>
        /// None represents the absence of a border. This value indicates that no border should be applied.
        /// </summary>
        None = 0,

        /// <summary>
        /// TopLeft specifies a border type where the border is aligned to the top-left corner of the container.
        /// It is often used to define positioning or alignment in layouts or visual elements.
        /// </summary>
        TopLeft = 1,

        /// <summary>
        /// TopRight represents the top-right corner position or alignment in a given context.
        /// Typically used to define layouts, positioning, or alignment logic.
        /// </summary>
        TopRight = 2,

        /// <summary>
        /// BottomLeft represents the bottom-left corner or position in a coordinate system or layout.
        /// It may be used to denote alignment or position in various graphical or positional contexts.
        /// </summary>
        BottomLeft = 4,

        /// <summary>
        /// BottomRight represents the bottom-right corner of a bordered area or control.
        /// This enum member can be used to specify or reference operations and styles
        /// associated with the bottom-right border region.
        /// </summary>
        BottomRight = 8,

        /// <summary>
        /// All denotes a border type that applies to all edges or sides.
        /// This can be used to specify uniform behavior or styling across all edges.
        /// </summary>
        All = 15
    }

    /// <summary>
    /// BlendMode represents various modes that determine how two graphical elements,
    /// such as textures or colors, are combined or blended with each other.
    /// </summary>
    public enum BlendMode
    {
        /// <summary>
        /// The Normal blend mode represents standard blending,
        /// where the colors of the top layer are blended with the colors of the bottom layer
        /// without any specialized effects or transformations.
        /// </summary>
        Normal,

        /// <summary>
        /// Add represents an addition operation or functionality.
        /// It is used to perform or manage addition-related processes or tasks.
        /// </summary>
        Add,

        /// <summary>
        /// Subtract provides functionality to perform subtraction operations between numerical values.
        /// This class can handle various data types when subtracting two numbers.
        /// </summary>
        Subtract,

        /// <summary>
        /// Multiply is a blending mode that combines the colors of two layers
        /// by multiplying their corresponding channel values. This results in a
        /// darker image where white has no effect, and darker colors have more influence.
        /// </summary>
        Multiply,

        /// <summary>
        /// Screen represents a display device or surface used to render visual content.
        /// Provides functionality to manage and interact with the screen properties or behaviors.
        /// </summary>
        Screen,

        /// <summary>
        /// Overlay blend mode is used to blend two layers by combining Multiply and Screen blend modes.
        /// It applies the Multiply mode for darker areas and the Screen mode for lighter areas,
        /// resulting in an effect that enhances contrast and preserves highlights/shadows.
        /// </summary>
        Overlay
    }

    /// <summary>
    /// BackgroundRepeatMode specifies the repeat behavior of a background image.
    /// It determines how the image is repeated horizontally and vertically within its container.
    /// </summary>
    public enum BackgroundRepeatMode
    {
        /// <summary>
        /// Specifies that the background image should be repeated both vertically and horizontally
        /// to cover the entire area.
        /// </summary>
        Repeat,

        /// <summary>
        /// RepeatX specifies the horizontal repetition behavior for a texture or element.
        /// It defines how the content will repeat along the X-axis.
        /// </summary>
        RepeatX,

        /// <summary>
        /// Represents a background repeat mode where the background image is repeated
        /// along the vertical (Y) axis only, without repeating horizontally (X axis).
        /// </summary>
        RepeatY,

        /// <summary>
        /// NoRepeat is used to prevent duplication of data or processes.
        /// It ensures unique operations or entries as intended within a given context.
        /// </summary>
        NoRepeat
    }

    /// <summary>
    /// ClipShapeType defines the enumeration of various shapes available for creating clip masks.
    /// It is used to specify the geometric configuration that can be applied to clipping operations within a graphical context.
    /// </summary>
    public enum ClipShapeType
    {
        /// <summary>
        /// Circle defines a circular clip shape type that can be applied to UI components,
        /// enabling the creation of circular clipped regions.
        /// </summary>
        Circle,

        /// <summary>
        /// Ellipse represents a clipping shape used to define an oval or circular region.
        /// This shape can be applied to restrict the rendering area to an elliptical boundary.
        /// </summary>
        Ellipse,

        /// <summary>
        /// Polygon represents a closed geometric shape consisting of multiple straight lines connected in sequence.
        /// Typically used to define multi-sided shapes for various operations or rendering contexts.
        /// </summary>
        Polygon,

        /// <summary>
        /// Inset represents a type of clip shape that defines an inward offset or indentation from the edges.
        /// Commonly used for creating clipped areas with adjusted boundaries within a given region.
        /// </summary>
        Inset
    }

    /// <summary>
    /// BorderDrawMode defines the different modes for drawing borders around an element.
    /// It specifies the style or method in which borders should be rendered to provide visual emphasis or distinction.
    /// </summary>
    public enum BorderDrawMode
    {
        /// <summary>
        /// Inside specifies that the border should be drawn inside the boundaries of the element.
        /// It ensures the border is contained fully within the element's dimensions.
        /// </summary>
        Inside,

        /// <summary>
        /// Outside specifies that the border should be drawn on the outside of the element bounds,
        /// extending outward from the element's defined edge.
        /// </summary>
        Outside,

        /// <summary>
        /// Center specifies that the border should be drawn centered on the edge,
        /// distributing the line width equally on both sides of the boundary.
        /// </summary>
        Center
    }

    /// <summary>
    /// ImageType specifies the type of image, which can be used to
    /// classify and identify the format or purpose of an image within a system.
    /// </summary>
    public enum ImageType
    {
        PNG,

        /// <summary>
        /// JPEG represents the JPEG image format, commonly used for photographs and web images.
        /// It provides efficient compression with minimal loss of quality, suitable for various applications.
        /// </summary>
        JPEG,

        /// <summary>
        /// EXR represents an image format used primarily for high dynamic range (HDR) imaging.
        /// It is a common choice for professional graphics and visual effects due to its ability
        /// to store a high level of detail and wide color gamut.
        /// </summary>
        EXR,

        /// <summary>
        /// Represents the TGA (Truevision Graphics Adapter) image format.
        /// This format is commonly used for storing raster graphic data with varying color depths.
        /// </summary>
        TGA
    }

    /// <summary>
    /// MixBlendModeType defines the modes for blending colors or textures in rendering operations.
    /// This enum is typically used to specify how different elements should visually combine on the screen.
    /// </summary>
    public enum MixBlendModeType
    {
        /// <summary>
        /// Normal represents the standard blend mode where the upper layer is drawn
        /// without blending with the layers beneath it.
        /// This mode is commonly used for opaque content where no blending effects are desired.
        /// </summary>
        Normal,

        /// <summary>
        /// Multiply represents a blend mode where the colors of the source and background
        /// are multiplied together, resulting in a darker output. This mode is commonly used
        /// to achieve shading effects in graphics processing.
        /// </summary>
        Multiply,

        /// <summary>
        /// Screen is a blending mode where the colors of the layers are inverted,
        /// multiplied, and then inverted again, resulting in a brighter composition.
        /// It is typically used to lighten the underlying layers by blending them with the top layer.
        /// </summary>
        Screen,

        /// <summary>
        /// Overlay represents a blend mode where the colors of the layered elements
        /// are combined to produce a result based on their interaction. It preserves
        /// the highlights and shadows of the base layer while blending with the top layer.
        /// </summary>
        Overlay,

        /// <summary>
        /// Darken represents a blend mode that selects the darker color by comparing each pixel of the source and destination.
        /// This mode is typically used to create shadows or reduce the brightness of images.
        /// </summary>
        Darken,

        /// <summary>
        /// Lighten is a blend mode type where the output color is determined by
        /// comparing the base and blend colors, and selecting the lighter of the two.
        /// This mode emphasizes lighter areas in overlapping content.
        /// </summary>
        Lighten,

        /// <summary>
        /// Represents a blending mode where the background color is brightened to reflect the foreground color.
        /// This mode divides the foreground color by the inverted background color, resulting in a lighter visual effect.
        /// Commonly used in graphic design and compositing to achieve vibrant and dynamic effects.
        /// </summary>
        ColorDodge,

        /// <summary>
        /// ColorBurn specifies a blending mode where the colors of the blend layer darken
        /// the base layer by increasing the contrast between them, resulting in a more intense effect.
        /// This mode is often used for creating dramatic and high-contrast visuals.
        /// </summary>
        ColorBurn,

        /// <summary>
        /// HardLight represents a blend mode where the result combines Multiply and Screen modes
        /// depending on the values of the source and backdrop colors. It is used to emphasize
        /// highlights and shadows, creating a vivid and vibrant appearance.
        /// </summary>
        HardLight,

        /// <summary>
        /// Difference is a blend mode that subtracts the source color from the destination color
        /// or vice versa to always yield a positive result. It's used to emphasize the differences
        /// between two layers by producing a high-contrast effect.
        /// </summary>
        Difference,

        /// <summary>
        /// Exclusion specifies a blend mode that creates an effect similar to the difference blend mode,
        /// but with lower contrast. It is typically used for achieving softer blending effects between layers.
        /// </summary>
        Exclusion
    }
}