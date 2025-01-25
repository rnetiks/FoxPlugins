using UnityEngine;

namespace Autumn.Animation
{
    public class CenterAnimation : GUIAnimation
    {
        private readonly Rect defaultRect;
        private Rect endPosition;
        private float heightCoeff;
        private Rect position;
        public float CloseSpeed { get; set; }
        public float OpenSpeed { get; set; }

        public CenterAnimation(GUIBase _base, Rect pos) : this(_base, pos, 650f, 1300f)
        {
        }

        public CenterAnimation(GUIBase _base, Rect pos, float openSpeed, float closeSpeed) : base(_base)
        {
            defaultRect = pos;
            heightCoeff = pos.height / pos.width;
            OpenSpeed = openSpeed;
            CloseSpeed = closeSpeed;
        }

        /// <summary>
        /// Handles the opening animation of a centered GUI element by resizing and adjusting position
        /// to simulate a zoom-in effect.
        /// </summary>
        /// <returns>
        /// True if the animation is not yet complete, False when the target end position is reached.
        /// </returns>
        protected override bool Open()
        {
            Draw();
            float speed = Time.unscaledDeltaTime * OpenSpeed;
            position.x -= speed;
            position.y -= speed * heightCoeff;
            position.width += speed * 2f;
            position.height += speed * 2f * heightCoeff;
            return position.x > endPosition.x && position.y > endPosition.y;
        }

        protected override bool Close()
        {
            Draw();
            float speed = Time.unscaledDeltaTime * CloseSpeed;
            position.x += speed;
            position.y += speed * heightCoeff;
            position.width -= speed * 2f;
            position.height -= speed * 2f * heightCoeff;
            return position.x < endPosition.x && position.y < endPosition.y;
        }


        protected override void OnStartOpen()
        {
            position = new Rect(Style.ScreenWidth / 2f, Style.ScreenHeight / 2f, 0f, 0f);
            endPosition = Helper.GetScreenMiddle(defaultRect.width, defaultRect.height);
        }

        protected override void OnStartClose()
        {
            position = Helper.GetScreenMiddle(defaultRect.width, defaultRect.height);
            endPosition = new Rect(Style.ScreenWidth / 2f, Style.ScreenHeight / 2f, 0f, 0f);
        }

        private void Draw()
        {
            GUI.Box(position, string.Empty);
        }
    }
}