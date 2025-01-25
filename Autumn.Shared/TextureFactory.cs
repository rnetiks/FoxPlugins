using System;
using System.IO;
using PrismaLib.Enums;
using PrismaLib.Settings;
using PrismaLib.Settings.Type;
using UnityEngine;

namespace Autumn
{
    public static class TextureFactory
    {
        [Flags]
        public enum Border
        {
            TopLeft = 1,
            TopRight = 2,
            BottomLeft = 4,
            BottomRight = 8,
            All = 16
        }

        #region Filter

        public static Texture2D EdgeDetection(this Texture2D tex)
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
            tex.Apply();
            return tex;
        }

        public static Texture2D Vignette(this Texture2D tex, float strength)
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
                        new Color(pixelColor.r * factor, pixelColor.g * factor, pixelColor.b * factor, pixelColor.a));
                }
            }

            tex.Apply();
            return tex;
        }

        public static Texture2D Glow(this Texture2D tex, float strength)
        {
            int width = tex.width;
            int height = tex.height;
            Color[] pixels = tex.GetPixels();
            Color[] newPixels = new Color[pixels.Length];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixelColor = pixels[y * width + x];
                    float glow = Mathf.Clamp01(pixelColor.grayscale * strength);
                    newPixels[y * width + x] = new Color(glow, glow, glow, pixelColor.a);
                }
            }

            tex.SetPixels(newPixels);
            tex.Apply();
            return tex;
        }

        public static Texture2D Wave(this Texture2D tex, float amplitude, float frequency)
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
            tex.Apply();
            return tex;
        }

        public static Texture2D Swirl(this Texture2D tex, float strength)
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
            tex.Apply();
            return tex;
        }

        public static Texture2D ColorMask(this Texture2D tex, Color targetColor, float threshold)
        {
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    Color pixelColor = tex.GetPixel(x, y);
                    float distance = Vector3.Distance(new Vector3(pixelColor.r, pixelColor.g, pixelColor.b),
                        new Vector3(targetColor.r, targetColor.g, targetColor.b));

                    if (distance < threshold)
                    {
                        // Set the pixel to transparent or another color
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        public static Texture2D Posterize(this Texture2D tex, int levels)
        {
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

            tex.Apply();
            return tex;
        }

        public static Texture2D NoiseReduction(this Texture2D tex, int kernelSize)
        {
            int width = tex.width;
            int height = tex.height;
            Color[] pixels = tex.GetPixels();
            Color[] newPixels = new Color[pixels.Length];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float r = 0, g = 0, b = 0;
                    int count = 0;

                    for (int i = -kernelSize; i <= kernelSize; i++)
                    {
                        for (int j = -kernelSize; j <= kernelSize; j++)
                        {
                            int px = Mathf.Clamp(x + i, 0, width - 1);
                            int py = Mathf.Clamp(y + j, 0, height - 1);
                            Color pixelColor = pixels[py * width + px];
                            r += pixelColor.r;
                            g += pixelColor.g;
                            b += pixelColor.b;
                            count++;
                        }
                    }

                    newPixels[y * width + x] = new Color(r / count, g / count, b / count, 1);
                }
            }

            tex.SetPixels(newPixels);
            tex.Apply();
            return tex;
        }

        public static Texture2D Sepia(this Texture2D tex)
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

            tex.Apply();
            return tex;
        }

        public static Texture2D Blur(this Texture2D tex, int blurSize)
        {
            int width = tex.width;
            int height = tex.height;
            Color[] pixels = tex.GetPixels();
            Color[] newPixels = new Color[pixels.Length];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float r = 0, g = 0, b = 0;
                    int count = 0;

                    for (int i = -blurSize; i <= blurSize; i++)
                    {
                        for (int j = -blurSize; j <= blurSize; j++)
                        {
                            int px = Mathf.Clamp(x + i, 0, width - 1);
                            int py = Mathf.Clamp(y + j, 0, height - 1);
                            Color pixelColor = pixels[py * width + px];
                            r += pixelColor.r;
                            g += pixelColor.g;
                            b += pixelColor.b;
                            count++;
                        }
                    }

                    newPixels[y * width + x] = new Color(r / count, g / count, b / count, 1);
                }
            }

            tex.SetPixels(newPixels);
            tex.Apply();
            return tex;
        }

        public static Texture2D Invert(this Texture2D tex)
        {
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    Color pixelColor = tex.GetPixel(x, y);
                    Color invertedColor = new Color(1 - pixelColor.r, 1 - pixelColor.g, 1 - pixelColor.b, pixelColor.a);
                    tex.SetPixel(x, y, invertedColor);
                }
            }

            tex.Apply();
            return tex;
        }

        public static Texture2D Brightness(this Texture2D tex, float brightness)
        {
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    Color pixelColor = tex.GetPixel(x, y);
                    pixelColor.r = Mathf.Clamp01(pixelColor.r + brightness);
                    pixelColor.g = Mathf.Clamp01(pixelColor.g + brightness);
                    pixelColor.b = Mathf.Clamp01(pixelColor.b + brightness);
                    tex.SetPixel(x, y, pixelColor);
                }
            }

            tex.Apply();
            return tex;
        }

        public static Texture2D Pixelate(this Texture2D tex, int pixelSize)
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
            tex.Apply();
            return tex;
        }

        public static Texture2D Saturate(this Texture2D tex, float saturation)
        {
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    Color pixelColor = tex.GetPixel(x, y);
                    float gray = 0.299f * pixelColor.r + 0.587f * pixelColor.g + 0.114f * pixelColor.b;
                    Color adjustedColor = new Color(
                        Mathf.Clamp01(gray + saturation * (pixelColor.r - gray)),
                        Mathf.Clamp01(gray + saturation * (pixelColor.g - gray)),
                        Mathf.Clamp01(gray + saturation * (pixelColor.b - gray)),
                        pixelColor.a
                    );
                    tex.SetPixel(x, y, adjustedColor);
                }
            }

            tex.Apply();
            return tex;
        }

        public static Texture2D Hue(this Texture2D tex, float hue)
        {
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    Color pixelColor = tex.GetPixel(x, y);
                    float h, s, v;
                    Color.RGBToHSV(pixelColor, out h, out s, out v);
                    h = (h + hue) % 1.0f;
                    Color adjustedColor = Color.HSVToRGB(h, s, v);
                    tex.SetPixel(x, y, adjustedColor);
                }
            }

            tex.Apply();
            return tex;
        }

        public static Texture2D Sharpen(this Texture2D tex)
        {
            int width = tex.width;
            int height = tex.height;
            Color[] pixels = tex.GetPixels();
            Color[] newPixels = new Color[pixels.Length];

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float r = 0, g = 0, b = 0;
                    int index = y * width + x;

                    r = 5 * pixels[index].r -
                        pixels[index - width - 1].r - pixels[index - width].r - pixels[index - width + 1].r -
                        pixels[index - 1].r - pixels[index + 1].r -
                        pixels[index + width - 1].r - pixels[index + width].r - pixels[index + width + 1].r;

                    g = 5 * pixels[index].g -
                        pixels[index - width - 1].g - pixels[index - width].g - pixels[index - width + 1].g -
                        pixels[index - 1].g - pixels[index + 1].g -
                        pixels[index + width - 1].g - pixels[index + width].g - pixels[index + width + 1].g;

                    b = 5 * pixels[index].b -
                        pixels[index - width - 1].b - pixels[index - width].b - pixels[index - width + 1].b -
                        pixels[index - 1].b - pixels[index + 1].b -
                        pixels[index + width - 1].b - pixels[index + width].b - pixels[index + width + 1].b;

                    newPixels[index] = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), pixels[index].a);
                }
            }

            tex.SetPixels(newPixels);
            tex.Apply();
            return tex;
        }

        public static Texture2D Colorize(this Texture2D tex, Color tintColor)
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

            tex.Apply();
            return tex;
        }

        public static Texture2D Emboss(this Texture2D tex)
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
                    float gray = 0.299f * pixels[index].r + 0.587f * pixels[index].g + 0.114f * pixels[index].b;
                    float emboss = gray - 0.299f * pixels[index + 1].r - 0.587f * pixels[index + 1].g -
                        0.114f * pixels[index + 1].b + 0.5f;
                    newPixels[index] = new Color(emboss, emboss, emboss, pixels[index].a);
                }
            }

            tex.SetPixels(newPixels);
            tex.Apply();
            return tex;
        }

        public static Texture2D Contrast(this Texture2D tex, float contrast)
        {
            float factor = (259 * (contrast + 255)) / (255 * (259 - contrast));

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

            tex.Apply();
            return tex;
        }

        public static Texture2D Grayscale(this Texture2D tex)
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

            tex.Apply();
            return tex;
        }

        #endregion

        public static Texture2D SetBorder(this Texture2D tex, int distance, Border border, int aliasDistance = 0)
        {
            if ((border & Border.BottomLeft) != 0)
                tex = BorderBottomLeft(tex, distance, aliasDistance);
            if ((border & Border.BottomRight) != 0)
                tex = BorderBottomRight(tex, distance, aliasDistance);
            if ((border & Border.TopLeft) != 0)
                tex = BorderTopLeft(tex, distance, aliasDistance);
            if ((border & Border.TopRight) != 0)
                tex = BorderTopRight(tex, distance, aliasDistance);
            if ((border & Border.All) == Border.All)
            {
                tex = BorderTopLeft(tex, distance, aliasDistance);
                tex = BorderTopRight(tex, distance, aliasDistance);
                tex = BorderBottomLeft(tex, distance, aliasDistance);
                tex = BorderBottomRight(tex, distance, aliasDistance);
            }

            return tex;
        }

        public static Texture2D Crop(this Texture2D tex, Rect rect)
        {
            int width = tex.width;
            int height = tex.height;
            if (rect.width + rect.x > width || rect.height + rect.y > height)
                throw new ArgumentException("The provided rect is out of bounds.");

            Texture2D newTex = new Texture2D(width, height);
            for (int x = 0; x < rect.width; x++)
            {
                for (int y = 0; y < rect.height; y++)
                {
                    newTex.SetPixel(x, y, tex.GetPixel((int)(x + rect.x), (int)(y + rect.y)));
                }
            }

            newTex.Apply();
            return newTex;
        }

        public static Texture2D Fill(this Texture2D tex, Color c)
        {
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return tex;
        }

        public static Texture2D Gradient(this Texture2D tex, Color startColor, Color endColor, float degree)
        {
            float r = degree * Mathf.Deg2Rad;

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

            tex.Apply();
            return tex;
        }

        #region Math

        public static Texture2D Add(this Texture2D tex, Texture2D tex2)
        {
            if (tex.width != tex2.width || tex.height != tex2.height)
                throw new ArgumentException("Both textures must have the same size.");

            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, tex.GetPixel(x, y) + tex2.GetPixel(x, y));
                }
            }

            return tex;
        }

        public static Texture2D Subtract(this Texture2D tex, Texture2D tex2)
        {
            if (tex.width != tex2.width || tex.height != tex2.height)
                throw new ArgumentException("Both textures must have the same size.");

            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, tex.GetPixel(x, y) - tex2.GetPixel(x, y));
                }
            }

            return tex;
        }

        public static Texture2D Multiply(this Texture2D tex, Texture2D tex2)
        {
            if (tex.width != tex2.width || tex.height != tex2.height)
                throw new ArgumentException("Both textures must have the same size.");

            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, tex.GetPixel(x, y) * tex2.GetPixel(x, y));
                }
            }

            return tex;
        }

        public static Texture2D Add(this Texture2D tex, Color color)
        {
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, tex.GetPixel(x, y) + color);
                }
            }

            return tex;
        }

        public static Texture2D Subtract(this Texture2D tex, Color color)
        {
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, tex.GetPixel(x, y) - color);
                }
            }

            return tex;
        }

        public static Texture2D Multiply(this Texture2D tex, Color color)
        {
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, tex.GetPixel(x, y) * color);
                }
            }

            return tex;
        }

        #endregion

        #region Border

        public static Texture2D BorderTopRight(this Texture2D texture, int distance, int aliasDistance = 0)
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
                            float n =
                                Mathf.Clamp01(1 - Mathf.Pow((dist - (distance - aliasDistance)) / (2 * aliasDistance),
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

                texture.Apply();
            }

            return texture;
        }

        public static Texture2D BorderTopLeft(this Texture2D texture, int distance, int aliasDistance = 0)
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

                texture.Apply();
            }

            return texture;
        }

        public static Texture2D BorderBottomRight(this Texture2D texture, int distance, int aliasDistance = 0)
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

                texture.Apply();
            }

            return texture;
        }

        public static Texture2D BorderBottomLeft(this Texture2D texture, int distance, int aliasDistance = 0)
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

                texture.Apply();
            }

            return texture;
        }

        #endregion

        public static Texture2D TryLoad(string filepath, Func<string, Texture2D> onNotFound)
        {
            if (!Directory.Exists("AutumnTextures"))
                Directory.CreateDirectory("AutumnTextures");

            if (!File.Exists(Path.Combine("AutumnTextures", filepath)))
            {
                var texture2D = onNotFound(filepath);
                return texture2D;
            }

            var data = new Texture2D(1, 1);

            data.LoadImage(File.ReadAllBytes(Path.Combine("AutumnTextures", filepath)));
            data.Apply();
            return data;
        }

        public static Texture2D Save(this Texture2D tex, string filepath)
        {
            if (!Directory.Exists("AutumnTextures"))
                Directory.CreateDirectory("AutumnTextures");
            File.WriteAllBytes(Path.Combine("AutumnTextures", filepath), tex.EncodeToPNG());
            return tex;
        }
    }
}