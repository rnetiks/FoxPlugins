namespace Autumn.Animation
{
    internal class NoneAnimation : GUIAnimation
    {
        public NoneAnimation(GUIBase _base) : base(_base)
        {
        }

        protected override bool Open()
        {
            owner.Draw();
            return false;
        }

        protected override bool Close()
        {
            return false;
        }
    }
}