using System;
using UnityEngine;

namespace Compositor.KK
{
    public class ColorRampNode : BaseCompositorNode
    {
        public override string Title { get; } = "Color Ramp";
        public static string Group => "Color";
        protected override void InitializePorts()
        {
            _outputs.Add(new NodeOutput("Image", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("Alpha", SocketType.Alpha, new Vector2(Size.x, Size.y * 0.7f)));
        }
        public override void DrawContent(Rect contentRect)
        {
            throw new System.NotImplementedException();
        }
        public override void Process()
        {
            throw new System.NotImplementedException();
        }
    }
}