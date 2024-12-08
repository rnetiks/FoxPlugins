using UnityEngine;

namespace LHC.Core
{
    public abstract class Modal
    {
        public delegate void OnSubmit();

        private OnSubmit _submitHandler;
        public float Width = Screen.width;
        public float Height = Screen.height;

        public Modal(OnSubmit submitHandler)
        {
            _submitHandler = submitHandler;
        }

        public abstract void OnGUI();
    }
}