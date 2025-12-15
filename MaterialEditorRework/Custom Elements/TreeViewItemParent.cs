using System.Collections.Generic;
using System.Linq;
using System.Text;
using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework.CustomElements
{
	public class TreeViewItemParent
	{
		public static Texture2D SelectedBackgroundBorder;
		public static Texture2D ActiveBackgroundColorBorder;
		public static Texture2D ChevronDownIcon;
		public static Texture2D ChevronRightIcon;

		public static Texture2D HoverBackground;
		public static Texture2D SelectedBackground;
		public static Texture2D ActiveBackground;

		public static Texture2D EyeHoverBackground;

		private static GUIStyle _labelStyle;

		public static bool Initialized;

		public TreeViewItemChild[] Children;
		public bool Open;
		public bool Selected;

		public Renderer Renderer;

		public TreeViewItemParent(Vector2 size)
		{
			if (!Initialized)
			{
				// Assign icons
				ChevronDownIcon = new Texture2D(1, 1);
				ChevronRightIcon = new Texture2D(1, 1);
				string chevronDownData = Encoding.ASCII.GetString(KKAPI.Utilities.ResourceUtils.GetEmbeddedResource("MaterialEditorRework.Resources.SVGs.chevron-down.svg", typeof(TreeViewItemParent).Assembly));
				string chevronRightData = Encoding.ASCII.GetString(KKAPI.Utilities.ResourceUtils.GetEmbeddedResource("MaterialEditorRework.Resources.SVGs.chevron-right.svg", typeof(TreeViewItemParent).Assembly));
				ImageConversion.LoadImage(ChevronDownIcon, Svg.SvgContentToPngBytes(chevronDownData, 128, 128));
				ImageConversion.LoadImage(ChevronRightIcon, Svg.SvgContentToPngBytes(chevronRightData, 128, 128));

				HoverBackground = TextureFactory.SolidColor((int)size.x, (int)size.y, new Color32(249, 250, 251, 255)).BorderRadius(10, aliasDistance: 1).Apply()._texture;

				SelectedBackgroundBorder = TextureFactory.SolidColor((int)size.x, (int)size.y, new Color32(59, 130, 246, 255)).BorderRadius(10, aliasDistance: 0.5f).Apply()._texture;
				SelectedBackground = TextureFactory.SolidColor((int)size.x, (int)size.y, new Color32(219, 234, 254, 255)).BorderRadius(10, aliasDistance: 1).Apply()._texture;
				ActiveBackgroundColorBorder = TextureFactory.SolidColor((int)size.x, (int)size.y, new Color32(191, 219, 254, 255)).BorderRadius(10, aliasDistance: 1).Apply()._texture;
				ActiveBackground = TextureFactory.SolidColor((int)size.x, (int)size.y, new Color32(239, 246, 255, 255)).BorderRadius(10, aliasDistance: 1).Apply()._texture;
				EyeHoverBackground = TextureFactory.SolidColor(24, 24, new Color32(228, 228, 228, 255))
					.BorderRadius(12, aliasDistance: 0.5f);

				Initialized = true;
			}
		}

		public static GUIStyle LabelStyle
		{
			get
			{
				if (_labelStyle == null)
				{
					_labelStyle = new GUIStyle(GUI.skin.label);
					_labelStyle.alignment = TextAnchor.MiddleLeft;
					_labelStyle.normal.textColor = Color.black;
					_labelStyle.fontSize = 12;
				}

				return _labelStyle;
			}
		}

		public void Draw(Rect rect)
		{
			if (rect.Contains(Event.current.mousePosition) && !Selected)
				GUI.DrawTexture(rect, HoverBackground);
			if (Selected)
				drawSelectedBackground(rect);

			var hh = new Rect(rect.x + 261, rect.y + (rect.height / 2 - 8) - 4, 24, 24);
			if (hh.Contains(Event.current.mousePosition))
				GUI.DrawTexture(hh, EyeHoverBackground);

			GUI.DrawTexture(new Rect(rect.x + 265, rect.y + (rect.height / 2 - 8), 16, 16), Renderer.enabled ? Icons.EyeIcon : Icons.EyeDIcon);

			if (GUI.Button(hh, GUIContent.none, GUIStyle.none))
			{
				Renderer.enabled = !Renderer.enabled;
			}

			if (Children != null && Children.Any())
			{
				var position = new Rect(rect.x + 9, rect.y + 13, 24, 24);
				if (position.Contains(Event.current.mousePosition))
				{
					GUI.DrawTexture(position, EyeHoverBackground);
				}
				if (!Open)
					GUI.DrawTexture(new Rect(rect.x + 13, rect.y + 17, 16, 16), ChevronRightIcon);
				else
				{
					GUI.DrawTexture(new Rect(rect.x + 13, rect.y + 17, 16, 16), ChevronDownIcon);
				}
				if (GUI.Button(position, GUIContent.none, GUIStyle.none))
					Open = !Open;
			}

			GUI.SetNextControlName(this.GetHashCode().ToString());
			if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
			{
				Entry.Instance.listTreeviewView.DeselectAll();
				Selected = true;
				Entry.Instance.propertyHeaderView.SetItem(Renderer);
				Entry.Instance.propertyContentView.Target = Renderer;
			}


			GUI.DrawTexture(new Rect(rect.x + 37, rect.y + (rect.height / 2 - 8), 16, 16), Icons.BoxIcon);

			GUI.Label(new Rect(rect.x + 65, rect.y + 14, 160, 20), Renderer.name, LabelStyle);

			if (Open && Children != null && Children.Any())
			{
				for (int index = 0; index < Children.Length; index++)
				{
					var child = Children[index];
					child.Draw(new Rect(rect.x + 30, rect.y + 55 + 55 * index, rect.width - 40, 50));
				}
			}
		}

		private void drawSelectedBackground(Rect rect)
		{
			GUI.DrawTexture(rect, SelectedBackgroundBorder);
			GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), SelectedBackground);
		}

		private void drawActiveBackground(Rect rect)
		{
			GUI.DrawTexture(rect, ActiveBackgroundColorBorder);
			GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), ActiveBackground);
		}

		public int CalculateHeight()
		{
			int i = 0;
			i += 55;
			if (Children != null && Children.Any() && Open)
			{
				foreach (var child in Children)
				{
					i += 55;
				}
			}

			return i;
		}
	}
}