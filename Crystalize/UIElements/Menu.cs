using System;
using UnityEngine;

namespace Crystalize.UIElements
{
	/// <summary>
	/// Represents a flexible UI menu element that can be positioned on any side of a trigger button.
	/// The menu supports rich customization including sizing, styling, animations, and keyboard navigation.
	/// It provides automatic positioning based on screen boundaries, smooth open/close animations,
	/// and comprehensive event handling for menu state changes and item interactions.
	/// The menu can display any collection of IMGUIElement items with configurable spacing and layout.
	/// Thread safety is not guaranteed for this class.
	/// </summary>
	[Obsolete("This class is not fully implemented yet. It is currently in development and may not work as expected.")]
	public class Menu : IMGUIElement
	{
		public enum Side
		{
			Left,
			Right,
			Top,
			Bottom,
			Auto // Automatically choose best side based on screen position
		}

		public enum MenuState
		{
			Closed,
			Opening,
			Open,
			Closing
		}

		private float _offsetX;
		private float _offsetY;
		private Side _side;
		private Side _actualSide; // The side actually being used (for Auto mode)
		private const float _defaultOffset = 5f;
		private IMGUIElement[] _elements;
		private bool _reverseDrawDirection = false;

		// Sizing and layout
		private float _minWidth = 120f;
		private float _maxWidth = 300f;
		private float _itemHeight = 24f;
		private float _itemSpacing = 2f;
		private float _padding = 8f;

		// State management
		private MenuState _menuState = MenuState.Closed;
		private float _animationTime = 0f;
		private const float _animationDuration = 0.15f;

		// Button customization
		private string _buttonText = "Menu";
		private Texture2D _buttonIcon = null;
		private string _buttonTooltip = "";

		// Visual styling
		private Color _backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
		private Color _borderColor = new Color(0.4f, 0.4f, 0.4f, 1f);
		private Color _buttonColor = new Color(0.25f, 0.25f, 0.25f, 1f);
		private Color _buttonHoverColor = new Color(0.35f, 0.35f, 0.35f, 1f);
		private Color _buttonActiveColor = new Color(0.45f, 0.45f, 0.45f, 1f);
		private float _borderThickness = 1f;
		private float _cornerRadius = 4f;

		// Interaction
		private bool _closeOnClickOutside = true;
		private bool _closeOnItemClick = true;
		private int _selectedIndex = -1;
		private bool _keyboardNavigation = true;

		// Events
		public event Action<bool> OnMenuStateChanged; // true = opened, false = closed
		public event Action<int> OnItemSelected; // index of selected item
		public event Action<IMGUIElement, int> OnItemClicked; // element and index

		// Internal state
		private bool _wasMouseDownOutside;
		private Vector2 _lastMousePosition;
		private float _menuWidth;
		private float _menuHeight;
		private Vector2 _scrollPosition = Vector2.zero;
		private bool _needsScrolling = false;

		public Menu(Side side = Side.Auto, float offsetX = _defaultOffset, float offsetY = _defaultOffset)
		{
			_side = side;
			_actualSide = side;
			_offsetX = offsetX;
			_offsetY = offsetY;
		}

		#region Properties

		public bool IsMenuOpen => _menuState == MenuState.Open || _menuState == MenuState.Opening;
		public MenuState CurrentState => _menuState;
		public Side CurrentSide => _actualSide;
		public Vector2 CurrentOffset => new Vector2(_offsetX, _offsetY);
		public int SelectedIndex => _selectedIndex;

		public string ButtonText
		{
			get => _buttonText;
			set => _buttonText = value ?? "";
		}

		public Texture2D ButtonIcon
		{
			get => _buttonIcon;
			set => _buttonIcon = value;
		}

		public string ButtonTooltip
		{
			get => _buttonTooltip;
			set => _buttonTooltip = value ?? "";
		}

		public float MinWidth
		{
			get => _minWidth;
			set => _minWidth = Mathf.Max(50f, value);
		}

		public float MaxWidth
		{
			get => _maxWidth;
			set => _maxWidth = Mathf.Max(_minWidth, value);
		}

