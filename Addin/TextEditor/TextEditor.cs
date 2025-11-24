/*
*
* This is a WIP class intended for the eventual inclusion into RuntimeUnityEditor.
* It'll be deleted once it's been integrated into the main project.
*
*/

using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using UnityEngine;

namespace Addin
{
	public class TextEditor
	{
		#region Core Text State

		private string _text = "";
		private int _cursorPosition = 0;
		private int _selectionStart = -1;
		private int _selectionEnd = -1;
		private Vector2 _scrollPosition = Vector2.zero;

		#endregion

		#region Visual Properties

		private Color _backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
		private Color _textColor = Color.white;
		private Color _cursorColor = Color.white;
		private Color _selectionColor = new Color(0.3f, 0.5f, 0.8f, 0.5f);
		private Color _lineNumberColor = new Color(0.5f, 0.5f, 0.5f, 1f);
		private Color _lineNumberBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
		private Color _currentLineHighlightColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);

		private float _fontSize = 16f;
		private float _lineHeight;
		private float _padding = 8f;
		private float _borderThickness = 1f;
		private float _cursorWidth = 2f;
		private float _cursorBlinkRate = 1f;

		private bool _linting = true;

		#endregion

		#region Feature Flags

		private bool _multiLine = true;
		private bool _readOnly = false;
		private bool _showLineNumbers = true;
		private bool _highlightCurrentLine = true;
		private bool _wordWrap = false;
		private bool _enableUndo = true;
		private int _maxUndoSteps = 100;
		private int _tabSize = 4;

		#endregion

		#region State Management

		private bool _isFocused = false;
		private bool _isDragging = false;
		private float _cursorBlinkTime = 0f;
		private bool _cursorVisible = true;
		private Vector2 _dragStartPosition;
		private int _dragStartCursorPos;
		private float _lineNumberWidth = 40f;
		private Rect _textAreaRect;
		private string[] _cachedLines;
		private string _lastCachedText = null;
		private string[] cachedHighlightedLines;
		private bool _highlightCacheDirty = true;
		private GUIStyle _cachedTextStyle;
		private GUIStyle _cachedMeasureStyle;
		private StringBuilder _syntaxBuilder = new StringBuilder(256);

		#endregion

		#region Undo/Redo System

		private class UndoState
		{
			public string Text;
			public int CursorPosition;
			public int SelectionStart;
			public int SelectionEnd;

			public UndoState(string text, int cursor, int selStart, int selEnd)
			{
				Text = text;
				CursorPosition = cursor;
				SelectionStart = selStart;
				SelectionEnd = selEnd;
			}
		}

		private Stack<UndoState> _undoStack = new Stack<UndoState>();
		private Stack<UndoState> _redoStack = new Stack<UndoState>();

		#endregion

		#region Events

		public event Action<string> OnTextChanged;
		public event Action<int> OnCursorPositionChanged;
		public event Action<int, int> OnSelectionChanged;
		public event Action<bool> OnFocus;

		#endregion

		#region Properties

		public bool Linting
		{
			get => _linting;
			set => _linting = value;
		}

		public string Text
		{
			get => _text;
			set
			{
				if (_text != value)
				{
					SaveUndoState();
					_text = value ?? "";
					_highlightCacheDirty = true;
					_cursorPosition = Mathf.Clamp(_cursorPosition, 0, _text.Length);
					ClearSelection();
					OnTextChanged?.Invoke(_text);
				}
			}
		}

		public int CursorPosition
		{
			get => _cursorPosition;
			set
			{
				int newPos = Mathf.Clamp(value, 0, _text.Length);
				if (_cursorPosition != newPos)
				{
					_cursorPosition = newPos;
					EnsureCursorVisible();
					ClearSelection();
					ResetCursorBlink();
					OnCursorPositionChanged?.Invoke(_cursorPosition);
				}
			}
		}

		public bool HasSelection => _selectionStart >= 0 && _selectionEnd >= 0 && _selectionStart != _selectionEnd;

		public string SelectedText
		{
			get
			{
				if (!HasSelection) return "";
				int start = Mathf.Min(_selectionStart, _selectionEnd);
				int end = Mathf.Max(_selectionStart, _selectionEnd);
				return _text.Substring(start, end - start);
			}
		}

		public int SelectionStart
		{
			get => _selectionStart;
			set => SetSelection(value, _selectionEnd);
		}

		public int SelectionEnd
		{
			get => _selectionEnd;
			set => SetSelection(_selectionStart, value);
		}

		public bool MultiLine
		{
			get => _multiLine;
			set => _multiLine = value;
		}

		public bool ReadOnly
		{
			get => _readOnly;
			set => _readOnly = value;
		}

		public bool ShowLineNumbers
		{
			get => _showLineNumbers;
			set => _showLineNumbers = value;
		}

		public bool HighlightCurrentLine
		{
			get => _highlightCurrentLine;
			set => _highlightCurrentLine = value;
		}

		public bool WordWrap
		{
			get => _wordWrap;
			set => _wordWrap = value;
		}

		public float FontSize
		{
			get => _fontSize;
			set
			{
				_fontSize = Mathf.Max(8f, value);
				_lineHeight = _fontSize * 1.4f;
			}
		}

		public int TabSize
		{
			get => _tabSize;
			set => _tabSize = Mathf.Clamp(value, 1, 8);
		}

		public bool IsFocused => _isFocused;

		#endregion

		#region Constructor

