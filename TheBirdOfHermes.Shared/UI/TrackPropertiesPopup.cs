using System;
using System.Linq;
using UnityEngine;

namespace TheBirdOfHermes.UI
{
    public class TrackPropertiesPopup
    {
        private static readonly int WindowId = "TBOHTrackProps".GetHashCode();

        public AudioLane TargetLane { get; private set; }
        public AudioTrack SelectedTrack { get; private set; }
        public bool IsOpen { get; private set; }

        private Rect _windowRect;
        private Action<AudioTrack> _onRemove;
        private Vector2 _trackListScroll;

        private static readonly Color[] Palette = WindowStyles.TrackColors;

        private const float WindowWidth = 600f;
        private const float WindowHeight = 440f;
        private const float LeftPanelWidth = 260f;

        /// <summary>
        /// Opens the popup for a lane. If track is provided, it's auto-selected.
        /// </summary>
        public void Open(AudioLane lane, AudioTrack track, Vector2 screenPos, Action<AudioTrack> onRemove)
        {
            TargetLane = lane;
            SelectedTrack = track ?? lane.Tracks.FirstOrDefault();
            _onRemove = onRemove;
            IsOpen = true;
            _windowRect = new Rect(screenPos.x, screenPos.y, WindowWidth, WindowHeight);

            if (_windowRect.xMax > Screen.width)
                _windowRect.x = Screen.width - _windowRect.width;
            if (_windowRect.yMax > Screen.height)
                _windowRect.y = Screen.height - _windowRect.height;
        }

        public void Close()
        {
            IsOpen = false;
            TargetLane = null;
            SelectedTrack = null;
        }

        public void Draw()
        {
            if (!IsOpen || TargetLane == null) return;

            GUI.depth = -1000;
            _windowRect = GUI.Window(WindowId, _windowRect, DrawContent, "Track Properties", WindowStyles.WindowStyle);
            GUI.depth = 0;

            Event e = Event.current;
            if (e.type == EventType.MouseDown && !_windowRect.Contains(e.mousePosition))
            {
                Close();
                e.Use();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
            }

            if (_windowRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

        private void DrawContent(int id)
        {
            if (TargetLane == null)
            {
                Close();
                return;
            }

            float contentY = 22;
            float contentH = _windowRect.height - contentY;

            GUI.DrawTexture(new Rect(0, 0, _windowRect.width, _windowRect.height), WindowStyles.GetTexture(WindowStyles.WindowBg));
            GUIContent headerLabel = new GUIContent("Track Properties");
            var calcSize = WindowStyles.LabelBold.CalcSize(headerLabel);
            GUI.Label(new Rect(_windowRect.width / 2 - calcSize.x / 2, 0, calcSize.x, calcSize.y), headerLabel, WindowStyles.LabelBold);
            var leftRect = new Rect(0, contentY, LeftPanelWidth, contentH);
            DrawTrackList(leftRect);

            GUI.DrawTexture(new Rect(LeftPanelWidth, contentY, 1, contentH), WindowStyles.GetTexture(WindowStyles.LaneSeparator));

            var rightRect = new Rect(LeftPanelWidth, contentY, _windowRect.width - LeftPanelWidth, contentH);
            if (SelectedTrack != null)
                DrawTrackSettings(rightRect);
            else
                GUI.Label(new Rect(rightRect.x + 10, rightRect.y + 10, rightRect.width, 20), "No track selected");

            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 20));
        }

        private void DrawTrackList(Rect rect)
        {
            GUI.DrawTexture(rect, WindowStyles.GetTexture(new Color(0.1f, 0.1f, 0.13f)));

            float itemHeight = 28;
            float totalHeight = TargetLane.Tracks.Count * itemHeight;
            bool needsScroll = totalHeight > rect.height;

            if (needsScroll)
                _trackListScroll = GUI.BeginScrollView(rect, _trackListScroll, new Rect(0, 0, rect.width - 16, totalHeight));

            for (int i = 0; i < TargetLane.Tracks.Count; i++)
            {
                var track = TargetLane.Tracks[i];
                float y = needsScroll ? i * itemHeight : rect.y + i * itemHeight;
                float x = needsScroll ? 0 : rect.x;
                float w = needsScroll ? rect.width - 16 : rect.width;

                var itemRect = new Rect(x, y, w, itemHeight);

                if (track == SelectedTrack)
                    GUI.DrawTexture(itemRect, WindowStyles.GetTexture(WindowStyles.LaneBgSelected));

                GUI.DrawTexture(new Rect(itemRect.x + 2, itemRect.y + 4, 4, itemRect.height - 8),
                    WindowStyles.GetTexture(track.TrackColor));

                GUI.Label(new Rect(itemRect.x + 10, itemRect.y + 4, itemRect.width - 14, itemRect.height - 8),
                    track.Name, WindowStyles.LabelBold);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && itemRect.Contains(Event.current.mousePosition))
                {
                    SelectedTrack = track;
                    Event.current.Use();
                }
            }

