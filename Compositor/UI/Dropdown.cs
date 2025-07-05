using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class Dropdown
    {
        private string[] _options = new string[0];
        private int _selectedIndex = 0;
        private bool _isExpanded = false;
        private Vector2 _scrollPosition = Vector2.zero;

        public float MaxHeight { get; set; } = 200f;
        public float MinWidth { get; set; } = 100f;
        public string Placeholder { get; set; } = "Select...";

        public event Action<int> OnSelectionChanged;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetSelectedIndex(value);
        }

        public string SelectedOption
        {
            get => HasValidSelection ? _options[_selectedIndex] : null;
        }

        public bool IsExpanded => _isExpanded;

        public string[] Options
        {
            get => _options;
        }

        public bool HasValidSelection
        {
            get => _options != null && _selectedIndex >= 0 && _selectedIndex < _options.Length;
        }

        public int Count
        {
            get => _options?.Length ?? 0;
        }

        public Dropdown()
        {
            _options = new string[0];
        }

        public Dropdown(string[] options, int selectedIndex = 0)
        {
            UpdateList(options);
            SetSelectedIndex(selectedIndex);
        }

        public void UpdateList(string[] options)
        {
            _options = options ?? new string[0];

            if (_options.Length == 0)
            {
                _selectedIndex = -1;
            }
            else
            {
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _options.Length - 1);
            }

            _isExpanded = false;
        }

        public void UpdateList<T>(T[] items) where T : class
        {
            if (items == null)
            {
                UpdateList(new string[0]);
                return;
            }
            string[] options = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                options[i] = items[i]?.ToString() ?? "null";
            }

            UpdateList(options);
        }

        public void Draw()
        {
            Draw(GUILayout.MinWidth(MinWidth));
        }

        public void Draw(params GUILayoutOption[] options)
        {
            if (_options ==  null || _options.Length == 0)
            {
                DrawEmptyDropdown(options);
                return;
            }

            DrawDropdownButton(options);

            if (_isExpanded)
            {
                DrawDropdownList();
            }
        }

        public void Draw(Rect rect)
        {
            if (_options == null || _options.Length == 0)
            {
                DrawEmptyDropdown(rect);
                return;
            }

            Rect buttonRect = new Rect(rect.x, rect.y, rect.width, 20);

            DrawDropdownButton(buttonRect);

            if (_isExpanded)
            {
                Rect dropdownRect = new Rect(rect.x, rect.y + 22, rect.width, Mathf.Min(MaxHeight, _options.Length * 20 + 10));
                DrawDropdownList(dropdownRect);
            }
        }

        public void SetSelectedIndex(int index)
        {
            int oldIndex = _selectedIndex;

            if (_options == null || _options.Length == 0)
            {
                _selectedIndex = -1;
            }
            else
            {
                _selectedIndex = Mathf.Clamp(index, 0, _options.Length - 1);
            }

            if (oldIndex != _selectedIndex)
            {
                OnSelectionChanged?.Invoke(_selectedIndex);
            }
        }

        public void SetSelectedOption(string option)
        {
            if (_options == null)
                return;

            for (var i = 0; i < _options.Length; i++)
            {
                if (_options[i] != option)
                    continue;
                SetSelectedIndex(i);
                return;
            }
        }

        public void Close() => _isExpanded = false;

        private void DrawEmptyDropdown(params GUILayoutOption[] options)
        {
            GUI.enabled = false;
            GUILayout.Button("No options available", options);
            GUI.enabled = true;
        }

        public void DrawEmptyDropdown(Rect rect)
        {
            GUI.enabled = false;
            GUI.Button(rect, "No options available");
            GUI.enabled = true;
        }

        private void DrawDropdownButton(params GUILayoutOption[] options)
        {
            string buttonText = HasValidSelection ? _options[_selectedIndex] : Placeholder;
            string displaytext = $"{buttonText}";

            if (GUILayout.Button(displaytext, options))
            {
                _isExpanded = !_isExpanded;
            }
        }

        private void DrawDropdownButton(Rect rect)
        {
            string buttonText = HasValidSelection ? _options[_selectedIndex] : Placeholder;
            string displaytext = $"{buttonText}";

            if (GUI.Button(rect, displaytext))
            {
                _isExpanded = !_isExpanded;
            }
        }

        private void DrawDropdownList()
        {
            float itemHeight = 20;
            float totalHeight = Mathf.Min(MaxHeight, _options.Length * itemHeight + 10);

            GUILayout.BeginVertical("box", GUILayout.Height(totalHeight));

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            for (var i = 0; i < _options.Length; i++)
            {
                bool isSelected = i == _selectedIndex;

                if (isSelected)
                    GUI.backgroundColor = Color.cyan;

                if (GUILayout.Button(_options[i], GUILayout.Height(itemHeight)))
                {
                    SetSelectedIndex(i);
                    _isExpanded = false;
                }

                if (isSelected)
                    GUI.backgroundColor = Color.white;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void  DrawDropdownList(Rect rect)
        {
            GUI.Box(rect, "");

            float itemHeight = 20;
            int visibleItems = Mathf.FloorToInt((rect.height - 10) / itemHeight);

            Rect scrollRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);
            Rect contentRect = new Rect(0, 0, scrollRect.width - 20, _options.Length * itemHeight);

            _scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, contentRect);

            for (var i = 0; i < _options.Length; i++)
            {
                Rect itemRect = new Rect(0, i * itemHeight, contentRect.width, itemHeight);
                bool isSelected = i == _selectedIndex;
                if (isSelected)
                    GUI.backgroundColor = Color.cyan;

                if (GUI.Button(itemRect, _options[i]))
                {
                    SetSelectedIndex(i);
                    _isExpanded = false;
                }

                if (isSelected)
                    GUI.backgroundColor = Color.white;
            }

            GUI.EndScrollView();
        }
    }
}