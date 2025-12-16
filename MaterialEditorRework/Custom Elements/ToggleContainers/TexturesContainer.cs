using MaterialEditorRework.Views;
using UnityEngine;

namespace MaterialEditorRework.CustomElements.ToggleContainers
{
	public class TexturesContainer : ToggleContainerBase
	{
		public TexturesContainer(Vector2 size) : base(size)
		{
		}
		public override void DrawHeader(Rect rect)
		{
			GUI.DrawTexture(new Rect(rect.x + 16, rect.y + rect.height / 2 - 10, 20, 20), Icons.ImageIcon);
			GUI.Label(new Rect(rect.x + 48, rect.y + rect.height / 2 - 10, 100, 20), "Textures", Styles.BoldBlack);
		}
		public override void DrawContent(Rect rect)
		{

		}
	}
}