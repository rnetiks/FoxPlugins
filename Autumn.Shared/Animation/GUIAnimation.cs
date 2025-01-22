using System;

namespace Autumn.Animation
{
    public abstract class GUIAnimation
    {
        /// <summary>
        /// Owner <seealso cref="GUIBase"/>.
        /// </summary>
        protected GUIBase owner;

        public GUIAnimation(GUIBase _base)
        {
            owner = _base;
        }

        /// <summary>
        /// Calls before opening animation starts executing.
        /// </summary>
        protected virtual void OnStartOpen()
        {
        }

        /// <summary>
        /// Calls before closing animation starts executing.
        /// </summary>
        protected virtual void OnStartClose()
        {
        }


        /// <summary>
        /// Draws open animation.
        /// </summary>
        protected abstract bool Open();

        /// <summary>
        /// Draws closing animation.
        /// </summary>
        /// <returns><seealso cref="false"/> when animation is complete. <seealso cref="true"/>otherwise.</returns>
        protected abstract bool Close();

        /// <summary>
        /// Starts closing animation.
        /// </summary>
        public Action StartClose(Action onEnd)
        {
            OnStartClose();
            Action act = () =>
            {
                if (Close())
                    return;
                onEnd();
                owner.OnGUI = null;
            };
            return act;
        }

        /// <summary>
        /// Starts opening animation.
        /// </summary>
        public Action StartOpen(Action onEnd)
        {
            OnStartOpen();
            Action act = () =>
            {
                if (Open())
                    return;
                onEnd();
                owner.OnGUI = owner.Draw;
            };
            return act;
        }
    }
}