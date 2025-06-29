using UnityEngine;

namespace Compositor.KK.Vector
{
    public class CombineVectorNode : BaseCompositorNode
    {
        public override string Title { get; } = "Combine Vector";
        public static string Group => "Vector";

        private float[] x;
        private float[] y;
        private float[] z;
        private float[] w;
        private float sliderX, sliderY, sliderZ, sliderW;
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("X", typeof(float[]), new Vector2(0, Size.y * 0.5f)));
            _inputs.Add(new NodeInput("Y", typeof(float[]), new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Z", typeof(float[]), new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("W", typeof(float[]), new Vector2(0, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("Vector", typeof(Vector4[]), new Vector2(Size.x, Size.y * 0.5f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {
            if (_inputs.Count < 4 ||
                !(_inputs[0].Value is float[] xValues) ||
                !(_inputs[1].Value is float[] yValues) ||
                !(_inputs[2].Value is float[] zValues) ||
                !(_inputs[3].Value is float[] wValues))
            {
                return;
            }

            int maxComponentCount = Mathf.Max(xValues.Length, yValues.Length, zValues.Length, wValues.Length);
            Vector4[] vectorArray = CreateVectorArray(xValues, yValues, zValues, wValues, maxComponentCount);
            _outputs[0].SetValue(vectorArray);
        }

        private Vector4[] CreateVectorArray(float[] xValues, float[] yValues, float[] zValues, float[] wValues, int size)
        {
            var vectorArray = new Vector4[size];
            for (int i = 0; i < size; i++)
            {
                float x = i < xValues.Length ? xValues[i] : 0f;
                float y = i < yValues.Length ? yValues[i] : 0f;
                float z = i < zValues.Length ? zValues[i] : 0f;
                float w = i < wValues.Length ? wValues[i] : 0f;
                vectorArray[i] = new Vector4(x, y, z, w);
            }
            return vectorArray;
        }
    }
}