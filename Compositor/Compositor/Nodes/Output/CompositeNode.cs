using System;
using System.IO;
using DefaultNamespace;
using DefaultNamespace.Compositor;
using TexFac.Universal;
using UnityEngine;

namespace Compositor.KK
{
    public class CompositeNode : BaseCompositorNode
    {
        public override string Title => "Composite";
        public static string Group => "Output";
        
        private Texture2D _displayTexture;

        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Texture", typeof(Texture2D), new Vector2(0, Size.y * 0.8f)));
        }

        public override void DrawContent(Rect contentRect)
        {
            if (_displayTexture != null)
            {
                var aspect = (float)_displayTexture.width / _displayTexture.height;
                var textureRect = new Rect(8, 5, contentRect.width - 16, (contentRect.width - 16) / aspect);

                if (textureRect.height > contentRect.height - 35)
                {
                    textureRect.height = contentRect.height - 35;
                    textureRect.width = textureRect.height * aspect;
                    textureRect.x = (contentRect.width - textureRect.width) / 2;
                }

                var borderRect = new Rect(textureRect.x - 1, textureRect.y - 1, textureRect.width + 2, textureRect.height + 2);
                GUI.DrawTexture(borderRect, GUIUtils.GetColorTexture(GUIUtils.Colors.NodeBorder));
                GUI.DrawTexture(textureRect, _displayTexture);

                var buttonRect = new Rect(8, contentRect.height - 25, contentRect.width - 16, 20);
                if (GUI.Button(buttonRect, "Export", CompositorStyles.ExportButton))
                {
                    ExportTexture();
                }
            }
            else
            {
                GUI.Label(new Rect(8, 25, contentRect.width - 16, 30), "No data found", CompositorStyles.NodeContent);
            }
        }

        public override void Process()
        {
            if (_inputs.Count > 0)
            {
                _displayTexture = _inputs[0].GetValue<Texture2D>();
            }
        }

        private void ExportTexture()
        {
            if (_displayTexture != null)
            {
                // TODO change CPUTextureElement with ITextureElement for choice between CPU and GPU
                // var element = new CPUTextureElement(_displayTexture);
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var filename = $"compositor_output_{timestamp}.png";

                File.WriteAllBytes(filename, _displayTexture.EncodeToJPG(100));
                // element.Save(filename);
                if (Entry._openAfterExport.Value)
                {
                    System.Diagnostics.Process.Start(filename);
                }
            }
        }
    }
}