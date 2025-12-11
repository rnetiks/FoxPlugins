using MaterialEditorRework.Views;
using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework.CustomElements.ToggleContainers
{
	public class ColorsContainer : ToggleContainerBase
	{
		public ColorsContainer(Vector2 size) : base(size)
		{
		}
		
		public override void DrawHeader(Rect rect)
		{
			GUI.DrawTexture(rect, _headerTextureClose);
		}
		
		public override void DrawContent(Rect rect)
		{
			throw new System.NotImplementedException();
		}
		public override int GetHeight()
		{
			throw new System.NotImplementedException();
		}
	}
}