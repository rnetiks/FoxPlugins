using UnityEngine;

namespace Compositor.KK.Vector
{
    public class SeparateVectorNode : BaseCompositorNode
    {
        public override string Title { get; } = "Separate Vector";
        public static string Group => "Vector";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Vector", new []{typeof(Vector2), typeof(Vector3), typeof(Vector4)}, new Vector2(0, Size.y * 0.5f)));
            _outputs.Add(new NodeOutput("X", typeof(float), new Vector2(Size.x, Size.y * 0.5f)));
            _outputs.Add(new NodeOutput("Y", typeof(float), new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Z", typeof(float), new Vector2(Size.x, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("W", typeof(float), new Vector2(Size.x, Size.y * 0.8f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {

        }
    }
}