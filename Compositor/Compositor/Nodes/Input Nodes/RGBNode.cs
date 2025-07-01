using UnityEngine;

namespace Compositor.KK
{
    public class RGBNode : BaseCompositorNode
    {
        public override string Title { get; } = "RGB";
        public static string Group => "Input";
        protected override void InitializePorts()
        {
            _outputs.Add(new NodeOutput("RGBA", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
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