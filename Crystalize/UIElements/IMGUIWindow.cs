using UnityEngine;

namespace Crystalize.UIElements
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
        private ResizeDirection _currentResizeDirection = ResizeDirection.None;
        private Vector2 _resizeStartMousePos;
        private Rect _resizeStartRect;
        private GUIStyle _windowStyle;
        private GUIStyle _resizeIndicatorStyle;
        private const float RESIZE_EDGE_OFFSET = 8f;
        private const float INDICATOR_SIZE = 8f;
        
        public Vector2 MinSize { get; set; } = new Vector2(100, 80);
        

        public GUIStyle WindowStyle
        {
            get
            {
                if (_windowStyle == null)
                {
                    _windowStyle = new GUIStyle(GUI.skin.window);
                }
                return _windowStyle;
            }
            set => _windowStyle = value;
        }
        
        private GUIStyle ResizeIndicatorStyle
        {
            get
            {
                if (_resizeIndicatorStyle == null)
                {
                    _resizeIndicatorStyle = new GUIStyle();
                    _resizeIndicatorStyle.normal.background = CreateColorTexture(new Color(0.7f, 0.7f, 0.7f, 0.8f));
                    _resizeIndicatorStyle.border = new RectOffset(1, 1, 1, 1);
                }
                return _resizeIndicatorStyle;
            }
        }

        protected IMGUIWindow(string id, string title, Rect initialRect)
        {
            Id = id;
            Title = title;
            WindowRect = initialRect;
        }

        public bool Open()
        {
            return IsVisible = true;
        }

        public bool Close()
        {
            return IsVisible = false;
        }

        public virtual void OnGUI()
        {
            if (!IsVisible) return;

            if (IsResizable)
            {
                HandleResizing();
            }

            WindowRect = GUI.Window(Id.GetHashCode(), WindowRect, DrawWindow, Title, WindowStyle);

            if (IsResizable && !_isResizing)
            {
                DrawResizeIndicators();
            }

            if (WindowRect.Contains(Event.current.mousePosition))
                UnityEngine.Input.ResetInputAxes();
        }

        private void HandleResizing()
        {
            Event currentEvent = Event.current;
            
            if (!_isResizing)
            {
                _currentResizeDirection = MouseOverEdge(WindowRect, RESIZE_EDGE_OFFSET);
                
                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && _currentResizeDirection != ResizeDirection.None)
                {
                    _isResizing = true;
                    _resizeStartMousePos = currentEvent.mousePosition;
                    _resizeStartRect = WindowRect;
                    currentEvent.Use();
                    UnityEngine.Input.ResetInputAxes();
                }
            }
            else
            {
                if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
                {
                    Vector2 mouseDelta = currentEvent.mousePosition - _resizeStartMousePos;
                    WindowRect = CalculateResizedRect(_resizeStartRect, mouseDelta, _currentResizeDirection);
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
                {
                    _isResizing = false;
                    _currentResizeDirection = ResizeDirection.None;
                    currentEvent.Use();
                }
            }
        }

        private Rect CalculateResizedRect(Rect originalRect, Vector2 mouseDelta, ResizeDirection direction)
        {
            Rect newRect = originalRect;

            if ((direction & ResizeDirection.Left) != 0)
            {
                float newWidth = originalRect.width - mouseDelta.x;
                if (newWidth >= MinSize.x)
                {
                    newRect.x = originalRect.x + mouseDelta.x;
                    newRect.width = newWidth;
                }
            }
            if ((direction & ResizeDirection.Right) != 0)
            {
                newRect.width = Mathf.Max(MinSize.x, originalRect.width + mouseDelta.x);
            }
            if ((direction & ResizeDirection.Top) != 0)
            {
                float newHeight = originalRect.height - mouseDelta.y;
                if (newHeight >= MinSize.y)
                {
                    newRect.y = originalRect.y + mouseDelta.y;
                    newRect.height = newHeight;
                }
            }
            if ((direction & ResizeDirection.Bottom) != 0)
            {
                newRect.height = Mathf.Max(MinSize.y, originalRect.height + mouseDelta.y);
            }

            return newRect;
        }

        private void DrawResizeIndicators()
        {
            if (_currentResizeDirection == ResizeDirection.None) return;
            
            Color originalColor = GUI.color;
            GUI.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);

            switch (_currentResizeDirection)
            {
                case ResizeDirection.Left:
                    DrawIndicator(new Rect(WindowRect.x - INDICATOR_SIZE/2, WindowRect.y + WindowRect.height/2 - INDICATOR_SIZE/2, INDICATOR_SIZE, INDICATOR_SIZE));
                    break;
                case ResizeDirection.Right:
                    DrawIndicator(new Rect(WindowRect.xMax - INDICATOR_SIZE/2, WindowRect.y + WindowRect.height/2 - INDICATOR_SIZE/2, INDICATOR_SIZE, INDICATOR_SIZE));
                    break;
                case ResizeDirection.Top:
                    DrawIndicator(new Rect(WindowRect.x + WindowRect.width/2 - INDICATOR_SIZE/2, WindowRect.y - INDICATOR_SIZE/2, INDICATOR_SIZE, INDICATOR_SIZE));
                    break;
                case ResizeDirection.Bottom:
                    DrawIndicator(new Rect(WindowRect.x + WindowRect.width/2 - INDICATOR_SIZE/2, WindowRect.yMax - INDICATOR_SIZE/2, INDICATOR_SIZE, INDICATOR_SIZE));
                    break;
                case ResizeDirection.TopLeft:
                    DrawIndicator(new Rect(WindowRect.x - INDICATOR_SIZE/2, WindowRect.y - INDICATOR_SIZE/2, INDICATOR_SIZE, INDICATOR_SIZE));
                    break;
                case ResizeDirection.TopRight:
                    DrawIndicator(new Rect(WindowRect.xMax - INDICATOR_SIZE/2, WindowRect.y - INDICATOR_SIZE/2, INDICATOR_SIZE, INDICATOR_SIZE));
                    break;
                case ResizeDirection.BottomLeft:
                    DrawIndicator(new Rect(WindowRect.x - INDICATOR_SIZE/2, WindowRect.yMax - INDICATOR_SIZE/2, INDICATOR_SIZE, INDICATOR_SIZE));
                    break;
                case ResizeDirection.BottomRight:
                    DrawIndicator(new Rect(WindowRect.xMax - INDICATOR_SIZE/2, WindowRect.yMax - INDICATOR_SIZE/2, INDICATOR_SIZE, INDICATOR_SIZE));
                    break;
            }

            GUI.color = originalColor;
        }

        private void DrawIndicator(Rect rect)
        {
            GUI.Box(rect, "", ResizeIndicatorStyle);
        }

        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private enum ResizeDirection
        {
            None = 0,
            Top = 1,
            Bottom = 2,
            Left = 4,
            Right = 8,
            TopLeft = Top | Left,
            TopRight = Top | Right,
            BottomLeft = Bottom | Left,
            BottomRight = Bottom | Right
        }

        private ResizeDirection MouseOverEdge(Rect rect, float offset)
        {
            int t = Shader.PropertyToID("");
            Vector2 mouse = Event.current.mousePosition;

            bool left = mouse.x >= rect.x - offset && mouse.x <= rect.x + offset;
            bool right = mouse.x >= rect.xMax - offset && mouse.x <= rect.xMax + offset;
            bool top = mouse.y >= rect.y - offset && mouse.y <= rect.y + offset;
            bool bottom = mouse.y >= rect.yMax - offset && mouse.y <= rect.yMax + offset;

            if (left && top) return ResizeDirection.TopLeft;
            if (right && top) return ResizeDirection.TopRight;
            if (left && bottom) return ResizeDirection.BottomLeft;
            if (right && bottom) return ResizeDirection.BottomRight;

            if (left && mouse.y >= rect.y && mouse.y <= rect.yMax) return ResizeDirection.Left;
            if (right && mouse.y >= rect.y && mouse.y <= rect.yMax) return ResizeDirection.Right;
            if (top && mouse.x >= rect.x && mouse.x <= rect.xMax) return ResizeDirection.Top;
            if (bottom && mouse.x >= rect.x && mouse.x <= rect.xMax) return ResizeDirection.Bottom;

            return ResizeDirection.None;
        }

        private void DrawWindow(int windowId)
        {
            DrawContent(WindowRect);

            if (IsDraggable && !_isResizing)
                GUI.DragWindow();
        }

        protected abstract void DrawContent(Rect rect);
    }
}