using UnityEngine;

namespace Compositor.KK.Vector
{
    public class NormalNode : BaseCompositorNode
    {
        public override string Title { get; } = "Normal";
        public static string Group => "Vector";
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