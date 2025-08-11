using UnityEngine;

namespace Prototype.UIElements
{
    /// <summary>
    /// Represents a base class for creating customizable Unity IMGUI windows.
    /// </summary>
    /// <remarks>
    /// The IMGUIWindow class provides the foundation for creating draggable and resizable windows
    /// in Unity using the IMGUI system. It supports setting window properties, rendering using
    /// provided GUI methods, and defining custom content through inheritance.
    /// Derive from this class and implement the <see cref="DrawContent"/> method to define
    /// the specific content and behavior of the window.
    /// </remarks>
    public abstract class IMGUIWindow
    {
        public string Id { get; protected set; }
        public string Title { get; set; }
        public Rect WindowRect { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsResizable { get; set; } = true;
        public bool IsDraggable { get; set; } = true;
        private bool _isResizing;

        protected IMGUIWindow(string id, string title, Rect initialRect)
        {
            Id = id;
            Title = title;
            WindowRect = initialRect;
        }

        public virtual void OnGUI()
        {
            if (!IsVisible) return;
            

            if (IsDraggable)
            {
                WindowRect = GUI.Window(Id.GetHashCode(), WindowRect, DrawWindow, Title);
            }
            else
            {
                GUILayout.BeginArea(WindowRect);
                DrawContent(WindowRect);
                GUILayout.EndArea();
            }
        }

        private enum ResizeDirection
        {
            Top,
            Bottom,
            Left,
            Right,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            None
        }

        private ResizeDirection MouseOverEdge(Rect rect, float offset)
        {
            var mouse = Event.current.mousePosition;

            var left = mouse.x > rect.x - offset && mouse.x < rect.x + offset;
            var right = mouse.x > rect.xMax - offset && mouse.x < rect.xMax + offset;
            var top = mouse.y > rect.y - offset && mouse.y < rect.y + offset;
            var bottom = mouse.y > rect.yMax - offset && mouse.y < rect.yMax + offset;
            
            switch (left)
            {
                case true when bottom:
                    return ResizeDirection.BottomLeft;
                case true when top:
                    return ResizeDirection.TopLeft;
            }
            switch (right)
            {
                case true when bottom:
                    return ResizeDirection.BottomRight;
                case true when top:
                    return ResizeDirection.TopRight;
            }
            if(left)
                return ResizeDirection.Left;
            if(right)
                return ResizeDirection.Right;
            if(top)
                return ResizeDirection.Top;
            return bottom ? ResizeDirection.Bottom : ResizeDirection.None;
        }

        private void DrawWindow(int windowId)
        {
            DrawContent(WindowRect);
            
            if (IsDraggable)
                GUI.DragWindow();
        }

        protected abstract void DrawContent(Rect rect);
    }

}