using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Studio;
using UnityEngine;

namespace PoseLib.KKS
{
    public class UIManager : IDisposable
    {
        private readonly PoseLibraryManager _poseManager;
        public static ManualLogSource _logger;
        private readonly ScreenshotManager _screenshotManager;

        private bool _isUIOpen;
        private bool _isSaveWindowOpen;
        private Rect _windowRect;
        private Rect _saveWindowRect;

        private readonly UIState _uiState;
        private readonly SaveWindowState _saveState;
        private readonly UITheme _theme;

        private Dictionary<Color, Texture2D> _colorCache = new Dictionary<Color, Texture2D>();
        private Vector2 _scrollPosition = Vector2.zero;

        public UIManager(PoseLibraryManager poseManager, ManualLogSource logger)
        {
            _poseManager = poseManager;
            _logger = logger;
            _screenshotManager = new ScreenshotManager(logger);
            _uiState = new UIState();
            _saveState = new SaveWindowState();
            _theme = new UITheme();

            InitializeWindows();
        }

        public void ToggleUI()
        {
            _isUIOpen = !_isUIOpen;
        }

        public void Update()
        {
            UpdateSearchCooldown();
        }

        public void OnGUI()
        {
            if (!_isUIOpen) return;

            if (!_theme.IsInitialized)
                _theme.InitializeStyles();

            if (_isSaveWindowOpen)
            {
                RenderSaveWindow();
            }
            else
            {
                RenderMainWindow();
            }
        }

        private void InitializeWindows()
        {
            var width = Mathf.Min(Screen.width * 0.8f, 1200);
            var height = Mathf.Min(Screen.height * 0.8f, 800);
            _windowRect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);

            _saveWindowRect = new Rect((Screen.width - 500) / 2, (Screen.height - 400) / 2, 500, 400);
        }

