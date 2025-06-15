using System;
using System.ComponentModel;
using DefaultNamespace;
using DefaultNamespace.Compositor;
using TexFac.Universal;
using UIBuilder;
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
            _outputs.Add(new NodeOutput("Texture", typeof(Texture2D), new Vector2(Size.x - 20, Size.y * 0.5f)));
        }

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
                _inputs.Add(new NodeInput("Input", typeof(Texture2D), new Vector2(20, Size.y * 0.3f)));
                _outputs.Add(new NodeOutput("Output", typeof(Texture2D), new Vector2(Size.x - 20, Size.y * 0.3f)));
            }

            public override void DrawContent(Rect contentRect)
            {
                GUI.Label(new Rect(8, 5, 80, 16), "Transform:", CompositorStyles.NodeContent);

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

    public class OutputNode : BaseCompositorNode
    {
        public override string Title => "Output";
        private Texture2D _displayTexture;

        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Texture", typeof(Texture2D), new Vector2(20, Size.y * 0.5f)));
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
                GUI.Label(new Rect(8, 25, contentRect.width - 16, 30), "No inputs found", CompositorStyles.NodeContent);
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
                var element = new CPUTextureElement(_displayTexture);
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var filename = $"compositor_output_{timestamp}.png";

                element.Save(filename);
                Entry.Logger.LogDebug($"Texture exported as {filename}");
            }
        }
    }

    public class FilterNode : BaseCompositorNode
    {
        public override string Title => "Filter";
        private FilterType _selectedFilter = FilterType.None;
        private float _filterValue = 1.0f;
        private Texture2D _processedTexture;

        public enum FilterType
        {
            None,
            Blur,
            Brightness,
            Contrast,
            Saturation,
            Grayscale,
            Sepia
        }


        protected override void InitializePorts()
        {
            _inputs.Add(new NodeInput("Input", typeof(Texture2D), new Vector2(20, Size.y * 0.3f)));
            _outputs.Add(new NodeOutput("Output", typeof(Texture2D), new Vector2(Size.x - 20, Size.y * 0.3f)));
        }

        public override void DrawContent(Rect contentRect)
        {
            GUI.Label(new Rect(8, 5, 60, 16), "Filter:", CompositorStyles.NodeContent);
            
            var filterNames = System.Enum.GetNames(typeof(FilterType));
            var currentIndex = (int)_selectedFilter;
            
            float buttonWidth = (contentRect.width - 16) / 3f - 2f;
            float buttonHeight = 18f;
            float startY = 25f;
            
            for (var i = 0; i < filterNames.Length; i++)
            {
                int row = i / 3;
                int col = i % 3;
                var buttonRect = new Rect(8 + col * (buttonWidth + 2), startY + row * (buttonHeight + 2), buttonWidth, buttonHeight);

                var buttonStyle = (i == currentIndex) ? CompositorStyles.FilterButtonSelected : CompositorStyles.FilterButton;

                if (GUI.Button(buttonRect, filterNames[i], buttonStyle))
                {
                    _selectedFilter = (FilterType)i;
                }
            }

            if (_selectedFilter != FilterType.None)
            {
                float sliderY = startY + (filterNames.Length / 3 + 1) * (buttonHeight + 2) + 10;

                GUI.Label(new Rect(8, sliderY, 50, 16), "Value:", CompositorStyles.NodeContent);
                
                var sliderRect = new Rect(8, sliderY + 18, contentRect.width - 16, 16);
                _filterValue = GUI.HorizontalSlider(sliderRect, _filterValue, 0f, 2f);

                var valueStyle = GUIStyleBuilder.CreateFrom(CompositorStyles.NodeContent)
                    .WithAlignment(TextAnchor.MiddleCenter)
                    .WithNormalState(textColor: GUIUtils.Colors.TextAccent);
                GUI.Label(new Rect(8, sliderY + 38, contentRect.width - 16, 16), _filterValue.ToString("F2"), valueStyle);
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
            switch (_selectedFilter)
            {
                case FilterType.Blur:
                    element.Blur((int)(_filterValue * 5));
                    break;
                case FilterType.Brightness:
                    element.Brightness(_filterValue - 1);
                    break;
                case FilterType.Contrast:
                    element.Contrast(_filterValue * 100 - 100);
                    break;
                case FilterType.Saturation:
                    element.Saturation(_filterValue);
                    break;
                case FilterType.Grayscale:
                    element.Grayscale();
                    break;
                case FilterType.Sepia:
                    element.Sepia();
                    break;
                case FilterType.None:
                    break;
            }

            _processedTexture = element.GetTexture();
            _outputs[0].SetValue(_processedTexture);
        }
    }
}