using UnityEngine;

namespace PoseLib.KKS
{
    public abstract class Modal
    {
        private Rect _size;
        private string _title;

        public Rect Size => _size;
        public string Title => _title;

        public Modal(Rect size, string title)
        {
            _size = size;
            _title = title;
        }

        public void OnGUI()
        {
            GUI.Window(94301, _size, DrawModal, "", GUIStyle.none);

            if (_size.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }
        private void DrawModal(int id)
        {
            DrawHeader();
            DrawContent();
        }

        private void DrawHeader()
        {
            var headerRect = new Rect(0, 0, _size.width, _size.height);
            GUI.Box(headerRect, "", UIManager._theme.HeaderStyle);
            // GUI.Box(new Rect(0, 0, _size.width, _size.height), "", UIManager._theme.WindowStyle);

            var titleRect = new Rect(10, 5, _size.width - 60, 20);
            GUI.Label(titleRect, "Confirm", UIManager._theme.TitleStyle);
        }

        public abstract void DrawContent();
    }
}