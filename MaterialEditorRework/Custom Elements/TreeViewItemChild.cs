using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework.CustomElements
{
	public class TreeViewItemChild
	{
		public bool Selected;
		public Material Material { get; set; }
		public TreeViewItemParent Parent { get; set; }

		public static Texture2D _backgroundTexture;
		public static Texture2D _backgroundTextureLeft;

		public TreeViewItemChild()
		{
		}

		private void GenerateTextures(Rect rect)
		{
			_backgroundTextureLeft = TextureFactory.SolidColor(30, 50, new Color32(0, 119, 255, 255))
				.BorderRadius(10, BorderType.TopLeft | BorderType.BottomLeft, 0.5f);
			_backgroundTexture = TextureFactory.SolidColor((int)(rect.width - 4), (int)rect.height, new Color32(239, 246, 255, 255))
				.BorderRadius(10, aliasDistance: 0.5f);
		}

		public void Draw(Rect rect)
		{
			if (_backgroundTexture == null || _backgroundTextureLeft == null)
				GenerateTextures(rect);

			if (Selected)
			{
				GUI.DrawTexture(new Rect(rect.x, rect.y, 30, 50), _backgroundTextureLeft);
				GUI.DrawTexture(new Rect(rect.x + 4, rect.y, rect.width - 4, rect.height), _backgroundTexture);
			}
			GUI.DrawTexture(new Rect(rect.x + 14, rect.y + rect.height / 2 - 8, 16, 16), Icons.LayerIcon);
			
			GUI.Label(new Rect(rect.x + 40, rect.y + rect.height / 2 - 17, rect.width - 40, 20), Material.name.Replace("(Instance)", ""), Styles.DefaultLabelBlack);
			GUI.Label(new Rect(rect.x + 40, rect.y + rect.height / 2 - 3, rect.width - 40, 20), Material.shader.name, Styles.DefaultLabelGray);
			if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
			{
				SelectMaterial();
			}
		}
		private void SelectMaterial()
		{
			Entry.Instance.listTreeviewView.DeselectAll();
			Parent.Selected = true;
			Selected = true;
			Entry.Instance.propertyHeaderView.SetItem(Material);
			Entry.Instance.propertyContentView.Target = Material;
		}
	}
}