using Autumn.Elements;
using UnityEngine;

namespace Autumn
{
    public class GUIDrawer
    {
        private GUIBase owner;
        private GUIDrawerObject drawer;

        public int Depth
        {
            set
            {
                if (drawer == null)
                {
                    return;
                }

                if (drawer.layer == value) return;
                drawer.layer = value;
                drawer.Cancel();
            }
            get
            {
                if (drawer == null)
                {
                    return -1;
                }
                return drawer.layer;
            }
        }

        public GUIDrawer(GUIBase myBase)
        {
            owner = myBase;
        }

        public void Enable()
        {
            if (drawer != null)
            {
                return;
            }
            drawer = new GameObject(owner.Name + "_DrawerObject").AddComponent<GUIDrawerObject>();
            drawer.layer = -1;
            drawer.owner = this;
            drawer.Cancel();
            Object.DontDestroyOnLoad(drawer);
        }

        public void Enable(int currentDepth)
        {
            if (drawer != null)
            {
                return;
            }
            drawer = new GameObject(owner.Name + "_DrawerObject").AddComponent<GUIDrawerObject>();
            drawer.layer = currentDepth;
            drawer.owner = this;
            drawer.Cancel();
            Object.DontDestroyOnLoad(drawer);
        }

        public void Disable()
        {
            if (drawer == null)
            {
                return;
            }
            Object.Destroy(drawer);
            drawer = null;
        }

        private class GUIDrawerObject : MonoBehaviour
        {
            internal GUIDrawer owner;
            internal int layer;
            private bool needCancel = true;

            internal void Cancel()
            {
                needCancel = true;
            }

            private void OnGUI()
            {
                if (needCancel)
                {
                    needCancel = false;
                    return;
                }

                if (owner.owner.OnGUI == null) return;
                
                GUI.depth = layer;
                owner.owner.OnGUI();
            }

            private void Update()
            {
                owner.owner.Update();
            }
        }
    }

    public class Window: GUIBase
    {
        public Window(string name, int layer = -1) : base(name, layer)
        {
            EnableImmediate();
        }

        protected internal override void Draw()
        {
            DropdownSelection.CreateNew(Rect.zero, new[] { "12", "123" }, null);
        }
    }
}