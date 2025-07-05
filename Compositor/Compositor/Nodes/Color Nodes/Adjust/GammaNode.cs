using UnityEngine;

namespace Compositor.KK
{
    public class GammaNode : BaseCompositorNode
    {
        public override string Title { get; } = "Gamma";
        public static string Group => "Color/Adjust";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Gamma", SocketType.Alpha, new Vector2(0, Size.y * 0.7f)));
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