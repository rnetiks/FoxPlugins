using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using SmartRectV0;
using Studio;
using TexFac.Universal;
using UnityEngine;

namespace PoseLib.KKS
{
    public class UIManager : IDisposable
    {
        private readonly PoseLibraryManager _poseManager;
        private readonly ManualLogSource _logger;
        private readonly TextureManager _textureManager;

        private bool _isUIOpen;
        private bool _isSaveWindowOpen;
        private Rect _windowRect;
        private GUIStyle _windowStyle;
        private GUIStyle _fontStyle;

        private readonly UIState _uiState;
        private readonly SaveWindowState _saveState;

        public UIManager(PoseLibraryManager poseManager, ManualLogSource logger)
        {
            _poseManager = poseManager;
            _logger = logger;
            _textureManager = new TextureManager();
            _uiState = new UIState();
            _saveState = new SaveWindowState();

            InitializeWindow();
            CreateGUIStyles();
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

            if (_isSaveWindowOpen)
            {
                RenderSaveWindow();
                return;
            }

            RenderMainWindow();
        }

        private void InitializeWindow()
        {
            var width = Screen.width / 2f;
            var height = Screen.height / 1.3f;
            _windowRect = new Rect(100, 100, width, height);
        }

        private void CreateGUIStyles()
        {
            var backgroundTexture = _textureManager.GetBackgroundTexture(_windowRect.width, _windowRect.height);

            _windowStyle = new GUIStyle
            {
                normal = { background = backgroundTexture },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };

            _fontStyle = new GUIStyle
            {
                normal = { textColor = Color.black },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void RenderMainWindow()
        {
            if (_windowRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();

            _windowRect = GUI.Window(Constants.WINDOW_ID, _windowRect, DrawMainWindow, "PoseLibrary", _windowStyle);
        }

        private void DrawMainWindow(int id)
        {
            var selectedCharacters = KKAPI.Studio.StudioAPI.GetSelectedCharacters().ToArray();
            var rect = new SmartRect(Constants.UI_OFFSET_X, Constants.UI_OFFSET_Y, 100, 20, 5, 5);

            DrawTopControls(rect, selectedCharacters);
            DrawSearchAndColumnControls(rect);
            DrawPoseGrid(selectedCharacters);
            DrawPagination();
            GUI.DragWindow();
        }

        private void DrawTopControls(SmartRect rect, OCIChar[] selectedCharacters)
        {
            if (selectedCharacters.Length > 0)
            {
                if (GUI.Button(rect, "Save Pose"))
                    OpenSaveWindow(selectedCharacters[0]);
                rect.NextColumn();
            }
            else
            {
                GUI.Label(new Rect(0, 0, _windowRect.width, _windowRect.height),
                    "No character selected", _fontStyle);
            }
        }

        private void DrawSearchAndColumnControls(SmartRect rect)
        {
            _uiState.SearchQuery = GUI.TextField(rect.SetWidth(200), _uiState.SearchQuery);

            if (GUI.Button(rect.MoveX(205).SetWidth(20), "+"))
                _uiState.PreviewColumns = Mathf.Min(_uiState.PreviewColumns + 1, Constants.MAX_PREVIEW_COLUMNS);

            if (GUI.Button(rect.MoveX(25).SetWidth(20), "-"))
                _uiState.PreviewColumns = Mathf.Max(_uiState.PreviewColumns - 1, Constants.MIN_PREVIEW_COLUMNS);

            rect.NextRow();
        }

        private void DrawPoseGrid(OCIChar[] selectedCharacters)
        {
            if (selectedCharacters.Length == 0) return;

            var searchQuery = new SearchQuery
            {
                Text = _uiState.SearchQuery,
                Page = _uiState.CurrentPage,
            };

            var poses = _poseManager.SearchPoses(searchQuery);

            if (poses.Count == 0)
            {
                GUI.Label(new Rect(0, 0, _windowRect.width, _windowRect.height),
                    "No poses found", _fontStyle);
                return;
            }

            DrawPoseIcons(poses, selectedCharacters);
        }

        private void DrawPoseIcons(List<PoseSearchResult> poses, OCIChar[] selectedCharacters)
        {
            var previewSize = (_windowRect.width - (Constants.UI_OFFSET_X * _uiState.PreviewColumns + 5)) / _uiState.PreviewColumns;
            var imageRect = new SmartRect(Constants.UI_OFFSET_X, 80, previewSize, previewSize, Constants.UI_OFFSET_X, 5);

            for (int i = 0; i < poses.Count; i++)
            {
                var pose = poses[i];
                DrawSinglePoseIcon(pose, imageRect, selectedCharacters);

                imageRect.NextColumn();
                if ((i + 1) % _uiState.PreviewColumns == 0)
                    imageRect.NextRow();
            }
        }

        private void DrawSinglePoseIcon(PoseSearchResult pose, SmartRect imageRect, OCIChar[] selectedCharacters)
        {
            GUI.DrawTexture(imageRect, pose.PreviewTexture);

            var isVanillaPose = pose.FilePath.Contains("UserData");
            var fileExtension = Path.GetExtension(pose.FilePath).ToLower();

            var labelRect = new Rect(imageRect.X, imageRect.Y, imageRect.Width, 20);
            var labelText = Path.GetFileNameWithoutExtension(pose.FilePath);
            var labelStyle = new GUIStyle(_fontStyle)
            {
                normal = { textColor = Color.white }
            };
            GUI.DrawTexture(labelRect, GetColorTexture(new Color(0, 0, 0, 0.5f)));
            GUI.Label(labelRect, labelText, labelStyle);

            if (!imageRect.ToRect().Contains(Event.current.mousePosition)) return;

            DrawPoseButtons(pose, imageRect, selectedCharacters);
        }

        Dictionary<Color, Texture2D> _colorCache = new Dictionary<Color, Texture2D>();

        private Texture2D GetColorTexture(Color color)
        {
            if (_colorCache.TryGetValue(color, out var texture))
                return texture;

            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            _colorCache.Add(color, texture);
            return texture;
        }

        private void DrawPoseButtons(PoseSearchResult pose, SmartRect imageRect, OCIChar[] selectedCharacters)
        {
            var buttonWidth = Mathf.Max(imageRect.Width * 0.33333f, 50);
            var buttonHeight = 30;

            var isVanillaPose = pose.FilePath.Contains("UserData");
            if (!isVanillaPose)
            {
                if (GUI.Button(new SmartRect(imageRect).SetWidth(buttonWidth).SetHeight(buttonHeight), "Delete"))
                {
                    _poseManager.DeletePose(pose.FilePath);
                    return;
                }
            }

            var loadButton = new SmartRect(imageRect)
                .SetWidth(buttonWidth)
                .SetHeight(buttonHeight)
                .MoveToEndY(imageRect, buttonHeight);

            if (GUI.Button(loadButton, "Load"))
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
        }

        private void DrawPagination()
        {
            const int buttonWidth = 30;
            var footerWidth = 3 * buttonWidth + 2 * 5;
            var footerX = (_windowRect.width - footerWidth) * 0.5f;
            var footer = new SmartRect(footerX, _windowRect.height - 25, buttonWidth, 20, 5, 5);

            var previousPage = _uiState.CurrentPage;

            if (GUI.Button(footer, "<"))
                _uiState.CurrentPage = Mathf.Max(_uiState.CurrentPage - 1, 1);

            var pageText = GUI.TextField(footer.NextColumn(), _uiState.CurrentPage.ToString());
            if (int.TryParse(pageText, out var newPage))
                _uiState.CurrentPage = Mathf.Max(newPage, 1);

            if (GUI.Button(footer.NextColumn(), ">"))
                _uiState.CurrentPage++;

            if (previousPage != _uiState.CurrentPage)
                _uiState.SearchCooldownTimer = 0;
        }

        private void OpenSaveWindow(OCIChar character)
        {
            _saveState.PoseData = _poseManager.ExtractFkDataFromCharacter(character);
            _saveState.Screenshot = _textureManager.CreateScreenshotTexture();
            _isSaveWindowOpen = true;
        }

        private void RenderSaveWindow()
        {
            var centerX = Screen.width / 2f;
            var centerY = Screen.height / 2f;
            var windowRect = new SmartRect(centerX - 200, centerY - 150, 400, 300);

            GUI.DrawTexture(windowRect, _textureManager.GetBackgroundTexture(400, 300));

            if (DrawCloseButton(windowRect))
            {
                CloseSaveWindow();
                return;
            }

            DrawScreenshotPreview();
            DrawSaveWindowControls(windowRect);
        }

        private bool DrawCloseButton(SmartRect windowRect)
        {
            return GUI.Button(new SmartRect(windowRect).SetWidth(30).SetHeight(30).MoveToEndX(windowRect, 30), "X");
        }

        private void DrawScreenshotPreview()
        {
            if (_saveState.Screenshot != null)
            {
                GUI.DrawTexture(new Rect(Screen.width - 300, Screen.height - 300, 300, 300),
                    _saveState.Screenshot.GetTexture());
            }
        }

        private void DrawSaveWindowControls(SmartRect windowRect)
        {
            var centerX = Screen.width / 2f;
            var centerY = Screen.height / 2f;
            var controlsRect = new SmartRect(centerX - 150, centerY - 70, 300, 20);

            GUI.Label(controlsRect, "Filename", _fontStyle);
            _saveState.FileName = GUI.TextArea(controlsRect.NextRow(), _saveState.FileName);

            GUI.Label(controlsRect.NextRow(), "Tags", _fontStyle);
            _saveState.Tags = GUI.TextArea(controlsRect.NextRow(), _saveState.Tags);

            controlsRect.NextRow().BeginHorizontal(2);

            if (GUI.Button(controlsRect, "Take Screenshot"))
                TakeScreenshot();

            if (GUI.Button(controlsRect.NextColumn(), "Save") && !_saveState.FileName.IsEmptyOrWhitespace())
                SavePose();
        }

        private void TakeScreenshot()
        {
            _isUIOpen = false;
            _saveState.Screenshot?.LoadScreen();
            _saveState.Screenshot = _textureManager.ProcessScreenshot(_saveState.Screenshot);
            _isUIOpen = true;
        }

        private void SavePose()
        {
            try
            {
                _poseManager.SavePose(_saveState.FileName, _saveState.Tags,
                    _saveState.PoseData, _saveState.Screenshot);
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
            _textureManager?.Dispose();
        }
    }

    public class UIState
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string PreviousSearchQuery { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int PreviewColumns { get; set; } = Constants.DEFAULT_PREVIEW_COLUMNS;
        public float SearchCooldownTimer { get; set; }
    }

    public class SaveWindowState
    {
        public string FileName { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public Dictionary<string, ChangeAmount> PoseData { get; set; } = new Dictionary<string, ChangeAmount>();
        public BaseTextureElement Screenshot { get; set; }

        public void Reset()
        {
            FileName = string.Empty;
            Tags = string.Empty;
            PoseData.Clear();
            Screenshot = null;
        }
    }
}