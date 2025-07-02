using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK
{
    public class RGBANode : LazyCompositorNode
    {
        private float r, g, b, a;
        public override string Title { get; } = "RGBA";
        public static string Group => "Color";
        protected override void InitializePorts()
        {
            _outputs.Add(new NodeOutput("Color", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
        }
        public override void DrawContent(Rect contentRect)
        {
        }
        protected override void ProcessInternal()
        {
            
        }
    }
}