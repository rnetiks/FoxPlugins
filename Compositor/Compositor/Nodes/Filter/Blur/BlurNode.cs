using UnityEngine;

namespace Compositor.KK.Filter.Blur
{
    public class Blur : BaseCompositorNode
    {
        public override string Title { get; } = "Blur";
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