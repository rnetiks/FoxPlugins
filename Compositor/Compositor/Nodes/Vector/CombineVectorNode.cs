using UnityEngine;

namespace Compositor.KK.Vector
{
    public class CombineVectorNode : BaseCompositorNode
    {
        public override string Title { get; } = "Combine Vector";
        public static string Group => "Vector";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("X", typeof(float), new Vector2(0, Size.y * 0.5f)));
            _inputs.Add(new NodeInput("Y", typeof(float), new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Z", typeof(float), new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("W", typeof(float), new Vector2(0, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("Vector", typeof(Vector4), new Vector2(Size.x, Size.y * 0.5f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {

        }
    }
}