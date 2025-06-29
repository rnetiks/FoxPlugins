using UnityEngine;

namespace Compositor.KK
{
    public class AlphaOverNode : BaseCompositorNode
    {
        public override string Title { get; } = "Alpha Over";
        public static string Group => "Color/Mix";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", typeof(byte[]), new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("Image", typeof(byte[]), new Vector2(0, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("Image", typeof(byte[]), new Vector2(Size.x, Size.y * 0.6f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {
            if (_inputs[0].IsConnected && _inputs[1].IsConnected)
            {
                var backgroundData = _inputs[0].GetValue<byte[]>();
                var foregroundData = _inputs[1].GetValue<byte[]>();
        
                if (backgroundData == null || foregroundData == null) return;
        
                byte[] result = new byte[backgroundData.Length];
        
                for (int i = 0; i < backgroundData.Length; i += 4)
                {
                    // Background RGBA
                    float bgR = backgroundData[i] / 255f;
                    float bgG = backgroundData[i + 1] / 255f;
                    float bgB = backgroundData[i + 2] / 255f;
                    float bgA = backgroundData[i + 3] / 255f;
            
                    // Foreground RGBA
                    float fgR = foregroundData[i] / 255f;
                    float fgG = foregroundData[i + 1] / 255f;
                    float fgB = foregroundData[i + 2] / 255f;
                    float fgA = foregroundData[i + 3] / 255f;
            
                    // Alpha over blending
                    float outA = fgA + bgA * (1f - fgA);
                    float outR = (fgR * fgA + bgR * bgA * (1f - fgA)) / outA;
                    float outG = (fgG * fgA + bgG * bgA * (1f - fgA)) / outA;
                    float outB = (fgB * fgA + bgB * bgA * (1f - fgA)) / outA;
            
                    // Convert back to bytes
                    result[i] = (byte)(outR * 255f);
                    result[i + 1] = (byte)(outG * 255f);
                    result[i + 2] = (byte)(outB * 255f);
                    result[i + 3] = (byte)(outA * 255f);
                }
        
                _outputs[0].SetValue(result);
            }
        }
    }
}