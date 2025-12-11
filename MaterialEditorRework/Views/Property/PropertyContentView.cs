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
				if (_target is Renderer)
				{
					_texturesContainer = null;
					_colorsContainer = null;
					_propertiesContainer = null;
					_renderSettingsContainer = null;
				}
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
			if(_renderSettingsContainer == null)
				_renderSettingsContainer = new RenderSettingsContainer(new Vector2(rect.width - 32, 50));
			_renderSettingsContainer.Draw(new Rect(rect.x + 16, rect.y + 16, rect.width - 32, 50), 
				new Rect(rect.x + 16, rect.y + 66, rect.width - 32, Target is Renderer r ? 80 : 100));
		}

		private void DrawMaterial(Material material, Rect rect)
		{

		}
	}
}