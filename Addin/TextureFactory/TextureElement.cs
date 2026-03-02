using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TexFac
{
    /// <summary>
    /// Represents a texture processing element with a collection of operations for manipulation and transformation.
    /// </
    public class TextureElement
    {
        internal Texture2D _texture;
        public int Width => _texture.width;
        public int Height => _texture.height;
        protected private bool _isDirty = false;
        protected private List<Action<Texture2D>> _pendingOperations = new List<Action<Texture2D>>();

        public static implicit operator Texture2D(TextureElement r)
        {
            return r.GetTexture();
        }

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
        public TextureElement Save(string filepath, ImageType imageType = ImageType.PNG)
        {
            Apply();

            if (filepath.EndsWith(imageType.ToString()))
                filepath += imageType.ToString().ToLower();
            File.WriteAllBytes(filepath, GetBytes(imageType));

            return this;
        }

        private byte[] GetBytes(ImageType imageType = ImageType.PNG)
        {
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

        public Rect GetImageChunkRect(Texture2D texture2D, int chunkId, int chunkCount = 4)
        {
            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));

            if (chunkId < 0 || chunkId >= chunkCount)
                throw new ArgumentOutOfRangeException(nameof(chunkId));

            int cols = Mathf.CeilToInt(Mathf.Sqrt(chunkCount));
            int rows = Mathf.CeilToInt((float)chunkCount / cols);

            float chunkWidth = (float)texture2D.width / cols;
            float chunkHeight = (float)texture2D.height / rows;

            int col = chunkId % cols;
            int row = chunkId / cols;

            return new Rect(col * chunkWidth, row * chunkHeight, chunkWidth, chunkHeight);
        }

        public Texture2D GetImageChunk(Texture2D texture2D, int chunkId, int chunkCount = 4)
        {
            if (texture2D == null)
                throw new ArgumentNullException(nameof(texture2D));
            if (chunkId < 0 || chunkId >= chunkCount)
                throw new ArgumentOutOfRangeException(nameof(chunkId));

            var imageChunkData = GetImageChunkRect(texture2D, chunkId, chunkCount);
            var imageChunk = new Texture2D((int)imageChunkData.width, (int)imageChunkData.height, TextureFormat.ARGB32, false);
            var pixels = texture2D.GetPixels((int)imageChunkData.x, (int)imageChunkData.y, (int)imageChunkData.width, (int)imageChunkData.height);
            imageChunk.SetPixels(pixels);
            imageChunk.Apply();
            return imageChunk;
        }

        #region Styling Properties

        /// <summary>
        /// Fast fill with a specific color.
        /// </summary>
        public unsafe TextureElement BackgroundColor(byte r, byte g, byte b, byte a)
        {
            return AddOperation(tex =>
            {
                if (!TextureFormatHandler.IsFormatSupported(tex.format))
                    return;
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

                tex.LoadRawTextureData(ptr, tex.width * tex.height * 4);
            });
        }

        /// <summary>
        /// Creates a gradient background.
        /// </summary>
        public unsafe TextureElement BackgroundGradient(Color startColor, Color endColor, float angle = 0)
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
                
                
                tex.LoadRawTextureData(ptr, tex.width * tex.height * 4);
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
        public TextureElement BorderRadius(int radius, BorderType borderType = BorderType.All, float aliasDistance = 0)
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
        /// Increases or decreases the canvas area from the anchor position while keeping the inner texture intact
        /// </summary>
        /// <param name="width">The width of the canvas to be added.</param>
        /// <param name="height">The height of the canvas to be added.</param>
        /// <param name="anchor">The position of the canvas anchor within the texture.</param>
        /// <returns>A <see cref="TextureElement"/> instance with the added canvas applied.</returns>
        public TextureElement Canvas(int width, int height, CanvasPosition anchor)
        {
            return AddOperation(tex =>
            {
                var newTex = new Texture2D(width, height, tex.format, false);
                int offsetX, offsetY;

                for (var i = 0; i < newTex.width; i++)
                {
                    for (var i1 = 0; i1 < newTex.height; i1++)
                    {
                        newTex.SetPixel(i, i1, Color.clear);
                    }
                }

                switch (anchor)
                {
                    case CanvasPosition.TopLeft:
                        offsetX = 0;
                        offsetY = height - tex.height;
                        break;
                    case CanvasPosition.TopCenter:
                        offsetX = (width - tex.width) / 2;
                        offsetY = height - tex.height;
                        break;
                    case CanvasPosition.TopRight:
                        offsetX = width - tex.width;
                        offsetY = height - tex.height;
                        break;
                    case CanvasPosition.MiddleLeft:
                        offsetX = 0;
                        offsetY = (height - tex.height) / 2;
                        break;
                    case CanvasPosition.MiddleCenter:
                        offsetX = (width - tex.width) / 2;
                        offsetY = (height - tex.height) / 2;
                        break;
                    case CanvasPosition.MiddleRight:
                        offsetX = width - tex.width;
                        offsetY = (height - tex.height) / 2;
                        break;
                    case CanvasPosition.BottomLeft:
                        offsetX = 0;
                        offsetY = 0;
                        break;
                    case CanvasPosition.BottomCenter:
                        offsetX = (width - tex.width) / 2;
                        offsetY = 0;
                        break;
                    case CanvasPosition.BottomRight:
                        offsetX = width - tex.width;
                        offsetY = 0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null);
                }

                newTex.SetPixels32(offsetX, offsetY, tex.width, tex.height, tex.GetPixels32());
                newTex.Apply();
                _texture = newTex;
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
            return Scale((int)(Width * size), (int)(Height * size), filterMode);
        }

        /// <summary>
        /// Scales the texture to the specified dimensions.
        /// </summary>
        public TextureElement Scale(int newWidth, int newHeight, FilterMode filterMode = FilterMode.Bilinear)
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

        public unsafe TextureElement RotateUnsafe(float angle)
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
        public unsafe TextureElement Noise(float intensity, bool monochrome = true)
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
        /// Reduces the number of colors.
        /// </summary>
        public unsafe TextureElement PosterizeUnsafe(int levels)
        {
            return AddOperation(tex =>
            {
                if (TextureFormatHandler.IsFormatSupported(tex.format))
                {
                    levels = Mathf.Max(2, levels);
                    var handler = TextureFormatHandler.GetHandler(tex.format);
                    var srcData = tex.GetRawTextureData();
                    GCHandle handle = GCHandle.Alloc(srcData, GCHandleType.Pinned);
                    var pData = (byte*)handle.AddrOfPinnedObject().ToPointer();

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

        [Obsolete("Very slow")]
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

        public TextureElement Translate(Vector2 offset, bool expand = false)
        {
            int width = _texture.width;
            int height = _texture.height;
            if (expand)
            {
                return AddOperation(tex =>
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
                });
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

        /// <summary>
        /// Applies blend modes between textures.
        /// </summary>
        public TextureElement MixBlend(Texture2D overlayTexture, MixBlendType blend)
        {
            return AddOperation(tex =>
            {
                // TODO Kinda bullshit, let's find a better way with even different sizes
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

                        // TODO How fucking drunk was I? That's possibly millions of switch statements. Put for loops in every case.
                        switch (blend)
                        {
                            case MixBlendType.Normal:
                                resultColor = Color.Lerp(baseColor, overlayColor, overlayColor.a);
                                break;

                            case MixBlendType.Multiply:
                                resultColor = Color.Lerp(baseColor, baseColor * overlayColor, overlayColor.a);
                                break;

                            case MixBlendType.Screen:
                                Color screenColor = new Color(
                                    1 - (1 - baseColor.r) * (1 - overlayColor.r),
                                    1 - (1 - baseColor.g) * (1 - overlayColor.g),
                                    1 - (1 - baseColor.b) * (1 - overlayColor.b),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, screenColor, overlayColor.a);
                                break;

                            case MixBlendType.Overlay:
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

                            case MixBlendType.Darken:
                                Color darkenColor = new Color(
                                    Mathf.Min(baseColor.r, overlayColor.r),
                                    Mathf.Min(baseColor.g, overlayColor.g),
                                    Mathf.Min(baseColor.b, overlayColor.b),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, darkenColor, overlayColor.a);
                                break;

                            case MixBlendType.Lighten:
                                Color lightenColor = new Color(
                                    Mathf.Max(baseColor.r, overlayColor.r),
                                    Mathf.Max(baseColor.g, overlayColor.g),
                                    Mathf.Max(baseColor.b, overlayColor.b),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, lightenColor, overlayColor.a);
                                break;

                            case MixBlendType.ColorDodge:
                                Color dodgeColor = new Color(
                                    (overlayColor.r == 1) ? 1 : Mathf.Min(1, baseColor.r / (1 - overlayColor.r)),
                                    (overlayColor.g == 1) ? 1 : Mathf.Min(1, baseColor.g / (1 - overlayColor.g)),
                                    (overlayColor.b == 1) ? 1 : Mathf.Min(1, baseColor.b / (1 - overlayColor.b)),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, dodgeColor, overlayColor.a);
                                break;

                            case MixBlendType.ColorBurn:
                                Color burnColor = new Color(
                                    (overlayColor.r == 0) ? 0 : 1 - Mathf.Min(1, (1 - baseColor.r) / overlayColor.r),
                                    (overlayColor.g == 0) ? 0 : 1 - Mathf.Min(1, (1 - baseColor.g) / overlayColor.g),
                                    (overlayColor.b == 0) ? 0 : 1 - Mathf.Min(1, (1 - baseColor.b) / overlayColor.b),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, burnColor, overlayColor.a);
                                break;

                            case MixBlendType.HardLight:
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

                            case MixBlendType.Difference:
                                Color diffColor = new Color(
                                    Mathf.Abs(baseColor.r - overlayColor.r),
                                    Mathf.Abs(baseColor.g - overlayColor.g),
                                    Mathf.Abs(baseColor.b - overlayColor.b),
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, diffColor, overlayColor.a);
                                break;

                            case MixBlendType.Exclusion:
                                Color exclColor = new Color(
                                    baseColor.r + overlayColor.r - 2 * baseColor.r * overlayColor.r,
                                    baseColor.g + overlayColor.g - 2 * baseColor.g * overlayColor.g,
                                    baseColor.b + overlayColor.b - 2 * baseColor.b * overlayColor.b,
                                    baseColor.a
                                );
                                resultColor = Color.Lerp(baseColor, exclColor, overlayColor.a);
                                break;
                            case MixBlendType.Add:
                                resultColor = baseColor + overlayColor;
                                break;
                            case MixBlendType.Subtract:
                                resultColor = baseColor - overlayColor;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(blend), blend, null);
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
                Object.Destroy(go);
                Object.Destroy(cameraObj);
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

        #region Drawing Operations

        public TextureElement DrawLine(int x0, int y0 , int x1, int y1, Color color, int thickness = 1, bool antialias = true)
        {
            return AddOperation(tex =>
            {
                if (antialias && thickness == 1)
                    DrawLineWu(tex, x0, y0, x1, y1, color);
                else
                    DrawLineBresenham(tex, x0, y0, x1, y1, color, thickness);
            });
        }

        public TextureElement DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1, bool antialias = true)
        {
            return DrawLine((int)start.x, (int)start.y, (int)end.x, (int)end.y, color, thickness, antialias);
        }

        public TextureElement DrawLines(Vector2[] points, Color color, int thickness = 1, bool antialias = true)
        {
            if (points == null || points.Length < 2)
                throw new ArgumentException("DrawLines requires at least 2 points", nameof(points));

            return AddOperation(tex =>
            {
                for (var i = 0; i < points.Length - 1; i++)
                {
                    if (antialias && thickness == 1)
                        DrawLineWu(tex, (int)points[i].x, (int)points[i].y, (int)points[i + 1].x, (int)points[i + 1].y, color);
                    else
                        DrawLineBresenham(tex, (int)points[i].x, (int)points[i].y, (int)points[i + 1].x, (int)points[i + 1].y, color, thickness);
                }
            });
        }

        public TextureElement DrawRectangle(Rect rect, Color color, int thickness = 1)
        {
            return AddOperation(tex =>
            {
                int x = (int)rect.x;
                int y = (int)rect.y;
                int w = (int)rect.width;
                int h = (int)rect.height;

                for (var i = 0; i < thickness; i++)
                {
                    for (int px = x - i; px < x + w + i; px++)
                        SetPixelSafe(tex, px, y - i, color);

                    for (int px = x - i; px < x + w + i; px++)
                        SetPixelSafe(tex, px, y + h - 1 + i, color);

                    for (int py = y - i; py < y + h + i; py++)
                        SetPixelSafe(tex, x - i, py, color);

                    for (int py = y - i; py < y + h + i; py++)
                        SetPixelSafe(tex, x + w - 1 + i, py, color);
                }
            });
        }

        public TextureElement FillRectangle(Rect rect, Color color)
        {
            return AddOperation(tex =>
            {
                int startX = (int)rect.x;
                int startY = (int)rect.y;
                int endX = Mathf.Min(tex.width, (int)(rect.x + rect.width));
                int endY = Mathf.Min(tex.height, (int)(rect.y + rect.height));

                for (int x = startX; x < endX; x++)
                {
                    for (int y = startY; y < endY; y++)
                    {
                        if (color.a < 1f)
                        {
                            Color existing = tex.GetPixel(x, y);
                            tex.SetPixel(x, y, BlendPixel(existing, color));
                        }
                        else
                        {
                            tex.SetPixel(x, y, color);
                        }
                    }
                }
            });
        }

        public TextureElement DrawCircle(int centerX, int centerY, int radius, Color color, int thickness = 1)
        {
            return AddOperation(tex =>
            {
                for (int t = 0; t < thickness; t++)
                {
                    int r = radius + t;
                    DrawCircleMidpoint(tex, centerX, centerY, r, color);
                }
            });
        }

        public TextureElement FillCircle(int centerX, int centerY, int radius, Color color)
        {
            return AddOperation(tex =>
            {
                int rSq = radius * radius;
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        if (x * x + y * y <= rSq)
                        {
                            SetPixelSafe(tex, centerX + x, centerY + y, color);
                        }
                    }
                }
            });
        }

        public TextureElement DrawEllipse(int centerX, int centerY, int radiusX, int radiusY, Color color, int thickness = 1)
        {
            return AddOperation(tex =>
            {
                for (int t = 0; t < thickness; t++)
                {
                    DrawEllipseBresenham(tex, centerX, centerY, radiusX + t, radiusY + t, color);
                }
            });
        }

        public TextureElement FillEllipse(int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            return AddOperation(tex =>
            {
                float rxSq = radiusX * radiusX;
                float rySq = radiusY * radiusY;

                for (int y = -radiusY; y <= radiusY; y++)
                {
                    float normalizedY = (float)(y * y) / rySq;
                    int halfWidth = (int)(radiusX * Mathf.Sqrt(1f - normalizedY));

                    for (int x = -halfWidth; x <= halfWidth; x++)
                    {
                        SetPixelSafe(tex, centerX + x, centerY + y, color);
                    }
                }
            });
        }

        public TextureElement DrawArc(int centerX, int centerY, int radiusX, int radiusY, float startAngle, float sweepAngle, Color color, int thickness = 1)
        {
            return AddOperation(tex =>
            {
                float startRad = startAngle * Mathf.Deg2Rad;
                float sweepRad = sweepAngle * Mathf.Deg2Rad;
                float endRad = startRad + sweepRad;

                int circumference = (int)(Mathf.PI * (3 * (radiusX + radiusY) - Mathf.Sqrt((3 * radiusX + radiusY) * (radiusX + 3 * radiusY))));
                int steps = Mathf.Max(circumference * 2, 360);

                float angleStep = sweepRad / steps;

                for (int t = 0; t < thickness; t++)
                {
                    int rx = radiusX + t;
                    int ry = radiusY + t;

                    float prevAngle = startRad;
                    for (int i = 1; i <= steps; i++)
                    {
                        float angle = startRad + angleStep * i;

                        int x0 = centerX + (int)(Mathf.Cos(prevAngle) * rx);
                        int y0 = centerY + (int)(Mathf.Sin(prevAngle) * ry);
                        int x1 = centerX + (int)(Mathf.Cos(angle) * rx);
                        int y1 = centerY + (int)(Mathf.Sin(angle) * ry);

                        DrawLineBresenham(tex, x0, y0, x1, y1, color, 1);
                        prevAngle = angle;
                    }
                }
            });
        }
        
        public TextureElement FillPie(int centerX, int centerY, int radiusX, int radiusY, float startAngle, float sweepAngle, Color color)
        {
            return AddOperation(tex =>
            {
                float startRad = startAngle * Mathf.Deg2Rad;
                float sweepRad = sweepAngle * Mathf.Deg2Rad;

                float rxSq = radiusX * radiusX;
                float rySq = radiusY * radiusY;

                for (int x = -radiusX; x <= radiusX; x++)
                {
                    for (int y = -radiusY; y <= radiusY; y++)
                    {
                        float normalizedDist = (float)(x * x) / rxSq + (float)(y * y) / rySq;
                        if (normalizedDist > 1f)
                            continue;

                        float angle = Mathf.Atan2(y, x);
                        if (angle < 0) angle += Mathf.PI * 2;

                        float normStart = startRad % (Mathf.PI * 2);
                        if (normStart < 0) normStart += Mathf.PI * 2;
                        float normEnd = normStart + sweepRad;

                        bool inSweep = false;
                        if (sweepRad >= 0)
                        {
                            if (normEnd <= Mathf.PI * 2)
                                inSweep = angle >= normStart && angle <= normEnd;
                            else
                                inSweep = angle >= normStart || angle <= (normEnd - Mathf.PI * 2);
                        }
                        else
                        {
                            float absEnd = normStart + sweepRad;
                            if (absEnd >= 0)
                                inSweep = angle <= normStart && angle >= absEnd;
                            else
                                inSweep = angle <= normStart || angle >= (absEnd + Mathf.PI * 2);
                        }

                        if (inSweep)
                            SetPixelSafe(tex, centerX + x, centerY + y, color);
                    }
                }
            });
        }
        
        public TextureElement DrawPolygon(Vector2[] points, Color color, int thickness = 1)
        {
            if (points == null || points.Length < 3)
                throw new ArgumentException("DrawPolygon requires at least 3 points.", nameof(points));

            return AddOperation(tex =>
            {
                for (int i = 0; i < points.Length; i++)
                {
                    int next = (i + 1) % points.Length;
                    DrawLineBresenham(tex, (int)points[i].x, (int)points[i].y, (int)points[next].x, (int)points[next].y, color, thickness);
                }
            });
        }
        
        public TextureElement FillPolygon(Vector2[] points, Color color)
        {
            if (points == null || points.Length < 3)
                throw new ArgumentException("FillPolygon requires at least 3 points.", nameof(points));

            return AddOperation(tex =>
            {
                float minY = float.MaxValue, maxY = float.MinValue;
                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i].y < minY) minY = points[i].y;
                    if (points[i].y > maxY) maxY = points[i].y;
                }

                int yStart = Mathf.Max(0, (int)minY);
                int yEnd = Mathf.Min(tex.height - 1, (int)maxY);

                List<float> intersections = new List<float>();
                for (int y = yStart; y <= yEnd; y++)
                {
                    intersections.Clear();
                    int j = points.Length - 1;

                    for (int i = 0; i < points.Length; j = i++)
                    {
                        float yi = points[i].y;
                        float yj = points[j].y;

                        if ((yi <= y && yj > y) || (yj <= y && yi > y))
                        {
                            float t = (y - yi) / (yj - yi);
                            intersections.Add(points[i].x + t * (points[j].x - points[i].x));
                        }
                    }

                    intersections.Sort();

                    for (int i = 0; i + 1 < intersections.Count; i += 2)
                    {
                        int xStart = Mathf.Max(0, (int)intersections[i]);
                        int xEnd = Mathf.Min(tex.width - 1, (int)intersections[i + 1]);

                        for (int x = xStart; x <= xEnd; x++)
                        {
                            SetPixelSafe(tex, x, y, color);
                        }
                    }
                }
            });
        }
        
        public TextureElement DrawPath(Vector2[] points, Color color, int thickness = 1, bool antiAlias = false)
        {
            return DrawLines(points, color, thickness, antiAlias);
        }
        
        public TextureElement DrawClosedPath(Vector2[] points, Color color, int thickness = 1)
        {
            if (points == null || points.Length < 2)
                throw new ArgumentException("DrawClosedPath requires at least 2 points.", nameof(points));

            return AddOperation(tex =>
            {
                for (int i = 0; i < points.Length; i++)
                {
                    int next = (i + 1) % points.Length;
                    DrawLineBresenham(tex, (int)points[i].x, (int)points[i].y, (int)points[next].x, (int)points[next].y, color, thickness);
                }
            });
        }
        
        public TextureElement DrawBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color color, int thickness = 1, int segments = 64)
        {
            return AddOperation(tex =>
            {
                Vector2 prev = p0;
                for (int i = 1; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    float u = 1f - t;
                    float tt = t * t;
                    float uu = u * u;
                    float uuu = uu * u;
                    float ttt = tt * t;

                    Vector2 point = uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
                    DrawLineBresenham(tex, (int)prev.x, (int)prev.y, (int)point.x, (int)point.y, color, thickness);
                    prev = point;
                }
            });
        }
        
        public TextureElement DrawQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, Color color, int thickness = 1, int segments = 64)
        {
            return AddOperation(tex =>
            {
                Vector2 prev = p0;
                for (int i = 1; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    float u = 1f - t;
                    Vector2 point = u * u * p0 + 2f * u * t * p1 + t * t * p2;
                    DrawLineBresenham(tex, (int)prev.x, (int)prev.y, (int)point.x, (int)point.y, color, thickness);
                    prev = point;
                }
            });
        }
        
        public TextureElement DrawRoundedRectangle(Rect rect, int radius, Color color, int thickness = 1)
        {
            return AddOperation(tex =>
            {
                int x = (int)rect.x;
                int y = (int)rect.y;
                int w = (int)rect.width;
                int h = (int)rect.height;
                radius = Mathf.Min(radius, Mathf.Min(w / 2, h / 2));

                for (int t = 0; t < thickness; t++)
                {
                    int r = radius;
                    int ox = x - t;
                    int oy = y - t;
                    int ow = w + t * 2;
                    int oh = h + t * 2;

                    // Top edge
                    for (int px = ox + r; px < ox + ow - r; px++)
                        SetPixelSafe(tex, px, oy + oh - 1, color);
                    // Bottom edge
                    for (int px = ox + r; px < ox + ow - r; px++)
                        SetPixelSafe(tex, px, oy, color);
                    // Left edge
                    for (int py = oy + r; py < oy + oh - r; py++)
                        SetPixelSafe(tex, ox, py, color);
                    // Right edge
                    for (int py = oy + r; py < oy + oh - r; py++)
                        SetPixelSafe(tex, ox + ow - 1, py, color);

                    // Corner arcs using midpoint circle
                    DrawCornerArc(tex, ox + r, oy + oh - 1 - r, r, color, 1); // Top-left
                    DrawCornerArc(tex, ox + ow - 1 - r, oy + oh - 1 - r, r, color, 0); // Top-right
                    DrawCornerArc(tex, ox + r, oy + r, r, color, 2); // Bottom-left
                    DrawCornerArc(tex, ox + ow - 1 - r, oy + r, r, color, 3); // Bottom-right
                }
            });
        }
        
        public TextureElement FillRoundedRectangle(Rect rect, int radius, Color color)
        {
            return AddOperation(tex =>
            {
                int rx = (int)rect.x;
                int ry = (int)rect.y;
                int w = (int)rect.width;
                int h = (int)rect.height;
                radius = Mathf.Min(radius, Mathf.Min(w / 2, h / 2));

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        bool inside = true;

                        // Check corners
                        if (x < radius && y < radius) // Bottom-left
                            inside = (x - radius) * (x - radius) + (y - radius) * (y - radius) <= radius * radius;
                        else if (x >= w - radius && y < radius) // Bottom-right
                            inside = (x - (w - 1 - radius)) * (x - (w - 1 - radius)) + (y - radius) * (y - radius) <= radius * radius;
                        else if (x < radius && y >= h - radius) // Top-left
                            inside = (x - radius) * (x - radius) + (y - (h - 1 - radius)) * (y - (h - 1 - radius)) <= radius * radius;
                        else if (x >= w - radius && y >= h - radius) // Top-right
                            inside = (x - (w - 1 - radius)) * (x - (w - 1 - radius)) + (y - (h - 1 - radius)) * (y - (h - 1 - radius)) <= radius * radius;

                        if (inside)
                            SetPixelSafe(tex, rx + x, ry + y, color);
                    }
                }
            });
        }
        
        public TextureElement DrawRegularPolygon(int centerX, int centerY, int radius, int sides, Color color, int thickness = 1, float rotationDegrees = 0f)
        {
            if (sides < 3)
                throw new ArgumentException("A polygon requires at least 3 sides.", nameof(sides));

            return AddOperation(tex =>
            {
                Vector2[] verts = GetRegularPolygonVertices(centerX, centerY, radius, sides, rotationDegrees);
                for (int i = 0; i < verts.Length; i++)
                {
                    int next = (i + 1) % verts.Length;
                    DrawLineBresenham(tex, (int)verts[i].x, (int)verts[i].y, (int)verts[next].x, (int)verts[next].y, color, thickness);
                }
            });
        }
        
        public TextureElement FillRegularPolygon(int centerX, int centerY, int radius, int sides, Color color, float rotationDegrees = 0f)
        {
            if (sides < 3)
                throw new ArgumentException("A polygon requires at least 3 sides.", nameof(sides));

            Vector2[] verts = GetRegularPolygonVertices(centerX, centerY, radius, sides, rotationDegrees);
            return FillPolygon(verts, color);
        }
        
        public TextureElement DrawTriangle(Vector2 a, Vector2 b, Vector2 c, Color color, int thickness = 1)
        {
            return DrawPolygon(new[] { a, b, c }, color, thickness);
        }
        
        public TextureElement FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            return FillPolygon(new[] { a, b, c }, color);
        }
        
        public TextureElement DrawCross(int centerX, int centerY, int size, Color color, int thickness = 1)
        {
            return AddOperation(tex =>
            {
                DrawLineBresenham(tex, centerX - size, centerY, centerX + size, centerY, color, thickness);
                DrawLineBresenham(tex, centerX, centerY - size, centerX, centerY + size, color, thickness);
            });
        }
        
        public TextureElement DrawGrid(int cellWidth, int cellHeight, Color color, int thickness = 1)
        {
            return AddOperation(tex =>
            {
                // Vertical lines
                for (int x = 0; x < tex.width; x += cellWidth)
                {
                    DrawLineBresenham(tex, x, 0, x, tex.height - 1, color, thickness);
                }

                // Horizontal lines
                for (int y = 0; y < tex.height; y += cellHeight)
                {
                    DrawLineBresenham(tex, 0, y, tex.width - 1, y, color, thickness);
                }
            });
        }
        
        public TextureElement FloodFill(int startX, int startY, Color fillColor, float tolerance = 0.01f)
        {
            return AddOperation(tex =>
            {
                if (startX < 0 || startX >= tex.width || startY < 0 || startY >= tex.height)
                    return;

                Color targetColor = tex.GetPixel(startX, startY);
                if (ColorsMatch(targetColor, fillColor, tolerance))
                    return;

                Stack<Vector2Int> stack = new Stack<Vector2Int>();
                stack.Push(new Vector2Int(startX, startY));

                bool[,] visited = new bool[tex.width, tex.height];

                while (stack.Count > 0)
                {
                    Vector2Int pos = stack.Pop();
                    int x = pos.x;
                    int y = pos.y;

                    if (x < 0 || x >= tex.width || y < 0 || y >= tex.height)
                        continue;
                    if (visited[x, y])
                        continue;

                    Color current = tex.GetPixel(x, y);
                    if (!ColorsMatch(current, targetColor, tolerance))
                        continue;

                    visited[x, y] = true;
                    tex.SetPixel(x, y, fillColor);

                    stack.Push(new Vector2Int(x + 1, y));
                    stack.Push(new Vector2Int(x - 1, y));
                    stack.Push(new Vector2Int(x, y + 1));
                    stack.Push(new Vector2Int(x, y - 1));
                }
            });
        }
        
        public TextureElement DrawPixel(int x, int y, Color color)
        {
            return AddOperation(tex =>
            {
                SetPixelSafe(tex, x, y, color);
            });
        }
        
        public TextureElement DrawImage(Texture2D source, int destX, int destY, float opacity = 1f)
        {
            return AddOperation(tex =>
            {
                opacity = Mathf.Clamp01(opacity);

                for (int x = 0; x < source.width; x++)
                {
                    for (int y = 0; y < source.height; y++)
                    {
                        int px = destX + x;
                        int py = destY + y;
                        if (px < 0 || px >= tex.width || py < 0 || py >= tex.height)
                            continue;

                        Color srcColor = source.GetPixel(x, y);
                        srcColor.a *= opacity;

                        if (srcColor.a <= 0f)
                            continue;

                        Color dstColor = tex.GetPixel(px, py);
                        tex.SetPixel(px, py, BlendPixel(dstColor, srcColor));
                    }
                }
            });
        }
        
        public TextureElement DrawDashedLine(int x0, int y0, int x1, int y1, Color color, int dashLength = 6, int gapLength = 4, int thickness = 1)
        {
            return AddOperation(tex =>
            {
                float dx = x1 - x0;
                float dy = y1 - y0;
                float length = Mathf.Sqrt(dx * dx + dy * dy);
                if (length < 1f) return;

                float segmentLength = dashLength + gapLength;
                float traveled = 0f;

                while (traveled < length)
                {
                    float dashEnd = Mathf.Min(traveled + dashLength, length);
                    float t0 = traveled / length;
                    float t1 = dashEnd / length;

                    int sx = (int)(x0 + dx * t0);
                    int sy = (int)(y0 + dy * t0);
                    int ex = (int)(x0 + dx * t1);
                    int ey = (int)(y0 + dy * t1);

                    DrawLineBresenham(tex, sx, sy, ex, ey, color, thickness);
                    traveled += segmentLength;
                }
            });
        }
        
        public TextureElement DrawDottedLine(int x0, int y0, int x1, int y1, Color color, int spacing = 4, int dotRadius = 1)
        {
            return AddOperation(tex =>
            {
                float dx = x1 - x0;
                float dy = y1 - y0;
                float length = Mathf.Sqrt(dx * dx + dy * dy);
                if (length < 1f) return;

                int dotCount = (int)(length / spacing);
                for (int i = 0; i <= dotCount; i++)
                {
                    float t = (float)i / Mathf.Max(1, dotCount);
                    int cx = (int)(x0 + dx * t);
                    int cy = (int)(y0 + dy * t);

                    if (dotRadius <= 1)
                        SetPixelSafe(tex, cx, cy, color);
                    else
                    {
                        int rSq = dotRadius * dotRadius;
                        for (int px = -dotRadius; px <= dotRadius; px++)
                        for (int py = -dotRadius; py <= dotRadius; py++)
                            if (px * px + py * py <= rSq)
                                SetPixelSafe(tex, cx + px, cy + py, color);
                    }
                }
            });
        }
        
        public TextureElement DrawArrow(int x0, int y0, int x1, int y1, Color color, int thickness = 1, int headLength = 12, float headAngle = 30f)
        {
            return AddOperation(tex =>
            {
                DrawLineBresenham(tex, x0, y0, x1, y1, color, thickness);

                float angle = Mathf.Atan2(y1 - y0, x1 - x0);
                float headRad = headAngle * Mathf.Deg2Rad;

                int ax = x1 - (int)(Mathf.Cos(angle - headRad) * headLength);
                int ay = y1 - (int)(Mathf.Sin(angle - headRad) * headLength);
                int bx = x1 - (int)(Mathf.Cos(angle + headRad) * headLength);
                int by = y1 - (int)(Mathf.Sin(angle + headRad) * headLength);

                DrawLineBresenham(tex, x1, y1, ax, ay, color, thickness);
                DrawLineBresenham(tex, x1, y1, bx, by, color, thickness);
            });
        }
        
        public TextureElement DrawSpline(Vector2[] points, Color color, int thickness = 1, int segmentsPerCurve = 32)
        {
            if (points == null || points.Length < 4)
                throw new ArgumentException("DrawSpline requires at least 4 points.", nameof(points));

            return AddOperation(tex =>
            {
                for (int i = 0; i < points.Length - 3; i++)
                {
                    Vector2 p0 = points[i];
                    Vector2 p1 = points[i + 1];
                    Vector2 p2 = points[i + 2];
                    Vector2 p3 = points[i + 3];

                    Vector2 prev = p1;
                    for (int s = 1; s <= segmentsPerCurve; s++)
                    {
                        float t = (float)s / segmentsPerCurve;
                        float tt = t * t;
                        float ttt = tt * t;

                        Vector2 point = 0.5f * (
                            (2f * p1) +
                            (-p0 + p2) * t +
                            (2f * p0 - 5f * p1 + 4f * p2 - p3) * tt +
                            (-p0 + 3f * p1 - 3f * p2 + p3) * ttt
                        );

                        DrawLineBresenham(tex, (int)prev.x, (int)prev.y, (int)point.x, (int)point.y, color, thickness);
                        prev = point;
                    }
                }
            });
        }

        #endregion

        #region Private Helpers

        private static void ApplyBorderTopRight(Texture2D texture, int distance, float aliasDistance)
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

        private static void ApplyBorderTopLeft(Texture2D texture, int distance, float aliasDistance)
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

        private static void ApplyBorderBottomRight(Texture2D texture, int distance, float aliasDistance)
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

        private static void ApplyBorderBottomLeft(Texture2D texture, int distance, float aliasDistance)
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
        
        private static void SetPixelSafe(Texture2D tex, int x, int y, Color color)
        {
            if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
            {
                if (color.a < 1f)
                {
                    Color existing = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, BlendPixel(existing, color));
                }
                else
                {
                    tex.SetPixel(x, y, color);
                }
            }
        }

        private static Color BlendPixel(Color dst, Color src)
        {
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0f) return Color.clear;

            return new Color(
                (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA,
                (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA,
                (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA,
                outA
            );
        }
        
        private static bool ColorsMatch(Color a, Color b, float tolerance)
        {
            return Mathf.Abs(a.r - b.r) <= tolerance &&
                   Mathf.Abs(a.g - b.g) <= tolerance &&
                   Mathf.Abs(a.b - b.b) <= tolerance &&
                   Mathf.Abs(a.a - b.a) <= tolerance;
        }
        
        private static void DrawLineBresenham(Texture2D tex, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (thickness <= 1)
                {
                    SetPixelSafe(tex, x0, y0, color);
                }
                else
                {
                    int half = thickness / 2;
                    int halfSq = half * half;
                    for (int px = -half; px <= half; px++)
                    {
                        for (int py = -half; py <= half; py++)
                        {
                            if (px * px + py * py <= halfSq)
                                SetPixelSafe(tex, x0 + px, y0 + py, color);
                        }
                    }
                }

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }
        
        private static void DrawLineWu(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
        {
            bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);

            if (steep)
            {
                Swap(ref x0, ref y0);
                Swap(ref x1, ref y1);
            }

            if (x0 > x1)
            {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }

            float dx = x1 - x0;
            float dy = y1 - y0;
            float gradient = dx == 0 ? 1f : dy / dx;

            // First endpoint
            float xEnd = x0;
            float yEnd = y0 + gradient * (xEnd - x0);
            float xGap = 1f - Fract(x0 + 0.5f);
            int xPx1 = (int)xEnd;
            int yPx1 = (int)yEnd;

            if (steep)
            {
                PlotWu(tex, yPx1, xPx1, color, (1f - Fract(yEnd)) * xGap);
                PlotWu(tex, yPx1 + 1, xPx1, color, Fract(yEnd) * xGap);
            }
            else
            {
                PlotWu(tex, xPx1, yPx1, color, (1f - Fract(yEnd)) * xGap);
                PlotWu(tex, xPx1, yPx1 + 1, color, Fract(yEnd) * xGap);
            }

            float intery = yEnd + gradient;

            // Second endpoint
            xEnd = x1;
            yEnd = y1 + gradient * (xEnd - x1);
            xGap = Fract(x1 + 0.5f);
            int xPx2 = (int)xEnd;
            int yPx2 = (int)yEnd;

            if (steep)
            {
                PlotWu(tex, yPx2, xPx2, color, (1f - Fract(yEnd)) * xGap);
                PlotWu(tex, yPx2 + 1, xPx2, color, Fract(yEnd) * xGap);
            }
            else
            {
                PlotWu(tex, xPx2, yPx2, color, (1f - Fract(yEnd)) * xGap);
                PlotWu(tex, xPx2, yPx2 + 1, color, Fract(yEnd) * xGap);
            }

            // Main loop
            for (int x = xPx1 + 1; x < xPx2; x++)
            {
                if (steep)
                {
                    PlotWu(tex, (int)intery, x, color, 1f - Fract(intery));
                    PlotWu(tex, (int)intery + 1, x, color, Fract(intery));
                }
                else
                {
                    PlotWu(tex, x, (int)intery, color, 1f - Fract(intery));
                    PlotWu(tex, x, (int)intery + 1, color, Fract(intery));
                }
                intery += gradient;
            }
        }
        
        private static void PlotWu(Texture2D tex, int x, int y, Color color, float brightness)
        {
            if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) return;
            Color c = color;
            c.a *= brightness;
            Color existing = tex.GetPixel(x, y);
            tex.SetPixel(x, y, BlendPixel(existing, c));
        }

        private static float Fract(float x) => x - Mathf.Floor(x);

        private static void Swap(ref int a, ref int b)
        {
            int tmp = a;
            a = b;
            b = tmp;
        }
        
        private static void DrawCircleMidpoint(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            int x = radius;
            int y = 0;
            int err = 1 - radius;

            while (x >= y)
            {
                SetPixelSafe(tex, cx + x, cy + y, color);
                SetPixelSafe(tex, cx - x, cy + y, color);
                SetPixelSafe(tex, cx + x, cy - y, color);
                SetPixelSafe(tex, cx - x, cy - y, color);
                SetPixelSafe(tex, cx + y, cy + x, color);
                SetPixelSafe(tex, cx - y, cy + x, color);
                SetPixelSafe(tex, cx + y, cy - x, color);
                SetPixelSafe(tex, cx - y, cy - x, color);

                y++;
                if (err < 0)
                {
                    err += 2 * y + 1;
                }
                else
                {
                    x--;
                    err += 2 * (y - x) + 1;
                }
            }
        }
        
        private static void DrawEllipseBresenham(Texture2D tex, int cx, int cy, int rx, int ry, Color color)
        {
            if (rx <= 0 || ry <= 0) return;

            long rxSq = (long)rx * rx;
            long rySq = (long)ry * ry;
            long twoRxSq = 2 * rxSq;
            long twoRySq = 2 * rySq;

            int x = 0;
            int y = ry;
            long px = 0;
            long py = twoRxSq * y;

            // Region 1
            long p = (long)(rySq - rxSq * ry + 0.25f * rxSq);
            while (px < py)
            {
                SetPixelSafe(tex, cx + x, cy + y, color);
                SetPixelSafe(tex, cx - x, cy + y, color);
                SetPixelSafe(tex, cx + x, cy - y, color);
                SetPixelSafe(tex, cx - x, cy - y, color);

                x++;
                px += twoRySq;
                if (p < 0)
                    p += rySq + px;
                else
                {
                    y--;
                    py -= twoRxSq;
                    p += rySq + px - py;
                }
            }

            // Region 2
            p = (long)(rySq * (x + 0.5f) * (x + 0.5f) + rxSq * (y - 1) * (y - 1) - rxSq * rySq);
            while (y >= 0)
            {
                SetPixelSafe(tex, cx + x, cy + y, color);
                SetPixelSafe(tex, cx - x, cy + y, color);
                SetPixelSafe(tex, cx + x, cy - y, color);
                SetPixelSafe(tex, cx - x, cy - y, color);

                y--;
                py -= twoRxSq;
                if (p > 0)
                    p += rxSq - py;
                else
                {
                    x++;
                    px += twoRySq;
                    p += rxSq - py + px;
                }
            }
        }
        
        private static void DrawCornerArc(Texture2D tex, int cx, int cy, int radius, Color color, int quadrant)
        {
            int x = radius;
            int y = 0;
            int err = 1 - radius;

            while (x >= y)
            {
                switch (quadrant)
                {
                    case 0: // Top-right
                        SetPixelSafe(tex, cx + x, cy + y, color);
                        SetPixelSafe(tex, cx + y, cy + x, color);
                        break;
                    case 1: // Top-left
                        SetPixelSafe(tex, cx - x, cy + y, color);
                        SetPixelSafe(tex, cx - y, cy + x, color);
                        break;
                    case 2: // Bottom-left
                        SetPixelSafe(tex, cx - x, cy - y, color);
                        SetPixelSafe(tex, cx - y, cy - x, color);
                        break;
                    case 3: // Bottom-right
                        SetPixelSafe(tex, cx + x, cy - y, color);
                        SetPixelSafe(tex, cx + y, cy - x, color);
                        break;
                }

                y++;
                if (err < 0)
                    err += 2 * y + 1;
                else
                {
                    x--;
                    err += 2 * (y - x) + 1;
                }
            }
        }
        
        private static Vector2[] GetRegularPolygonVertices(int centerX, int centerY, int radius, int sides, float rotationDegrees)
        {
            Vector2[] verts = new Vector2[sides];
            float angleStep = Mathf.PI * 2f / sides;
            float offsetRad = rotationDegrees * Mathf.Deg2Rad;

            for (int i = 0; i < sides; i++)
            {
                float angle = angleStep * i + offsetRad;
                verts[i] = new Vector2(
                    centerX + radius * Mathf.Cos(angle),
                    centerY + radius * Mathf.Sin(angle)
                );
            }

            return verts;
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
    /// Represents the subtractive blending operation, where the values of the
    /// source and destination are subtracted from one another, producing a
    /// darker result. Typically used in graphics rendering to achieve
    /// specific blending effects.
    /// </summary>
    public enum MixBlendType
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
        Exclusion,
        /// <summary>
        /// Add represents a blend mode where the color values of the source and destination are added together,
        /// resulting in a lighter composite image. Useful for effects like highlights or light-based interactions.
        /// </summary>
        Add,
        /// <summary>
        /// Subtract represents a blend mode where the values of the source and destination
        /// are subtracted from each other. This mode is used to create effects where darker
        /// colors are emphasized by reducing brightness in overlapping areas.
        /// </summary>
        Subtract
    }

    public enum CanvasPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
}