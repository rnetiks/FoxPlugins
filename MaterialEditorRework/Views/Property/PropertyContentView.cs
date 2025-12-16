using MaterialEditorRework.CustomElements.ToggleContainers;
using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework.Views.Property
{
    public class PropertyContentView : BaseElementView
    {
        private RenderSettingsContainer _renderSettingsContainer;
        private TexturesContainer _texturesContainer;
        private ColorsContainer _colorsContainer;
        private PropertiesContainer _propertiesContainer;

        private bool _instantiated;
        private object _target;
        public object Target
        {
            get
            {
                return _target;
            }

            set
            {
                _target = value;
                _texturesContainer = null;
                _colorsContainer = null;
                _propertiesContainer = null;
                _renderSettingsContainer = null;
                _instantiated = false;
            }
        }

        public override void Draw(Rect rect)
        {
            GUI.DrawTexture(rect, TextureCache.GetOrCreateSolid(new Color(0.95f, 0.96f, 0.96f)));
            if (Target is Renderer renderer)
            {
                DrawRenderer(renderer, rect);
            }
            else if (Target is Material material)
            {
                DrawMaterial(material, rect);
            }
        }

        private void DrawRenderer(Renderer renderer, Rect rect)
        {
            if (!_instantiated)
            {
                _renderSettingsContainer = new RenderSettingsContainer(new Vector2(rect.width - 32, 50));
                _instantiated = true;
            }
            _renderSettingsContainer.Draw(new Rect(rect.x + 16, rect.y + 16, rect.width - 32, 50),
                new Rect(rect.x + 16, rect.y + 66, rect.width - 32, 90));
        }

        private void DrawMaterial(Material material, Rect rect)
        {
            float rectWidth = rect.width - 32;
            if (!_instantiated)
            {
                _texturesContainer = new TexturesContainer(new Vector2(rectWidth, 100));
                _colorsContainer = new ColorsContainer(new Vector2(rectWidth, 100));
                _propertiesContainer = new PropertiesContainer(new Vector2(rectWidth, 100), material);
                _instantiated = true;
            }

            float pos = 0;

            _texturesContainer.Draw(new Rect(rect.x + 16, rect.y + 16, rectWidth, 50),
                new Rect(rect.x + 16, rect.y + 66, rectWidth, 50));
            pos += _texturesContainer.Height;
            
            _colorsContainer.Draw(new Rect(rect.x + 16, rect.y + pos + 32, rectWidth, 50),
                new Rect(rect.x + 16, rect.y + pos + 32 + 50, rectWidth, 50));
            pos += _colorsContainer.Height;
            _propertiesContainer.Draw(new Rect(rect.x + 16, rect.y + pos + 48, rectWidth, 50), 
                new Rect(rect.x + 16, rect.y + pos + 48 + 50, rectWidth, 50));
        }
    }
}