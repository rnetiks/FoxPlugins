using UnityEngine;

namespace Compositor.KK
{
    public class RGB2HSVArrayNode : BaseCompositorNode
    {
        public override string Title { get; } = "RGB2HSVArray";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("R", typeof(float[]), new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("G", typeof(float[]), new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("B", typeof(float[]), new Vector2(0, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("H", typeof(float[]), new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("S", typeof(float[]), new Vector2(Size.x, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("V", typeof(float[]), new Vector2(Size.x, Size.y * 0.8f)));
        }
        public override void DrawContent(Rect contentRect) { }
        public override void Process()
        {
            var length = Inputs[0].GetValue<float[]>().Length;
            float[] h = new float[length];
            float[] s = new float[length];
            float[] v = new float[length];

            var r = Inputs[0].GetValue<float[]>();
            var g = Inputs[1].GetValue<float[]>();
            var b = Inputs[2].GetValue<float[]>();
            
            for (int i = 0; i < length; i++)
            {
                var hsvColor = HsvColor.FromRgb(new Color(r[i], g[i], b[i]));
                h[i] = hsvColor.H;
                s[i] = hsvColor.S;
                v[i] = hsvColor.V;
            }

            _outputs[0].SetValue(h);
            _outputs[1].SetValue(s);
            _outputs[2].SetValue(v);
        }
    }
}