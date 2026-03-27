using System.Collections.Generic;
using TheBirdOfHermes.Audio;
using TheBirdOfHermes.Undo;
using UnityEngine;

namespace TheBirdOfHermes.UI
{
    public class FilterConfigModal
    {
        private static readonly int WindowId = "TBOHFilterConfig".GetHashCode();

        public bool IsOpen { get; private set; }

        private AudioFilterBase _filter;
        private List<AudioTrack> _tracks;
        private UndoManager _undoManager;
        private Rect _windowRect;

        private const float ModalWidth = 280f;
        private const float MinHeight = 120f;
        private const float MaxHeight = 600f;

        private float _measuredContentHeight;
        private bool _firstFrame;

        public void Open(AudioFilterBase filter, IEnumerable<AudioTrack> tracks, UndoManager undoManager)
        {
            _filter = filter;
            _tracks = new List<AudioTrack>(tracks);
            _undoManager = undoManager;
            IsOpen = true;
            _firstFrame = true;
            _measuredContentHeight = MinHeight;

            _windowRect = new Rect(
                Screen.width / 2f - ModalWidth / 2f,
                Screen.height / 2f - MinHeight / 2f,
                ModalWidth, MinHeight);
        }

        public void Close()
        {
            IsOpen = false;
            _filter = null;
            _tracks = null;
            _undoManager = null;
        }

        /// <summary>
        /// Returns true if this modal is currently blocking interaction (e.g. for filter config).
        /// </summary>
        public bool IsBlocking => IsOpen;

        public void Draw()
        {
            if (!IsOpen || _filter == null || _tracks == null || _tracks.Count == 0) return;

            var prevDepth = GUI.depth;
            GUI.depth = -3000;

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height),
                WindowStyles.GetTexture(new Color(0, 0, 0, 0.4f)));

            string title = _tracks.Count > 1 ? $"{_filter.Name} ({_tracks.Count} tracks)" : _filter.Name;
            _windowRect = GUI.Window(WindowId, _windowRect, DrawContent, title, WindowStyles.WindowStyle);

            if (_windowRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
            }

            GUI.depth = prevDepth;
        }

        private void DrawContent(int id)
        {
            GUI.DrawTexture(new Rect(0, 0, _windowRect.width, _windowRect.height),
                WindowStyles.GetTexture(WindowStyles.WindowBg));

            GUILayout.Space(6);

            _filter.OnDraw();

            if (_tracks.Count > 1)
            {
                GUILayout.Space(4);
                GUILayout.Label($"Applying to {_tracks.Count} tracks", WindowStyles.HintLabel);
            }

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(70), GUILayout.Height(22)))
                Close();

            GUILayout.Space(8);

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
            if (GUILayout.Button("Apply", GUILayout.Width(70), GUILayout.Height(22)))
            {
                var cmd = new ApplyFilterCommand(_filter.Name, _tracks, _filter);
                foreach (var track in _tracks)
                    track.ApplyFilter(_filter);
                _undoManager?.Push(cmd);
                Close();
            }
            GUI.backgroundColor = prevBg;

            GUILayout.Space(4);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            if (Event.current.type == EventType.Repaint)
            {
                Rect last = GUILayoutUtility.GetLastRect();
                float contentHeight = last.yMax + 24f;
                contentHeight = Mathf.Clamp(contentHeight, MinHeight, MaxHeight);

                if (_firstFrame || Mathf.Abs(contentHeight - _measuredContentHeight) > 2f)
                {
                    _measuredContentHeight = contentHeight;
                    float centerY = _windowRect.y + _windowRect.height / 2f;
                    _windowRect.height = contentHeight;
                    _windowRect.y = centerY - contentHeight / 2f;

                    if (_windowRect.y < 0) _windowRect.y = 0;
                    if (_windowRect.yMax > Screen.height) _windowRect.y = Screen.height - _windowRect.height;

                    _firstFrame = false;
                }
            }

            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 20));
        }
    }
}