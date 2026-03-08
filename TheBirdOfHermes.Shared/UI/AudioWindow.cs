using System;
using System.Collections.Generic;
using KKAPI.Utilities;
using TheBirdOfHermes.Audio;
using UnityEngine;

namespace TheBirdOfHermes.UI
{
    public class AudioWindow
    {
        private static readonly int WindowId = "TBOHAudioWindow".GetHashCode();

        private readonly TrackManager _manager;
        private readonly TrackPropertiesPopup _propsPopup = new TrackPropertiesPopup();

        public bool IsOpen { get; set; }
        public Action<float> OnSeek { get; set; }

        private Rect _windowRect = new Rect(100, 100, 900, 400);
        private Vector2 _trackScroll;

        private float _viewStartTime;
        private float _viewDuration = 30f;
        private float _zoom = 1f;

        private enum DragMode
        {
            None,
            MoveTrack,
            TrimLeft,
            TrimRight,
            ReorderTrack,
            Pan
        }

        private DragMode _dragMode;
        private AudioTrack _dragTrack;
        private float _dragStartValue;
        private float _dragMouseStartX;
        private int _dragReorderFromIndex;
        private int _dragReorderCurrentIndex;
        private float _dragStartOffset;

        private List<float> _activeSnapLines;
        private float? _snappedValue;

        private bool _isResizing;
        private Vector2 _resizeStart;
        private Rect _resizeStartRect;

