using UnityEngine;

namespace Compositor.KK.Utilities
{
    public class SwitchViewNode : BaseCompositorNode
    {
        public override string Title { get; } = "Switch View";
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