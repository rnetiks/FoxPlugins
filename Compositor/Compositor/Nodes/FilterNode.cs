using DefaultNamespace.Compositor;
using TexFac.Universal;
using UIBuilder;
using UnityEngine;

namespace Compositor.KK
{
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
            _inputs.Add(new NodeInput("Input", typeof(Texture2D), new Vector2(0, Size.y * 0.8f)));
            _outputs.Add(new NodeOutput("Output", typeof(Texture2D), new Vector2(Size.x, Size.y * 0.8f)));
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