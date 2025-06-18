using UnityEngine;

namespace Compositor.KK
{
    public class CombineColorNode : BaseCompositorNode
    {
        public override string Title { get; } = "Combine Color";
        public static string Group => "Color/Mix";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Red", typeof(float), new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Green", typeof(float), new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("Blue", typeof(float), new Vector2(0, Size.y * 0.8f)));
            _inputs.Add(new NodeInput("Alpha", typeof(float), new Vector2(0, Size.y * 0.9f)));
            _outputs.Add(new NodeOutput("Image", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.6f)));
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