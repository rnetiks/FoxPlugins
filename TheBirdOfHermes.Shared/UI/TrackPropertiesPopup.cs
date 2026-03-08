using System;
using UnityEngine;

namespace TheBirdOfHermes.UI
{
    public class TrackPropertiesPopup
    {
        private static readonly int WindowId = "TBOHTrackProps".GetHashCode();

        public AudioTrack Target { get; private set; }
        public bool IsOpen { get; private set; }

        private Rect _windowRect;
        private Action<AudioTrack> _onRemove;

        private static readonly Color[] Palette = WindowStyles.TrackColors;

        /// <summary>
        /// Opens the Track Properties Popup and initializes its state.
        /// This method sets the target track, position, and the delegate to execute upon removal.
        /// The popup's position is adjusted to ensure it remains within the screen boundaries.
        /// </summary>
        /// <param name="track">The <see cref="AudioTrack"/> that is the target of the popup.</param>
        /// <param name="screenPos">The screen position where the popup should appear.</param>
        /// <param name="onRemove">An action delegate that is called when the track is removed.</param>
        public void Open(AudioTrack track, Vector2 screenPos, Action<AudioTrack> onRemove)
        {
            Target = track;
            _onRemove = onRemove;
            IsOpen = true;
            _windowRect = new Rect(screenPos.x, screenPos.y, 260, 370);

            if (_windowRect.xMax > Screen.width)
                _windowRect.x = Screen.width - _windowRect.width;
            if (_windowRect.yMax > Screen.height)
                _windowRect.y = Screen.height - _windowRect.height;
        }

        /// <summary>
        /// Closes the Track Properties Popup by resetting its state.
        /// This method sets the popup's visibility to false and clears the current target track.
        /// It is typically used to dismiss or finalize the interaction with the popup interface.
        /// </summary>
        public void Close()
        {
            IsOpen = false;
            Target = null;
        }

        /// <summary>
        /// Renders the graphical interface associated with the component.
        /// This method invokes Unity's GUI system to set up window elements
        /// and handles user interactions such as mouse clicks and keyboard events.
        /// Typically used to display or manage data related to audio tracks or similar entities.
        /// </summary>
        public void Draw()
        {
            if (!IsOpen || Target == null) return;

            _windowRect = GUI.Window(WindowId, _windowRect, DrawContent, "Track Properties", WindowStyles.WindowStyle);

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
            
            if(_windowRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

        /// <summary>
        /// Renders the content of the "Track Properties" popup window.
        /// This method defines the layout and functionality for various controls,
        /// such as labels, text fields, sliders, and buttons, allowing users
        /// to view and edit properties related to a track.
        /// </summary>
        /// <param name="id">The unique identifier for the window. Used internally by Unity's GUI system.</param>
        private void DrawContent(int id)
        {
            if (Target == null) { Close(); return; }

            GUILayout.Space(4);

            GUILayout.Label("Name");
            Target.Name = GUILayout.TextField(Target.Name);

            GUILayout.Space(6);

            GUILayout.Label("Offset (seconds)");
            GUILayout.BeginHorizontal();
            var offsetStr = GUILayout.TextField(Target.Offset.ToString("F3"), GUILayout.Width(80));
            if (float.TryParse(offsetStr, out float newOffset))
                Target.Offset = Mathf.Max(0f, newOffset);
            if (GUILayout.Button("-", GUILayout.Width(22)))
                Target.Offset = Mathf.Max(0f, Target.Offset - 0.05f);
            if (GUILayout.Button("+", GUILayout.Width(22)))
                Target.Offset += 0.05f;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.Label($"Trim Start (0 - {Target.FullDuration:F2}s)");
            GUILayout.BeginHorizontal();
            var trimSStr = GUILayout.TextField(Target.TrimStart.ToString("F3"), GUILayout.Width(80));
            if (float.TryParse(trimSStr, out float newTrimS))
            {
                Target.TrimStart = newTrimS;
                Target.ClampTrim();
            }
            if (GUILayout.Button("-", GUILayout.Width(22)))
            {
                Target.TrimStart -= 0.05f;
                Target.ClampTrim();
            }
            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                Target.TrimStart += 0.05f;
                Target.ClampTrim();
            }
            if (GUILayout.Button("Reset", GUILayout.Width(42)))
                Target.TrimStart = 0f;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.Label($"Trim End (0 - {Target.FullDuration:F2}s)");
            GUILayout.BeginHorizontal();
            var trimEStr = GUILayout.TextField(Target.TrimEnd.ToString("F3"), GUILayout.Width(80));
            if (float.TryParse(trimEStr, out float newTrimE))
            {
                Target.TrimEnd = newTrimE;
                Target.ClampTrim();
            }
            if (GUILayout.Button("-", GUILayout.Width(22)))
            {
                Target.TrimEnd -= 0.05f;
                Target.ClampTrim();
            }
            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                Target.TrimEnd += 0.05f;
                Target.ClampTrim();
            }
            if (GUILayout.Button("Reset", GUILayout.Width(42)))
                Target.TrimEnd = 0f;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.Label($"Volume: {Target.Volume:P0}");
            Target.Volume = GUILayout.HorizontalSlider(Target.Volume, 0f, 1f);

            GUILayout.Label("Color");
            GUILayout.BeginHorizontal();
            foreach (var c in Palette)
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = c;
                if (GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20)))
                    Target.TrackColor = c;
                GUI.backgroundColor = prev;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            var prevColor = GUI.color;
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Remove Track"))
            {
                _onRemove?.Invoke(Target);
                Close();
            }
            GUI.color = prevColor;

            GUI.DragWindow();
        }
    }
}
