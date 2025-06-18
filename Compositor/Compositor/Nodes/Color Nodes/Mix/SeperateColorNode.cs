using UnityEngine;

namespace Compositor.KK
{
    public class SeperateColorNode : BaseCompositorNode
    {
        public override string Title { get; } = "Seperate Color";
        public static string Group => "Color/Mix";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", typeof(Texture2D), new Vector2(0, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Red", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Green", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("Blue", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("Alpha", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.9f)));
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