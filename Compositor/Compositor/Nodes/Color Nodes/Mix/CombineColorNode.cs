using UnityEngine;

namespace Compositor.KK
{
    /// <summary>
    /// The Combine Color Node combines an image from its composite color channels.
    /// The node can combine multiple Color Models depending on the Mode property.
    /// </summary>
    public class CombineColorNode : BaseCompositorNode
    {
        public override string Title { get; } = "Combine Color";
        public static string Group => "Color/Mix";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Red", SocketType.Alpha, new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Green", SocketType.Alpha, new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("Blue", SocketType.Alpha, new Vector2(0, Size.y * 0.8f)));
            _inputs.Add(new NodeInput("Alpha", SocketType.Alpha, new Vector2(0, Size.y * 0.9f)));
            _outputs.Add(new NodeOutput("Image", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        /// <summary>
        /// Processes the inputs of the CombineColorNode and combines them into a single output.
        /// </summary>
        /// <remarks>
        /// The method reads the input channels (red, green, blue, and alpha) as byte arrays,
        /// combines them to create an interleaved RGBA byte array, and sets the combined data
        /// to the output. The length of the resulting output array is based on the largest input array.
        /// For any missing data in a channel beyond its length, the default value of 0 is used.
        /// The method only executes if the output has active connections.
        /// </remarks>
        public override void Process()
        {
            if (_outputs[0].Connections.Count > 0)
            {
                var r = _inputs[0].GetValue<float[]>();
                var g = _inputs[1].GetValue<float[]>();
                var b = _inputs[2].GetValue<float[]>();
                var a = _inputs[3].GetValue<float[]>();
                var arrSize = Mathf.Max(r.Length, g.Length, b.Length, a.Length);

                var result = new float[arrSize];
                for (var i = 0; i < arrSize; i += 4)
                {
                    result[i] = i < r.Length ? r[i] : 0;
                    result[i + 1] = i < g.Length ? g[i + 1] : 0;
                    result[i + 2] = i < b.Length ? b[i + 2] : 0;
                    result[i + 3] = i < a.Length ? a[i + 3] : 0;
                }
                _outputs[0].SetValue(result);
            }
        }
    }
}