		public float ItemHeight
		{
			get => _itemHeight;
			set => _itemHeight = Mathf.Max(16f, value);
		}

		public float ItemSpacing
		{
			get => _itemSpacing;
			set => _itemSpacing = Mathf.Max(0f, value);
		}

		public float Padding
		{
			get => _padding;
			set => _padding = Mathf.Max(0f, value);
		}

		public bool CloseOnClickOutside
		{
			get => _closeOnClickOutside;
			set => _closeOnClickOutside = value;
		}

		public bool CloseOnItemClick
		{
			get => _closeOnItemClick;
			set => _closeOnItemClick = value;
		}

		public bool KeyboardNavigation
		{
			get => _keyboardNavigation;
			set => _keyboardNavigation = value;
		}

		#endregion

		#region Configuration Methods

		public void SetSide(Side side)
		{
			_side = side;
		}

		public void SetOffset(float offsetX, float offsetY)
		{
			_offsetX = offsetX;
			_offsetY = offsetY;
		}

		public void SetOffset(Vector2 offset)
		{
			SetOffset(offset.x, offset.y);
		}

		public void SetColors(Color background, Color border, Color button, Color buttonHover, Color buttonActive)
		{
			_backgroundColor = background;
			_borderColor = border;
			_buttonColor = button;
			_buttonHoverColor = buttonHover;
			_buttonActiveColor = buttonActive;
		}

		public void SetData(IMGUIElement[] elements)
		{
			_elements = elements ?? new IMGUIElement[0];
			CalculateMenuSize();
		}

		public void AddItem(IMGUIElement element)
		{
			if (_elements == null)
			{
				_elements = new IMGUIElement[] { element };
			}
			else
			{
				IMGUIElement[] newElements = new IMGUIElement[_elements.Length + 1];
				Array.Copy(_elements, newElements, _elements.Length);
				newElements[_elements.Length] = element;
				_elements = newElements;
			}
			CalculateMenuSize();
		}

		public void RemoveItem(int index)
		{
			if (_elements == null || index < 0 || index >= _elements.Length) return;

			IMGUIElement[] newElements = new IMGUIElement[_elements.Length - 1];
			Array.Copy(_elements, 0, newElements, 0, index);
			Array.Copy(_elements, index + 1, newElements, index, _elements.Length - index - 1);
			_elements = newElements;

			if (_selectedIndex >= _elements.Length)
				_selectedIndex = _elements.Length - 1;

			CalculateMenuSize();
		}

		public void ClearItems()
		{
			_elements = new IMGUIElement[0];
			_selectedIndex = -1;
			CalculateMenuSize();
		}

		#endregion

		#region Menu Control

		public void OpenMenu()
		{
			if (_menuState == MenuState.Closed || _menuState == MenuState.Closing)
			{
				_menuState = MenuState.Opening;
				_animationTime = 0f;
				OnMenuStateChanged?.Invoke(true);
			}
		}

		public void CloseMenu()
		{
			if (_menuState == MenuState.Open || _menuState == MenuState.Opening)
			{
				_menuState = MenuState.Closing;
				_animationTime = 0f;
				_selectedIndex = -1;
				OnMenuStateChanged?.Invoke(false);
			}
		}

		public void ToggleMenu()
		{
			if (IsMenuOpen)
				CloseMenu();
			else
				OpenMenu();
		}

		#endregion

		public override void OnGUI(Rect rect)
		{
			Event currentEvent = Event.current;
			int controlID = GUIUtility.GetControlID(FocusType.Passive);

			UpdateAnimation();
			HandleKeyboardInput(currentEvent);

			bool buttonClicked = DrawButton(rect, currentEvent, controlID);

			if (buttonClicked)
			{
				ToggleMenu();
			}

			if (_closeOnClickOutside && IsMenuOpen)
			{
				HandleClickOutside(currentEvent);
			}

			if (_menuState != MenuState.Closed)
			{
				DrawMenu(rect, currentEvent);
			}
		}