            if (needsScroll)
                GUI.EndScrollView();
        }

        private void DrawTrackSettings(Rect rect)
        {
            var track = SelectedTrack;
            if (track == null) return;

            GUILayout.BeginArea(rect);

            GUILayout.Space(4);

            GUILayout.Label("Name");
            track.Name = GUILayout.TextField(track.Name);

            GUILayout.Space(6);

            GUILayout.Label("Offset (seconds)");
            GUILayout.BeginHorizontal();
            var offsetStr = GUILayout.TextField(track.Offset.ToString("F3"), GUILayout.Width(80));
            if (float.TryParse(offsetStr, out float newOffset))
                track.Offset = newOffset;
            if (GUILayout.Button("-", GUILayout.Width(22)))
                track.Offset -= 0.05f;
            if (GUILayout.Button("+", GUILayout.Width(22)))
                track.Offset += 0.05f;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.Label($"Trim Start (0 - {track.FullDuration:F2}s)");
            GUILayout.BeginHorizontal();
            var trimSStr = GUILayout.TextField(track.TrimStart.ToString("F3"), GUILayout.Width(80));
            if (float.TryParse(trimSStr, out float newTrimS))
            {
                track.TrimStart = newTrimS;
                track.ClampTrim();
            }
            if (GUILayout.Button("-", GUILayout.Width(22)))
            {
                track.TrimStart -= 0.05f;
                track.ClampTrim();
            }
            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                track.TrimStart += 0.05f;
                track.ClampTrim();
            }
            if (GUILayout.Button("Reset", GUILayout.Width(42)))
                track.TrimStart = 0f;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.Label($"Trim End (0 - {track.FullDuration:F2}s)");
            GUILayout.BeginHorizontal();
            var trimEStr = GUILayout.TextField(track.TrimEnd.ToString("F3"), GUILayout.Width(80));
            if (float.TryParse(trimEStr, out float newTrimE))
            {
                track.TrimEnd = newTrimE;
                track.ClampTrim();
            }
            if (GUILayout.Button("-", GUILayout.Width(22)))
            {
                track.TrimEnd -= 0.05f;
                track.ClampTrim();
            }
            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                track.TrimEnd += 0.05f;
                track.ClampTrim();
            }
            if (GUILayout.Button("Reset", GUILayout.Width(42)))
                track.TrimEnd = 0f;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.Label($"Fade In: {track.FadeInDuration:F2}s");
            track.FadeInDuration = GUILayout.HorizontalSlider(track.FadeInDuration, 0f, track.EffectiveDuration);
            track.ClampFade();

            GUILayout.Label($"Fade Out: {track.FadeOutDuration:F2}s");
            track.FadeOutDuration = GUILayout.HorizontalSlider(track.FadeOutDuration, 0f, track.EffectiveDuration);
            track.ClampFade();

            GUILayout.Space(4);

            GUILayout.Label("Waveform Mode");
            GUILayout.BeginHorizontal();
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = track.NormalizationMode == WaveformMode.Normal ? Color.green : Color.gray;
            if (GUILayout.Button("Normal", GUILayout.Width(60)))
                track.NormalizationMode = WaveformMode.Normal;
            GUI.backgroundColor = track.NormalizationMode == WaveformMode.Max ? Color.green : Color.gray;
            if (GUILayout.Button("Max", GUILayout.Width(40)))
                track.NormalizationMode = WaveformMode.Max;
            GUI.backgroundColor = track.NormalizationMode == WaveformMode.Volume ? Color.green : Color.gray;
            if (GUILayout.Button("Volume", GUILayout.Width(60)))
                track.NormalizationMode = WaveformMode.Volume;
            GUI.backgroundColor = prevBg;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.Label("Color");
            GUILayout.BeginHorizontal();
            foreach (var c in Palette)
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = c;
                if (GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20)))
                    track.TrackColor = c;
                GUI.backgroundColor = prev;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            var prevColor = GUI.color;
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Remove Track"))
            {
                _onRemove?.Invoke(track);
                SelectedTrack = TargetLane.Tracks.FirstOrDefault();
                if (TargetLane.Tracks.Count == 0)
                    Close();
            }
            GUI.color = prevColor;

            GUILayout.EndArea();
        }
    }
}