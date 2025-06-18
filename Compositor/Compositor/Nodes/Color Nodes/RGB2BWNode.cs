using UnityEngine;

namespace Compositor.KK
{
    public class RGB2BWNode : BaseCompositorNode
    {
        public override string Title { get; } = "RGB to BW";
        public static string Group => "Color";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", typeof(Texture2D), new Vector2(0, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Value", typeof(float), new Vector2(Size.x, Size.y * 0.6f)));
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