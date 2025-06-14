using System;
using UnityEngine;

namespace TexFac.Universal
{
    public interface ITextureElement
    {
        // Properties
        int Width { get; }
        int Height { get; }

        // Core Methods
        ITextureElement Apply();
        Texture2D GetTexture();
        ITextureElement AddOperation(Action<Texture2D> operation);
        ITextureElement Save(string filepath, ImageType imageType = ImageType.PNG);
        byte[] GetBytes(ImageType imageType = ImageType.PNG);
        Texture2D LoadScreen();

        // Background Methods
        ITextureElement BackgroundColor(byte r, byte g, byte b, byte a);
        ITextureElement BackgroundGradient(Color32 color1, Color32 color2, float angle);
        ITextureElement BackgroundGradient(Color startColor, Color endColor, float angle = 0);
        ITextureElement BackgroundRadialGradient(Color centerColor, Color outerColor, Vector2? center = null);
        ITextureElement BackgroundPattern(Texture2D patternTexture, BackgroundRepeatMode repeatMode = BackgroundRepeatMode.Repeat);

        // Styling Methods
        ITextureElement BorderRadius(int radius, BorderType borderType = BorderType.All, int aliasDistance = 0);
        ITextureElement Border(int width, Color color, BorderDrawMode drawMode = BorderDrawMode.Outside);
        ITextureElement Opacity(float value);
        ITextureElement BoxShadow(int offsetX, int offsetY, int blurRadius, Color shadowColor);

        // Scaling and Transformation
        ITextureElement Scale(float size, FilterMode filterMode = FilterMode.Bilinear);
        ITextureElement Scale(int newWidth, int newHeight, FilterMode filterMode = FilterMode.Bilinear);
        ITextureElement Rotate(float angle, bool resizeCanvas = true);
        ITextureElement RotateUnsafe(float angle);

        // Clipping and Masking
        ITextureElement ClipPath(ClipShapeType shapeType, params float[] parameters);
        ITextureElement Mask(Texture2D maskTexture);

        // Filter Effects
        ITextureElement Brightness(float value);
        ITextureElement Noise(float intensity, bool monochrome = true);
        ITextureElement Sharpen(float intensity = 1.0f);
        ITextureElement Posterize(int levels);
        ITextureElement PosterizeUnsafe(int levels);
        ITextureElement Threshold(float threshold);
        ITextureElement GaussianBlur(int radius, float sigma = 1.0f);
        ITextureElement Contrast(float value);
        ITextureElement Saturation(float value);
        ITextureElement Hue(float value);
        ITextureElement Grayscale();
        ITextureElement Invert();
        ITextureElement Sepia();
        ITextureElement Blur(int radius);
        ITextureElement Pixelate(int pixelSize);
        ITextureElement EdgeDetection();
        ITextureElement Vignette(float strength);
        ITextureElement ColorTint(Color tintColor);

        // Transformation Effects
        ITextureElement Crop(Rect rect);
        ITextureElement Translate(Vector2 offset, bool expand = false);
        ITextureElement Wave(float amplitude, float frequency);
        ITextureElement Swirl(float strength);

        // Blend Operations
        ITextureElement Blend(ITextureElement other, BlendMode blendMode = BlendMode.Normal);
        ITextureElement MixBlendMode(Texture2D overlayTexture, MixBlendModeType blendMode);
        ITextureElement DrawText(string text, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, Font font = null);
    }
}