		private bool DrawButton(Rect rect, Event currentEvent, int controlID)
		{
			bool isHovering = rect.Contains(currentEvent.mousePosition);
			bool isActive = GUIUtility.hotControl == controlID;
			bool isMenuOpen = IsMenuOpen;

			// Determine button color
			Color buttonColor = _buttonColor;
			if (isMenuOpen)
				buttonColor = _buttonActiveColor;
			else if (isHovering)
				buttonColor = _buttonHoverColor;

			// Draw button background
			DrawRoundedRect(rect, buttonColor, _borderColor, _borderThickness, _cornerRadius);

			// Handle button interaction
			bool clicked = false;
			if (isHovering && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
			{
				GUIUtility.hotControl = controlID;
				currentEvent.Use();
			}

			if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
			{
				if (isHovering)
					clicked = true;
				GUIUtility.hotControl = 0;
				currentEvent.Use();
			}

			// Draw button content
			DrawButtonContent(rect);

			// Show tooltip
			if (isHovering && !string.IsNullOrEmpty(_buttonTooltip))
			{
				GUI.tooltip = _buttonTooltip;
			}

			return clicked;
		}

		private void DrawButtonContent(Rect rect)
		{
			if (_buttonIcon != null)
			{
				// Draw icon
				float iconSize = Mathf.Min(rect.width - 8f, rect.height - 4f);
				Rect iconRect = new Rect(
					rect.x + (rect.width - iconSize) * 0.5f,
					rect.y + (rect.height - iconSize) * 0.5f,
					iconSize,
					iconSize
				);
				GUI.DrawTexture(iconRect, _buttonIcon);

				// Draw text beside icon if there's space
				if (!string.IsNullOrEmpty(_buttonText) && rect.width > iconSize + 30f)
				{
					Rect textRect = new Rect(iconRect.xMax + 4f, rect.y, rect.xMax - iconRect.xMax - 8f, rect.height);
					DrawButtonText(textRect, _buttonText);
				}
			}
			else
			{
				// Draw text only
				DrawButtonText(rect, _buttonText);
			}
		}

		private void DrawButtonText(Rect rect, string text)
		{
			GUIStyle style = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = Color.white },
				fontSize = 11,
				fontStyle = FontStyle.Bold
			};

			GUI.Label(rect, text, style);
		}

		private void DrawMenu(Rect buttonRect, Event currentEvent)
		{
			if (_elements == null || _elements.Length == 0) return;

			CalculateMenuSize();
			Rect menuRect = CalculateMenuPosition(buttonRect);

			// Apply animation
			float animationProgress = GetAnimationProgress();
			if (animationProgress < 1f)
			{
				if (_menuState == MenuState.Opening)
				{
					// Scale animation
					float scale = Mathf.SmoothStep(0f, 1f, animationProgress);
					menuRect.width *= scale;
					menuRect.height *= scale;

					// Adjust position to maintain anchor point
					AdjustMenuRectForAnimation(ref menuRect, scale);
				}
				else if (_menuState == MenuState.Closing)
				{
					float scale = Mathf.SmoothStep(1f, 0f, animationProgress);
					menuRect.width *= scale;
					menuRect.height *= scale;

					AdjustMenuRectForAnimation(ref menuRect, scale);
				}
			}

			// Draw menu background and border
			Color bgColor = _backgroundColor;
			bgColor.a *= GetAnimationProgress();

			DrawRoundedRect(menuRect, bgColor, _borderColor, _borderThickness, _cornerRadius);

			// Draw menu items
			if (animationProgress > 0.3f) // Start drawing items partway through animation
			{
				DrawMenuItems(menuRect, currentEvent);
			}
		}

