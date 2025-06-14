using System;
using UnityEngine;

namespace TexFac.Universal
{
    public class GPUTextureElement : ITextureElement
    {
        public GPUTextureElement(int width, int height, TextureFormat format)
        {
            throw new NotImplementedException();
        }
        public ITextureElement BackgroundGradient(Color32 color1, Color32 color2, float angle)
        {
            throw new NotImplementedException();
        }
        public ITextureElement BackgroundGradient(Color startColor, Color endColor, float angle = 0)
        {
            throw new NotImplementedException();
        }
        public ITextureElement BackgroundRadialGradient(Color centerColor, Color outerColor, Vector2? center = null)
        {
            throw new NotImplementedException();
        }
        public ITextureElement BackgroundPattern(Texture2D patternTexture, BackgroundRepeatMode repeatMode = BackgroundRepeatMode.Repeat)
        {
            throw new NotImplementedException();
        }
        public ITextureElement BorderRadius(int radius, BorderType borderType = BorderType.All, int aliasDistance = 0)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Border(int width, Color color, BorderDrawMode drawMode = BorderDrawMode.Outside)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Opacity(float value)
        {
            throw new NotImplementedException();
        }
        public ITextureElement BoxShadow(int offsetX, int offsetY, int blurRadius, Color shadowColor)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Scale(float size, FilterMode filterMode = FilterMode.Bilinear)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Scale(int newWidth, int newHeight, FilterMode filterMode = FilterMode.Bilinear)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Rotate(float angle, bool resizeCanvas = true)
        {
            throw new NotImplementedException();
        }
        public ITextureElement RotateUnsafe(float angle)
        {
            throw new NotImplementedException();
        }
        public ITextureElement ClipPath(ClipShapeType shapeType, params float[] parameters)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Mask(Texture2D maskTexture)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Brightness(float value)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Noise(float intensity, bool monochrome = true)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Sharpen(float intensity = 1)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Posterize(int levels)
        {
            throw new NotImplementedException();
        }
        public ITextureElement PosterizeUnsafe(int levels)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Threshold(float threshold)
        {
            throw new NotImplementedException();
        }
        public ITextureElement GaussianBlur(int radius, float sigma = 1)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Contrast(float value)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Saturation(float value)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Hue(float value)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Grayscale()
        {
            throw new NotImplementedException();
        }
        public ITextureElement Invert()
        {
            throw new NotImplementedException();
        }
        public ITextureElement Sepia()
        {
            throw new NotImplementedException();
        }
        public ITextureElement Blur(int radius)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Pixelate(int pixelSize)
        {
            throw new NotImplementedException();
        }
        public ITextureElement EdgeDetection()
        {
            throw new NotImplementedException();
        }
        public ITextureElement Vignette(float strength)
        {
            throw new NotImplementedException();
        }
        public ITextureElement ColorTint(Color tintColor)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Crop(Rect rect)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Translate(Vector2 offset, bool expand = false)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Wave(float amplitude, float frequency)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Swirl(float strength)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Blend(ITextureElement other, BlendMode blendMode = BlendMode.Normal)
        {
            throw new NotImplementedException();
        }
        public ITextureElement MixBlendMode(Texture2D overlayTexture, MixBlendModeType blendMode)
        {
            throw new NotImplementedException();
        }
        public ITextureElement DrawText(string text, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, Font font = null)
        {
            throw new NotImplementedException();
        }
        public Texture2D LoadScreen()
        {
            throw new NotImplementedException();
        }
        public ITextureElement BackgroundColor(byte r, byte g, byte b, byte a)
        {
            throw new NotImplementedException();
            return AddOperation(tex =>
            {
                RenderTexture rt = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                ComputeShader shader = Resources.Load<ComputeShader>("Shaders/Fill");
                int kernelId = shader.FindKernel("Fill");
                shader.SetVector("_Color1", new Vector4(r, g, b, a));
                shader.SetTexture(kernelId, Shader.PropertyToID("_Color"), rt);
                shader.Dispatch(kernelId, tex.width / 8, tex.height / 8, 1);
                RenderTexture.active = rt;
                Texture2D tempTex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tempTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tempTex.Apply();
                RenderTexture.active = null;
            });
        }
        public int Width { get; }
        public int Height { get; }
        public ITextureElement Apply()
        {
            throw new NotImplementedException();
        }
        public Texture2D GetTexture()
        {
            throw new NotImplementedException();
        }
        public ITextureElement AddOperation(Action<Texture2D> operation)
        {
            throw new NotImplementedException();
        }
        public ITextureElement Save(string filepath, ImageType imageType = ImageType.PNG)
        {
            throw new NotImplementedException();
        }
        public byte[] GetBytes(ImageType imageType = ImageType.PNG)
        {
            throw new NotImplementedException();
        }
    }
}