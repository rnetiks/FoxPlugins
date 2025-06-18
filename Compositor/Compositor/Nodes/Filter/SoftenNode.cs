using UnityEngine;

namespace Compositor.KK.Filter
{
    public class SoftenNode : BaseCompositorNode
    {
        public override string Title { get; } = "Soften";
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