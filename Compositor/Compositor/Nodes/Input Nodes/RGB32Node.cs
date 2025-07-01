using UnityEngine;

namespace Compositor.KK
{
    public class RGB32Node : BaseCompositorNode
    {
        public override string Title { get; } = "RGB32";
        public static string Group => "Input";
        protected override void InitializePorts()
        {
            _outputs.Add(new NodeOutput("Color", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
        }
        public override void DrawContent(Rect contentRect)
        {}
        public override void Process()
        {
            throw new System.NotImplementedException();
        }
    }
}