using System;
using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK.Utilities
{
    public class MathNode : BaseCompositorNode
    {
        public override string Title { get; } = "Math";
        public static string Group => "Utilities";

        private enum mode
        {
            add,
            sub,
            mul,
            div,
        }

        private mode _mode;
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("In", SocketType.A, new Vector2(0, Size.y * 0.5f)));
            _inputs.Add(new NodeInput("In", SocketType.A, new Vector2(0, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Out", SocketType.A, new Vector2(Size.x, Size.y * 0.5f)));
        }
        public override void DrawContent(Rect contentRect)
        {
            Entry.Logger.LogDebug(contentRect);
            if (GUI.Button(new Rect(contentRect.x, 0, contentRect.width, 20), _mode.ToString()))
            {
                _mode = (mode)(((int)_mode + 1) % Enum.GetValues(typeof(mode)).Length);
            }

            if (!_inputs[0].IsConnected)
                GUI.HorizontalSlider(new Rect(contentRect.x + 30, _inputs[0].LocalPosition.y - 6, contentRect.width - 60, 20), 0, 1, 0);
            if(!_inputs[1].IsConnected)
                GUI.HorizontalSlider(new Rect(contentRect.x + 30, _inputs[1].LocalPosition.y - 6, contentRect.width - 60, 20), 0, 1, 0);
        }
        public override void Process()
        {
            switch (_mode)
            {

                case mode.add:
                    _outputs[0].SetValue((float)_inputs[0].Value + (float)_inputs[1].Value);
                    break;
                case mode.sub:
                    _outputs[0].SetValue((float)_inputs[0].Value - (float)_inputs[1].Value);
                    break;
                case mode.mul:
                    _outputs[0].SetValue((float)_inputs[0].Value * (float)_inputs[1].Value);
                    break;
                case mode.div:
                    _outputs[0].SetValue((float)_inputs[0].Value / (float)_inputs[1].Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}