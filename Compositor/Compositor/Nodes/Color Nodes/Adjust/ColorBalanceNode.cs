using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK
{
    public class ColorBalanceNode : BaseCompositorNode
    {
        public override string Title { get; } = "Color Balance";
        public static string Group => "Color/Adjust";

        private int selectionIndex;

        private Dropdown correctionFormat;
        private CompositorGradient curveSelector;

        private int height = 400;
        protected override void Initialize()
        {
            Size = new Vector2(300, height);
            correctionFormat = new Dropdown(new [] { "Lift/Gamma/Gain", "Offset/Power/Slope", "White Point" });
            correctionFormat.OnSelectionChanged += CorrectionFormatOnOnSelectionChanged;
            curveSelector = new CompositorGradient();
        }
        private void CorrectionFormatOnOnSelectionChanged(int obj)
        {
            selectionIndex = obj;
            switch (obj)
            {
                case 0:
                    Size = new Vector2(300, height);
                    break;
                case 1:
                    Size = new Vector2(400, height);
                    break;
                case 2:
                    Size = new Vector2(200, height);
                    break;
            }
        }
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Factor", SocketType.Alpha, new Vector2(0, Size.y - 30)));
            _inputs.Add(new NodeInput("Color", SocketType.RGBA, new Vector2(0, Size.y - 10)));
            _outputs.Add(new NodeOutput("Color", SocketType.RGBA, new Vector2(0, Size.y - 10)) { PortMode = NodeOutput.PortPositioning.Reposition });
        }
        public override void DrawContent(Rect contentRect)
        {
            if (!correctionFormat.IsExpanded)
                curveSelector.Draw(new Rect(contentRect.x, contentRect.y + 20, contentRect.width, contentRect.width));

            correctionFormat.Draw(new Rect(contentRect.x, contentRect.y, contentRect.width, contentRect.width / 2));
        }
        public override void Process()
        {

        }
    }
}