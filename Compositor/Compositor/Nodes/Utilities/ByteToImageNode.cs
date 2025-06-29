using UnityEngine;

namespace Compositor.KK.Utilities
{
    public class ByteToImageNode : BaseCompositorNode
    {
        public override string Title => "ByteArray to Image";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Input", typeof(byte[]), new Vector2(0, Size.x * 0.5f)));
            _outputs.Add(new NodeOutput("Output", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.5f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {
            var input = _inputs[0].GetValue<byte[]>();
            var output = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            output.LoadRawTextureData(input);
            output.Apply();
            _outputs[0].SetValue(output);
        }
    }
}