using UnityEngine;

namespace Compositor.KK.Transform
{
    public class RotateNode : BaseCompositorNode
    {
        public override string Title { get; } = "Rotate";
        public static string Group => "Transform";
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