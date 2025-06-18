using UnityEngine;

namespace Compositor.KK
{
    public class ColorBalanceNode : BaseCompositorNode
    {
        public override string Title { get; } = "Color Balance";
        public static string Group => "Color/Adjust";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Factor", typeof(float), new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Image", typeof(Texture2D), new Vector2(0, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("Image", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.6f)));;
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