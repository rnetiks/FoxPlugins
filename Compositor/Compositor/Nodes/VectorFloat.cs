using System;
using UnityEngine;

namespace Compositor.KK
{
    public class VectorFloat : BaseCompositorNode
    {
        public override string Title => "Vector to Float";
        public static string Group => "Math";
        
        private float x, y, z, w;
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Vector", new []{typeof(Vector2), typeof(Vector3), typeof(Vector4)}, new Vector2(0, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("X", typeof(float), new Vector2(Size.x, Size.y * 0.5f)));
            _outputs.Add(new NodeOutput("Y", typeof(float), new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Z", typeof(float), new Vector2(Size.x, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("W", typeof(float), new Vector2(Size.x, Size.y * 0.8f)));
        }
        public override void DrawContent(Rect contentRect) { }
        public override void Process()
        {
            x = 0f;
            y = 0f;
            z = 0f;
            w = 0f;
            var input = _inputs[0].Value;
            switch (input)
            {
                case Vector2 v2:
                    x = v2.x;
                    y = v2.y;
                    break;
                case Vector3 v3:
                    x = v3.x;
                    y = v3.y;
                    z = v3.z;
                    break;
                case Vector4 v4:
                    x = v4.x;
                    y = v4.y;
                    z = v4.z;
                    w = v4.w;
                    break;
            }
            
            _outputs[0].SetValue(x);
            _outputs[1].SetValue(y);
            _outputs[2].SetValue(z);
            _outputs[3].SetValue(w);
        }
    }
}