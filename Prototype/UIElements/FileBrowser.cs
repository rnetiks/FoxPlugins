// Still in development

/*using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Prototype.UIElements
{
    /// <summary>
    /// A Unity user interface component for browsing and selecting files or directories, implemented with the IMGUI framework.
    /// </summary>
    /// <remarks>
    /// This class facilitates the creation of a file browsing dialog with features such as path navigation, filtering support,
    /// and selection callbacks. It provides a resizable and draggable window that integrates with Unity's IMGUI system.
    /// </remarks>
    public class FileBrowser : IMGUIWindow
    {
        private string _currentPath;
        private List<string> _files = new List<string>();
        private List<string> _directories = new List<string>();
        private VirtualScrollView _scrollView;
        private string _selectedPath = "";
        private Action<string> _onFileSelected;
        private string _fileFilter = "";

        public FileBrowser(string id) : base(id, "File Browser", new Rect(100, 100, 600, 400))
        {
            _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _scrollView = new VirtualScrollView();
            RefreshFileList();
        }

        public void ShowOpenDialog(string filter, Action<string> onFileSelected)
        {
            _fileFilter = filter;
            _onFileSelected = onFileSelected;
            IsVisible = true;
            RefreshFileList();
        }
        
        protected override void DrawContent(Rect rect)
        {
            var contentRect = new Rect(10, 20, rect.width - 20, rect.height - 60);

            var pathRect = new Rect(contentRect.x, contentRect.y, contentRect.width, 25);
            GUI.Label(pathRect, $"Path: {_currentPath}");

            var listRect = new Rect(contentRect.x, contentRect.y + 30, contentRect.width, contentRect.height - 70);

            _scrollView.BeginScrollView(listRect, _directories.Count + _files.Count, out var viewRect);

            int itemIndex = 0;

            foreach (string dir in _directories)
            {
                if (itemIndex >= _scrollView.VisibleStartIndex && itemIndex <= _scrollView.VisibleEndIndex)
                {
                    var itemRect = _scrollView.GetItemRect(itemIndex);
                    itemRect.width = viewRect.width;

                    if (GUI.Button(itemRect, $"{Path.GetFileName(dir)}", GUI.skin.label))
                    {
                        _currentPath = dir;
                        RefreshFileList();
                    }
                }
                itemIndex++;
            }

            foreach (string file in _files)
            {
                if (itemIndex >= _scrollView.VisibleStartIndex && itemIndex <= _scrollView.VisibleEndIndex)
                {
                    var itemRect = _scrollView.GetItemRect(itemIndex);
                    itemRect.width = viewRect.width;

                    bool isSelected = file == _selectedPath;
                    // var style = isSelected ? IMGUIManager.Themes.GetThemedStyle("label", "selected") : GUI.skin.label;

                    if (GUI.Button(itemRect, $"{Path.GetFileName(file)}"/*, style#1#))
                    {
                        _selectedPath = file;
                    }
                }
                itemIndex++;
            }

            _scrollView.EndScrollView();

            var buttonRect = new Rect(contentRect.x, contentRect.yMax - 30, 100, 25);

            if (GUI.Button(buttonRect, "Open") && !string.IsNullOrEmpty(_selectedPath))
            {
                _onFileSelected?.Invoke(_selectedPath);
                IsVisible = false;
            }

            buttonRect.x += 110;
            if (GUI.Button(buttonRect, "Cancel"))
            {
                IsVisible = false;
            }
        }

        private void RefreshFileList()
        {
            try
            {
                _directories.Clear();
                _files.Clear();

                if (Directory.Exists(_currentPath))
                {
                    var parent = Directory.GetParent(_currentPath);
                    if (parent != null)
                        _directories.Add(parent.FullName);

                    _directories.AddRange(Directory.GetDirectories(_currentPath));

                    var files = Directory.GetFiles(_currentPath);
                    foreach (string file in files)
                    {
                        if (string.IsNullOrEmpty(_fileFilter) || file.EndsWith(_fileFilter))
                            _files.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to refresh file list: {ex.Message}");
            }
        }
    }
}*/