		public TextEditor(string initialText = "", bool multiLine = true)
		{
			_text = initialText ?? "";
			_multiLine = multiLine;
			_lineHeight = _fontSize * 1.4f;
		}

		#endregion

		#region Core Methods

		private Dictionary<int, Texture2D> _colors = new Dictionary<int, Texture2D>();

		public Texture2D GetColor(Color color)
		{
			if (_colors.ContainsKey(color.GetHashCode()))
				return _colors[color.GetHashCode()];

			Texture2D texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, color);
			texture.Apply();
			_colors.Add(color.GetHashCode(), texture);
			return texture;
		}

		public void OnGUI(Rect rect)
		{
			Event currentEvent = Event.current;
			int controlID = GUIUtility.GetControlID(FocusType.Keyboard);

			UpdateCursorBlink();

			HandleFocus(rect, currentEvent, controlID);

			Rect contentRect = new Rect(
				rect.x + _borderThickness,
				rect.y + _borderThickness,
				rect.width - _borderThickness * 2f,
				rect.height - _borderThickness * 2f
			);

			GUI.DrawTexture(contentRect, GetColor(_backgroundColor));

			float lineNumberAreaWidth = _showLineNumbers ? _lineNumberWidth : 0f;
			Rect textAreaRect = new Rect(
				contentRect.x + lineNumberAreaWidth,
				contentRect.y,
				contentRect.width - lineNumberAreaWidth,
				contentRect.height
			);

			_textAreaRect = textAreaRect;

			if (_showLineNumbers)
			{
				Rect lineNumberRect = new Rect(contentRect.x, contentRect.y, lineNumberAreaWidth, contentRect.height);
				GUI.DrawTexture(lineNumberRect, GetColor(_lineNumberBackgroundColor));
			}

			string[] lines = GetLines();
			float contentHeight = lines.Length * _lineHeight + _padding * 2f;
			float contentWidth = CalculateContentWidth(lines) + _padding * 2f;

			Rect viewRect = new Rect(0, 0, contentWidth, contentHeight);

			_scrollPosition = GUI.BeginScrollView(textAreaRect, _scrollPosition, viewRect);

			if (_highlightCurrentLine && _isFocused)
			{
				DrawCurrentLineHighlight(lines);
			}

			if (HasSelection)
			{
				DrawSelection(lines);
			}

			DrawText(_linting ? GetLintedLines() : lines);

			if (_isFocused && _cursorVisible)
			{
				DrawCursor(lines);
			}


			GUI.EndScrollView();
			if (_isFocused)
			{
				HandleInput(currentEvent, textAreaRect);
			}
			if (_showLineNumbers)
			{
				DrawLineNumbers(contentRect, lines);
			}

			}

		#endregion

		public struct Vector2Int
		{
			public int x;
			public int y;
			public Vector2Int(int x, int y)
			{
				this.x = x;
				this.y = y;
			}
		}

		#region Selection Methods

		public void SetSelection(int start, int end)
		{
			_selectionStart = Mathf.Clamp(start, -1, _text.Length);
			_selectionEnd = Mathf.Clamp(end, -1, _text.Length);

			if (_selectionStart >= 0 && _selectionEnd >= 0)
			{
				OnSelectionChanged?.Invoke(_selectionStart, _selectionEnd);
			}
		}

		public void SelectAll()
		{
			_selectionStart = 0;
			_selectionEnd = _text.Length;
			_cursorPosition = _text.Length;
			OnSelectionChanged?.Invoke(_selectionStart, _selectionEnd);
		}

		public void ClearSelection()
		{
			_selectionStart = -1;
			_selectionEnd = -1;
		}

		public void SelectWord(int position)
		{
			if (string.IsNullOrEmpty(_text)) return;

			position = Mathf.Clamp(position, 0, _text.Length);

			int start = position;
			int end = position;

			while (start > 0 && !char.IsWhiteSpace(_text[start - 1]))
				start--;

			while (end < _text.Length && !char.IsWhiteSpace(_text[end]))
				end++;

			SetSelection(start, end);
			_cursorPosition = end;
		}

		#endregion

		#region Cursor Movement

		public void MoveCursorToStart()
		{
			CursorPosition = 0;
		}

		public void MoveCursorToEnd()
		{
			CursorPosition = _text.Length;
		}

		public void MoveCursorToLine(int lineIndex)
		{
			string[] lines = GetLines();
			if (lineIndex < 0 || lineIndex >= lines.Length) return;

			int position = 0;
			for (int i = 0; i < lineIndex; i++)
			{
				position += lines[i].Length + 1; }

			CursorPosition = position;
		}

		public void MoveCursorToLineAndColumn(int lineIndex, int columnIndex)
		{
			string[] lines = GetLines();
			if (lineIndex < 0 || lineIndex >= lines.Length) return;

			int position = 0;
			for (int i = 0; i < lineIndex; i++)
			{
				position += lines[i].Length + 1;
			}

			position += Mathf.Clamp(columnIndex, 0, lines[lineIndex].Length);
			CursorPosition = position;
		}

		public Vector2Int GetCursorLineAndColumn()
		{
			string[] lines = GetLines();
			int charCount = 0;

			for (int i = 0; i < lines.Length; i++)
			{
				int lineLength = lines[i].Length + 1; if (charCount + lineLength > _cursorPosition)
				{
					return new Vector2Int(i, _cursorPosition - charCount);
				}
				charCount += lineLength;
			}

			return new Vector2Int(lines.Length - 1, lines[lines.Length - 1].Length);
		}

		#endregion

		#region Text Editing

		public void InsertText(string textToInsert)
		{
			if (_readOnly || string.IsNullOrEmpty(textToInsert)) return;

			SaveUndoState();

			if (HasSelection)
			{
				DeleteSelection();
			}

			_text = _text.Insert(_cursorPosition, textToInsert);
			_cursorPosition += textToInsert.Length;
			EnsureCursorVisible();

			OnTextChanged?.Invoke(_text);
			OnCursorPositionChanged?.Invoke(_cursorPosition);
		}

		public void DeleteSelection()
		{
			if (!HasSelection) return;

			int start = Mathf.Min(_selectionStart, _selectionEnd);
			int end = Mathf.Max(_selectionStart, _selectionEnd);

			_text = _text.Remove(start, end - start);
			_cursorPosition = start;
			ClearSelection();

			OnTextChanged?.Invoke(_text);
		}

		public void Backspace()
		{
			if (_readOnly) return;

			SaveUndoState();

			if (HasSelection)
			{
				DeleteSelection();
			}
			else if (_cursorPosition > 0)
			{
				_text = _text.Remove(_cursorPosition - 1, 1);
				_cursorPosition--;
				EnsureCursorVisible();
				OnTextChanged?.Invoke(_text);
			}
		}

		public void Detab()
		{
			if (_readOnly) return;
			SaveUndoState();

			if (HasSelection)
			{
				int start = Math.Min(_selectionStart, _selectionEnd);
				int end = Math.Max(_selectionStart, _selectionEnd);
				string[] lines = _text.Substring(start, end - start).Split('\n');

				for (int i = 0; i < lines.Length; i++)
				{
					if (lines[i].StartsWith("\t"))
					{
						lines[i] = lines[i].Substring(1);
					}
					else
					{
						int spacesToRemove = Math.Min(_tabSize, lines[i].Length);
						int actualSpaces = 0;
						for (int j = 0; j < spacesToRemove; j++)
						{
							if (lines[i][j] == ' ') actualSpaces++;
							else break;
						}
						if (actualSpaces > 0)
							lines[i] = lines[i].Substring(actualSpaces);
					}
				}

				string newText = string.Join("\n", lines);
				_text = _text.Substring(0, start) + newText + _text.Substring(end);
			}
			else
			{
				Vector2Int lineCol = GetCursorLineAndColumn();
				string[] lines = GetLines();
				if (lineCol.x < lines.Length)
				{
					if (lines[lineCol.x].StartsWith("\t"))
					{
						lines[lineCol.x] = lines[lineCol.x].Substring(1);
					}
					else
					{
						int spacesToRemove = Math.Min(_tabSize, lines[lineCol.x].Length);
						int actualSpaces = 0;
						for (int i = 0; i < spacesToRemove; i++)
						{
							if (lines[lineCol.x][i] == ' ') actualSpaces++;
							else break;
						}
						if (actualSpaces > 0)
							lines[lineCol.x] = lines[lineCol.x].Substring(actualSpaces);
					}
					_text = string.Join("\n", lines);
				}
			}
			_highlightCacheDirty = true;
			OnTextChanged?.Invoke(_text);
		}

		public void Delete()
		{
			if (_readOnly) return;

			SaveUndoState();

			if (HasSelection)
			{
				DeleteSelection();
			}
			else if (_cursorPosition < _text.Length)
			{
				_text = _text.Remove(_cursorPosition, 1);
				OnTextChanged?.Invoke(_text);
			}
		}

		#endregion

		#region Clipboard Operations

		public void Copy()
		{
			if (HasSelection)
			{
				GUIUtility.systemCopyBuffer = SelectedText;
			}
		}

		public void Cut()
		{
			if (_readOnly || !HasSelection) return;

			SaveUndoState();
			Copy();
			DeleteSelection();
		}

		public void Paste()
		{
			if (_readOnly) return;

			string clipboardText = GUIUtility.systemCopyBuffer;
			if (!string.IsNullOrEmpty(clipboardText))
			{
				if (!_multiLine)
				{
					clipboardText = clipboardText.Replace("\n", "").Replace("\r", "");
				}

				InsertText(clipboardText);
			}
		}

		#endregion

		#region Undo/Redo

		public void Undo()
		{
			if (!_enableUndo || _undoStack.Count == 0) return;

			_redoStack.Push(new UndoState(_text, _cursorPosition, _selectionStart, _selectionEnd));

			UndoState state = _undoStack.Pop();
			_text = state.Text;
			_cursorPosition = state.CursorPosition;
			_selectionStart = state.SelectionStart;
			_selectionEnd = state.SelectionEnd;
			EnsureCursorVisible();

			OnTextChanged?.Invoke(_text);
			OnCursorPositionChanged?.Invoke(_cursorPosition);
		}

		public void Redo()
		{
			if (!_enableUndo || _redoStack.Count == 0) return;

			_undoStack.Push(new UndoState(_text, _cursorPosition, _selectionStart, _selectionEnd));

			UndoState state = _redoStack.Pop();
			_text = state.Text;
			_cursorPosition = state.CursorPosition;
			_selectionStart = state.SelectionStart;
			_selectionEnd = state.SelectionEnd;
			EnsureCursorVisible();

			OnTextChanged?.Invoke(_text);
			OnCursorPositionChanged?.Invoke(_cursorPosition);
		}

		private void SaveUndoState()
		{
			if (!_enableUndo) return;

			_undoStack.Push(new UndoState(_text, _cursorPosition, _selectionStart, _selectionEnd));
			_redoStack.Clear();

			while (_undoStack.Count > _maxUndoSteps)
			{
				var temp = new Stack<UndoState>();
				for (int i = 0; i < _maxUndoSteps; i++)
				{
					temp.Push(_undoStack.Pop());
				}
				_undoStack.Clear();
				while (temp.Count > 0)
				{
					_undoStack.Push(temp.Pop());
				}
			}
		}

		#endregion

		#region Input Handling

		private void HandleInput(Event currentEvent, Rect textAreaRect)
		{
			if (currentEvent.type == EventType.MouseDown && textAreaRect.Contains(currentEvent.mousePosition))
			{
				HandleMouseDown(currentEvent);
			}
			else if (currentEvent.type == EventType.MouseDrag && _isDragging)
			{
				HandleMouseDrag(currentEvent);
			}
			else if (currentEvent.type == EventType.MouseUp)
			{
				_isDragging = false;
			}
			else if (currentEvent.type == EventType.KeyDown)
			{
				HandleKeyDown(currentEvent);
			}
		}

		private void HandleMouseDown(Event currentEvent)
		{
			Vector2 localMousePos = currentEvent.mousePosition - new Vector2(_textAreaRect.x, _textAreaRect.y) + _scrollPosition;
			int clickedPosition = GetCharacterIndexAtPosition(localMousePos);

			if (currentEvent.clickCount == 2)
			{
				SelectWord(clickedPosition);
			}
			else if (currentEvent.shift)
			{
				if (!HasSelection)
				{
					_selectionStart = _cursorPosition;
				}
				_selectionEnd = clickedPosition;
				_cursorPosition = clickedPosition;
				OnSelectionChanged?.Invoke(_selectionStart, _selectionEnd);
			}
			else
			{
				_cursorPosition = clickedPosition;
				ClearSelection();
				_isDragging = true;
				_dragStartPosition = currentEvent.mousePosition;
				_dragStartCursorPos = _cursorPosition;
			}

			ResetCursorBlink();
			currentEvent.Use();
		}

		private void HandleMouseDrag(Event currentEvent)
		{
			Vector2 localMousePos = currentEvent.mousePosition - new Vector2(_textAreaRect.x, _textAreaRect.y) + _scrollPosition;
			int draggedPosition = GetCharacterIndexAtPosition(localMousePos);

			if (draggedPosition != _cursorPosition)
			{
				if (!HasSelection)
				{
					_selectionStart = _dragStartCursorPos;
				}

				_selectionEnd = draggedPosition;
				_cursorPosition = draggedPosition;
				OnSelectionChanged?.Invoke(_selectionStart, _selectionEnd);
			}

			currentEvent.Use();
		}

		private void HandleKeyDown(Event currentEvent)
		{
			bool ctrl = currentEvent.control || currentEvent.command;
			bool shift = currentEvent.shift;
			bool alt = currentEvent.alt;
			
			switch (currentEvent.keyCode)
			{
				case KeyCode.Backspace:
					Backspace();
					currentEvent.Use();
					_highlightCacheDirty = true;
					break;

				case KeyCode.Delete:
					Delete();
					currentEvent.Use();
					_highlightCacheDirty = true;
					break;

				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					if (_multiLine && !_readOnly)
					{
						InsertText("\n");
						_highlightCacheDirty = true;
					}
					currentEvent.Use();
					return;
				case KeyCode.Tab:
					if (!_readOnly)
					{
						if (shift)
						{
							Detab();
						}
						else
							InsertText(new string(' ', _tabSize));
						_highlightCacheDirty = true;
					}
					currentEvent.Use();
					return;
				case KeyCode.LeftArrow:
					if (ctrl)
						MoveCursorWordLeft(shift);
					else
						MoveCursorLeft(shift);
					currentEvent.Use();
					break;

				case KeyCode.RightArrow:
					if (ctrl)
						MoveCursorWordRight(shift);
					else
						MoveCursorRight(shift);
					currentEvent.Use();
					break;

				case KeyCode.UpArrow:
					MoveCursorUp(shift);
					currentEvent.Use();
					break;

				case KeyCode.DownArrow:
					MoveCursorDown(shift);
					currentEvent.Use();
					break;

				case KeyCode.Home:
					if (ctrl)
						MoveCursorToStart();
					else
						MoveCursorToLineStart(shift);
					currentEvent.Use();
					break;

				case KeyCode.End:
					if (ctrl)
						MoveCursorToEnd();
					else
						MoveCursorToLineEnd(shift);
					currentEvent.Use();
					break;

				case KeyCode.A:
					if (ctrl)
					{
						SelectAll();
						currentEvent.Use();
					}
					break;

				case KeyCode.C:
					if (ctrl)
					{
						Copy();
						currentEvent.Use();
					}
					break;

				case KeyCode.X:
					if (ctrl)
					{
						Cut();
						currentEvent.Use();
						_highlightCacheDirty = true;
					}
					break;

				case KeyCode.V:
					if (ctrl)
					{
						Paste();
						currentEvent.Use();
						_highlightCacheDirty = true;
					}
					break;

				case KeyCode.Z:
					if (ctrl)
					{
						Undo();
						currentEvent.Use();
						_highlightCacheDirty = true;
					}
					break;

				case KeyCode.Y:
					if (ctrl)
					{
						Redo();
						currentEvent.Use();
						_highlightCacheDirty = true;
					}
					break;

				default:
					if (currentEvent.character != '\0' && currentEvent.character != '\t' && currentEvent.character != '\n' && !ctrl && !_readOnly || ctrl && alt)
					{
						InsertText(currentEvent.character.ToString());
						currentEvent.Use();
						_highlightCacheDirty = true;
					}
					break;
			}
		}

		#endregion

		#region Cursor Movement Helpers

		public Vector2 GetCursorLocalPosition()
		{
			Vector2Int lineCol = GetCursorLineAndColumn();
			string[] lines = GetLines();

			GUIStyle measureStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = (int)_fontSize
			};

			string lineUpToCursor = "";
			if (lineCol.x < lines.Length && lineCol.y <= lines[lineCol.x].Length)
			{
				lineUpToCursor = lines[lineCol.x].Substring(0, lineCol.y);
			}

			float localX = _padding + measureStyle.CalcSize(new GUIContent(lineUpToCursor)).x;
			float localY = lineCol.x * _lineHeight + _padding;

			return new Vector2(localX, localY);
		}

		private void EnsureCursorVisible()
		{
			if (_textAreaRect.width == 0 || _textAreaRect.height == 0)
				return; _scrollPosition.x = 0f;
			Vector2 cursorLocalPos = GetCursorLocalPosition();

			string[] lines = GetLines();
			float contentHeight = lines.Length * _lineHeight + _padding * 2f;
			float contentWidth = CalculateContentWidth(lines) + _padding * 2f;

			const float scrollbarSize = 15f;

			float viewableWidth = _textAreaRect.width;
			float viewableHeight = _textAreaRect.height;

			bool hasVerticalScrollbar = contentHeight > _textAreaRect.height;
			bool hasHorizontalScrollbar = contentWidth > _textAreaRect.width;

			if (hasVerticalScrollbar)
				viewableWidth -= scrollbarSize;

			if (hasHorizontalScrollbar)
				viewableHeight -= scrollbarSize;

			float cursorTop = cursorLocalPos.y;
			float cursorBottom = cursorLocalPos.y + _lineHeight;
			float viewTop = _scrollPosition.y;
			float viewBottom = _scrollPosition.y + viewableHeight;

			if (cursorTop < viewTop)
			{
				_scrollPosition.y = cursorTop;
			}
			else if (cursorBottom > viewBottom)
			{
				_scrollPosition.y = cursorBottom - viewableHeight;
			}

			float cursorLeft = cursorLocalPos.x;
			float cursorRight = cursorLocalPos.x + _cursorWidth;
			float viewLeft = _scrollPosition.x;
			float viewRight = _scrollPosition.x + viewableWidth;

			const float margin = 10f;

			if (cursorLeft < viewLeft)
			{
				_scrollPosition.x = Mathf.Max(0, cursorLeft - margin);
			}
			else if (cursorRight > viewRight)
			{
				_scrollPosition.x = Mathf.Max(0, cursorRight - viewableWidth + margin);
			}

			_scrollPosition.x = Mathf.Max(0, _scrollPosition.x);
			_scrollPosition.y = Mathf.Max(0, _scrollPosition.y);
		}
		private void MoveCursorLeft(bool extendSelection)
		{
			if (extendSelection)
			{
				if (!HasSelection)
					_selectionStart = _cursorPosition;
				_cursorPosition = Mathf.Max(0, _cursorPosition - 1);
				_selectionEnd = _cursorPosition;
			}
			else
			{
				if (HasSelection)
				{
					_cursorPosition = Mathf.Min(_selectionStart, _selectionEnd);
					ClearSelection();
				}
				else
				{
					_cursorPosition = Mathf.Max(0, _cursorPosition - 1);
				}
			}
			EnsureCursorVisible();
			ResetCursorBlink();
		}

		private void MoveCursorRight(bool extendSelection)
		{
			if (extendSelection)
			{
				if (!HasSelection)
					_selectionStart = _cursorPosition;
				_cursorPosition = Mathf.Min(_text.Length, _cursorPosition + 1);
				_selectionEnd = _cursorPosition;
			}
			else
			{
				if (HasSelection)
				{
					_cursorPosition = Mathf.Max(_selectionStart, _selectionEnd);
					ClearSelection();
				}
				else
				{
					_cursorPosition = Mathf.Min(_text.Length, _cursorPosition + 1);
				}
			}
			EnsureCursorVisible();
			ResetCursorBlink();
		}

		private void MoveCursorWordLeft(bool extendSelection)
		{
			if (extendSelection && !HasSelection)
				_selectionStart = _cursorPosition;

			while (_cursorPosition > 0 && char.IsWhiteSpace(_text[_cursorPosition - 1]))
				_cursorPosition--;

			while (_cursorPosition > 0 && !char.IsWhiteSpace(_text[_cursorPosition - 1]))
				_cursorPosition--;

			if (extendSelection)
				_selectionEnd = _cursorPosition;
			else
				ClearSelection();
			EnsureCursorVisible();

			ResetCursorBlink();
		}

		private void MoveCursorWordRight(bool extendSelection)
		{
			if (extendSelection && !HasSelection)
				_selectionStart = _cursorPosition;

			while (_cursorPosition < _text.Length && !char.IsWhiteSpace(_text[_cursorPosition]))
				_cursorPosition++;

			while (_cursorPosition < _text.Length && char.IsWhiteSpace(_text[_cursorPosition]))
				_cursorPosition++;

			if (extendSelection)
				_selectionEnd = _cursorPosition;
			else
				ClearSelection();
			EnsureCursorVisible();

			ResetCursorBlink();
		}

		private void MoveCursorUp(bool extendSelection)
		{
			Vector2Int lineCol = GetCursorLineAndColumn();
			if (lineCol.x > 0)
			{
				string[] lines = GetLines();
				int newPosition = 0;
				for (int i = 0; i < lineCol.x - 1; i++)
				{
					newPosition += lines[i].Length + 1;
				}
				newPosition += Mathf.Clamp(lineCol.y, 0, lines[lineCol.x - 1].Length);

				if (extendSelection)
				{
					if (!HasSelection)
						_selectionStart = _cursorPosition;
					_cursorPosition = newPosition;
					_selectionEnd = _cursorPosition;
					OnSelectionChanged?.Invoke(_selectionStart, _selectionEnd);
				}
				else
				{
					_cursorPosition = newPosition;
					ClearSelection();
				}

				OnCursorPositionChanged?.Invoke(_cursorPosition);
				EnsureCursorVisible();
			}
			ResetCursorBlink();
		}

		private void MoveCursorDown(bool extendSelection)
		{
			Vector2Int lineCol = GetCursorLineAndColumn();
			string[] lines = GetLines();
			if (lineCol.x < lines.Length - 1)
			{
				int newPosition = 0;
				for (int i = 0; i <= lineCol.x; i++)  {
					newPosition += lines[i].Length + 1;
				}
				newPosition += Mathf.Clamp(lineCol.y, 0, lines[lineCol.x + 1].Length);

				if (extendSelection)
				{
					if (!HasSelection)
						_selectionStart = _cursorPosition;
					_cursorPosition = newPosition;
					_selectionEnd = _cursorPosition;
					OnSelectionChanged?.Invoke(_selectionStart, _selectionEnd);
				}
				else
				{
					_cursorPosition = newPosition;
					ClearSelection();
				}

				OnCursorPositionChanged?.Invoke(_cursorPosition);
				EnsureCursorVisible();
			}
			ResetCursorBlink();
		}

		private void MoveCursorToLineStart(bool extendSelection)
		{
			Vector2Int lineCol = GetCursorLineAndColumn();
			string[] lines = GetLines();

			int newPosition = 0;
			for (int i = 0; i < lineCol.x; i++)
			{
				newPosition += lines[i].Length + 1;
			}

			if (extendSelection)
			{
				if (!HasSelection)
					_selectionStart = _cursorPosition;
				_cursorPosition = newPosition;
				_selectionEnd = _cursorPosition;
				OnSelectionChanged?.Invoke(_selectionStart, _selectionEnd);
			}
			else
			{
				_cursorPosition = newPosition;
				ClearSelection();
			}

			OnCursorPositionChanged?.Invoke(_cursorPosition);
			EnsureCursorVisible();
			ResetCursorBlink();
		}

		private void MoveCursorToLineEnd(bool extendSelection)
		{
			Vector2Int lineCol = GetCursorLineAndColumn();
			string[] lines = GetLines();

			int newPosition = 0;
			for (int i = 0; i < lineCol.x; i++)
			{
				newPosition += lines[i].Length + 1;
			}
			newPosition += lines[lineCol.x].Length;

			if (extendSelection)
			{
				if (!HasSelection)
					_selectionStart = _cursorPosition;
				_cursorPosition = newPosition;
				_selectionEnd = _cursorPosition;
				OnSelectionChanged?.Invoke(_selectionStart, _selectionEnd);
			}
			else
			{
				_cursorPosition = newPosition;
				ClearSelection();
			}

			OnCursorPositionChanged?.Invoke(_cursorPosition);
			EnsureCursorVisible();
			ResetCursorBlink();
		}

		#endregion

		#region Drawing Methods

		private void DrawText(string[] lines)
		{
			GUIStyle textStyle = GetTextStyle();

			int firstVisibleLine = Mathf.Max(0, Mathf.FloorToInt(_scrollPosition.y / _lineHeight));
			int lastVisibleLine = Mathf.Min(lines.Length, Mathf.CeilToInt((_scrollPosition.y + _textAreaRect.height) / _lineHeight) + 1);

			bool ib = false;
			for (int i = firstVisibleLine; i < lastVisibleLine; i++)
			{
				Rect lineRect = new Rect(_padding, i * _lineHeight + _padding, 2000, _lineHeight + 5);
				GUI.Label(lineRect, lines[i] /*ApplySyntaxHighlighting(lines[i], ref ib)*/, textStyle);
			}
		}

		private string ApplySyntaxHighlighting(string line, ref bool inMultilineComment)
		{
			if (string.IsNullOrEmpty(line))
				return line;

			_syntaxBuilder.Remove(0, _syntaxBuilder.Length);
			int i = 0;

			while (i < line.Length)
			{
				if (inMultilineComment)
				{
					int endComment = line.IndexOf("*/", i);
					if (endComment >= 0)
					{
						_syntaxBuilder.Append($"<color=#57A64A>{line.Substring(i, endComment - i + 2)}</color>");
						i = endComment + 2;
						inMultilineComment = false;
						continue;
					}
					_syntaxBuilder.Append($"<color=#57A64A>{line.Substring(i)}</color>");
					break;
				}

				if (i < line.Length - 1 && line[i] == '/' && line[i + 1] == '*')
				{
					inMultilineComment = true;
					int endComment = line.IndexOf("*/", i + 2);
					if (endComment >= 0)
					{
						_syntaxBuilder.Append($"<color=#57A64A>{line.Substring(i, endComment - i + 2)}</color>");
						i = endComment + 2;
						inMultilineComment = false;
						continue;
					}
					_syntaxBuilder.Append($"<color=#57A64A>{line.Substring(i)}</color>");
					break;
				}

				if (i < line.Length - 1 && line[i] == '/' && line[i + 1] == '/')
				{
					_syntaxBuilder.Append($"<color=#57A64A>{line.Substring(i)}</color>");
					break;
				}

				if (line[i] == '"')
				{
					int endQuote = i + 1;
					while (endQuote < line.Length)
					{
						if (line[endQuote] == '"' && line[endQuote - 1] != '\\')
							break;
						endQuote++;
					}

					if (endQuote < line.Length)
					{
						string str = line.Substring(i, endQuote - i + 1);
						_syntaxBuilder.Append($"<color=#D69D85>{str}</color>");
						i = endQuote + 1;
						continue;
					}
				}

				if (char.IsLetter(line[i]) || line[i] == '_')
				{
					int start = i;
					while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
						i++;

					string identifier = line.Substring(start, i - start);

					if (IsKeyword(identifier))
					{
						_syntaxBuilder.Append($"<color=#569CD6>{identifier}</color>");
					}
					else if (char.IsUpper(identifier[0]))
					{
						_syntaxBuilder.Append($"<color=#4EC9B0>{identifier}</color>");
					}
					else if (i < line.Length && line[i] == '(')
					{
						_syntaxBuilder.Append($"<color=#DCDCAA>{identifier}</color>");
					}
					else
					{
						_syntaxBuilder.Append(identifier);
					}
					continue;
				}

				if (char.IsDigit(line[i]))
				{
					int start = i;
					while (i < line.Length && (char.IsDigit(line[i]) || line[i] == '.' || line[i] == 'f' || line[i] == 'd'))
						i++;

					_syntaxBuilder.Append($"<color=#B5CEA8>{line.Substring(start, i - start)}</color>");
					continue;
				}

				_syntaxBuilder.Append(line[i]);
				i++;
			}

			return _syntaxBuilder.ToString();
		}

		private bool IsKeyword(string word)
		{
			string[] keywords =
			{
				"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
				"class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
				"enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
				"foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
				"long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
				"private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
				"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
				"try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "var",
				"virtual", "void", "volatile", "while"
			};

			return Array.IndexOf(keywords, word) >= 0;
		}

		private void DrawCursor(string[] lines)
		{
			Vector2Int lineCol = GetCursorLineAndColumn();

			GUIStyle measureStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = (int)_fontSize
			};

			string lineUpToCursor = "";
			if (lineCol.x < lines.Length && lineCol.y <= lines[lineCol.x].Length)
			{
				lineUpToCursor = lines[lineCol.x].Substring(0, lineCol.y);
			}

			float cursorX = _padding + measureStyle.CalcSize(new GUIContent(lineUpToCursor)).x;
			float cursorY = lineCol.x * _lineHeight + _padding;

			Rect cursorRect = new Rect(cursorX, cursorY, _cursorWidth, _lineHeight);
			GUI.DrawTexture(cursorRect, GetColor(_cursorColor));
		}

		private void DrawSelection(string[] lines)
		{
			if (!HasSelection) return;

			int start = Mathf.Min(_selectionStart, _selectionEnd);
			int end = Mathf.Max(_selectionStart, _selectionEnd);

			GUIStyle measureStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = (int)_fontSize
			};

			int charCount = 0;
			for (int i = 0; i < lines.Length; i++)
			{
				int lineStart = charCount;
				int lineEnd = charCount + lines[i].Length;

				if (end >= lineStart && start <= lineEnd)
				{
					int selStart = Mathf.Max(0, start - charCount);
					int selEnd = Mathf.Min(lines[i].Length, end - charCount);

					string beforeSelection = lines[i].Substring(0, selStart);
					string selection = lines[i].Substring(selStart, selEnd - selStart);

					float startX = _padding + measureStyle.CalcSize(new GUIContent(beforeSelection)).x;
					float width = measureStyle.CalcSize(new GUIContent(selection)).x;

					if (width < 2f) width = 6f; Rect selectionRect = new Rect(startX, i * _lineHeight + _padding, width, _lineHeight);
					GUI.DrawTexture(selectionRect, GetColor(_selectionColor));
				}

				charCount += lines[i].Length + 1; }
		}

		private void DrawCurrentLineHighlight(string[] lines)
		{
			Vector2Int lineCol = GetCursorLineAndColumn();
			Rect highlightRect = new Rect(0, lineCol.x * _lineHeight + _padding, 4000, _lineHeight);
			GUI.DrawTexture(highlightRect, GetColor(_currentLineHighlightColor));
		}

		private void DrawLineNumbers(Rect contentRect, string[] lines)
		{
			GUIStyle lineNumberStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = (int)_fontSize,
				normal = { textColor = _lineNumberColor },
				alignment = TextAnchor.MiddleRight,
				padding = new RectOffset { right = 5 }
			};

			Rect lineNumberAreaRect = new Rect(
				contentRect.x,
				contentRect.y,
				_lineNumberWidth,
				contentRect.height
			);

			GUI.BeginGroup(lineNumberAreaRect);

			float scrollOffsetY = _scrollPosition.y;
			int firstVisibleLine = Mathf.Max(0, Mathf.FloorToInt(scrollOffsetY / _lineHeight));
			int lastVisibleLine = Mathf.Min(lines.Length, Mathf.CeilToInt((scrollOffsetY + contentRect.height) / _lineHeight) + 1);

			for (int i = firstVisibleLine; i < lastVisibleLine; i++)
			{
				Rect lineNumRect = new Rect(0, i * _lineHeight - scrollOffsetY + _padding, _lineNumberWidth + 5, _lineHeight);
				GUI.Label(lineNumRect, (i + 1).ToString(), lineNumberStyle);
			}

			GUI.EndGroup();
		}

		#endregion

		#region Utility Methods

		private void HandleFocus(Rect rect, Event currentEvent, int controlID)
		{
			bool wasClicked = currentEvent.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition);
			bool wasFocused = _isFocused;

			if (wasClicked)
			{
				_isFocused = true;
				GUIUtility.keyboardControl = controlID;
				if (!wasFocused)
				{
					OnFocus?.Invoke(true);
				}
			}
			else if (currentEvent.type == EventType.MouseDown && !rect.Contains(currentEvent.mousePosition))
			{
				if (_isFocused)
				{
					_isFocused = false;
					GUIUtility.keyboardControl = 0;
					OnFocus?.Invoke(false);
				}
			}
		}

		private void UpdateCursorBlink()
		{
			_cursorBlinkTime += Time.deltaTime;
			if (_cursorBlinkTime >= _cursorBlinkRate)
			{
				_cursorVisible = !_cursorVisible;
				_cursorBlinkTime = 0f;
			}
		}

		private void ResetCursorBlink()
		{
			_cursorBlinkTime = 0f;
			_cursorVisible = true;
		}

		private string[] GetLines()
		{
			/*if (_text != _lastCachedText)
			{
				_cachedLines = string.IsNullOrEmpty(_text) ? new string[] { "" } : _text.Split('\n');
				_lastCachedText = _text;
			}*/
			if (_highlightCacheDirty)
			{
				_cachedLines = string.IsNullOrEmpty(_text) ? new string[] { "" } : _text.Split('\n');
				}

			return _cachedLines;
		}

		private string[] GetLintedLines()
		{
			if (_highlightCacheDirty)
			{
				string[] lines = GetLines();
				cachedHighlightedLines = new string[lines.Length];
				bool isMultilineComment = false;

				for (var i = 0; i < lines.Length; i++)
				{
					cachedHighlightedLines[i] = ApplySyntaxHighlighting(lines[i], ref isMultilineComment);
				}

				_highlightCacheDirty = false;
			}

			return cachedHighlightedLines;
		}

		private GUIStyle GetTextStyle()
		{
			if (_cachedTextStyle == null)
			{
				_cachedTextStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = (int)_fontSize,
					normal = { textColor = _textColor },
					alignment = TextAnchor.UpperLeft,
					wordWrap = _wordWrap,
					richText = true
				};
			}

			return _cachedTextStyle;
		}

		private float _cachedMaxWidth;

		private float CalculateContentWidth(string[] lines)
		{
			if (_highlightCacheDirty)
			{
				GUIStyle measureStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = (int)_fontSize
				};

				float maxWidth = 0f;
				foreach (string line in lines)
				{
					float lineWidth = measureStyle.CalcSize(new GUIContent(line)).x;
					maxWidth = Mathf.Max(maxWidth, lineWidth);
				}

				_cachedMaxWidth = maxWidth;
			}

			return _cachedMaxWidth;
		}

		private int GetCharacterIndexAtPosition(Vector2 position)
		{
			position -= new Vector2(_padding, _padding);

			int lineIndex = Mathf.FloorToInt(position.y / _lineHeight);
			string[] lines = GetLines();
			lineIndex = Mathf.Clamp(lineIndex, 0, lines.Length - 1);

			GUIStyle measureStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = (int)_fontSize
			};

			string line = lines[lineIndex];
			float currentX = 0f;
			int columnIndex = 0;

			for (int i = 0; i < line.Length; i++)
			{
				float charWidth = measureStyle.CalcSize(new GUIContent(line[i].ToString())).x;
				if (position.x < currentX + charWidth * 0.5f)
				{
					columnIndex = i;
					break;
				}
				currentX += charWidth;
				columnIndex = i + 1;
			}

			int absolutePosition = 0;
			for (int i = 0; i < lineIndex; i++)
			{
				absolutePosition += lines[i].Length + 1;
			}
			absolutePosition += columnIndex;

			return Mathf.Clamp(absolutePosition, 0, _text.Length);
		}

		#endregion

		#region Color Configuration

		public void SetColors(Color background, Color text, Color cursor, Color selection)
		{
			_backgroundColor = background;
			_textColor = text;
			_cursorColor = cursor;
			_selectionColor = selection;
		}

		public void SetLineNumberColors(Color lineNumberColor, Color lineNumberBackground)
		{
			_lineNumberColor = lineNumberColor;
			_lineNumberBackgroundColor = lineNumberBackground;
		}

		#endregion
	}
}