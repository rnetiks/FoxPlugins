using UnityEngine;

namespace Addin
{
    public class ScrollView
    {
        private float _dragStartScroll;
        private float _dragStartY;
        private bool _isDraggingThumb;
        private float _marginOfPixels = 0f;
        private int _scrollbarID;
        private Vector2 _scrollPosition;

        public ScrollView(float scrollbarThickness = 2f)
        {
            ScrollbarThickness = scrollbarThickness;
            _scrollbarID = GUIUtility.GetControlID(FocusType.Passive);
        }

        public float ScrollbarThickness { get; set; } = 2f;
        public Color ScrollbarBackgroundColor { get; set; } = new Color(0.1f, 0.1f, 0.1f, 0.3f);
        public Color ScrollThumbColor { get; set; } = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        public Color ScrollThumbHoverColor { get; set; } = new Color(0.5f, 0.5f, 0.5f, 0.9f);
        public Color ScrollThumbActiveColor { get; set; } = new Color(0.6f, 0.6f, 0.6f, 1f);
        public float ScrollSensitivity { get; set; } = 20f;
        public bool AutoHide { get; set; } = false;
        public float ThumbMinSize { get; set; } = 20f;

        public Vector2 ScrollPosition => _scrollPosition;

        /// <summary>
        ///     Begins a custom scroll view with a slim scrollbar
        /// </summary>
        /// <param name="position">The area in which the scroll view is drawn</param>
        /// <param name="contentHeight">The total height of the scrollable content</param>
        /// <param name="contentWidth">Optional width of content, defaults to position width minus scrollbar</param>
        /// <returns>The rectangle for content rendering</returns>
        public Rect BeginScrollView(Rect position, float contentHeight, float contentWidth = -1f)
        {
            if (contentWidth < 0)
                contentWidth = position.width - ScrollbarThickness;

            bool needsScrollbar = contentHeight > position.height;

            if (AutoHide && !needsScrollbar)
            {
                _scrollPosition.y = 0;
                return new Rect(position.x, position.y, position.width, position.height);
            }

            // Meep Meep
            if (position.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.ScrollWheel)
                {
                    _scrollPosition.y += Event.current.delta.y * ScrollSensitivity;
                    _scrollPosition.y = Mathf.Clamp(_scrollPosition.y, 0, Mathf.Max(0, contentHeight - position.height));
                    Event.current.Use();
                }
            }

            /************************* I wanna have a foxgirl wife TT-TT */

            if (needsScrollbar)
            {
                DrawCustomScrollbar(position, contentHeight);
            }

            GUI.BeginGroup(new Rect(position.x, position.y, contentWidth, position.height));

            return new Rect(0, -_scrollPosition.y, contentWidth, contentHeight);
        }

        public void EndScrollView()
        {
            GUI.EndGroup();
        }

        private void DrawCustomScrollbar(Rect viewRect, float contentHeight)
        {
            float scrollbarX = viewRect.x + viewRect.width - ScrollbarThickness;
            var scrollbarRect = new Rect(scrollbarX, viewRect.y, ScrollbarThickness, viewRect.height);

            GUI.color = ScrollbarBackgroundColor;
            GUI.DrawTexture(scrollbarRect, Texture2D.whiteTexture);

            float visibleRatio = Mathf.Clamp01(viewRect.height / contentHeight);
            float thumbHeight = Mathf.Max(ThumbMinSize, viewRect.height * visibleRatio);
            float scrollRange = contentHeight - viewRect.height;
            float thumbRange = viewRect.height - thumbHeight;
            float thumbY = thumbRange > 0 ? _scrollPosition.y / scrollRange * thumbRange : 0;

            var thumbRect = new Rect(scrollbarX, viewRect.y + thumbY, ScrollbarThickness, thumbHeight);

            var e = Event.current;
            bool isHovering = thumbRect.Contains(e.mousePosition);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (isHovering && e.button == 0)
                    {
                        _isDraggingThumb = true;
                        _dragStartY = e.mousePosition.y;
                        _dragStartScroll = _scrollPosition.y;
                        e.Use();
                    }
                    else if (scrollbarRect.Contains(e.mousePosition) && e.button == 0)
                    {
                        float clickY = e.mousePosition.y - viewRect.y;
                        float targetThumbY = clickY - thumbHeight * 0.5f;
                        _scrollPosition.y = targetThumbY / thumbRange * scrollRange;
                        _scrollPosition.y = Mathf.Clamp(_scrollPosition.y, 0, scrollRange);
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isDraggingThumb)
                    {
                        float dragDelta = e.mousePosition.y - _dragStartY;
                        float scrollDelta = dragDelta / thumbRange * scrollRange;
                        _scrollPosition.y = Mathf.Clamp(_dragStartScroll + scrollDelta, 0, scrollRange);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_isDraggingThumb && e.button == 0)
                    {
                        _isDraggingThumb = false;
                        e.Use();
                    }
                    break;
            }

            var thumbColor = _isDraggingThumb ? ScrollThumbActiveColor :
                isHovering ? ScrollThumbHoverColor : ScrollThumbColor;
            GUI.color = thumbColor;
            GUI.DrawTexture(thumbRect, Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        /// <summary>
        ///     Sets the scroll position programmatically
        /// </summary>
        public void SetScrollPosition(float y)
        {
            _scrollPosition.y = y;
        }

        /// <summary>
        ///     Scrolls to a specific item position
        /// </summary>
        public void ScrollTo(float targetY, float viewHeight, float contentHeight)
        {
            float maxScroll = Mathf.Max(0, contentHeight - viewHeight);
            _scrollPosition.y = Mathf.Clamp(targetY, 0, maxScroll);
        }

        /// <summary>
        ///     Ensures a specific rect is visible in the scroll view
        /// </summary>
        public void ScrollToRect(Rect rect, float viewHeight)
        {
            float rectTop = rect.y;
            float rectBottom = rect.y + rect.height;

            if (rectTop < _scrollPosition.y)
            {
                _scrollPosition.y = rectTop;
            }
            else if (rectBottom > _scrollPosition.y + viewHeight)
            {
                _scrollPosition.y = rectBottom - viewHeight;
            }
        }
    }
}