using UnityEngine;

namespace Compositor.KK.Vector
{
    public class CombineVectorNode : BaseCompositorNode
    {
        public override string Title { get; } = "Combine Vector";
        public static string Group => "Vector";

        private float[] x;
        private float[] y;
        private float[] z;
        private float[] w;
        private float sliderX, sliderY, sliderZ, sliderW;
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("X", SocketType.Alpha, new Vector2(0, Size.y * 0.5f)));
            _inputs.Add(new NodeInput("Y", SocketType.Alpha, new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Z", SocketType.Alpha, new Vector2(0, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("Vector", SocketType.Vector, new Vector2(Size.x, Size.y * 0.5f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {

        }
    }
}