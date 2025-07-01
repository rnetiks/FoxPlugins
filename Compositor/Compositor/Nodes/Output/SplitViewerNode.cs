using UnityEngine;

namespace Compositor.KK
{
    public class SplitViewerNode : BaseCompositorNode
    {
        public override string Title { get; } = "Split Viewer";
        public static string Group = "Output";
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.5f)));
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.7f)));
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