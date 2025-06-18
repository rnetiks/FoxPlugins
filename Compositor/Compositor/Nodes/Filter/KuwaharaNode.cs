using UnityEngine;

namespace Compositor.KK.Compositor.Nodes.Filter
{
    public class KuwaharaNode : BaseCompositorNode
    {
        public override string Title { get; } = "Kuwahara";
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