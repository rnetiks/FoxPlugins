using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefaultNamespace
{
    public class MultiselectDropdown
    {
        private string[] _options = new string[0];
        private HashSet<int> _selectedIndices = new HashSet<int>();
        private bool _isExpanded = false;
        private Vector2 _scrollPosition = Vector2.zero;

        public float MaxHeighjt { get; set; } = 200f;
        public float MinWidth { get; set; } = 100f;
        public string Placeholder { get; set; } = "Select...";
        public string AllSelectedText { get; set; } = "All Selected";
        public string NoneSelectedText { get; set; } = "None Selected";

        public event Action<int[]> OnSelectionChanged;

        public int[] SelectedIndices
        {
            get =>  _selectedIndices.ToArray();
            set => SetSelectedIndices(value);
        }

        public string[] SelectedOptions => _selectedIndices.Where(i => i >= 0 && i < _options.Length).Select(i => _options[i]).ToArray();
        
        public bool IsExpanded => _isExpanded;
        
        public string[] Options => _options;
        
        public int SelectedCount =>  _selectedIndices.Count;

        public MultiselectDropdown()
        {
            _options = new string[0];
        }

        public MultiselectDropdown(string[] options)
        {
            UpdateList(options);
        }

        public void UpdateList(string[] options)
        {
            _options = options ?? new string[0];
            _selectedIndices.RemoveWhere(i => i < 0 ||i >= _options.Length);
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
            for (var i = 0; i < items.Length; i++)
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
            if (_options == null || _options.Length == 0)
            {
                DrawEmptyDropdown();
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
                DrawEmptyDropdown();
                return;
            }

            Rect buttonRect = new Rect(rect.x, rect.y, rect.width, 20);
            DrawDropdownButton(buttonRect);

            if (_isExpanded)
            {
                Rect dropdownRect = new Rect(rect.x, rect.y + 22, rect.width, Mathf.Min(MaxHeighjt, _options.Length * 20 + 60));
                DrawDropdownList(dropdownRect);
            }
        }

        private void DrawEmptyDropdown(params  GUILayoutOption[] options)
        {
            GUI.enabled = false;
            GUILayout.Button("No options available", options);
            GUI.enabled = true;
        }

        private void DrawEmptyDropdown(Rect rect)
        {
            GUI.enabled = false;
            GUI.Button(rect, "No options available");
            GUI.enabled = true;
        }

        private void DrawDropdownButton(params GUILayoutOption[] options)
        {
            string buttonText = GetButtonText();

            if (GUILayout.Button(buttonText, options))
            {
                _isExpanded = !_isExpanded;
            }
        }

        private void DrawDropdownButton(Rect rect)
        {
            string buttonText = GetButtonText();

            if (GUI.Button(rect, buttonText))
            {
                _isExpanded = !_isExpanded;
            }
        }

        private string GetButtonText()
        {
            if (_selectedIndices.Count == 0)
                return NoneSelectedText;
            if (_selectedIndices.Count == _options.Length)
                return AllSelectedText;
            if (_selectedIndices.Count == 1)
                return _options[_selectedIndices.First()];

            return $"{_selectedIndices.Count} Selected";
        }

        private void DrawDropdownList()
        {
            float itemHeight = 20;
            float controlsHeight = 40;
            float totalHeight = Mathf.Min(MaxHeighjt, _options.Length * itemHeight + controlsHeight + 10);
            
            GUILayout.BeginVertical("Box", GUILayout.Height(totalHeight));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("All", GUILayout.Height(18)))
            {
                SelectAll();
            }

            if (GUILayout.Button("None", GUILayout.Height(18)))
            {
                ClearSelection();
            }
            if (GUILayout.Button("Invert", GUILayout.Height(18)))
            {
                InvertSelection();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            
            for (var i = 0; i < _options.Length; i++)
            {
                GUILayout.BeginHorizontal();

                bool isSelected = _selectedIndices.Contains(i);
                bool newSelected = GUILayout.Toggle(isSelected, "", GUILayout.Width(20));

                if (newSelected != isSelected)
                {
                    if(newSelected)
                        _selectedIndices.Add(i);
                    else
                        _selectedIndices.Remove(i);
                    
                    OnSelectionChanged?.Invoke(SelectedIndices);
                }

                if (GUILayout.Button(_options[i], GUI.skin.label, GUILayout.Height(itemHeight)))
                {
                    if (_selectedIndices.Contains(i))
                        _selectedIndices.Remove(i);
                    else
                        _selectedIndices.Add(i);
                    
                    OnSelectionChanged?.Invoke(SelectedIndices);
                }
                
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawDropdownList(Rect rect)
        {
            GUI.Box(rect, "");

            float itemHeight = 20;
            float controlsHeight = 22;

            Rect controlsRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, controlsHeight);
            GUILayout.BeginArea(controlsRect);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("All", GUILayout.Height(18)))
            {
                SelectAll();
            }
            if (GUILayout.Button("None", GUILayout.Height(18)))
            {
                ClearSelection();
            }
            if (GUILayout.Button("Invert", GUILayout.Height(18)))
            {
                InvertSelection();
            }
            
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            
            Rect scrollRect = new Rect(rect.x + 5, rect.y + controlsHeight + 10, rect.width - 10, rect.height - controlsHeight - 15);
            Rect contentRect = new Rect(0, 0, scrollRect.width - 20, _options.Length * itemHeight);
            
            _scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, contentRect);
            
            for (var i = 0; i < _options.Length; i++)
            {
                Rect itemRect = new Rect(0, i * itemHeight, contentRect.width, itemHeight);
                Rect toggleRect = new Rect(0, i * itemHeight, 20, itemHeight);
                Rect labelRect = new Rect(25, i * itemHeight, contentRect.width - 25, itemHeight);

                bool isSelected = _selectedIndices.Contains(i);
                bool newSelected = GUI.Toggle(toggleRect, isSelected, "");

                if (newSelected != isSelected)
                {
                    if (newSelected)
                        _selectedIndices.Add(i);
                    else
                        _selectedIndices.Remove(i);
                    
                    OnSelectionChanged?.Invoke(SelectedIndices);
                }

                if (GUI.Button(labelRect, _options[i], GUI.skin.label))
                {
                    if(_selectedIndices.Contains(i))
                        _selectedIndices.Remove(i);
                    else
                        _selectedIndices.Add(i);
                    
                    OnSelectionChanged?.Invoke(SelectedIndices);
                }
            }
            
            GUI.EndScrollView();
        }

        public void SetSelectedIndices(int[] indices)
        {
            var oldIndices = _selectedIndices.ToArray();
            _selectedIndices.Clear();

            if (indices != null)
            {
                foreach (int index in indices)
                {
                    if (index >= 0 && index < _options.Length)
                        _selectedIndices.Add(index);
                }
            }
            
            if(!oldIndices.SequenceEqual(_selectedIndices.ToArray()))
                OnSelectionChanged?.Invoke(SelectedIndices);
        }

        public void SelectAll()
        {
            _selectedIndices.Clear();
            for (var i = 0; i < _options.Length; i++)
            {
                _selectedIndices.Add(i);
            }
            OnSelectionChanged?.Invoke(SelectedIndices);
        }

        public void ClearSelection()
        {
            if (_selectedIndices.Count > 0)
            {
                _selectedIndices.Clear();
                OnSelectionChanged?.Invoke(SelectedIndices);
            }
        }

        public void InvertSelection()
        {
            var newSelection = new HashSet<int>();
            for (var i = 0; i < _options.Length; i++)
            {
                if(!_selectedIndices.Contains(i))
                    newSelection.Add(i);
            }
            _selectedIndices = newSelection;
            OnSelectionChanged?.Invoke(SelectedIndices);
        }

        public bool IsSelected(int index)
        {
            return _selectedIndices.Contains(index);
        }

        public void Close()
        {
            _isExpanded = false;
        }
    }
}