using System.Linq;
using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK
{
    public class AlphaConvertNode : BaseCompositorNode
    {
        public override string Title { get; } = "Alpha Convert";
        public static string Group => "Color";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Image", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
        }

        Dropdown _alphaTypeDropdown;

        protected override void Initialize()
        {
            _alphaTypeDropdown = new Dropdown(new [] { "To Premultiplied", "To Straight" });
        }
        public override void DrawContent(Rect contentRect)
        {
            _alphaTypeDropdown.Draw(contentRect);
        }
        public override void Process()
        {
            switch (_alphaTypeDropdown.SelectedIndex)
            {
                case 0:
                {
                    var input = _inputs[0].GetValue<byte[]>();
                    var output = new byte[input.Length];
                    for (var i = 0; i < input.Length; i+=4)
                    {
                        byte alpha = input[i + 3];
                    
                        output[i] = (byte)(input[i] * alpha);
                        output[i + 1] = (byte)(input[i + 1] * alpha);
                        output[i + 2] = (byte)(input[i + 2] * alpha);
                        output[i + 3] = alpha;
                    }
                    _outputs[0].SetValue(output);
                    break;
                }
                case 1:
                {
                    var input = _inputs[0].GetValue<byte[]>();
                    var output = new byte[input.Length];
                
                    for (var i = 0; i < output.Length; i+=4)
                    {
                        byte alpha = input[i + 3];
                        if (alpha > 0.0001f)
                        {
                            output[i] = (byte)(input[i] / alpha);
                            output[i + 1] = (byte)(input[i + 1] / alpha);
                            output[i + 2] = (byte)(input[i + 2] / alpha);
                        }
                        else
                        {
                            output[i] = 0;
                            output[i + 1] = 0;
                            output[i + 2] = 0;
                        }
                        output[i + 3] = alpha;
                    }
                    
                    _outputs[0].SetValue(output);
                    Dispose();
                    break;
                }
                default:
                    throw new System.NotImplementedException();
            }
        }
    }
}