using UnityEngine;

namespace Compositor.KK.Vector
{
    public class SeparateVectorNode : BaseCompositorNode
    {
        public override string Title { get; } = "Separate Vector";
        public static string Group => "Vector";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Vector", SocketType.Vector, new Vector2(0, Size.y * 0.5f)));
            _outputs.Add(new NodeOutput("X", SocketType.A, new Vector2(Size.x, Size.y * 0.5f)));
            _outputs.Add(new NodeOutput("Y", SocketType.A, new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Z", SocketType.A, new Vector2(Size.x, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("W", SocketType.A, new Vector2(Size.x, Size.y * 0.8f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {

        }
    }
}