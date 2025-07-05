using System;
using DefaultNamespace;
using UnityEngine;
using Array = Compositor.KK.Compositor.Array;

namespace Compositor.KK
{
    public class RGBNode : BaseCompositorNode
    {
        public override string Title { get; } = "RGB";
        public static string Group => "Input";
        private CompositorColorSelector _colorSelector;
        protected override void Initialize()
        {
            _colorSelector = new CompositorColorSelector(Color.white);
        }
        protected override void InitializePorts()
        {
            _outputs.Add(new NodeOutput("RGBA", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.9f)));
        }
        public override void DrawContent(Rect contentRect)
        {
            _colorSelector.Draw(new Rect(contentRect.x + 5, contentRect.y, contentRect.width - 20, contentRect.height - 20));
        }
        public unsafe override void Process()
        {
            Color color = _colorSelector.SelectedColor;
            var pixelData = new float[Entry.ImageHeight * Entry.ImageWidth];
            fixed (float* pPixelData = pixelData)
            {
                for (var i = 0; i < pixelData.Length; i+=4)
                {
                    pixelData[i] = color.r;
                    pixelData[i + 1] = color.g;
                    pixelData[i + 2] = color.b;
                    pixelData[i + 3] = 1;
                }
            }
            
            _outputs[0].SetValue(pixelData);
        }
    }
}