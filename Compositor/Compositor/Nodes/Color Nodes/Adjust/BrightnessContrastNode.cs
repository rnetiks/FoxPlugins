using Compositor.KK.Compositor;
using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK
{
    /// <summary>
    /// Represents a compositor node for adjusting brightness and contrast in an image processing pipeline.
    /// </summary>
    /// <remarks>
    /// This class derives from <see cref="BaseCompositorNode"/> and provides functionality for altering
    /// brightness and contrast attributes of an input image. It is part of the compositor framework
    /// and categorized under the "Color/Adjust" group.
    /// </remarks>
    public class BrightnessContrastNode : BaseCompositorNode
    {
        public override string Title { get; } = "Brightness/Contrast";
        public static string Group => "Color/Adjust";

        private float brightness;
        private float contrast;
        private CompositorSlider brightnessSlider;
        private CompositorSlider contrastSlider;

        protected override void Initialize()
        {
            brightnessSlider = new CompositorSlider(0, -1, 1, 0)
            {
                Label = "Brightness"
            };
            brightnessSlider.OnValueChanged += f => brightness = f;

            contrastSlider = new CompositorSlider(0, -1, 1, 0)
            {
                Label = "Contrast"
            };
            contrastSlider.OnValueChanged += f => contrast = f;
        }
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("", SocketType.Alpha, new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("Contrast", SocketType.Alpha, new Vector2(0, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("Image", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
        }
        public override void DrawContent(Rect contentRect)
        {
            var brightnessPosition = CompositorRenderer.Instance.GetPortScaledPosition(_inputs[1].LocalPosition);
            var contrastPosition = CompositorRenderer.Instance.GetPortScaledPosition(_inputs[2].LocalPosition);

            if (!_inputs[1].IsConnected)
            {
                _inputs[1].Name = "";
                brightnessSlider.Draw(new Rect(contentRect.x + 10, brightnessPosition.y - 7.5f, contentRect.width - 50, 15));
            }
            else
            {
                _inputs[1].Name = "Brightness";
            }

            if (!_inputs[2].IsConnected)
            {
                _inputs[2].Name = "";
                contrastSlider.Draw(new Rect(contentRect.x + 10, contrastPosition.y - 7.5f, contentRect.width - 50, 15));
            }
            else
            {
                _inputs[2].Name = "Contrast";
            }
        }
        public override void Process()
        {
            if (!_inputs[0].IsConnected)
                return;

            var imageData = _inputs[0].GetValue<float[]>();

            // Tiny optimization, stops the node from taking unnecessary resources
            if (!_inputs[1].IsConnected && !_inputs[2].IsConnected && brightness == 0 && contrast == 0)
            {
                _outputs[0].SetValue(imageData);
                return;
            }
            float[] brightnessData;
            brightnessData = _inputs[1].IsConnected ? _inputs[1].GetValue<float[]>() : Array.FastFill(imageData.Length / 4, brightness);

            float[] contrastData;
            contrastData = _inputs[2].IsConnected ? _inputs[2].GetValue<float[]>() : Array.FastFill(imageData.Length / 4, contrast);
            Entry.Logger.LogDebug($"{brightness}, {contrast}, {contrastData.Length}, {brightnessData.Length}, {imageData.Length}");

            for (var i = 0; i < imageData.Length; i += 4)
            {
                var idx = i / 4;
                float brightnessValue = brightnessData[idx];
                float contrastValue = contrastData[idx];

                float contrastFactor = contrastValue + 1f;

                for (int j = 0; j < 3; j++)
                {
                    float pixel = imageData[i + j];
                    pixel = (pixel - 0.5f) * contrastFactor + 0.5f + brightnessValue;
                    imageData[i + j] = pixel;
                }
            }
            _outputs[0].SetValue(imageData);
        }
    }
}