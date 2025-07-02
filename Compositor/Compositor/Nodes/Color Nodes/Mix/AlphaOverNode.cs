using Compositor.KK.Compositor;
using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK
{
    /// <summary>
    /// The Alpha Over node is used to layer an image on top of another with alpha blending.
    /// </summary>
    public class AlphaOverNode : BaseCompositorNode
    {
        public override string Title { get; } = "Alpha Over";
        public static string Group => "Color/Mix";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("Image", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.7f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {
            if (_inputs[0].IsConnected && _inputs[1].IsConnected)
            {
                var backgroundData = _inputs[0].GetValue<float[]>();
                var foregroundData = _inputs[1].GetValue<float[]>();

                if (backgroundData == null || foregroundData == null) return;

                ManagedArrayData result = new ManagedArrayData(backgroundData.Length);

                for (int i = 0; i < backgroundData.Length; i += 4)
                {
                    float bgR = backgroundData[i];
                    float bgG = backgroundData[i + 1];
                    float bgB = backgroundData[i + 2];
                    float bgA = backgroundData[i + 3];

                    float fgR = foregroundData[i];
                    float fgG = foregroundData[i + 1];
                    float fgB = foregroundData[i + 2];
                    float fgA = foregroundData[i + 3];

                    float outA = fgA + bgA * (1f - fgA);
                    float outR = (fgR * fgA + bgR * bgA * (1f - fgA)) / outA;
                    float outG = (fgG * fgA + bgG * bgA * (1f - fgA)) / outA;
                    float outB = (fgB * fgA + bgB * bgA * (1f - fgA)) / outA;

                    result.Data[i] = outR;
                    result.Data[i + 1] = outG;
                    result.Data[i + 2] = outB;
                    result.Data[i + 3] = outA;
                }

                _outputs[0].SetValue(result.Data);
                result.Dispose();
            }
        }
    }
}