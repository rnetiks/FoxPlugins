using UnityEngine;

namespace Compositor.KK.Vector
{
    public class ImageSizeNode : BaseCompositorNode
    {
        public override string Title { get; }
        public static string Group => "Vector";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Texture", SocketType.RGBA, new Vector2(0, Size.y * 0.5f)));
            _outputs.Add(new NodeOutput("Size", SocketType.Vector, new Vector2(Size.x, Size.y * 0.5f)));
        }
        public override void DrawContent(Rect contentRect)
        {
        }
        public override void Process()
        {
            var nodeInput = _inputs[0];
            var texture2D = nodeInput.GetValue<Texture2D>();
            if (nodeInput.IsConnected && texture2D != null)
            {
                var texture = texture2D;
                _outputs[0].SetValue(new Vector2(texture.width, texture.height));
            }
        }
    }
}