using UnityEngine;

namespace LHC.Core
{
    public class FloatModal : Modal
    {
        public FloatModal(OnSubmit submitHandler) : base(submitHandler)
        {
        }
        
        public override void OnGUI()
        {
            GUI.DrawTexture(new Rect(), Texture2D.blackTexture);
        }
    }
}