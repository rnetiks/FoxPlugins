using System;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using Array = Compositor.KK.Compositor.Array;

namespace Compositor.KK
{
    /// <summary>
    /// The Z Combine node combines two images based on their Z-depth maps.
    /// It overlays the images using the provided Z values to detect which parts of one image are in front of the other 
    /// </summary>
    public class ZCombineNode : LazyCompositorNode
    {
        public override string Title => "Z Combine";
        public static string Group => "Color/Mix";

        byte image1Value = Byte.MaxValue, image2Value = Byte.MaxValue, z1Value = Byte.MaxValue, z2Value = Byte.MaxValue;
        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.6f)));
            _inputs.Add(new NodeInput("Z", SocketType.A, new Vector2(0, Size.y * 0.7f)));
            _inputs.Add(new NodeInput("Image", SocketType.RGBA, new Vector2(0, Size.y * 0.8f)));
            _inputs.Add(new NodeInput("Z", SocketType.A, new Vector2(0, Size.y * 0.9f)));
            _outputs.Add(new NodeOutput("Image", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Z", SocketType.A, new Vector2(Size.x, Size.y * 0.7f)));
        }
        public override void DrawContent(Rect contentRect)
        {
            float offsetY = 5;

            if (!_inputs[0].IsConnected)
            {
                var spos = CompositorRenderer.Instance.GetPortScaledPosition(_inputs[0].LocalPosition);
                var tmp = (byte)GUI.HorizontalSlider(new Rect(contentRect.x + 40, spos.y - offsetY, contentRect.width - 80, 20), image1Value, 0, 255);
                if (tmp != image1Value)
                {
                    image1Value = tmp;
                    NotifyOutputChanged();
                }
            }
            if (!_inputs[1].IsConnected)
            {
                var spos = CompositorRenderer.Instance.GetPortScaledPosition(_inputs[1].LocalPosition);
                var tmp = (byte)GUI.HorizontalSlider(new Rect(contentRect.x + 40, spos.y - offsetY, contentRect.width - 80, 20), z1Value, 0, 255);
                if (tmp != z1Value)
                {
                    z1Value = tmp;
                    NotifyOutputChanged();
                }
            }
            if (!_inputs[2].IsConnected)
            {
                var spos = CompositorRenderer.Instance.GetPortScaledPosition(_inputs[2].LocalPosition);
                var tmp = (byte)GUI.HorizontalSlider(new Rect(contentRect.x + 40, spos.y - offsetY, contentRect.width - 80, 20), image2Value, 0, 255);
                if (tmp != image2Value)
                {
                    image2Value = tmp;
                    NotifyOutputChanged();
                }
            }
            if (!_inputs[3].IsConnected)
            {
                var spos = CompositorRenderer.Instance.GetPortScaledPosition(_inputs[3].LocalPosition);
                var tmp = (byte)GUI.HorizontalSlider(new Rect(contentRect.x + 40, spos.y - offsetY, contentRect.width - 80, 20), z2Value, 0, 255);
                if (tmp != z2Value)
                {
                    z2Value = tmp;
                    NotifyOutputChanged();
                }
            }
        }
        protected override void ProcessInternal()
        {
        }
    }
}