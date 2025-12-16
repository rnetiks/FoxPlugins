using System.Text;
using TexFac.Universal;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorRework
{
	public class Icons
	{
		private const int IconSize = 128;
		
		private static Texture2D _boxIcon;
		private static Texture2D _chevronDownIcon;
		private static Texture2D _chevronRightIcon;
		private static Texture2D _copyIcon;
		private static Texture2D _downloadIcon;
		private static Texture2D _uploadIcon;
		private static Texture2D _eyeIcon;
		private static Texture2D _eyeDIcon;
		private static Texture2D _imageIcon;
		private static Texture2D _layersIcon;
		private static Texture2D _paletteIcon;
		private static Texture2D _refreshIcon;
		private static Texture2D _searchIcon;
		private static Texture2D _settingsIcon;
		
		public static Texture2D BoxIcon {
			get
			{
				if (_boxIcon == null)
				{
					var rawData = GetSvgData("box.svg");
					var bytes = Svg.SvgContentToPngBytes(rawData, IconSize, IconSize);
					_boxIcon = new Texture2D(IconSize, IconSize);
					ImageConversion.LoadImage(_boxIcon, bytes);
				}
				
				return _boxIcon;
			}
		}
		public static Texture2D ChevronDownIcon
		{
			get
			{
				if (_chevronDownIcon == null)
				{
					var rawData = GetSvgData("chevron-down.svg");
					var bytes = Svg.SvgContentToPngBytes(rawData, IconSize, IconSize);
					_chevronDownIcon = new Texture2D(IconSize, IconSize);
					ImageConversion.LoadImage(_chevronDownIcon, bytes);
				}
				
				return _chevronDownIcon;
			}
		}
		public static Texture2D ChevronRightIcon
		{
			get
			{
				if (_chevronRightIcon == null)
				{
					var rawData = GetSvgData("chevron-right.svg");
					var bytes = Svg.SvgContentToPngBytes(rawData, IconSize, IconSize);
					_chevronRightIcon = new Texture2D(IconSize, IconSize);
					ImageConversion.LoadImage(_chevronRightIcon, bytes);
				}
				
				return _chevronRightIcon;
			}
		}
		public static Texture2D CopyIcon
		{
			get
			{
				if (_copyIcon == null)
				{
					var rawData = GetSvgData("copy.svg");
					var bytes = Svg.SvgContentToPngBytes(rawData, IconSize, IconSize);
					_copyIcon = new Texture2D(IconSize, IconSize);
					ImageConversion.LoadImage(_copyIcon, bytes);
				}
				
				return _copyIcon;
			}
		}

		public static Texture2D EyeIcon
		{
			get
			{
				if(_eyeIcon == null)
				{
					var rawData = GetSvgData("eye.svg");
					var bytes = Svg.SvgContentToPngBytes(rawData, IconSize, IconSize);
					_eyeIcon = new Texture2D(IconSize, IconSize);
					ImageConversion.LoadImage(_eyeIcon, bytes);
				}
				
				return _eyeIcon;
			}
		}

		public static Texture2D EyeDIcon
		{
			get
			{
				if (_eyeDIcon == null)
				{
					var rawData = GetSvgData("eyeD.svg");
					var bytes = Svg.SvgContentToPngBytes(rawData, IconSize, IconSize);
					_eyeDIcon = new Texture2D(IconSize, IconSize);
					ImageConversion.LoadImage(_eyeDIcon, bytes);
				}
				
				return _eyeDIcon;
			}
		}

		public static Texture2D SettingsIcon
		{
			get
			{
				if (_settingsIcon == null)
				{
					var rawData = GetSvgData("settings.svg");
					var bytes = Svg.SvgContentToPngBytes(rawData, IconSize, IconSize);
					_settingsIcon = new Texture2D(IconSize, IconSize);
					ImageConversion.LoadImage(_settingsIcon, bytes);
				}

				return _settingsIcon;
			}
		}

		public static Texture2D LayerIcon
		{
			get
			{
				if (_layersIcon == null)
				{
					var rawData = GetSvgData("layers.svg");
					var bytes = Svg.SvgContentToPngBytes(rawData, IconSize, IconSize);
					_layersIcon = new Texture2D(IconSize, IconSize);
					ImageConversion.LoadImage(_layersIcon, bytes);
				}
				
				return _layersIcon;
			}
		}

		public static Texture2D ImageIcon
		{
			get
			{
				if (_imageIcon == null)
				{
					var rawData = GetSvgData("image.svg");
					var bytes = Svg.SvgContentToPngBytes(rawData, IconSize, IconSize);
					_imageIcon = new Texture2D(IconSize, IconSize);
					ImageConversion.LoadImage(_imageIcon, bytes);
				}
				
				return _imageIcon;
			}
		}

		public static Texture2D PaletteIcon
		{
			get
			{
				if (_paletteIcon == null)
				{
					var rawData = GetSvgData("palette.svg");
					var bytes = Svg.SvgContentToPngBytes(rawData, IconSize, IconSize);
					_paletteIcon = new Texture2D(IconSize, IconSize);
					ImageConversion.LoadImage(_paletteIcon, bytes);
				}

				return _paletteIcon;
			}
		}

		private static string GetSvgData(string path)
		{
			var rawData = KKAPI.Utilities.ResourceUtils.GetEmbeddedResource("MaterialEditorRework.Resources.SVGs." + path, typeof(Icons).Assembly);
			return Encoding.ASCII.GetString(rawData);
		}
	}
}