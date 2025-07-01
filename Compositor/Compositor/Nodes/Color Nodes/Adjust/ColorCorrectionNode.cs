using RootMotion.FinalIK;
using UnityEngine;

namespace Compositor.KK
{
    public class ColorCorrectionNode : BaseCompositorNode
    {
        public override string Title { get; } = "Color Correction";
        public static string Group => "Color/Adjust";

        private bool red;
        private bool green;
        private bool blue;

        private float master_saturation, master_contrast, master_gamma, master_gain, master_lift;
        private float highlight_saturation, highlight_contrast, highlight_gamma, highlight_gain, highlight_lift;
        private float midtones_saturation, midtones_contrast, midtones_gamma, midtones_gain, midtones_lift;
        private float shadows_saturation, shadows_contrast, shadows_gamma, shadows_gain, shadows_lift;
        private float midtones_start, midtones_end;
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Mask", SocketType.A, new Vector2(0, Size.y * 0.7f)));
            _outputs.Add(new NodeOutput("Image", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
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