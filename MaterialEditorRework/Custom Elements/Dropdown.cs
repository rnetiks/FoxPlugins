using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework.CustomElements
{
	public class Dropdown
	{
		private string[] _options;
		private int _selectedIndex;
		private bool _isOpen = false;
		public bool IsOpen => _isOpen;
		private int _hoveredIndex = -1;

		private Texture2D _backgroundTexture;
		private Texture2D _backgroundBorderTexture;
		private Texture2D _selectedBorderTexture;
		private Texture2D _containerBackgroundTexture;
		private Texture2D _containerBorderTexture;
		private Texture2D _containerItemHoverTexture;
		private Texture2D _chevronDownIcon;

		private Vector2 _lastHeaderSize;
		private Vector2 _lastContainerSize;
		private Vector2 _lastContainerBackgroundSize;
		private Vector2? _maxContainerSize = null;
		private Vector2 _scrollPosition = Vector2.zero;

		public event Action<int> OnSelectionChanged;

		public Dropdown(string[] options, int defaultIndex = 0)
		{
			_options = options;
			_selectedIndex = defaultIndex;
		}

		public Dropdown()
		{
			_options = new string[0];
			_selectedIndex = -1;
		}

		public int SelectedIndex => _selectedIndex;
		public string SelectedOption => _options[_selectedIndex];
		
		public string[] Items => _options;

		public void SelectIndex(int index)
		{
			if (index >= 0 && index < _options.Length && index != _selectedIndex)
			{
				_selectedIndex = index;
				OnSelectionChanged?.Invoke(index);
			}
		}

		public void SetOptions(string[] options)
		{
			_options = options;
		}

		public void SelectOption(string option)
		{
			int index = Array.IndexOf(_options, option);
			if (index >= 0)
			{
				SelectIndex(index);
			}
		}

		private void GenerateHeaderImages(Vector2 size)
		{
			if (!_backgroundTexture || _lastHeaderSize != size)
			{
				_backgroundTexture = TextureFactory.SolidColor((int)size.x, (int)size.y, new Color32(233, 233, 237, 255))
					.BorderRadius(10, aliasDistance: 0.5f);
			}

			if (!_backgroundBorderTexture || _lastHeaderSize != size)
			{
				_backgroundBorderTexture = TextureFactory.SolidColor((int)size.x + 2, (int)size.y + 2, new Color32(209, 213, 219, 255))
					.BorderRadius(10, aliasDistance: 0.5f);
			}

			if (!_selectedBorderTexture || _lastHeaderSize != size)
			{
				_selectedBorderTexture = TextureFactory.SolidColor((int)size.x + 6, (int)size.y + 6, new Color32(0, 96, 223, 255))
					.BorderRadius(10, aliasDistance: 0.5f);
			}

			if (!_chevronDownIcon)
			{
				string chevronDownData = Encoding.ASCII.GetString(KKAPI.Utilities.ResourceUtils.GetEmbeddedResource("MaterialEditorRework.Resources.SVGs.chevron-down.svg", typeof(Dropdown).Assembly));
				byte[] bytes = Svg.SvgContentToPngBytes(chevronDownData, 128, 128);
				_chevronDownIcon = new Texture2D(128, 128);
				ImageConversion.LoadImage(_chevronDownIcon, bytes);
			}

			_lastHeaderSize = size;
		}

		private void GenerateContainerBackgroundImages(Vector2 size)
		{
			if (!_containerBackgroundTexture || _lastContainerBackgroundSize != size)
			{
				_containerBackgroundTexture = TextureFactory.SolidColor((int)size.x, (int)size.y, new Color32(233, 233, 237, 255))
					.BorderRadius(10, aliasDistance: 0.5f);
			}

			if (!_containerBorderTexture || _lastContainerBackgroundSize != size)
			{
				_containerBorderTexture = TextureFactory.SolidColor((int)size.x + 2, (int)size.y + 2, new Color32(209, 213, 219, 255))
					.BorderRadius(10, aliasDistance: 0.5f);
			}

			_lastContainerBackgroundSize = size;
		}

		private void GenerateContainerItemImages(Vector2 size)
		{
			if (!_containerItemHoverTexture || _lastContainerSize != size)
			{
				_containerItemHoverTexture = TextureFactory.SolidColor((int)size.x, (int)size.y, new Color32(243, 244, 246, 255))
					.BorderRadius(6, aliasDistance: 0.5f);
				_lastContainerSize = size;
			}
		}

		private GUIStyle _labelStyle
		{
			get
			{
				if (!StyleCache.Styles.ContainsKey("black_label"))
				{
					StyleCache.Styles.Add("black_label", new GUIStyle(GUI.skin.label)
					{
						alignment = TextAnchor.MiddleLeft,
						normal = { textColor = Color.black },
						fontSize = 12
					});
				}

				return StyleCache.Styles["black_label"];
			}
		}

		public void Draw(Rect rect)
		{
			GenerateHeaderImages(new Vector2(rect.width, rect.height));

			// Draw header
			GUI.DrawTexture(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), _backgroundBorderTexture);
			GUI.DrawTexture(rect, _backgroundTexture);
			if (_options.Length > 0 && _selectedIndex >= 0 && _selectedIndex < _options.Length)
				GUI.Label(new Rect(rect.x + 8, rect.y, rect.width - 32, rect.height), _options[_selectedIndex], _labelStyle);

			// Draw chevron icon
			Matrix4x4 matrixBackup = GUI.matrix;
			if (_isOpen)
			{
				// Rotate chevron 180 degrees when open
				GUIUtility.RotateAroundPivot(180, new Vector2(rect.x + rect.width - 12, rect.y + rect.height / 2));
			}
			GUI.DrawTexture(new Rect(rect.x + rect.width - 20, rect.y + rect.height / 2 - 8, 16, 16), _chevronDownIcon);
			GUI.matrix = matrixBackup;

			// Handle header click
			if (GUI.Button(rect, "", GUIStyle.none) && _options.Length > 0)
			{
				_isOpen = !_isOpen;
				if (_isOpen)
				{
					// Reset scroll when opening
					_scrollPosition = Vector2.zero;
				}
			}

			if (_isOpen)
			{
				float totalContentHeight = rect.height * _options.Length;
				float containerHeight = totalContentHeight;
				bool needsScroll = false;

				if (_maxContainerSize.HasValue)
				{
					if (_maxContainerSize.Value.y > 0 && totalContentHeight > _maxContainerSize.Value.y)
					{
						containerHeight = _maxContainerSize.Value.y;
						needsScroll = true;
					}
				}

				Rect containerRect = new Rect(rect.x, rect.y + rect.height, rect.width, containerHeight);

				GenerateContainerBackgroundImages(new Vector2(containerRect.width, containerRect.height));
				GenerateContainerItemImages(new Vector2(rect.width - 8, rect.height - 4));

				GUI.DrawTexture(new Rect(containerRect.x - 1, containerRect.y - 1, containerRect.width + 2, containerRect.height + 2), _containerBorderTexture);
				GUI.DrawTexture(containerRect, _containerBackgroundTexture);

				Rect scrollViewRect = new Rect(containerRect.x + 4, containerRect.y + 2, containerRect.width - 8, containerRect.height - 4);
				Rect scrollContentRect = new Rect(0, 0, scrollViewRect.width - (needsScroll ? 20 : 0), totalContentHeight);

				if (needsScroll)
				{
					_scrollPosition = GUI.BeginScrollView(scrollViewRect, _scrollPosition, scrollContentRect, false, true);
				}
				else
				{
					GUI.BeginGroup(scrollViewRect);
				}

				_hoveredIndex = -1;
				for (int i = 0; i < _options.Length; i++)
				{
					float yOffset = i * rect.height;
					Rect optionRect = new Rect(0, yOffset, scrollContentRect.width, rect.height - 4);

					Vector2 mousePos = Event.current.mousePosition;

					bool isHovered = optionRect.Contains(mousePos);

					if (isHovered)
					{
						_hoveredIndex = i;
						GUI.DrawTexture(optionRect, _containerItemHoverTexture);
					}

					GUI.Label(new Rect(optionRect.x + 4, optionRect.y, optionRect.width - 8, optionRect.height), _options[i], _labelStyle);

					if (Event.current.type == EventType.MouseDown &&  optionRect.Contains(Event.current.mousePosition))
					{
						Event.current.Use();
						SelectIndex(i);
						_isOpen = false;
					}
				}

				if (needsScroll)
				{
					GUI.EndScrollView();
				}
				else
				{
					GUI.EndGroup();
				}

				if (_isOpen && Event.current.type == EventType.MouseDown && !containerRect.Contains(Event.current.mousePosition))
				{
					_isOpen = false;
				}
			}

		}

		public void SetMaxContainerSize(Vector2 size)
		{
			_maxContainerSize = size;
		}

		public void ClearMaxContainerSize()
		{
			_maxContainerSize = null;
		}
	}
}