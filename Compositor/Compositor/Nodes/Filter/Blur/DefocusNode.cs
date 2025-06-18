using UnityEngine;

namespace Compositor.KK.Filter.Blur
{
    public class DefocusNode : BaseCompositorNode
    {
        public override string Title { get; } = "Defocus";
        public static string Group => "Filter/Blur";
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