        public AudioWindow(TrackManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Renders the audio window and its associated content if the window is open.
        /// </summary>
        public void Draw()
        {
            if (!IsOpen) return;

            _windowRect = GUI.Window(WindowId, _windowRect, DrawWindowContent, "Audio Tracks", WindowStyles.WindowStyle);
            _propsPopup.Draw();

            if (_windowRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

        /// <summary>
        /// Draws the content of the window with the specified window ID.
        /// </summary>
        /// <param name="id">The unique identifier of the window being drawn.</param>
        private void DrawWindowContent(int id)
        {
            var totalRect = new Rect(0, 0, _windowRect.width, _windowRect.height);

            GUI.DrawTexture(totalRect, WindowStyles.GetTexture(WindowStyles.WindowBg));

            DrawToolbar(new Rect(0, 18, totalRect.width, WindowStyles.ToolbarHeight));

            float rulerY = 18 + WindowStyles.ToolbarHeight;
            DrawRuler(new Rect(WindowStyles.HeaderWidth, rulerY, totalRect.width - WindowStyles.HeaderWidth, WindowStyles.RulerHeight));

            float lanesY = rulerY + WindowStyles.RulerHeight;
            float lanesHeight = totalRect.height - WindowStyles.ToolbarHeight - WindowStyles.RulerHeight - 4;
            DrawTrackLanes(new Rect(0, lanesY, totalRect.width, lanesHeight));


            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 18));
        }

        /// <summary>
        /// Draws the toolbar within the specified rect.
        /// </summary>
        /// <param name="rect">The rect where the toolbar will be drawn.</param>
        private void DrawToolbar(Rect rect)
        {
            GUI.DrawTexture(rect, WindowStyles.GetTexture(WindowStyles.ToolbarBg));

            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Load Audio", GUILayout.Width(80)))
                LoadAudioFile();

            GUILayout.Space(10);
            GUILayout.Label("Master:", GUILayout.Width(52));
            _manager.MasterVolume = GUILayout.HorizontalSlider(_manager.MasterVolume, 0f, 1f, GUILayout.Width(80));
            GUILayout.Label($"{_manager.MasterVolume:P0}", GUILayout.Width(35));

            GUILayout.Space(10);
            GUILayout.Label($"Zoom: {_zoom:F1}x", GUILayout.Width(85));
            if (GUILayout.Button("-",  GUILayout.Width(20)))
                SetZoom(_zoom / 1.5f);
            if (GUILayout.Button("+", GUILayout.Width(20)))
                SetZoom(_zoom * 1.5f);
            if (GUILayout.Button("Fit", GUILayout.Width(30)))
                FitToContent();

            /*GUILayout.Space(10);
            GUI.color = _manager.SnapEnabled ? Color.green : Color.gray;
            if (GUILayout.Button("Snap", GUILayout.Width(46)))
                _manager.SnapEnabled = !_manager.SnapEnabled;
            GUI.color = Color.white;*/

            GUILayout.FlexibleSpace();

            if (_manager.TrackCount > 0 && GUILayout.Button("Clear All", GUILayout.Width(75)))
                _manager.ClearAll();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Draws the ruler inside the specified rect.
        /// </summary>
        /// <param name="rect">The rect where the ruler will be drawn.</param>
        private void DrawRuler(Rect rect)
        {
            GUI.DrawTexture(rect, WindowStyles.GetTexture(WindowStyles.RulerBg));

            float duration = GetViewDuration();
            float[] intervals = { 0.00001f, 0.0001f, 0.001f, 0.01f, 0.05f, 0.1f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 30f, 60f, 120f, 240f, 480f, 1000f, 10000f, 100000f };
            float interval = intervals[intervals.Length - 1];
            foreach (float iv in intervals)
            {
                if (duration / iv <= 20)
                {
                    interval = iv;
                    break;
                }
            }

            float firstLine = Mathf.Ceil(_viewStartTime / interval) * interval;
            for (float t = firstLine; t <= _viewStartTime + duration; t += interval)
            {
                float normX = (t - _viewStartTime) / duration;
                if (normX < 0 || normX > 1) continue;

                float x = rect.x + normX * rect.width;
                GUI.DrawTexture(new Rect(x, rect.y, 1, rect.height), WindowStyles.GetTexture(WindowStyles.LaneSeparator));
                GUI.Label(new Rect(x + 2, rect.y, 60, rect.height), WindowStyles.FormatTime(t), WindowStyles.RulerLabel);
            }
        }

        /// <summary>
        /// Renders the track lanes within the audio window, applying appropriate styling, layout, and user interaction handling.
        /// </summary>
        /// <param name="rect">The rectangular area within the audio window where the track lanes will be drawn.</param>
        private void DrawTrackLanes(Rect rect)
        {
            try
            {
                var tracks = _manager.Tracks;
                float totalHeight = tracks.Count * WindowStyles.LaneHeight;
                bool needsScroll = totalHeight > rect.height;

                if (needsScroll)
                {
                    _trackScroll = GUI.BeginScrollView(rect, _trackScroll,
                        new Rect(0, 0, rect.width - 16, totalHeight));
                }

                for (int i = 0; i < tracks.Count; i++)
                {
                    float y = needsScroll ? i * WindowStyles.LaneHeight : rect.y + i * WindowStyles.LaneHeight;
                    float laneWidth = needsScroll ? rect.width - 16 : rect.width;
                    var laneRect = new Rect(needsScroll ? 0 : rect.x, y, laneWidth, WindowStyles.LaneHeight);

                    DrawTrackLane(laneRect, tracks[i], i);
                }

                if (needsScroll)
                    GUI.EndScrollView();

                DrawLanesPlayhead(rect);

                HandleLaneAreaInput(rect);

                if (_dragMode == DragMode.MoveTrack && _activeSnapLines != null)
                    DrawSnapLines(rect);
            }
            catch (NullReferenceException e)
            {
                Entry.Logger.LogWarning($"Object reference not set to an instance of an object for object: {e.TargetSite} | {e.Message} | {e.StackTrace}");
            }
        }

        /// <summary>
        /// Renders a track lane within the audio window, including background, header, waveform,
        /// and handling user input for the specified audio track.
        /// </summary>
        /// <param name="laneRect">The rectangle defining the dimensions and position of the track lane to draw.</param>
        /// <param name="track">The audio track represented by the track lane.</param>
        /// <param name="index">The index of the track lane being drawn, used for styling alternations and logic.</param>
        private void DrawTrackLane(Rect laneRect, AudioTrack track, int index)
        {
            bool isSelected = track == _manager.SelectedTrack;
            Color bgColor = isSelected ? WindowStyles.LaneBgSelected : (index % 2 == 0 ? WindowStyles.LaneBg : WindowStyles.LaneBgAlt);
            GUI.DrawTexture(laneRect, WindowStyles.GetTexture(bgColor));

            GUI.DrawTexture(new Rect(laneRect.x, laneRect.yMax - 1, laneRect.width, 1),
                WindowStyles.GetTexture(WindowStyles.LaneSeparator));

            var headerRect = new Rect(laneRect.x, laneRect.y, WindowStyles.HeaderWidth, laneRect.height);
            DrawTrackHeader(headerRect, track, index);

            var waveArea = new Rect(laneRect.x + WindowStyles.HeaderWidth, laneRect.y,
                laneRect.width - WindowStyles.HeaderWidth, laneRect.height);
            DrawTrackWaveform(waveArea, track);

            HandleTrackInput(waveArea, track, index);
        }

        /// <summary>
        /// Draws the track header section within the specified rectangular area,
        /// displaying track details, volume controls, and action buttons.
        /// </summary>
        /// <param name="rect">The rectangular area where the track header will be drawn.</param>
        /// <param name="track">The track associated with the header, providing its name, color, and other details.</param>
        /// <param name="index">The index of the track within the list of tracks.</param>
        private void DrawTrackHeader(Rect rect, AudioTrack track, int index)
        {
            Color hdrBg = track == _manager.SelectedTrack ? WindowStyles.HeaderBgSelected : WindowStyles.HeaderBg;
            GUI.DrawTexture(rect, WindowStyles.GetTexture(hdrBg));

            GUI.DrawTexture(new Rect(rect.x, rect.y, 4, rect.height), WindowStyles.GetTexture(track.TrackColor));

            GUI.DrawTexture(new Rect(rect.xMax - 1, rect.y, 1, rect.height), WindowStyles.GetTexture(WindowStyles.LaneSeparator));

            float padding = 8f;
            float y = rect.y + 2;
            float w = rect.width - padding - 4;

            GUI.Label(new Rect(rect.x + padding, y, w, 16), track.Name, WindowStyles.LabelBold);
            y += 16;

            var volRect = new Rect(rect.x + padding, y, w - 35, 14);
            track.Volume = GUI.HorizontalSlider(volRect, track.Volume, 0f, 1f);
            GUI.Label(new Rect(volRect.xMax + 2, y - 6, 33, 20), $"{track.Volume:P0}");
            y += 16;

            float btnX = rect.x + padding;
            float btnW = 26f;

            var prevBgColor = GUI.backgroundColor;
            GUI.backgroundColor = track == _manager.SelectedTrack ? Color.green : Color.gray;
            if (GUI.Button(new Rect(btnX, y, btnW, 16), "S"))
                _manager.SelectTrack(track);
            GUI.backgroundColor = prevBgColor;
            btnX += btnW + 2;

            GUI.backgroundColor = track.IsMuted ? Color.red : Color.gray;
            if (GUI.Button(new Rect(btnX, y, btnW, 16), "M"))
                track.IsMuted = !track.IsMuted;
            GUI.backgroundColor = prevBgColor;
            btnX += btnW + 2;

            if (index > 0 && GUI.Button(new Rect(btnX, y, 18, 16), "\u25B2"))
                _manager.MoveTrack(index, index - 1);
            btnX += 20;

            if (index < _manager.TrackCount - 1 && GUI.Button(new Rect(btnX, y, 18, 16), "\u25BC"))
                _manager.MoveTrack(index, index + 1);
        }

        /// <summary>
        /// Draws the waveform representation of the provided audio track in the specified rectangular area.
        /// </summary>
        /// <param name="areaRect">The rectangular area within which the track waveform is rendered.</param>
        /// <param name="track">The audio track whose waveform is to be drawn.</param>
        private void DrawTrackWaveform(Rect areaRect, AudioTrack track)
        {
            if (!track.HasAudio) return;

            float duration = GetViewDuration();
            float pxPerSecond = areaRect.width / duration;

            float fullStartX = areaRect.x + (track.Offset - track.TrimStart - _viewStartTime) * pxPerSecond;
            float fullWidth = track.FullDuration * pxPerSecond;

            float activeStartX = areaRect.x + (track.TimelineStart - _viewStartTime) * pxPerSecond;
            float activeWidth = track.EffectiveDuration * pxPerSecond;

            if (activeStartX + activeWidth < areaRect.x || activeStartX > areaRect.xMax) return;

            if (track.TrimStart > 0)
            {
                float trimLeftX = fullStartX;
                float trimLeftW = track.TrimStart * pxPerSecond;
                var trimRect = ClipRect(new Rect(trimLeftX, areaRect.y + 2, trimLeftW, areaRect.height - 4), areaRect);
                if (trimRect.width > 0)
                    GUI.DrawTexture(trimRect, WindowStyles.GetTexture(WindowStyles.TrimmedRegion));
            }

            if (track.TrimEnd > 0)
            {
                float trimRightX = activeStartX + activeWidth;
                float trimRightW = track.TrimEnd * pxPerSecond;
                var trimRect = ClipRect(new Rect(trimRightX, areaRect.y + 2, trimRightW, areaRect.height - 4), areaRect);
                if (trimRect.width > 0)
                    GUI.DrawTexture(trimRect, WindowStyles.GetTexture(WindowStyles.TrimmedRegion));
            }

            var blockRect = ClipRect(new Rect(activeStartX, areaRect.y + 2, activeWidth, areaRect.height - 4), areaRect);
            if (blockRect.width <= 0) return;

            GUI.DrawTexture(blockRect, WindowStyles.GetTexture(WindowStyles.WaveformBg));

            int waveWidth = (int)blockRect.width;
            int waveHeight = (int)blockRect.height;
            if (waveWidth > 0 && waveHeight > 0)
            {
                float audioStartTime = track.TrimStart;
                float audioVisibleDuration = track.EffectiveDuration;

                if (activeStartX < areaRect.x)
                {
                    float clippedTime = (areaRect.x - activeStartX) / pxPerSecond;
                    audioStartTime += clippedTime;
                    audioVisibleDuration -= clippedTime;
                }
                if (activeStartX + activeWidth > areaRect.xMax)
                {
                    float clippedTime = (activeStartX + activeWidth - areaRect.xMax) / pxPerSecond;
                    audioVisibleDuration -= clippedTime;
                }

                if (audioVisibleDuration > 0)
                {
                    if (track.Waveform.NeedsRebuild(waveWidth, audioStartTime, audioVisibleDuration))
                        track.Waveform.Rebuild(waveWidth, waveHeight, audioStartTime, audioVisibleDuration, 0f);

                    if (track.Waveform.Texture != null)
                    {
                        var prevColor = GUI.color;
                        GUI.color = track.TrackColor;
                        GUI.DrawTexture(blockRect, track.Waveform.Texture);
                        GUI.color = prevColor;
                    }
                }
            }

            DrawTrimHandle(new Rect(activeStartX - WindowStyles.HandleWidth / 2, areaRect.y, WindowStyles.HandleWidth, areaRect.height), areaRect);
            DrawTrimHandle(new Rect(activeStartX + activeWidth - WindowStyles.HandleWidth / 2, areaRect.y, WindowStyles.HandleWidth, areaRect.height), areaRect);
        }

        /// <summary>
        /// Draws the trim handle at the specified position, applying clip bounds and visual styles.
        /// Handles hover and drag states for visual feedback.
        /// </summary>
        /// <param name="handleRect">The rectangular area representing the position and size of the trim handle.</param>
        /// <param name="clipTo">The bounds to clip the trim handle rendering within.</param>
        private void DrawTrimHandle(Rect handleRect, Rect clipTo)
        {
            handleRect = ClipRect(handleRect, clipTo);
            if (handleRect.width <= 0) return;

            bool hover = handleRect.Contains(Event.current.mousePosition);
            Color handleColor = _dragMode == DragMode.TrimLeft || _dragMode == DragMode.TrimRight
                ? WindowStyles.HandleDrag
                : hover
                    ? WindowStyles.HandleHover
                    : WindowStyles.HandleNormal;

            GUI.DrawTexture(handleRect, WindowStyles.GetTexture(handleColor));
        }

        /// <summary>
        /// Draws the playhead on the track lanes, indicating the current playback position within the visible portion of the audio timeline.
        /// </summary>
        /// <param name="lanesRect">The rectangular area representing the track lanes in which the playhead will be drawn.</param>
        private void DrawLanesPlayhead(Rect lanesRect)
        {
            float duration = GetViewDuration();
            float playbackTime = GetPlaybackTime();
            float normX = (playbackTime - _viewStartTime) / duration;
            if (normX < 0 || normX > 1) return;

            float x = lanesRect.x + WindowStyles.HeaderWidth + normX * (lanesRect.width - WindowStyles.HeaderWidth);
            GUI.DrawTexture(new Rect(x - 1, lanesRect.y, 2, lanesRect.height), WindowStyles.GetTexture(WindowStyles.Playhead));
        }

        /// <summary>
        /// Draws snap lines on the audio lanes to indicate points of alignment, such as snapping to time markers or other tracks.
        /// </summary>
        /// <param name="lanesRect">The rectangular region of the lanes where the snap lines should be drawn.</param>
        private void DrawSnapLines(Rect lanesRect)
        {
            if (_activeSnapLines == null) return;

            float duration = GetViewDuration();
            float waveAreaX = lanesRect.x + WindowStyles.HeaderWidth;
            float waveAreaW = lanesRect.width - WindowStyles.HeaderWidth;

            foreach (float t in _activeSnapLines)
            {
                float normX = (t - _viewStartTime) / duration;
                if (normX < 0 || normX > 1) continue;

                float x = waveAreaX + normX * waveAreaW;
                GUI.DrawTexture(new Rect(x - 1, lanesRect.y, 2, lanesRect.height),
                    WindowStyles.GetTexture(WindowStyles.SnapLine));
            }
        }

        /// <summary>
        /// Processes user input for manipulating an audio track in the specified area.
        /// </summary>
        /// <param name="waveArea">The rectangular area on the screen representing the waveform display for the track.</param>
        /// <param name="track">The audio track being manipulated.</param>
        /// <param name="index">The index of the track within the track list.</param>
        private void HandleTrackInput(Rect waveArea, AudioTrack track, int index)
        {
            Event e = Event.current;
            if (!waveArea.Contains(e.mousePosition) && _dragMode == DragMode.None) return;

            float duration = GetViewDuration();
            float pxPerSecond = waveArea.width / duration;

            float activeStartX = waveArea.x + (track.TimelineStart - _viewStartTime) * pxPerSecond;
            float activeWidth = track.EffectiveDuration * pxPerSecond;

            var blockRect = new Rect(activeStartX, waveArea.y, activeWidth, waveArea.height);
            var leftHandle = new Rect(activeStartX - WindowStyles.HandleWidth, waveArea.y, WindowStyles.HandleWidth * 2, waveArea.height);
            var rightHandle = new Rect(activeStartX + activeWidth - WindowStyles.HandleWidth, waveArea.y, WindowStyles.HandleWidth * 2, waveArea.height);

            if (e.type == EventType.MouseDown && e.button == 1 && blockRect.Contains(e.mousePosition))
            {
                _propsPopup.Open(track, e.mousePosition, t => _manager.RemoveTrack(t));
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && _dragMode == DragMode.None)
            {
                if (leftHandle.Contains(e.mousePosition))
                {
                    _dragMode = DragMode.TrimLeft;
                    _dragTrack = track;
                    _dragStartValue = track.TrimStart;
                    _dragMouseStartX = e.mousePosition.x;
                    e.Use();
                }
                else if (rightHandle.Contains(e.mousePosition))
                {
                    _dragMode = DragMode.TrimRight;
                    _dragTrack = track;
                    _dragStartValue = track.TrimEnd;
                    _dragMouseStartX = e.mousePosition.x;
                    e.Use();
                }
                else if (blockRect.Contains(e.mousePosition))
                {
                    _dragMode = DragMode.MoveTrack;
                    _dragTrack = track;
                    _dragStartOffset = track.Offset;
                    _dragMouseStartX = e.mousePosition.x;
                    _activeSnapLines = _manager.GetSnapLines(track);
                    _snappedValue = null;
                    e.Use();
                }
            }

        }

        /// <summary>
        /// Handles user input within the lane area, including dragging, scrolling, zooming,
        /// and seeking actions related to audio editing timelines.
        /// </summary>
        /// <param name="rect">The rectangular area representing the bounds of the lane region.</param>
        private void HandleLaneAreaInput(Rect rect)
        {
            Event e = Event.current;

            float waveAreaX = rect.x + WindowStyles.HeaderWidth;
            float waveAreaW = rect.width - WindowStyles.HeaderWidth;
            float duration = GetViewDuration();
            float pxPerSecond = waveAreaW / duration;

            if (e.type == EventType.MouseDrag && _dragMode != DragMode.None && _dragTrack != null)
            {
                float deltaX = e.mousePosition.x - _dragMouseStartX;
                float deltaTime = deltaX / pxPerSecond;

                switch (_dragMode)
                {
                    case DragMode.MoveTrack:
                        float proposed = _dragStartOffset + deltaTime;
                        proposed = Mathf.Max(0f, proposed);
                        float snapped = _manager.TrySnap(_dragTrack, proposed);
                        _dragTrack.Offset = snapped;
                        _snappedValue = Mathf.Abs(snapped - proposed) > 0.001f ? snapped : (float?)null;
                        break;

                    case DragMode.TrimLeft:
                        _dragTrack.TrimStart = _dragStartValue + deltaTime;
                        _dragTrack.ClampTrim();
                        break;

                    case DragMode.TrimRight:
                        float newTrimEnd = _dragStartValue - deltaTime;
                        _dragTrack.TrimEnd = newTrimEnd;
                        _dragTrack.ClampTrim();
                        break;
                }

                e.Use();
            }

            if (e.type == EventType.MouseUp && _dragMode != DragMode.None)
            {
                _dragMode = DragMode.None;
                _dragTrack = null;
                _activeSnapLines = null;
                _snappedValue = null;
                e.Use();
            }

            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                float deltaTime = -e.delta.x / pxPerSecond;
                _viewStartTime += deltaTime;
                _viewStartTime = Mathf.Max(0f, _viewStartTime);
                e.Use();
            }

            var waveRect = new Rect(waveAreaX, rect.y, waveAreaW, rect.height);
            if (e.type == EventType.ScrollWheel && waveRect.Contains(e.mousePosition))
            {
                float mouseNormX = (e.mousePosition.x - waveAreaX) / waveAreaW;
                float timeAtMouse = _viewStartTime + mouseNormX * duration;

                float zoomDelta = -e.delta.y * 0.1f;
                float newZoom = Mathf.Clamp(_zoom * (1 + zoomDelta), 0.00001f, 500f);

                float newDuration = _viewDuration / newZoom;
                _viewStartTime = timeAtMouse - mouseNormX * newDuration;
                _viewStartTime = Mathf.Max(0f, _viewStartTime);
                _zoom = newZoom;

                e.Use();
            }

            if (e.type == EventType.MouseDown && e.button == 0 && _dragMode == DragMode.None && waveRect.Contains(e.mousePosition))
            {
                float normX = (e.mousePosition.x - waveAreaX) / waveAreaW;
                float time = _viewStartTime + normX * duration;
                OnSeek?.Invoke(Mathf.Max(0f, time));
                e.Use();
            }
        }

        /// <summary>
        /// Prompts the user to select an audio file using a file dialog and adds the selected file as a new track in the track manager.
        /// Logs information about the added track or an error if the file loading fails.
        /// </summary>
        private void LoadAudioFile()
        {
            string filter = AudioLoader.GetFileFilter();
            var selection = OpenFileDialog.ShowDialog("Select audio file", "", filter, "", OpenFileDialog.OpenSaveFileDialgueFlags.OFN_FILEMUSTEXIST);
            if (selection.Length > 0 && !string.IsNullOrEmpty(selection[0]))
            {
                try
                {
                    _manager.AddTrackFromFile(selection[0]);
                    Entry.Logger.LogInfo($"Added track: {selection[0]}");
                }
                catch (Exception ex)
                {
                    Entry.Logger.LogError($"Failed to load audio: {ex}");
                }
            }
        }

        /// <summary>
        /// Adjusts the zoom level of the audio window, modifying the visible duration while maintaining the view's center point.
        /// </summary>
        /// <param name="newZoom">The new zoom level to apply, clamped between a minimum and maximum value.</param>
        private void SetZoom(float newZoom)
        {
            float center = _viewStartTime + GetViewDuration() / 2f;
            _zoom = Mathf.Clamp(newZoom, 0.00001f, 500f);
            float newDuration = GetViewDuration();
            _viewStartTime = Mathf.Max(0f, center - newDuration / 2f);
        }

        /// <summary>
        /// Adjusts the audio window's view to ensure all loaded tracks fit within the visible timeline range.
        /// Sets the view's starting time and adjusts the zoom level based on the maximum timeline endpoint of all tracks.
        /// </summary>
        private void FitToContent()
        {
            if (_manager.TrackCount == 0)
            {
                _viewStartTime = 0f;
                _zoom = 1f;
                return;
            }

            float maxEnd = 0f;
            foreach (var track in _manager.Tracks)
            {
                if (track.TimelineEnd > maxEnd)
                    maxEnd = track.TimelineEnd;
            }

            maxEnd = Mathf.Max(maxEnd, 1f);
            _viewStartTime = 0f;
            _zoom = _viewDuration / (maxEnd * 1.1f);
            _zoom = Mathf.Clamp(_zoom, 0.00001f, 500f);
        }

        /// <summary>
        /// Calculates and retrieves the visible duration of the audio timeline based on the current zoom level.
        /// </summary>
        /// <returns>
        /// The duration of the visible portion of the audio timeline in seconds.
        /// </returns>
        private float GetViewDuration()
        {
            return _viewDuration / _zoom;
        }

        private Func<float> _getPlaybackTime;
        /// <summary>
        /// Sets the function used to retrieve the current playback time.
        /// </summary>
        /// <param name="getter">A function that returns the current playback time as a float.</param>
        public void SetPlaybackTimeGetter(Func<float> getter) => _getPlaybackTime = getter;

        /// <summary>
        /// Retrieves the current playback time of the audio timeline.
        /// </summary>
        /// <returns>
        /// The current playback time as a float. If no playback time function is set, returns 0.
        /// </returns>
        private float GetPlaybackTime()
        {
            return _getPlaybackTime?.Invoke() ?? 0f;
        }

        /// <summary>
        /// Clips a rectangle to ensure it fits within the specified bounds.
        /// </summary>
        /// <param name="r">The rectangle to be clipped.</param>
        /// <param name="bounds">The boundary rectangle within which <paramref name="r"/> will be clipped.</param>
        /// <returns>A new rectangle representing the clipped area. If the rectangles do not overlap, the width and height will be zero.</returns>
        private static Rect ClipRect(Rect r, Rect bounds)
        {
            float x1 = Mathf.Max(r.x, bounds.x);
            float y1 = Mathf.Max(r.y, bounds.y);
            float x2 = Mathf.Min(r.xMax, bounds.xMax);
            float y2 = Mathf.Min(r.yMax, bounds.yMax);
            if (x2 <= x1 || y2 <= y1) 
                return new Rect(x1, y1, 0, 0);
            return new Rect(x1, y1, x2 - x1, y2 - y1);
        }
    }
}