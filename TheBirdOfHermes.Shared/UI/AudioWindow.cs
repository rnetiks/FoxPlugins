using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        public event Action<float> OnSeekEvent;
        
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
            Pan,
            FadeInHandle,
            FadeOutHandle,
            RulerSeek
        }

        private DragMode _dragMode;
        private AudioTrack _dragTrack;
        private float _dragStartValue;
        private float _dragMouseStartX;
        private float _dragStartOffset;
        private AudioLane _dragSourceLane;
        private float _dragMouseStartY;

        private Dictionary<AudioTrack, float> _dragOriginalOffsets;

        private List<float> _activeSnapLines;
        private float? _snappedValue;

        private AudioTrack _hoveredTrack;

        public AudioWindow(TrackManager manager)
        {
            _manager = manager;
        }

        public void Draw()
        {
            if (!IsOpen) return;

            _windowRect = GUI.Window(WindowId, _windowRect, DrawWindowContent, "Audio Tracks", WindowStyles.WindowStyle);
            _propsPopup.Draw();

            if (_windowRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

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
            if (GUILayout.Button("-", GUILayout.Width(20)))
                SetZoom(_zoom / 1.5f);
            if (GUILayout.Button("+", GUILayout.Width(20)))
                SetZoom(_zoom * 1.5f);
            if (GUILayout.Button("Fit", GUILayout.Width(30)))
                FitToContent();

            GUILayout.FlexibleSpace();

            if (_manager.TrackCount > 0 && GUILayout.Button("Clear All", GUILayout.Width(75)))
                _manager.ClearAll();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        #region Ruler

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

            float playbackTime = GetPlaybackTime();
            float playNormX = (playbackTime - _viewStartTime) / duration;
            if (playNormX >= 0 && playNormX <= 1)
            {
                float px = rect.x + playNormX * rect.width;
                GUI.DrawTexture(new Rect(px - 1, rect.y, 2, rect.height), WindowStyles.GetTexture(WindowStyles.Playhead));
            }

            HandleRulerInput(rect);
        }

        private void HandleRulerInput(Rect rect)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition) && _dragMode == DragMode.None)
            {
                _dragMode = DragMode.RulerSeek;
                SeekFromRuler(rect, e.mousePosition.x);
                e.Use();
            }

            if (e.type == EventType.MouseDrag && _dragMode == DragMode.RulerSeek)
            {
                SeekFromRuler(rect, e.mousePosition.x);
                e.Use();
            }

            if (e.type == EventType.MouseUp && _dragMode == DragMode.RulerSeek)
            {
                _dragMode = DragMode.None;
                e.Use();
            }
        }

        private void SeekFromRuler(Rect rect, float mouseX)
        {
            float normX = Mathf.Clamp01((mouseX - rect.x) / rect.width);
            float time = _viewStartTime + normX * GetViewDuration();
            OnSeekEvent?.Invoke(Mathf.Max(0f, time));
        }

        #endregion

        #region Track Lanes

        private void DrawTrackLanes(Rect rect)
        {
            try
            {
                var lanes = _manager.Lanes;
                float totalHeight = lanes.Count * WindowStyles.LaneHeight;
                bool needsScroll = totalHeight > rect.height;

                if (needsScroll)
                {
                    _trackScroll = GUI.BeginScrollView(rect, _trackScroll,
                        new Rect(0, 0, rect.width - 16, totalHeight));
                }

                for (int i = 0; i < lanes.Count; i++)
                {
                    float y = needsScroll ? i * WindowStyles.LaneHeight : rect.y + i * WindowStyles.LaneHeight;
                    float laneWidth = needsScroll ? rect.width - 16 : rect.width;
                    var laneRect = new Rect(needsScroll ? 0 : rect.x, y, laneWidth, WindowStyles.LaneHeight);

                    DrawLane(laneRect, lanes[i], i);
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
                Entry.Logger.LogWarning($"NullRef in DrawTrackLanes: {e.TargetSite} | {e.Message}");
            }
        }

        private void DrawLane(Rect laneRect, AudioLane lane, int laneIndex)
        {
            bool hasSelectedTrack = lane.Tracks.Any(t => t.IsSelected);
            Color bgColor = hasSelectedTrack ? WindowStyles.LaneBgSelected : (laneIndex % 2 == 0 ? WindowStyles.LaneBg : WindowStyles.LaneBgAlt);
            GUI.DrawTexture(laneRect, WindowStyles.GetTexture(bgColor));

            GUI.DrawTexture(new Rect(laneRect.x, laneRect.yMax - 1, laneRect.width, 1),
                WindowStyles.GetTexture(WindowStyles.LaneSeparator));

            var headerRect = new Rect(laneRect.x, laneRect.y, WindowStyles.HeaderWidth, laneRect.height);
            DrawLaneHeader(headerRect, lane, laneIndex);

            var waveArea = new Rect(laneRect.x + WindowStyles.HeaderWidth, laneRect.y,
                laneRect.width - WindowStyles.HeaderWidth, laneRect.height);

            _hoveredTrack = null;
            foreach (var track in lane.Tracks)
            {
                HandleTrackInput(waveArea, track, lane, laneIndex);
                DrawTrackWaveform(waveArea, track);
            }
        }

        private void DrawLaneHeader(Rect rect, AudioLane lane, int laneIndex)
        {
            bool hasSelected = lane.Tracks.Any(t => t.IsSelected);
            Color hdrBg = hasSelected ? WindowStyles.HeaderBgSelected : WindowStyles.HeaderBg;
            GUI.DrawTexture(rect, WindowStyles.GetTexture(hdrBg));

            GUI.DrawTexture(new Rect(rect.xMax - 1, rect.y, 1, rect.height), WindowStyles.GetTexture(WindowStyles.LaneSeparator));

            float padding = 8f;
            float y = rect.y + 2;
            float w = rect.width - padding - 4;

            GUI.Label(new Rect(rect.x + padding, y, w, 16), $"Lane {laneIndex + 1}", WindowStyles.LabelBold);
            y += 16;

            var volRect = new Rect(rect.x + padding, y, w - 35, 14);
            lane.Volume = GUI.HorizontalSlider(volRect, lane.Volume, 0f, 1f);
            GUI.Label(new Rect(volRect.xMax + 2, y - 6, 33, 20), $"{lane.Volume:P0}");
            y += 16;

            float btnX = rect.x + padding;

            var prevBgColor = GUI.backgroundColor;
            GUI.backgroundColor = lane.IsMuted ? Color.red : Color.gray;
            if (GUI.Button(new Rect(btnX, y, 26, 16), "M"))
                lane.IsMuted = !lane.IsMuted;
            GUI.backgroundColor = prevBgColor;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.mousePosition.y > y + 18)
                {
                    _propsPopup.Open(lane, null, Event.current.mousePosition, t => _manager.RemoveTrack(t));
                    Event.current.Use();
                }
            }
        }

        #endregion

        #region Track Waveform Drawing

        private void DrawTrackWaveform(Rect areaRect, AudioTrack track)
        {
            if (!track.HasAudio) return;

            float duration = GetViewDuration();
            float pxPerSecond = areaRect.width / duration;

            float visualStartX = areaRect.x + (track.VisualStart - _viewStartTime) * pxPerSecond;
            float visualWidth = track.FullDuration * pxPerSecond;

            float audibleStartX = areaRect.x + (track.AudibleStart - _viewStartTime) * pxPerSecond;
            float audibleWidth = track.EffectiveDuration * pxPerSecond;

            if (visualStartX + visualWidth < areaRect.x || visualStartX > areaRect.xMax) return;
            
            var blockRect = ClipRect(new Rect(audibleStartX, areaRect.y + 2, audibleWidth, areaRect.height - 4), areaRect);
            if (blockRect.width > 0)
            {
                GUI.DrawTexture(blockRect, WindowStyles.GetTexture(WindowStyles.WaveformBg));

                int waveWidth = (int)blockRect.width;
                int waveHeight = (int)blockRect.height;
                if (waveWidth > 0 && waveHeight > 0)
                {
                    float audioStartTime = track.TrimStart;
                    float audioVisibleDuration = track.EffectiveDuration;

                    if (audibleStartX < areaRect.x)
                    {
                        float clippedTime = (areaRect.x - audibleStartX) / pxPerSecond;
                        audioStartTime += clippedTime;
                        audioVisibleDuration -= clippedTime;
                    }
                    if (audibleStartX + audibleWidth > areaRect.xMax)
                    {
                        float clippedTime = (audibleStartX + audibleWidth - areaRect.xMax) / pxPerSecond;
                        audioVisibleDuration -= clippedTime;
                    }

                    if (audioVisibleDuration > 0)
                    {
                        float laneVolume = track.Lane?.Volume ?? 1f;
                        float volScale = track.NormalizationMode == WaveformMode.Volume ? laneVolume : 1f;

                        if (track.Waveform.NeedsRebuild(waveWidth, audioStartTime, audioVisibleDuration,
                                track.NormalizationMode, track.FadeInDuration, track.FadeOutDuration, volScale))
                            track.Waveform.Rebuild(waveWidth, waveHeight, audioStartTime, audioVisibleDuration, 0f,
                                track.NormalizationMode, track.FadeInDuration, track.FadeOutDuration, volScale);

                        if (track.Waveform.Texture != null)
                        {
                            var prevColor = GUI.color;
                            GUI.color = track.TrackColor;
                            GUI.DrawTexture(blockRect, track.Waveform.Texture);
                            GUI.color = prevColor;
                        }
                    }
                }
            }

            if (track.IsSelected)
            {
                DrawBorder(ClipRect(new Rect(audibleStartX, areaRect.y + 2, audibleWidth, areaRect.height - 4), areaRect), WindowStyles.SelectionBorder, 2);
            }
            else if (track == _hoveredTrack)
            {
                DrawBorder(ClipRect(new Rect(audibleStartX, areaRect.y + 2, audibleWidth, areaRect.height - 4), areaRect), WindowStyles.HoverBorder, 1);
            }

            if (track.IsSelected || track == _hoveredTrack)
            {
                var nameRect = ClipRect(new Rect(audibleStartX + 2, areaRect.y + 3, audibleWidth - 4, 14), areaRect);
                if (nameRect.width > 20)
                {
                    GUI.DrawTexture(nameRect, WindowStyles.GetTexture(WindowStyles.TrackNameBg));
                    GUI.Label(nameRect, track.Name, WindowStyles.TrackNameLabel);
                }
            }


            if (track.IsSelected)
            {
                DrawTrimHandle(new Rect(audibleStartX - WindowStyles.HandleWidth / 2, areaRect.y, WindowStyles.HandleWidth, areaRect.height), areaRect);
                DrawTrimHandle(new Rect(audibleStartX + audibleWidth - WindowStyles.HandleWidth / 2, areaRect.y, WindowStyles.HandleWidth, areaRect.height), areaRect);
                DrawFadeHandle(audibleStartX + track.FadeInDuration * pxPerSecond, areaRect.y + 2, areaRect);
                DrawFadeHandle(audibleStartX + audibleWidth - track.FadeOutDuration * pxPerSecond, areaRect.y + 2, areaRect);
            }
        }


        private void DrawFadeHandle(float x, float y, Rect clipBounds)
        {
            if (x < clipBounds.x || x > clipBounds.xMax) return;
            float size = 8f;
            var handleRect = new Rect(x - size / 2, y, size, size);
            GUI.DrawTexture(handleRect, WindowStyles.GetTexture(WindowStyles.FadeHandle));
        }

        private void DrawBorder(Rect rect, Color color, int thickness)
        {
            if (rect.width <= 0 || rect.height <= 0) return;
            var tex = WindowStyles.GetTexture(color);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), tex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), tex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), tex);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), tex);
        }

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

        private void DrawLanesPlayhead(Rect lanesRect)
        {
            float duration = GetViewDuration();
            float playbackTime = GetPlaybackTime();
            float normX = (playbackTime - _viewStartTime) / duration;
            if (normX < 0 || normX > 1) return;

            float x = lanesRect.x + WindowStyles.HeaderWidth + normX * (lanesRect.width - WindowStyles.HeaderWidth);
            GUI.DrawTexture(new Rect(x - 1, lanesRect.y, 2, lanesRect.height), WindowStyles.GetTexture(WindowStyles.Playhead));
        }

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

        #endregion

        #region Input Handling

        private void HandleTrackInput(Rect waveArea, AudioTrack track, AudioLane lane, int laneIndex)
        {
            Event e = Event.current;
            if (!waveArea.Contains(e.mousePosition) && _dragMode == DragMode.None) return;

            float duration = GetViewDuration();
            float pxPerSecond = waveArea.width / duration;

            float audibleStartX = waveArea.x + (track.AudibleStart - _viewStartTime) * pxPerSecond;
            float audibleWidth = track.EffectiveDuration * pxPerSecond;

            var blockRect = new Rect(audibleStartX, waveArea.y, audibleWidth, waveArea.height);
            var leftHandle = new Rect(audibleStartX - WindowStyles.HandleWidth, waveArea.y, WindowStyles.HandleWidth * 2, waveArea.height);
            var rightHandle = new Rect(audibleStartX + audibleWidth - WindowStyles.HandleWidth, waveArea.y, WindowStyles.HandleWidth * 2, waveArea.height);

            float fadeInX = audibleStartX + track.FadeInDuration * pxPerSecond;
            float fadeOutX = audibleStartX + audibleWidth - track.FadeOutDuration * pxPerSecond;
            var fadeInHandle = new Rect(fadeInX - 6, waveArea.y, 12, 12);
            var fadeOutHandle = new Rect(fadeOutX - 6, waveArea.y, 12, 12);

            if (blockRect.Contains(e.mousePosition) && _dragMode == DragMode.None)
                _hoveredTrack = track;

            if (e.type == EventType.MouseDown && e.button == 1 && blockRect.Contains(e.mousePosition))
            {
                _manager.SelectTrack(track, false);
                _propsPopup.Open(lane, track, e.mousePosition, t => _manager.RemoveTrack(t));
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && _dragMode == DragMode.None)
            {
                if (track.IsSelected && fadeInHandle.Contains(e.mousePosition))
                {
                    _dragMode = DragMode.FadeInHandle;
                    _dragTrack = track;
                    _dragStartValue = track.FadeInDuration;
                    _dragMouseStartX = e.mousePosition.x;
                    e.Use();
                    return;
                }

                if (track.IsSelected && fadeOutHandle.Contains(e.mousePosition))
                {
                    _dragMode = DragMode.FadeOutHandle;
                    _dragTrack = track;
                    _dragStartValue = track.FadeOutDuration;
                    _dragMouseStartX = e.mousePosition.x;
                    e.Use();
                    return;
                }

                if (leftHandle.Contains(e.mousePosition) && track.IsSelected)
                {
                    _dragMode = DragMode.TrimLeft;
                    _dragTrack = track;
                    _dragStartValue = track.TrimStart;
                    _dragMouseStartX = e.mousePosition.x;
                    e.Use();
                    return;
                }

                if (rightHandle.Contains(e.mousePosition) && track.IsSelected)
                {
                    _dragMode = DragMode.TrimRight;
                    _dragTrack = track;
                    _dragStartValue = track.TrimEnd;
                    _dragMouseStartX = e.mousePosition.x;
                    e.Use();
                    return;
                }

                if (blockRect.Contains(e.mousePosition))
                {
                    if (!track.IsSelected)
                        _manager.SelectTrack(track, e.control);
                    else if (e.control)
                        _manager.SelectTrack(track, true);
                    _dragMode = DragMode.MoveTrack;
                    _dragTrack = track;
                    _dragStartOffset = track.Offset;
                    _dragMouseStartX = e.mousePosition.x;
                    _dragMouseStartY = e.mousePosition.y;
                    _dragSourceLane = lane;
                    _activeSnapLines = _manager.GetSnapLines(track);
                    _snappedValue = null;

                    _dragOriginalOffsets = new Dictionary<AudioTrack, float>();
                    foreach (var sel in _manager.SelectedTracks)
                        _dragOriginalOffsets[sel] = sel.Offset;

                    e.Use();
                }
            }
        }

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
                        HandleMoveTrackDrag(deltaTime, pxPerSecond, rect, e);
                        break;

                    case DragMode.TrimLeft:
                        _dragTrack.TrimStart = _dragStartValue + deltaTime;
                        _dragTrack.ClampTrim();
                        break;

                    case DragMode.TrimRight:
                        _dragTrack.TrimEnd = _dragStartValue - deltaTime;
                        _dragTrack.ClampTrim();
                        break;

                    case DragMode.FadeInHandle:
                        _dragTrack.FadeInDuration = Mathf.Max(0f, _dragStartValue + deltaTime);
                        _dragTrack.ClampFade();
                        break;

                    case DragMode.FadeOutHandle:
                        _dragTrack.FadeOutDuration = Mathf.Max(0f, _dragStartValue - deltaTime);
                        _dragTrack.ClampFade();
                        break;
                }

                e.Use();
            }

            if (e.type == EventType.MouseUp && _dragMode != DragMode.None)
            {
                if (_dragMode == DragMode.MoveTrack)
                    FinalizeTrackMove();

                _dragMode = DragMode.None;
                _dragTrack = null;
                _activeSnapLines = null;
                _snappedValue = null;
                _dragOriginalOffsets = null;
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
                if (Input.GetKey(KeyCode.LeftShift) && _dragMode == DragMode.MoveTrack)
                {
                    _manager.SnapPixelDistance = Mathf.Clamp(_manager.SnapPixelDistance + (e.delta.y > 0 ? -2 : 2), 2, 50);
                }
                else
                {
                    float mouseNormX = (e.mousePosition.x - waveAreaX) / waveAreaW;
                    float timeAtMouse = _viewStartTime + mouseNormX * duration;

                    float zoomDelta = -e.delta.y * 0.1f;
                    float newZoom = Mathf.Clamp(_zoom * (1 + zoomDelta), 0.00001f, 500f);

                    float newDuration = _viewDuration / newZoom;
                    _viewStartTime = timeAtMouse - mouseNormX * newDuration;
                    _viewStartTime = Mathf.Max(0f, _viewStartTime);
                    _zoom = newZoom;
                }

                e.Use();
            }

            if (e.type == EventType.MouseDown && e.button == 0 && _dragMode == DragMode.None && waveRect.Contains(e.mousePosition))
            {
                bool clickedOnTrack = false;
                foreach (var lane in _manager.Lanes)
                {
                    foreach (var track in lane.Tracks)
                    {
                        float trackStartX = waveAreaX + (track.AudibleStart - _viewStartTime) * pxPerSecond;
                        float trackWidth = track.EffectiveDuration * pxPerSecond;
                        int laneIdx = _manager.GetLaneIndex(lane);
                        float trackY = rect.y + laneIdx * WindowStyles.LaneHeight;
                        var trackRect = new Rect(trackStartX, trackY, trackWidth, WindowStyles.LaneHeight);
                        if (trackRect.Contains(e.mousePosition))
                        {
                            clickedOnTrack = true;
                            break;
                        }
                    }
                    if (clickedOnTrack) break;
                }

                if (!clickedOnTrack)
                {
                    _manager.DeselectAll();
                    e.Use();
                }
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete && _manager.SelectedTracks.Count > 0)
            {
                _manager.RemoveSelectedTracks();
                e.Use();
            }
        }

        private void HandleMoveTrackDrag(float deltaTime, float pxPerSecond, Rect lanesRect, Event e)
        {
            if (_dragOriginalOffsets == null) return;

            float proposed = _dragOriginalOffsets[_dragTrack] + deltaTime;
            float snapped = _manager.TrySnap(_dragTrack, proposed, pxPerSecond);
            float snapDelta = snapped - _dragOriginalOffsets[_dragTrack];
            _snappedValue = Mathf.Abs(snapped - proposed) > 0.001f ? snapped : (float?)null;

            foreach (var kvp in _dragOriginalOffsets)
            {
                kvp.Key.Offset = kvp.Value + snapDelta;
            }

            float mouseY = e.mousePosition.y;
            int targetLaneIndex = -1;
            for (int i = 0; i < _manager.Lanes.Count; i++)
            {
                float laneY = lanesRect.y + i * WindowStyles.LaneHeight;
                if (mouseY >= laneY && mouseY < laneY + WindowStyles.LaneHeight)
                {
                    targetLaneIndex = i;
                    break;
                }
            }

            if (targetLaneIndex >= 0)
            {
                var targetLane = _manager.GetLaneAtIndex(targetLaneIndex);
                if (targetLane != null && targetLane != _dragTrack.Lane)
                {
                    _manager.MoveTrackToLane(_dragTrack, targetLane);
                    _dragSourceLane = targetLane;
                }
            }
        }

        private void FinalizeTrackMove()
        {
            foreach (var track in _manager.SelectedTracks)
            {
                if (track.Lane != null)
                    _manager.ClampTrackPosition(track, track.Lane);
            }
        }

        #endregion

        #region Utility

        private void LoadAudioFile()
        {
            string filter = AudioLoader.GetFileFilter();
            var selection = OpenFileDialog.ShowDialog("Select audio file", "", filter, "", OpenFileDialog.OpenSaveFileDialgueFlags.OFN_FILEMUSTEXIST);
            /*SystemFileDialog.ShowDialog("Select audio file", "", out string selections, SystemFileDialog.FOS.FILEMUSTEXIST | SystemFileDialog.FOS.ALLOWMULTISELECT, filter);
            var selection = selections.Split('|');*/
            if (selection.Length > 0 && !string.IsNullOrEmpty(selection[0]))
            {
                foreach (string se in selection)
                {
                    try
                    {
                        _manager.AddTrackFromFile(se);
                    }
                    catch (Exception e)
                    {
                        Entry.Logger.LogError($"Failed to load audio: {e}");
                    }
                }
            }
        }

        private void SetZoom(float newZoom)
        {
            float center = _viewStartTime + GetViewDuration() / 2f;
            _zoom = Mathf.Clamp(newZoom, 0.00001f, 500f);
            float newDuration = GetViewDuration();
            _viewStartTime = Mathf.Max(0f, center - newDuration / 2f);
        }

        private void FitToContent()
        {
            if (_manager.TrackCount == 0)
            {
                _viewStartTime = 0f;
                _zoom = 1f;
                return;
            }

            float maxEnd = 0f;
            foreach (var track in _manager.AllTracks)
            {
                if (track.VisualEnd > maxEnd)
                    maxEnd = track.VisualEnd;
            }

            maxEnd = Mathf.Max(maxEnd, 1f);
            _viewStartTime = 0f;
            _zoom = _viewDuration / (maxEnd * 1.1f);
            _zoom = Mathf.Clamp(_zoom, 0.00001f, 500f);
        }

        private float GetViewDuration()
        {
            return _viewDuration / _zoom;
        }

        private Func<float> _getPlaybackTime;
        public void SetPlaybackTimeGetter(Func<float> getter) => _getPlaybackTime = getter;

        private float GetPlaybackTime()
        {
            return _getPlaybackTime?.Invoke() ?? 0f;
        }

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

        #endregion
    }
}