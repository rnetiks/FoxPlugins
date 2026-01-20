using UnityEngine;

namespace PoseLib.KKS
{
    public abstract class WarningModal : ModalBase
    {

        protected override void DrawContent(Rect size)
        {
            this.DrawContent();
        }

        protected abstract void DrawContent();
    }
}