        private void RenderMainWindow()
        {
            GUI.Box(_windowRect, "", _theme.WindowStyle);

            if (_windowRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();

            _windowRect = GUI.Window(Constants.WINDOW_ID, _windowRect, DrawMainWindow, "", GUIStyle.none);
        }

        private void DrawMainWindow(int id)
        {
            var selectedCharacters = KKAPI.Studio.StudioAPI.GetSelectedCharacters().ToArray();

            DrawWindowHeader();

            DrawTopControls(selectedCharacters);
            if (!isDirectoryDropdownOpen && !isSortingDropdownOpen)
                DrawMainContent(selectedCharacters);

            var headerRect = new Rect(0, 0, _windowRect.width, 30);
            GUI.DragWindow(headerRect);
        }

        private void DrawWindowHeader()
        {
            var headerRect = new Rect(0, 0, _windowRect.width, 30);
            GUI.Box(headerRect, "", _theme.HeaderStyle);

            var titleRect = new Rect(10, 5, _windowRect.width - 60, 20);
            GUI.Label(titleRect, "Pose Library", _theme.TitleStyle);

            var closeRect = new Rect(_windowRect.width - 35, 5, 25, 20);
            if (GUI.Button(closeRect, "×", _theme.CloseButtonStyle))
            {
                _isUIOpen = false;
            }
        }

        private void DrawTopControls(OCIChar[] selectedCharacters)
        {
            var controlsRect = new Rect(10, 35, _windowRect.width - 20, 60);
            GUI.Box(controlsRect, "", _theme.ControlsPanelStyle);

            var currentY = 40;
            var currentX = 15;

            if (selectedCharacters.Length > 0)
            {
                var saveButtonRect = new Rect(currentX, currentY, 100, 25);
                if (GUI.Button(saveButtonRect, "Save Pose", _theme.ButtonStyle))
                    OpenSaveWindow(selectedCharacters[0]);
                currentX += 110;
            }

            var searchLabelRect = new Rect(currentX, currentY, 50, 25);
            GUI.Label(searchLabelRect, "Search:", _theme.LabelStyle);
            currentX += 55;

            var searchRect = new Rect(currentX, currentY, 200, 25);
            _uiState.SearchQuery = GUI.TextField(searchRect, _uiState.SearchQuery, _theme.TextFieldStyle);
            currentX += 210;

            var dirLabelRect = new Rect(currentX, currentY, 50, 25);
            GUI.Label(dirLabelRect, "Folder:", _theme.LabelStyle);
            currentX += (int)dirLabelRect.width;

            var dirRect = new Rect(currentX, currentY, 120, 25);
            DrawDirectoryDropdown(dirRect);

            currentX += (int)dirRect.width + 15;

            var sortLabelRect = new Rect(currentX, currentY, 50, 25);
            GUI.Label(sortLabelRect, "Sort by:", _theme.LabelStyle);
            currentX += (int)sortLabelRect.width;

            var sortRect = new Rect(currentX, currentY, 120, 25);
            DrawSortingDropdown(sortRect);

            if (selectedCharacters.Length == 0)
            {
                var messageRect = new Rect(_windowRect.width - 200, currentY, 185, 25);
                GUI.Label(messageRect, "Select a character to save poses", _theme.WarningStyle);
            }
        }

        private bool isDirectoryDropdownOpen;

        private Vector2 directoryScrollPosition;
        private bool isSortingDropdownOpen;

        private List<string> _cachedDirectories;
        private string[] _cachedDisplayNames;
        private DateTime _lastDirectoryRefresh = DateTime.MinValue;
        private int _cachedSelectedIndex = 0;
        private string _lastSelectedDirectory = "";

        private void DrawDirectoryDropdown(Rect rect)
        {
            if (_cachedDirectories == null || (DateTime.Now - _lastDirectoryRefresh).TotalSeconds > 5)
            {
                _cachedDirectories = GetAvailableDirectories();
                _cachedDisplayNames = _cachedDirectories.Select(d => d == "ALL" ? "All Folders" : Path.GetFileName(d)).ToArray();
                _lastDirectoryRefresh = DateTime.Now;
            }

            if (_cachedDirectories.Count == 0) return;

            if (_lastSelectedDirectory != _uiState.SelectedDirectory)
            {
                _cachedSelectedIndex = _cachedDirectories.FindIndex(d => d == _uiState.SelectedDirectory);
                if (_cachedSelectedIndex == -1) _cachedSelectedIndex = 0;
                _lastSelectedDirectory = _uiState.SelectedDirectory;
            }

            if (GUI.Button(rect, _cachedDisplayNames[_cachedSelectedIndex], _theme.ButtonStyle))
            {
                isDirectoryDropdownOpen = !isDirectoryDropdownOpen;
            }

            if (isDirectoryDropdownOpen)
            {
                if (Event.current.type == EventType.MouseDown &&
                    !new Rect(rect.x, rect.y, rect.width + 20, rect.height * (_cachedDirectories.Count + 1)).Contains(Event.current.mousePosition))
                {
                    isDirectoryDropdownOpen = false;
                }

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    isDirectoryDropdownOpen = false;
                    Event.current.Use();
                }

                float totalHeight = _cachedDirectories.Count * rect.height;
                float visibleHeight = Mathf.Min(totalHeight, 200);
                Rect viewRect = new Rect(rect.x, rect.y + rect.height, rect.width + 20, visibleHeight);
                Rect contentRect = new Rect(0, 0, rect.width, totalHeight);
                directoryScrollPosition = GUI.BeginScrollView(viewRect, directoryScrollPosition, contentRect);

                for (int i = 0; i < _cachedDirectories.Count; i++)
                {
                    Rect itemRect = new Rect(0, i * rect.height, rect.width, rect.height);

                    if (GUI.Button(itemRect, _cachedDisplayNames[i], _theme.DropdownStyle ?? _theme.ButtonStyle))
                    {
                        _uiState.SelectedDirectory = _cachedDirectories[i];
                        _uiState.CurrentPage = 1;
                        isDirectoryDropdownOpen = false;
                    }
                }

                GUI.EndScrollView();
            }
        }

        private void DrawSortingDropdown(Rect rect)
        {
            var sortOptions = new[] { "Name", "Name (Desc)", "Date Created", "Date Created (Desc)", "Date Modified", "Date Modified (Desc)" };
            var currentIndex = (int)_uiState.SortBy;

            if (GUI.Button(rect, sortOptions[currentIndex], _theme.ButtonStyle))
            {
                isSortingDropdownOpen = !isSortingDropdownOpen;
            }

            if (isSortingDropdownOpen)
            {
                if (Event.current.type == EventType.MouseDown &&
                    !new Rect(rect.x, rect.y, rect.width, rect.height * (sortOptions.Length + 1)).Contains(Event.current.mousePosition))
                {
                    isSortingDropdownOpen = false;
                }

                for (var i = 0; i < sortOptions.Length; i++)
                {
                    string displayText = sortOptions[i];
                    Rect itemRect = new Rect(rect.x, rect.y + rect.height + (i * rect.height), rect.width, rect.height);
                    if (GUI.Button(itemRect, displayText, _theme.DropdownStyle))
                    {
                        _uiState.SortBy = (SortBy)i;
                        _uiState.CurrentPage = 1;
                        isSortingDropdownOpen = false;
                    }
                }
            }
        }

