using System.Linq;
using Compositor.KK.Utils;
using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK
{
    public class ColorBalanceNode : BaseCompositorNode
    {
        public override string Title { get; } = "Color Balance";
        public static string Group => "Color/Adjust";

        private int selectionIndex;

        private Dropdown _correctionFormat;
        private CompositorColorSelector _colorSelector1;
        private CompositorColorSelector _colorSelector2;
        private CompositorColorSelector _colorSelector3;
        private CompositorSlider _factorSlider;
        private CompositorSlider _basisSlider;

        private float _factor = 1f;
        private float _basis = 0f;

        protected override void Initialize()
        {
            Size = new Vector2(600, 400);
            _correctionFormat = new Dropdown(new []
            {
                "Lift/Gamma/Gain", "Offset/Power/Slope"
            })
            {
                SelectedIndex = 0,
                MaxHeight = 300
            };
            _correctionFormat.OnSelectionChanged += CorrectionFormatOnOnSelectionChanged;
            
            _colorSelector1 = new CompositorColorSelector(Color.white);
            _colorSelector2 = new CompositorColorSelector(Color.white);
            _colorSelector3 = new CompositorColorSelector(Color.white);
            
            _factorSlider = new CompositorSlider(1, 0, 1, 1, "Fac");
            _factorSlider.OnValueChanged += f => _factor = f;
            
            _basisSlider = new CompositorSlider(0, 0, 1, 0, "Basis");
            _basisSlider.OnValueChanged += f => _basis = f;
        }

        private void CorrectionFormatOnOnSelectionChanged(int obj)
        {
            // On format change, reset the colors to default
            _colorSelector1.SetColor(Color.white);
            _colorSelector2.SetColor(Color.white);
            _colorSelector3.SetColor(Color.white);

            selectionIndex = obj;
        }

        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Factor", SocketType.Alpha, new Vector2(0, Size.y - 30)));
            _inputs.Add(new NodeInput("Color", SocketType.RGBA, new Vector2(0, Size.y - 10)));
            _outputs.Add(new NodeOutput("Color", SocketType.RGBA, new Vector2(Size.x, Size.y - 10)));
        }

        public override void DrawContent(Rect contentRect)
        {
            _correctionFormat.Draw(new Rect(contentRect.x, contentRect.y, contentRect.width, 20));
            if (_correctionFormat.IsExpanded)
                return;

            float width = contentRect.width / 3;

            _colorSelector1.Draw(new Rect(contentRect.x, 50, width, width));
            _colorSelector2.Draw(new Rect(contentRect.x + width, 50, width, width));
            _colorSelector3.Draw(new Rect(contentRect.x + width * 2, 50, width, width));

            if (selectionIndex == 0)
            {
                GUI.Label(new Rect(0, 270, 200, 20), "Lift");
                GUI.Label(new Rect(200, 270, 200, 20), "Gamma");
                GUI.Label(new Rect(400, 270, 200, 20), "Gain");
            }
            else
            {
                GUI.Label(new Rect(0, 270, 200, 20), "Offset");
                GUI.Label(new Rect(200, 270, 200, 20), "Power");
                GUI.Label(new Rect(400, 270, 200, 20), "Slope");
            }
            if (!_inputs[0].IsConnected)
            {
                var portScaledPosition = CompositorRenderer.Instance.GetPortScaledPosition(_inputs[0].LocalPosition).Move(50, -10);
                _factorSlider.Draw(new Rect(portScaledPosition, new Vector2(100, 20)).ResizeX(300));;
            }
        }

        public override void Process()
        {
            // TODO add Factory
            float[] data = _inputs[1].GetValue<float[]>();

            _outputs[0].SetValue(selectionIndex == 0
                ? ApplyLiftGammaGain(data, _colorSelector1.SelectedColor, _colorSelector2.SelectedColor, _colorSelector3.SelectedColor)
                : ApplyOffsetPowerSlope(data, _colorSelector1.SelectedColor, _colorSelector2.SelectedColor, _colorSelector3.SelectedColor));
        }

        private float[] ApplyLiftGammaGain(float[] input, Color lift, Color gamma, Color gain)
        {
            float[] result = new float[input.Length];
            for (var i = 0; i < input.Length; i+=4)
            {
                float r = input[i];
                float g = input[i + 1];
                float b = input[i + 2];

                r = (r - 1f) * lift.r + 1f;
                g = (g - 1f) * lift.g + 1;
                b = (b - 1f) * lift.b + 1;

                float gammaR = gamma.r == 0f ? 1e-6f : gamma.r;
                float gammaG = gamma.g == 0f ? 1e-6f : gamma.g;
                float gammaB = gamma.b == 0f ? 1e-6f : gamma.b;

                r = Mathf.Pow(Mathf.Max(r, 0f), 1f / gammaR);
                g = Mathf.Pow(Mathf.Max(g, 0f), 1f / gammaG);
                b = Mathf.Pow(Mathf.Max(b, 0f), 1f / gammaB);

                r *= gain.r;
                g *= gain.g;
                b *= gain.b;

                r = Mathf.Clamp01(r);
                g = Mathf.Clamp01(g);
                b = Mathf.Clamp01(b);
                
                result[i] = r;
                result[i + 1] = g;
                result[i + 2] = b;
                result[i + 3] = input[i + 3];
            }

            return result;
        }

        private float[] ApplyOffsetPowerSlope(float[] input, Color offset, Color power, Color slope)
        {
            float[] result = new float[input.Length];
            for (var i = 0; i < input.Length; i+=4)
            {
                float r = input[i];
                float g = input[i + 1];
                float b = input[i + 2];

                r = r * slope.r + offset.r;
                g = g * slope.g + offset.g;
                b = b * slope.b + offset.b;

                r = Mathf.Max(r, 0f);
                g = Mathf.Max(g, 0f);
                b = Mathf.Max(b, 0f);
                
                float powerR = power.r == 0f ? 1e-6f : power.r;
                float powerG = power.g == 0f ? 1e-6f : power.g;
                float powerB = power.b == 0f ? 1e-6f : power.b;
                
                r = Mathf.Pow(r, powerR);
                g = Mathf.Pow(g, powerG);
                b = Mathf.Pow(b, powerB);
                
                r = Mathf.Clamp01(r);
                g = Mathf.Clamp01(g);
                b = Mathf.Clamp01(b);
                
                result[i] = r;
                result[i + 1] = g;
                result[i + 2] = b;
                result[i + 3] = input[i + 3];
            }
            
            return result;
        }
    }
}