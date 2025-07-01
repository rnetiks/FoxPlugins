using System;
using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK.Utilities
{
    [Obsolete]
    public class ByteToImageNode : BaseCompositorNode
    {
        public override string Title => "ByteArray to Image";
        protected override void Initialize()
        {
            Size = new Vector2(150, 50);
        }
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Input", SocketType.RGBA, new Vector2(0, Size.y * 0.5f)));
            _outputs.Add(new NodeOutput("Output", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.5f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {
            var input = _inputs[0].GetValue<byte[]>();
            Entry.Logger.LogDebug($"Found {input.Length} bytes");
            var output = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            output.LoadRawTextureData(input);
            output.Apply();
            _outputs[0].SetValue(output);
        }
    }
}