        private List<string> GetAvailableDirectories()
        {
            var directories = new List<string> { "ALL" };

            var vanillaPosesPath = Path.Combine("UserData/studio", "pose");
            if (Directory.Exists(vanillaPosesPath))
            {
                directories.AddRange(Directory.GetDirectories(vanillaPosesPath, "*", SearchOption.AllDirectories));
            }

            return directories;
        }

        private void DrawMainContent(OCIChar[] selectedCharacters)
        {
            var contentRect = new Rect(10, 100, _windowRect.width - 20, _windowRect.height - 140);

            if (selectedCharacters.Length == 0)
            {
                DrawNoCharacterMessage(contentRect);
                return;
            }

            var searchQuery = new SearchQuery
            {
                Text = _uiState.SearchQuery,
                Page = _uiState.CurrentPage,
                Directory = _uiState.SelectedDirectory.Replace("/", "\\"),
                SortBy = _uiState.SortBy
            };

            var poses = _poseManager.SearchPoses(searchQuery);
            var totalPoses = _poseManager.GetTotalPoseCount(searchQuery);
            var totalPages = Mathf.Ceil((float)totalPoses / Constants.MAX_POSES_PER_PAGE);

            if (poses.Count == 0)
            {
                DrawNoPosesMessage(contentRect);
                return;
            }

            DrawPoseGrid(contentRect, poses, selectedCharacters);
            DrawPagination(totalPages);
        }

        private void DrawNoCharacterMessage(Rect contentRect)
        {
            var messageRect = new Rect(contentRect.x, contentRect.y + contentRect.height / 2 - 50, contentRect.width, 100);
            GUI.Box(messageRect, "", _theme.MessageBoxStyle);

            var textRect = new Rect(messageRect.x + 20, messageRect.y + 20, messageRect.width - 40, 60);
            GUI.Label(textRect, "Please select a character in the Studio to save and load poses.", _theme.MessageStyle);
        }

        private void DrawNoPosesMessage(Rect contentRect)
        {
            var messageRect = new Rect(contentRect.x, contentRect.y + contentRect.height / 2 - 30, contentRect.width, 60);
            GUI.Box(messageRect, "", _theme.MessageBoxStyle);

            var textRect = new Rect(messageRect.x + 20, messageRect.y + 15, messageRect.width - 40, 30);
            var message = string.IsNullOrEmpty(_uiState.SearchQuery) ? "No poses found in the selected directory." : $"No poses found matching '{_uiState.SearchQuery}'.";
            GUI.Label(textRect, message, _theme.MessageStyle);
        }

        private void DrawPoseGrid(Rect contentRect, List<PoseSearchResult> poses, OCIChar[] selectedCharacters)
        {
            var gridRect = new Rect(contentRect.x, contentRect.y, contentRect.width, contentRect.height + 5);

            var columns = Mathf.FloorToInt((gridRect.width - 20) / (Constants.PREVIEW_SIZE + 10));
            columns = Mathf.Clamp(columns, 2, 6);

            var itemWidth = (gridRect.width - 20 - (columns - 1) * 10) / columns;
            var itemHeight = itemWidth;
            var viewRect = new Rect(0, 0, gridRect.width - 20, Mathf.Ceil((float)poses.Count / columns) * (itemHeight + 10));
            _scrollPosition = GUI.BeginScrollView(gridRect, _scrollPosition, viewRect);

            for (int i = 0; i < poses.Count; i++)
            {
                var row = i / columns;
                var col = i % columns;
                var x = col * (itemWidth + 10);
                var y = row * (itemHeight + 10);

                var itemRect = new Rect(x, y, itemWidth, itemHeight);
                DrawPoseItem(itemRect, poses[i], selectedCharacters);
            }

            GUI.EndScrollView();
        }

