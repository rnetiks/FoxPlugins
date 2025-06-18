using UnityEngine;

namespace Compositor.KK
{
    public class GlareNode : BaseCompositorNode
    {
        public override string Title { get; } = "Glare";
        public static string Group => "Filter";
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