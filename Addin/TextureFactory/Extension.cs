using UnityEngine;

namespace TexFac.Universal
{
	public static class Extension
	{
		public static Texture2D RT2T2D(this RenderTexture renderTexture)
		{
			var currentActiveRenderTexture = RenderTexture.active;
			RenderTexture.active = renderTexture;
			var texture = new Texture2D(renderTexture.width, renderTexture.height);
			texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
			RenderTexture.active = currentActiveRenderTexture;
			return texture;
		} 
	}
}