        private void DrawPoseItem(Rect itemRect, PoseSearchResult pose, OCIChar[] selectedCharacters)
        {
            GUI.Box(itemRect, "", _theme.PoseItemStyle);

            var imageSize = Mathf.Min(itemRect.width - 60, Constants.PREVIEW_SIZE);
            var imageRect = new Rect(itemRect.x + (itemRect.width - imageSize) / 2, itemRect.y + 5, imageSize, imageSize);
            GUI.DrawTexture(imageRect, pose.PreviewTexture, ScaleMode.ScaleToFit);
            if (pose.FilePath.ToLower().EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
            {
                GUI.Label(imageRect, "No Texture\nIncluded", _theme.LabelCenterStyle);
            }

            var nameRect = new Rect(itemRect.x + 5, imageRect.yMax + 2, itemRect.width - 10, 20);
            var fileName = Path.GetFileNameWithoutExtension(pose.FilePath);
            GUI.Label(nameRect, fileName, _theme.PoseNameStyle);

            if (itemRect.Contains(Event.current.mousePosition))
            {
                DrawPoseItemButtons(itemRect, pose, selectedCharacters);
            }
        }

        private void DrawPoseItemButtons(Rect itemRect, PoseSearchResult pose, OCIChar[] selectedCharacters)
        {
            var buttonHeight = 25;
            var buttonY = itemRect.yMax - buttonHeight - 5;
            var buttonWidth = (itemRect.width - 15) / 2;

            var loadRect = new Rect(itemRect.x + 5, buttonY, buttonWidth, buttonHeight);
            if (GUI.Button(loadRect, "Load", _theme.LoadButtonStyle))
            {
                try
                {
                    _poseManager.LoadPoseToCharacters(pose.FilePath, selectedCharacters);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to load pose: {ex.Message}");
                }
            }

            var deleteRect = new Rect(itemRect.x + buttonWidth + 10, buttonY, buttonWidth, buttonHeight);
            if (GUI.Button(deleteRect, "Delete", _theme.DeleteButtonStyle))
            {
                if (GUI.changed)
                {
                    _poseManager.DeletePose(pose.FilePath);
                }
            }
        }

        private void DrawPagination(float totalPages)
        {
            var paginationRect = new Rect(10, _windowRect.height - 35, _windowRect.width - 20, 30);
            GUI.Box(paginationRect, "", _theme.PaginationStyle);

            var buttonWidth = 30;
            var centerX = paginationRect.x + paginationRect.width / 2;

            var prevRect = new Rect(centerX - 80, paginationRect.y + 5, buttonWidth, 20);
            GUI.enabled = _uiState.CurrentPage > 1;
            if (GUI.Button(prevRect, "◀", _theme.PaginationButtonStyle))
                _uiState.CurrentPage = Mathf.Max(_uiState.CurrentPage - 1, 1);

            var pageInfoRect = new Rect(centerX - 40, paginationRect.y + 5, 80, 20);
            GUI.enabled = true;
            GUI.Label(pageInfoRect, $"{_uiState.CurrentPage} / {(int)totalPages}", _theme.PaginationTextStyle);

            var nextRect = new Rect(centerX + 50, paginationRect.y + 5, buttonWidth, 20);
            GUI.enabled = _uiState.CurrentPage < totalPages;
            if (GUI.Button(nextRect, "▶", _theme.PaginationButtonStyle))
                _uiState.CurrentPage = Mathf.Min(_uiState.CurrentPage + 1, (int)totalPages);

            GUI.enabled = true;
        }

        #region Save Window

        private void RenderSaveWindow()
        {
            var modalRect = new Rect(0, 0, Screen.width, Screen.height);
            GUI.Box(modalRect, "", _theme.ModalBackgroundStyle);

            GUI.Box(_saveWindowRect, "", _theme.WindowStyle);
            _saveWindowRect = GUI.Window(Constants.WINDOW_ID + 1, _saveWindowRect, DrawSaveWindow, "", GUIStyle.none);
            if (_saveWindowRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

        private void DrawSaveWindow(int id)
        {
            DrawSaveWindowHeader();

            DrawSaveWindowContent();

            var headerRect = new Rect(0, 0, _saveWindowRect.width, 30);
            GUI.DragWindow(headerRect);
        }

        private void DrawSaveWindowHeader()
        {
            var headerRect = new Rect(0, 0, _saveWindowRect.width, 30);
            GUI.Box(headerRect, "", _theme.HeaderStyle);

            var titleRect = new Rect(10, 5, _saveWindowRect.width - 60, 20);
            GUI.Label(titleRect, "Save Pose", _theme.TitleStyle);

            var closeRect = new Rect(_saveWindowRect.width - 35, 5, 25, 20);
            if (GUI.Button(closeRect, "×", _theme.CloseButtonStyle))
            {
                CloseSaveWindow();
            }
        }

        private void DrawSaveWindowContent()
        {
            var contentY = 40;
            var contentX = 15;
            var fieldWidth = _saveWindowRect.width - 30;

            GUI.Label(new Rect(contentX, contentY, fieldWidth, 20), "Filename:", _theme.LabelStyle);
            contentY += 25;
            _saveState.FileName = GUI.TextField(new Rect(contentX, contentY, fieldWidth, 25), _saveState.FileName, _theme.TextFieldStyle);
            contentY += 35;
            GUI.Label(new Rect(contentX, contentY, fieldWidth, 20), "Screenshot Options:", _theme.LabelStyle);
            contentY += 30;

            GUI.Label(new Rect(contentX, contentY, 100, 20), "Size:", _theme.LabelStyle);
            var sizeOptions = new[] { "256x256", "512x512", "1024x1024" };
            var sizeIndex = Array.IndexOf(new[] { 256, 512, 1024 }, _saveState.ScreenshotSize);
            if (sizeIndex == -1) sizeIndex = 0;

            var newSizeIndex = GUI.SelectionGrid(new Rect(contentX + 105, contentY, fieldWidth - 105, 20), sizeIndex, sizeOptions, 3, _theme.ToggleStyle);
            if (newSizeIndex != sizeIndex)
            {
                _saveState.ScreenshotSize = new[] { 256, 512, 1024 }[newSizeIndex];
            }
            contentY += 30;


            if (_saveState.Screenshot != null)
            {
                var previewSize = 150;
                var previewRect = new Rect(_saveWindowRect.width - previewSize - 15, contentY, previewSize, previewSize);
                GUI.Box(previewRect, "", _theme.PreviewBoxStyle);
                GUI.DrawTexture(previewRect, _saveState.Screenshot, ScaleMode.ScaleToFit);
            }

            var buttonY = _saveWindowRect.height - 45;
            var buttonWidth = 120;
            var buttonSpacing = 10;

            var screenshotRect = new Rect(contentX, buttonY, buttonWidth, 30);
            if (GUI.Button(screenshotRect, "Take Screenshot", _theme.ButtonStyle))
            {
                TakeScreenshot();
            }

            var saveRect = new Rect(contentX + buttonWidth + buttonSpacing, buttonY, buttonWidth, 30);
            GUI.enabled = !_saveState.FileName.IsNullOrWhiteSpace();
            if (GUI.Button(saveRect, "Save Pose", _theme.SaveButtonStyle))
            {
                SavePose();
            }
            GUI.enabled = true;

            var cancelRect = new Rect(contentX + 2 * (buttonWidth + buttonSpacing), buttonY, buttonWidth, 30);
            if (GUI.Button(cancelRect, "Cancel", _theme.CancelButtonStyle))
            {
                CloseSaveWindow();
            }
        }

        #endregion

        private void OpenSaveWindow(OCIChar character)
        {
            _saveState.Character = character;
            _saveState.Reset();
            _isSaveWindowOpen = true;
        }

        private void TakeScreenshot()
        {
            _isUIOpen = false;

            _saveState.Screenshot = _screenshotManager.TakeScreenshot(
                _saveState.ScreenshotSize,
                _saveState.ScreenshotSize);

            _isUIOpen = true;
            _isSaveWindowOpen = true;
        }

        private void SavePose()
        {
            try
            {
                _poseManager.SavePose(_saveState.FileName, _saveState.Character, _saveState.Screenshot);
                CloseSaveWindow();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save pose: {ex.Message}");
            }
        }

        private void CloseSaveWindow()
        {
            _saveState.Reset();
            _isSaveWindowOpen = false;
        }

        private void UpdateSearchCooldown()
        {
            _uiState.SearchCooldownTimer -= Time.deltaTime;

            if (_uiState.PreviousSearchQuery != _uiState.SearchQuery && _uiState.SearchCooldownTimer <= 0)
            {
                _uiState.CurrentPage = 1;
                _uiState.PreviousSearchQuery = _uiState.SearchQuery;
                _uiState.SearchCooldownTimer = Constants.SEARCH_COOLDOWN_DURATION;
            }
        }
        
        public void Dispose()
        {
            foreach (var texture in _colorCache.Values)
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
            _colorCache.Clear();
            _screenshotManager?.Dispose();
        }
    }

    public enum SortBy
    {
        Name = 0,
        NameDescending = 1,
        DateCreated = 2,
        DateCreatedDescending = 3,
        DateModified = 4,
        DateModifiedDescending = 5
    }

    public class UIState
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string PreviousSearchQuery { get; set; } = string.Empty;
        public string SelectedDirectory { get; set; } = "ALL";
        public SortBy SortBy { get; set; } = SortBy.Name;
        public int CurrentPage { get; set; } = 1;
        public float SearchCooldownTimer { get; set; }
    }

    public class SaveWindowState
    {
        public string FileName { get; set; } = string.Empty;
        public int ScreenshotSize { get; set; } = 256;
        public Texture2D Screenshot { get; set; }
        public OCIChar Character { get; set; }

        public void Reset()
        {
            FileName = string.Empty;
            ScreenshotSize = 256;
            if (Screenshot != null)
            {
                UnityEngine.Object.Destroy(Screenshot);
                Screenshot = null;
            }
        }
    }
}