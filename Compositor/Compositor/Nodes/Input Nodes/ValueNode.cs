using UnityEngine;

namespace Compositor.KK
{
    public class ValueNode : BaseCompositorNode
    {
        public override string Title { get; } = "Value";
        public static string Group => "Input";
        protected override void InitializePorts()
        {
            _outputs.Add(new NodeOutput("Value", typeof(float), new Vector2(Size.x, Size.y * 0.6f)));
        }
        public override void DrawContent(Rect contentRect)
        {

        }
        public override void Process()
        {

        }
    }
}