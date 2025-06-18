using UnityEngine;

namespace Compositor.KK.Layout
{
    public class RerouteNode : BaseCompositorNode
    {
        public override string Title { get; } = "Reroute";
        public static string Group => "Layout";
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