		private void DrawMenuItems(Rect menuRect, Event currentEvent)
		{
			Rect contentRect = new Rect(menuRect.x + _padding, menuRect.y + _padding,
				menuRect.width - _padding * 2f, menuRect.height - _padding * 2f);

			if (_needsScrolling)
			{
				// Calculate total content height for scrolling
				float totalContentHeight = (_elements.Length * _itemHeight) + ((_elements.Length - 1) * _itemSpacing);
				Rect viewRect = new Rect(0, 0, contentRect.width - 15f, totalContentHeight); // Account for scrollbar

				_scrollPosition = GUI.BeginScrollView(contentRect, _scrollPosition, viewRect);

				// Only draw visible items for performance
				float viewTop = _scrollPosition.y;
				float viewBottom = viewTop + contentRect.height;

				for (int i = 0; i < _elements.Length; i++)
				{
					float itemY = i * (_itemHeight + _itemSpacing);

					// Skip items outside view for performance
					if (itemY + _itemHeight < viewTop || itemY > viewBottom)
						continue;

					DrawMenuItem(i, new Rect(0, itemY, viewRect.width, _itemHeight), currentEvent);
				}

				GUI.EndScrollView();
			}
			else
			{
				// Draw all items normally
				for (int i = 0; i < _elements.Length; i++)
				{
					float itemY = contentRect.y + i * (_itemHeight + _itemSpacing);
					Rect itemRect = new Rect(contentRect.x, itemY, contentRect.width, _itemHeight);

					DrawMenuItem(i, itemRect, currentEvent);
				}
			}
		}

