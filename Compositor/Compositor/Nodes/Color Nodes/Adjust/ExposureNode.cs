using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK
{
    public class ExposureNode : BaseCompositorNode
    {
        public override string Title { get; } = "Exposure";
        public static string Group => "Color/Adjust";

        private CompositorSlider _factorSlider;
        private float _factor;

        protected override void Initialize()
        {
            _factorSlider = new CompositorSlider(1, -1, 1, "Exposure");
            _factorSlider.OnValueChanged += value => _factor = value;
        }
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Exposure", SocketType.Alpha, new Vector2(0, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("Image", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
        }
        public override void DrawContent(Rect contentRect)
        {
            throw new System.NotImplementedException();
        }
        public override void Process()
        {
            throw new System.NotImplementedException();
        }
    }
}