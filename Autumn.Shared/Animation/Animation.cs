using System;

namespace Autumn.Animation
{
    public abstract class Animation
    {
        protected GUIBase myBase;

        public Animation(GUIBase _base)
        {
            myBase = _base;
        }


        protected virtual void OnStartOpen()
        {
        }

        protected virtual void OnStartClose()
        {
        }

        protected abstract bool Open();
        protected abstract bool Close();

        public Action StartOpen(Action onEnd)
        {
            OnStartOpen();
            Action act = () =>
            {
                if (Open())
                    return;
                onEnd();
                myBase.OnGUI = myBase.Draw;
            };

            return act;
        }

        public Action StartClose(Action onEnd)
        {
            OnStartClose();
            Action act = () =>
            {
                if (Close())
                    return;
                onEnd();
                myBase.OnGUI = null;
            };

            return act;
        }
    }
}