		private void DrawMenuItem(int index, Rect itemRect, Event currentEvent)
		{
			// Handle item interaction
			bool isHovering = itemRect.Contains(currentEvent.mousePosition);
			bool isSelected = (_selectedIndex == index);

			if (isHovering && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
			{
				_selectedIndex = index;
				OnItemSelected?.Invoke(index);
				currentEvent.Use();
			}

			if (isHovering && currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
			{
				OnItemClicked?.Invoke(_elements[index], index);
				if (_closeOnItemClick)
					CloseMenu();
				currentEvent.Use();
			}

			// Draw item background if selected or hovered
			if (isSelected || isHovering)
			{
				Color itemBg = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.7f) : new Color(0.4f, 0.4f, 0.4f, 0.5f);
				GUI.DrawTexture(itemRect, Texture.GetOrCreateSolid(itemBg));
			}

			// Draw the element
			_elements[index].OnGUI(itemRect);
		}

		private void HandleKeyboardInput(Event currentEvent)
		{
			if (!_keyboardNavigation || !IsMenuOpen || _elements == null || _elements.Length == 0)
				return;

			if (currentEvent.type == EventType.KeyDown)
			{
				switch (currentEvent.keyCode)
				{
					case KeyCode.Escape:
						CloseMenu();
						currentEvent.Use();
						break;

					case KeyCode.UpArrow:
						_selectedIndex = (_selectedIndex <= 0) ? _elements.Length - 1 : _selectedIndex - 1;
						OnItemSelected?.Invoke(_selectedIndex);
						currentEvent.Use();
						break;

					case KeyCode.DownArrow:
						_selectedIndex = (_selectedIndex >= _elements.Length - 1) ? 0 : _selectedIndex + 1;
						OnItemSelected?.Invoke(_selectedIndex);
						currentEvent.Use();
						break;

					case KeyCode.Return:
					case KeyCode.KeypadEnter:
						if (_selectedIndex >= 0 && _selectedIndex < _elements.Length)
						{
							OnItemClicked?.Invoke(_elements[_selectedIndex], _selectedIndex);
							if (_closeOnItemClick)
								CloseMenu();
						}
						currentEvent.Use();
						break;
				}
			}
		}

		private void HandleClickOutside(Event currentEvent)
		{
			if (currentEvent.type == EventType.MouseDown)
			{
				_wasMouseDownOutside = true;
				_lastMousePosition = currentEvent.mousePosition;
			}

			if (currentEvent.type == EventType.MouseUp && _wasMouseDownOutside)
			{
				Vector2 dragDistance = currentEvent.mousePosition - _lastMousePosition;
				if (dragDistance.magnitude < 3f)
				{
					CloseMenu();
				}
				_wasMouseDownOutside = false;
			}
		}

		private void CalculateMenuSize()
		{
			if (_elements == null || _elements.Length == 0)
			{
				_menuWidth = _minWidth;
				_menuHeight = _padding * 2f;
				return;
			}

			_menuWidth = Mathf.Clamp(_minWidth, _minWidth, _maxWidth);

			_menuHeight = _padding * 2f + (_elements.Length * _itemHeight) + ((_elements.Length - 1) * _itemSpacing);
		}

		private Rect CalculateMenuPosition(Rect buttonRect)
		{
			if (_side == Side.Auto)
			{
				_actualSide = DetermineOptimalSide(buttonRect);
			}
			else
			{
				_actualSide = _side;
			}

			Vector2 position = Vector2.zero;

			switch (_actualSide)
			{
				case Side.Right:
					position = new Vector2(buttonRect.xMax + _offsetX, buttonRect.y + _offsetY);
					break;
				case Side.Left:
					position = new Vector2(buttonRect.x - _menuWidth - _offsetX, buttonRect.y + _offsetY);
					break;
				case Side.Bottom:
					position = new Vector2(buttonRect.x + _offsetX, buttonRect.yMax + _offsetY);
					break;
				case Side.Top:
					position = new Vector2(buttonRect.x + _offsetX, buttonRect.y - _menuHeight - _offsetY);
					break;
			}

			position.x = Mathf.Clamp(position.x, 0, Screen.width - _menuWidth);
			position.y = Mathf.Clamp(position.y, 0, Screen.height - _menuHeight);

			return new Rect(position.x, position.y, _menuWidth, _menuHeight);
		}

		private Side DetermineOptimalSide(Rect buttonRect)
		{
			float rightSpace = Screen.width - buttonRect.xMax;
			float leftSpace = buttonRect.x;
			float bottomSpace = Screen.height - buttonRect.yMax;
			float topSpace = buttonRect.y;

			if (bottomSpace >= _menuHeight)
				return Side.Bottom;
			if (rightSpace >= _menuWidth)
				return Side.Right;
			if (leftSpace >= _menuWidth)
				return Side.Left;
			return Side.Top;
		}

		private void AdjustMenuRectForAnimation(ref Rect menuRect, float scale)
		{
			switch (_actualSide)
			{
				case Side.Right:
					break;
				case Side.Left:
					menuRect.x += _menuWidth * (1f - scale);
					break;
				case Side.Bottom:
					break;
				case Side.Top:
					menuRect.y += _menuHeight * (1f - scale);
					break;
			}
		}

		private void UpdateAnimation()
		{
			if (_menuState == MenuState.Opening || _menuState == MenuState.Closing)
			{
				_animationTime += Time.deltaTime;

				if (_animationTime >= _animationDuration)
				{
					_animationTime = _animationDuration;

					if (_menuState == MenuState.Opening)
						_menuState = MenuState.Open;
					else if (_menuState == MenuState.Closing)
						_menuState = MenuState.Closed;
				}
			}
		}

		private float GetAnimationProgress()
		{
			if (_menuState == MenuState.Closed)
				return 0f;
			if (_menuState == MenuState.Open)
				return 1f;

			return Mathf.Clamp01(_animationTime / _animationDuration);
		}

		private void DrawRoundedRect(Rect rect, Color fillColor, Color borderColor, float borderThickness, float cornerRadius)
		{
			if (borderThickness > 0f)
			{
				Rect borderRect = new Rect(
					rect.x - borderThickness,
					rect.y - borderThickness,
					rect.width + borderThickness * 2f,
					rect.height + borderThickness * 2f
				);
				GUI.DrawTexture(borderRect, Texture.GetOrCreateSolid(borderColor));
			}

			GUI.DrawTexture(rect, Texture.GetOrCreateSolid(fillColor));
		}

		private IMGUIElement[] ReverseArray(IMGUIElement[] array)
		{
			if (array == null) return null;

			IMGUIElement[] reversed = new IMGUIElement[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				reversed[i] = array[array.Length - 1 - i];
			}
			return reversed;
		}

		public void ResetToDefaults()
		{
			CloseMenu();
			_selectedIndex = -1;
			_side = Side.Auto;
			_offsetX = _defaultOffset;
			_offsetY = _defaultOffset;
		}
	}
}