using UnityEngine;

namespace Compositor.KK.Utilities
{
    public class MathNode : BaseCompositorNode
    {
        public override string Title { get; } = "Math";
        public static string Group => "Utilities";
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