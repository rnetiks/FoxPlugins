using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework.CustomElements
{
	public class Textbox
	{
		#region MyRegion

		private TextureElement _background;
		private TextureElement _backgroundShadow;
		private static Texture2D _icon;

		private static GUIStyle _overrideTextboxStyle;

		private GUIStyle OverrideTextboxStyle
		{
			get
			{
				if (_overrideTextboxStyle == null)
				{
					_overrideTextboxStyle = new GUIStyle(GUI.skin.textField);
					_overrideTextboxStyle.normal.background = null;
					_overrideTextboxStyle.normal.textColor = Color.black;
					_overrideTextboxStyle.hover.background = null;
					_overrideTextboxStyle.focused.background = null;
					_overrideTextboxStyle.focused.textColor = Color.black;
					_overrideTextboxStyle.alignment = TextAnchor.MiddleLeft;

				}
				return _overrideTextboxStyle;
			}
			set => _overrideTextboxStyle = value;
		}

		private static GUIStyle _overridePlaceholderStyle;

		private GUIStyle OverridePlaceholderStyle
		{
			get
			{
				if (_overridePlaceholderStyle == null)
				{
					_overridePlaceholderStyle = new GUIStyle(GUI.skin.label);
					_overridePlaceholderStyle.normal.textColor = new Color(0.42f, 0.42f, 0.42f);
					_overridePlaceholderStyle.alignment = TextAnchor.MiddleLeft;
				}
				return _overridePlaceholderStyle;
			}
			set => _overridePlaceholderStyle = value;
		}

		#endregion

		public Textbox()
		{
			if (_icon == null)
			{
				_icon = new Texture2D(256, 256);

				byte[] resource = KKAPI.Utilities.ResourceUtils.GetEmbeddedResource("MaterialEditorRework.Resources.SVGs.search.svg", Assembly.GetExecutingAssembly());
				string svgData = Encoding.ASCII.GetString(resource);
				var colorData = Svg.SvgContentToPngBytes(svgData, 256, 256);
				_icon.LoadImage( colorData);
			}
		}

		public string Draw(Rect rect, string value, string placeholder)
		{

			if ( _background == null  || _backgroundShadow == null)
			{
				_background = TextureFactory.SolidColor((int)rect.width, (int)rect.height, new Color32(249, 250, 251, 255)).BorderRadius(10, aliasDistance: 1);
				_backgroundShadow = TextureFactory.SolidColor((int)rect.width, (int)rect.height, new Color32(231, 231, 231, 255)).BorderRadius(10, aliasDistance: 1);
			}


			GUI.skin.settings.cursorColor = Color.black;
			GUI.DrawTexture(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), _backgroundShadow);
			GUI.DrawTexture(rect, _background);
			GUI.DrawTexture(new Rect(rect.x + rect.height / 4, rect.y + rect.height / 4, rect.height / 2, rect.height / 2), _icon);
			if (string.IsNullOrEmpty(value))
				GUI.Label(new Rect(rect.height + rect.x + 4, rect.y, rect.width - rect.height, rect.height), placeholder, OverridePlaceholderStyle);
			
			if (Event.current.type == EventType.MouseDown && !rect.Contains(Event.current.mousePosition) && GUI.GetNameOfFocusedControl() == "SearchField")
			{
				GUI.FocusControl(null);
			}
			
			GUI.SetNextControlName("SearchField");
			return GUI.TextField(new Rect(rect.height + rect.x, rect.y, rect.width - rect.height, rect.height), value, OverrideTextboxStyle);
		}
	}
}