using UnityEngine;

namespace Compositor.KK.Mask
{
    public class BoxMaskNode : BaseCompositorNode
    {
        public override string Title { get; } = "Box Mask";
        public static string Group => "Mask";
        protected override void InitializePorts()
        {
            throw new System.NotImplementedException();
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