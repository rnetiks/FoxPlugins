using System.Diagnostics;
using BepInEx.Logging;
using TexFac.Universal;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorRework.Views
{
	public abstract class ToggleContainerBase
	{
		protected private Texture2D _headerTextureClose;
		protected private bool _isOpen = true;
		protected private Texture2D _headerTextureOpen;
		protected private Texture2D _footerTexture;

		public ToggleContainerBase(Vector2 size)
		{
			_headerTextureClose = TextureFactory.SolidColor((int)size.x, (int)size.y, Color.white)
				.BorderRadius(10, aliasDistance: 0.5f);
			_headerTextureOpen = TextureFactory.SolidColor((int)size.x, (int)size.y, Color.white)
				.BorderRadius(10, BorderType.TopLeft | BorderType.TopRight, 0.5f);
			_footerTexture = TextureFactory.SolidColor((int)size.x, 10, Color.white)
				.BorderRadius(10, BorderType.BottomLeft | BorderType.BottomRight, 0.5f);
			
			var q = TextureFactory.SolidColor((int)size.x, 10, Color.white)
				.BorderRadius(10, BorderType.BottomLeft | BorderType.BottomRight, 0.5f);
		}
		public void Draw(Rect header, Rect content)
		{
			DrawHeader(header);
			if (_isOpen)
				DrawContent(content);
		}

		public abstract void DrawHeader(Rect rect);
		public abstract void DrawContent(Rect rect);
		public abstract int GetHeight();
	}
}