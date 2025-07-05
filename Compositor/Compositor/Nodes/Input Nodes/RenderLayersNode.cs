using UnityEngine;

namespace Compositor.KK
{
    public class RenderLayersNode : BaseCompositorNode
    {
        public override string Title { get; } = "Render Layers";
        public static string Group => "Input";
        protected override void InitializePorts()
        {
            _outputs.Add(new NodeOutput("Image", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Alpha", SocketType.Alpha, new Vector2(Size.x, Size.y * 0.7f)));
        }
        public override void DrawContent(Rect contentRect)
        {
            
        }
        public override void Process()
        {
            throw new System.NotImplementedException();
        }
    }
}