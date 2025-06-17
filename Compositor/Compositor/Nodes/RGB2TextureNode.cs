using UnityEngine;

namespace Compositor.KK
{
    public class RGB2TextureNode : BaseCompositorNode
    {
        public override string Title { get; } = "RGB2Texture";
        private bool _size;
        protected override void InitializePorts()
        {
            Size = new Vector2(200, 350);
            _inputs.Add(new NodeInput("Format", typeof(TextureFormat), new Vector2(0, Size.y * 0.4f)));
            _inputs.Add(new NodeInput("Size", typeof(Vector2), new Vector2(0, Size.y * 0.5f)));
            _inputs.Add(new NodeInput("R", typeof(float[]), new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("G", typeof(float[]), new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("B", typeof(float[]), new Vector2(0, Size.y * 0.8f)));
            _inputs.Add(new NodeInput("A", typeof(float[]), new Vector2(0, Size.y * 0.9f)));
            _outputs.Add(new NodeOutput("Texture", typeof(Texture2D), new Vector2(Size.x, Size.y - 50)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {
            var size = (Vector2)_inputs[1].Value;
            float[] r = _inputs[2].GetValue<float[]>();
            float[] g = _inputs[3].GetValue<float[]>();
            float[] b = _inputs[4].GetValue<float[]>();
            var tex = new Texture2D((int)size.x, (int)size.y, (TextureFormat)_inputs[0].Value, false);
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    int offset = x + y * tex.width;
                    tex.SetPixel(x, y, new Color(
                        r[offset],
                        g[offset],
                        b[offset])
                    );
                }
            }
            tex.Apply();
            _outputs[0].SetValue(tex);
        }
    }
}