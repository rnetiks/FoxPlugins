using UnityEngine;

namespace Compositor.KK
{
    public class InvertColorNode : BaseCompositorNode
    {
        public override string Title { get; } = "Invert Color";
        public static string Group => "Color";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Color", SocketType.RGBA, new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Factor", SocketType.Alpha, new Vector2(0, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("Color", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
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