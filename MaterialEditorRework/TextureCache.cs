using System.Collections.Generic;
using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework
{
	public class TextureCache
	{
		private static Dictionary<Color, Texture2D> _textures = new Dictionary<Color, Texture2D>();

		public static Texture2D GetOrCreateSolid(Color color)
		{
			if (!_textures.TryGetValue(color, out var texture))
			{
				_textures[color] = TextureFactory.SolidColor(1, 1, color);
			}
			return texture;
		}

	}
}