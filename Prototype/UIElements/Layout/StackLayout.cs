using System.Collections.Generic;
using UnityEngine;

namespace Prototype.UIElements.Layout
{
    /// <summary>
    /// The StackLayout class provides mechanisms to manage a stack-based layout system,
    /// allowing for elements to be positioned in horizontal or vertical orientations with specified spacing.
    /// </summary>
    public class StackLayout
    {
        private readonly Stack<LayoutState> _layoutStack = new Stack<LayoutState>();

        private class LayoutState
        {
            public Rect rect;
            public bool isHorizontal;
            public float currentPosition;
            public float spacing;
        }

        public void PushHorizontal(Rect rect, float spacing = 5f)
        {
            _layoutStack.Push(new LayoutState
            {
                rect = rect,
                isHorizontal = true,
                currentPosition = rect.x,
                spacing = spacing
            });
        }

        public void PushVertical(Rect rect, float spacing = 5f)
        {
            _layoutStack.Push(new LayoutState
            {
                rect = rect,
                isHorizontal = false,
                currentPosition = rect.y,
                spacing = spacing
            });
        }

        public Rect GetNext(float width, float height)
        {
            if (_layoutStack.Count == 0)
                return new Rect(0, 0, width, height);

            var state = _layoutStack.Peek();
            Rect result;

            if (state.isHorizontal)
            {
                result = new Rect(state.currentPosition, state.rect.y, width, height);
                state.currentPosition += width + state.spacing;
            }
            else
            {
                result = new Rect(state.rect.x, state.currentPosition, width, height);
                state.currentPosition += height + state.spacing;
            }

            return result;
        }

        public void Pop()
        {
            if (_layoutStack.Count > 0)
                _layoutStack.Pop();
        }
    }
}