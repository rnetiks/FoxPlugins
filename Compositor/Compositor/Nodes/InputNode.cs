using System.ComponentModel;
using DefaultNamespace.Compositor;
using Unity.Linq;
using UnityEngine;

namespace Compositor.KK
{
    public class InputNode : BaseCompositorNode
    {
        public override string Title => "Input";
        private Texture2D _currentTexture;

        protected override void InitializePorts()
        {
            _outputs.Add(new NodeOutput("Texture", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.8f)));
        }

        public override void DrawContent(Rect contentRect)
        {
            if (_currentTexture != null)
            {
                var aspect = (float)_currentTexture.width / _currentTexture.height;
                var textureRect = new Rect(8, 5, contentRect.width - 16, (contentRect.width - 16) / aspect);

                if (textureRect.height > contentRect.height - 25)
                {
                    textureRect.height = contentRect.height - 25;
                    textureRect.width = textureRect.height * aspect;
                    textureRect.x = (contentRect.width - textureRect.width) / 2;
                }

                var borderRect = new Rect(textureRect.x - 1, textureRect.y - 1, textureRect.width + 2, textureRect.height + 2);
                GUI.DrawTexture(borderRect, GUIUtils.GetColorTexture(GUIUtils.Colors.NodeBorder));
                GUI.DrawTexture(textureRect, _currentTexture);

                var infoText = $"{_currentTexture.width}x{_currentTexture.height}";
                var infoRect = new Rect(8, textureRect.y + textureRect.height + 5, contentRect.width - 16, 15);
                GUI.Label(infoRect, infoText, CompositorStyles.NodeContent);
            }
            else
            {
                GUI.Label(new Rect(8, 25, contentRect.width - 16, 30), "Waiting for image...", CompositorStyles.NodeContent);
            }
        }

        public override void Process()
        {
            _currentTexture = TextureCache.GetLatestTexture();

            if (_outputs.Count > 0)
            {
                _outputs[0].SetValue(_currentTexture);
            }
        }
    }
}