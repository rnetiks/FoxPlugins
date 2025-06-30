using UnityEngine;

namespace Compositor.KK
{
    /// <summary>
    /// The Z Combine node combines two images based on their Z-depth maps.
    /// It overlays the images using the provided Z values to detect which parts of one image are in front of the other 
    /// </summary>
    public class ZCombineNode : BaseCompositorNode
    {
        public override string Title { get; } = "Z Combine";
        public static string Group => "Color/Mix";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", typeof(Texture2D), new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Z", typeof(float), new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("Image", typeof(Texture2D), new Vector2(0, Size.y * 0.8f)));
            _inputs.Add(new NodeInput("Z", typeof(float), new Vector2(0, Size.y * 0.9f)));
            _outputs.Add(new NodeOutput("Image", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Z", typeof(float), new Vector2(Size.x, Size.y * 0.7f)));
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