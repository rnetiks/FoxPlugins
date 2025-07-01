using UnityEngine;

namespace Compositor.KK
{
    public class HSVNode : BaseCompositorNode
    {
        public override string Title { get; } = "Hue/Saturation/Value";
        public static string Group => "Color/Adjust";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.4f)));
            _inputs.Add(new NodeInput("Hue", SocketType.A, new Vector2(0, Size.y * 0.5f)));
            _inputs.Add(new NodeInput("Saturation", SocketType.A, new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Value", SocketType.A, new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("Factor", SocketType.A, new Vector2(0, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("Image", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.4f)));
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