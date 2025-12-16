using Addin;
using MaterialEditorRework.Views;
using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework.CustomElements.ToggleContainers
{
	public class ColorsContainer : ToggleContainerBase
	{
		private ColorPicker colorPicker;
		public ColorsContainer(Vector2 size) : base(size)
		{
		}
		
		public override void DrawHeader(Rect rect)
		{
			GUI.DrawTexture(new Rect(rect.x + 16, rect.y + rect.height / 2 - 10, 20, 20), Icons.PaletteIcon);
			GUI.Label(new Rect(rect.x + 48, rect.y + rect.height / 2 - 10, 100, 20), "Colors", Styles.BoldBlack);
		}
		
		public override void DrawContent(Rect rect)
		{

		}
	}
}