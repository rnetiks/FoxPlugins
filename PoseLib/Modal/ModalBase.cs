using UnityEngine;

namespace PoseLib.KKS
{
    public abstract class ModalBase
    {
        private string _title;

        protected ModalBase(string title = "")
        {
            _title = title;
        }

        public void OnGUI(Rect size)
        {
            DrawContent(size);

            if (size.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

        protected abstract void DrawContent(Rect size);
    }
}