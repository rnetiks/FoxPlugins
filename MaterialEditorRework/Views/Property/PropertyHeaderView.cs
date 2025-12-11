using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework.Views.Property
{
	public class PropertyHeaderView : BaseElementView
	{
		private static Texture2D _buttonTexture;

		private bool isRenderer;

		private Renderer _renderer;
		private Material _material;
		public override void Draw(Rect rect)
		{
			if (_buttonTexture == null)
			{
				_buttonTexture = TextureFactory.SolidColor(162, 40, new Color(0.95f, 0.96f, 0.96f)).BorderRadius(10, aliasDistance: 0.5f);
			}
			
			GUI.DrawTexture(rect, TextureCache.GetOrCreateSolid(Color.white));

			var borderSizeValue = Entry.borderSize.Value;
			GUI.DrawTexture(new Rect(rect.x, rect.height - borderSizeValue, rect.width, borderSizeValue), TextureCache.GetOrCreateSolid(new Color(0.9f, 0.91f, 0.92f)));

			GUI.DrawTexture(new Rect(rect.x + 16, rect.y + 18, 16, 16), Icons.BoxIcon);
			if (isRenderer)
				GUI.Label(new Rect(rect.x + 36, rect.y + 16, rect.width - 32, 20), _renderer.name, Styles.DefaultLabelBlack);
			else
				GUI.Label(new Rect(rect.x + 36, rect.y + 16, rect.width - 32, 20), _material.name, Styles.DefaultLabelBlack);
			GUI.Label(new Rect(rect.x + 16, rect.y + 44, rect.width - 32, 20), "Renderer Settings", Styles.DefaultLabelBlack);


			/*GUI.DrawTexture(new Rect(rect.x + rect.width - 162 - 10, rect.y + rect.height / 2 - 20, 162, 40), _buttonTexture);
			GUI.DrawTexture(new Rect(rect.x + rect.width - 324 - 20, rect.y + rect.height / 2 - 20, 162, 40), _buttonTexture);*/
		}
		public void SetItem(Renderer renderer)
		{
			isRenderer = true;
			_renderer = renderer;
		}

		public void SetItem(Material material)
		{
			isRenderer = false;
			_material = material;
		}
	}
}