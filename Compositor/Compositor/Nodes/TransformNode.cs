using DefaultNamespace.Compositor;
using TexFac.Universal;
using UIBuilder;
using UnityEngine;

namespace Compositor.KK
{
    public class TransformNode : BaseCompositorNode
    {
        public override string Title => "Transform";
        private TransformType _selectedTransform = TransformType.None;
        private float _scaleValue = 1.0f;
        private float _rotationValue = 0.0f;
        private Vector2 _translateValue = Vector2.zero;
        private Texture2D _processedTexture;

        public enum TransformType
        {
            None,
            Scale,
            Rotate,
            Translate,
            Crop
        }

        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Input", typeof(Texture2D), new Vector2(0, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("Output", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.8f)));
        }

        public override void DrawContent(Rect contentRect)
        {
            var transformNames = System.Enum.GetNames(typeof(TransformType));
            var currentIndex = (int)_selectedTransform;

            float buttonWidth = (contentRect.width - 16) / 2f - 2f;
            float buttonHeight = 18f;
            float startY = 25f;

            for (int i = 0; i < transformNames.Length; i++)
            {
                int row = i / 2;
                int col = i % 2;
                var buttonRect = new Rect(8 + col * (buttonWidth + 2), startY + row * (buttonHeight + 2), buttonWidth, buttonHeight);
                var buttonStyle = (i == currentIndex) ? CompositorStyles.FilterButtonSelected : CompositorStyles.FilterButton;

                if (GUI.Button(buttonRect, transformNames[i], buttonStyle))
                {
                    _selectedTransform = (TransformType)i;
                }
            }

            float paramY = startY + (transformNames.Length / 2 + 1) * (buttonHeight + 2) + 10;
            switch (_selectedTransform)
            {
                case TransformType.Scale:
                    GUI.Label(new Rect(8, paramY, 50, 16), "Scale:", CompositorStyles.NodeContent);
                    _scaleValue = GUI.HorizontalSlider(new Rect(8, paramY + 18, contentRect.width - 16, 16), _scaleValue, 0.1f, 3f);
                    var scaleStyle = GUIStyleBuilder.CreateFrom(CompositorStyles.NodeContent)
                        .WithAlignment(TextAnchor.MiddleCenter)
                        .WithNormalState(textColor: GUIUtils.Colors.TextAccent);
                    GUI.Label(new Rect(8, paramY + 38, contentRect.width - 16, 16), _scaleValue.ToString("F2"), scaleStyle);
                    break;
                case TransformType.Rotate:
                    GUI.Label(new Rect(8, paramY, 60, 16), "Rotation:", CompositorStyles.NodeContent);
                    _rotationValue = GUI.HorizontalSlider(new Rect(8, paramY + 18, contentRect.width - 16, 16),
                        _rotationValue, -180f, 180f);
                    var rotateStyle = GUIStyleBuilder.CreateFrom(CompositorStyles.NodeContent)
                        .WithAlignment(TextAnchor.MiddleCenter)
                        .WithNormalState(textColor: GUIUtils.Colors.TextAccent);
                    GUI.Label(new Rect(8, paramY + 38, contentRect.width - 16, 16), $"{_rotationValue:F0}°", rotateStyle);
                    break;

                case TransformType.Translate:
                    GUI.Label(new Rect(8, paramY, 60, 16), "Offset X:", CompositorStyles.NodeContent);
                    _translateValue.x = GUI.HorizontalSlider(new Rect(8, paramY + 18, contentRect.width - 16, 16),
                        _translateValue.x, -100f, 100f);
                    GUI.Label(new Rect(8, paramY + 38, 60, 16), "Offset Y:", CompositorStyles.NodeContent);
                    _translateValue.y = GUI.HorizontalSlider(new Rect(8, paramY + 56, contentRect.width - 16, 16),
                        _translateValue.y, -100f, 100f);
                    break;
            }
        }

        public override void Process()
        {
            var inputTexture = _inputs[0].GetValue<Texture2D>();
            if (inputTexture == null)
            {
                _processedTexture = null;
                _outputs[0].SetValue(null);
                return;
            }

            var element = new CPUTextureElement(inputTexture);
            switch (_selectedTransform)
            {
                case TransformType.Scale:
                    element.Scale(_scaleValue);
                    break;
                case TransformType.Rotate:
                    element.Rotate(_rotationValue);
                    break;
                case TransformType.Translate:
                    element.Translate(_translateValue);
                    break;
                case TransformType.Crop:
                    var cropRect = new Rect(inputTexture.width * 0.1f, inputTexture.height * 0.1f, inputTexture.width * 0.8f, inputTexture.height * 0.8f);
                    element.Crop(cropRect);
                    break;
            }

            _processedTexture = element.GetTexture();
            _outputs[0].SetValue(_processedTexture);
        }
    }
}