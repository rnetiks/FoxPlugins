using Illusion.Extensions;
using UnityEngine;

namespace Compositor.KK
{
    public class RGB2HSVNode : BaseCompositorNode
    {
        public override string Title => "RGB2HSV";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("R", typeof(float), new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("G", typeof(float), new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("B", typeof(float), new Vector2(0, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("H", typeof(float), new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("S", typeof(float), new Vector2(Size.x, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("V", typeof(float), new Vector2(Size.x, Size.y * 0.8f)));
        }
        public override void DrawContent(Rect contentRect)
        {
            
        }
        public override void Process()
        {
            var r = _inputs[0].IsConnected ? (float)_inputs[0].Value : 0.0f;
            var g = _inputs[1].IsConnected ? (float)_inputs[1].Value : 0.0f;
            var b = _inputs[2].IsConnected ? (float)_inputs[2].Value : 0.0f;
            var hsv = HsvColor.FromRgb(new Color(r, g, b));
            _outputs[0].SetValue(hsv.H);
            _outputs[1].SetValue(hsv.S);
            _outputs[2].SetValue(hsv.V);
        }
    }
}