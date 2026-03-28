using System.Collections.Generic;
using UnityEngine;

namespace Addin
{
    public class VirtualScrollView
    {
        private readonly float _itemHeight;
        private readonly Dictionary<int, float> _itemHeights = new Dictionary<int, float>();
        private readonly bool _useVariableHeight;
        private Vector2 _scrollPosition;

        public VirtualScrollView(float itemHeight = 20f, bool useVariableHeight = false)
        {
            _itemHeight = itemHeight > 0 ? itemHeight : 30f;
            _useVariableHeight = useVariableHeight;
        }
        public Vector2 ScrollPosition => _scrollPosition;
        public int VisibleStartIndex { get; private set; }
        public int VisibleEndIndex { get; private set; }

        /// Begins a scroll view, allowing the contents to be scrollable within a specified area.
        /// Calculates the visible range of items based on the scroll position and area dimensions.
        /// The scroll view must be concluded with a corresponding call to EndScrollView.
        /// <param name="position">The area in which the scroll view is drawn.</param>
        /// <param name="totalItems">The total number of items to be potentially displayed within the scroll view.</param>
        /// <param name="viewRect">
        ///     The computed rectangle defining the complete scrollable content area, returned as an output
        ///     parameter.
        /// </param>
        public void BeginScrollView(Rect position, int totalItems, out Rect viewRect)
        {
            float totalHeight = _useVariableHeight ? CalculateTotalHeight(totalItems) : totalItems * _itemHeight;

            viewRect = new Rect(0, 0, position.width - 20, totalHeight);
            _scrollPosition = GUI.BeginScrollView(position, _scrollPosition, viewRect);

            CalculateVisibleRange(position, totalItems, totalHeight);
        }

        public void EndScrollView()
        {
            GUI.EndScrollView();
        }

        public Rect GetItemRect(int index)
        {
            if (!_useVariableHeight)
                return new Rect(0, index * _itemHeight, 0, _itemHeight);

            float y = 0;
            for (int i = 0; i < index; i++)
            {
                y += GetItemHeight(i);
            }
            return new Rect(0, y, 0, GetItemHeight(index));
        }

        public void SetItemHeight(int index, float height)
        {
            if (_useVariableHeight)
            {
                _itemHeights[index] = height;
            }
        }

        private float GetItemHeight(int index)
        {
            return _useVariableHeight && _itemHeights.TryGetValue(index, out float height) ? height : _itemHeight;
        }

        private float CalculateTotalHeight(int totalItems)
        {
            float total = 0;
            for (int i = 0; i < totalItems; i++)
            {
                total += GetItemHeight(i);
            }
            return total;
        }

        private void CalculateVisibleRange(Rect position, int totalItems, float totalHeight)
        {
            float viewTop = _scrollPosition.y;
            float viewBottom = viewTop + position.height;

            if (_useVariableHeight)
            {
                VisibleStartIndex = 0;
                VisibleEndIndex = totalItems - 1;

                bool foundStart = false;
                float currentY = 0;
                for (int i = 0; i < totalItems; i++)
                {
                    float itemHeight = GetItemHeight(i);
                    if (!foundStart && currentY + itemHeight >= viewTop)
                    {
                        VisibleStartIndex = i;
                        foundStart = true;
                    }

                    if (currentY > viewBottom)
                    {
                        VisibleEndIndex = i - 1;
                        break;
                    }
                    currentY += itemHeight;
                }
            }
            else
            {
                VisibleStartIndex = Mathf.Max(0, Mathf.FloorToInt(viewTop / _itemHeight) - 1);
                VisibleEndIndex = Mathf.Min(totalItems - 1, Mathf.CeilToInt(viewBottom / _itemHeight) + 1);
            }
        }
    }
}