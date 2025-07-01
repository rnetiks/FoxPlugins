using UnityEngine;

namespace Compositor.KK
{
    /// <summary>
    /// The Separate Color Node splits an image into its composite color channels.
    /// The node can output multiple Color Models depending on the Mode property.
    /// </summary>
    public class SeperateColorNode : BaseCompositorNode
    {
        public override string Title { get; } = "Seperate Color";
        public static string Group => "Color/Mix";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Red", SocketType.A, new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Green", SocketType.A, new Vector2(Size.x, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("Blue", SocketType.A, new Vector2(Size.x, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("Alpha", SocketType.A, new Vector2(Size.x, Size.y * 0.9f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {
            if (_outputs[0].Connections.Count > 0 || _outputs[1].Connections.Count > 0 || _outputs[2].Connections.Count > 0 || _outputs[3].Connections.Count > 0)
            {
                var image = _inputs[0].GetValue<byte[]>();
                if (image == null) return;

                var r = new byte[image.Length];
                var g = new byte[image.Length];
                var b = new byte[image.Length];
                var a = new byte[image.Length];

                for (var i = 0; i < image.Length; i += 4)
                {
                    r[i] = image[i];
                    r[i + 1] = 0;
                    r[i + 2] = 0;
                    r[i + 3] = 255;

                    g[i] = 0;
                    g[i + 1] = image[i + 1];
                    g[i + 2] = 0;
                    g[i + 3] = 255;

                    b[i] = 0;
                    b[i + 1] = 0;
                    b[i + 2] = image[i + 2];
                    b[i + 3] = 255;

                    a[i] = 0;
                    a[i + 1] = 0;
                    a[i + 2] = 0;
                    a[i + 3] = image[i + 3];
                }

                if (_outputs[0].Connections.Count > 0)
                    _outputs[0].SetValue(r);

                if (_outputs[1].Connections.Count > 0)
                    _outputs[1].SetValue(g);

                if (_outputs[2].Connections.Count > 0)
                    _outputs[2].SetValue(b);

                if (_outputs[3].Connections.Count > 0)
                    _outputs[3].SetValue(a);
            }
        }
    }
}