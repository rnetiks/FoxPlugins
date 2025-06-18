using UnityEngine;

namespace Compositor.KK
{
    public class ColorRampNode : BaseCompositorNode
    {
        public override string Title { get; } = "Color Ramp";
        public static string Group => "Color";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Factor", typeof(float), new Vector2(0, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Image", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Alpha", typeof(float[]), new Vector2(Size.x, Size.y * 0.7f)));
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