using UnityEngine;

namespace Compositor.KK
{
    public class RGBCurvesNode : BaseCompositorNode
    {
        public override string Title { get; } = "RGB Curves";
        public static string Group => "Color/Adjust";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Factor", SocketType.Alpha, new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("Black", SocketType.RGBA, new Vector2(0, Size.y * 0.8f)));
            _inputs.Add(new NodeInput("White", SocketType.RGBA, new Vector2(0, Size.y * 0.9f)));
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