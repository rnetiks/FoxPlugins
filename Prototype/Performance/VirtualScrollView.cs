using System.Collections.Generic;
using UnityEngine;

namespace Prototype.UIElements
{
    public class VirtualScrollView
    {
        private Vector2 _scrollPosition;
        private float _itemHeight;
        private int _visibleStartIndex;
        private int _visibleEndIndex;
        private readonly Dictionary<int, float> _itemHeights = new Dictionary<int, float>();
        private bool _useVariableHeight;

        public Vector2 ScrollPosition => _scrollPosition;
        public int VisibleStartIndex => _visibleStartIndex;
        public int VisibleEndIndex => _visibleEndIndex;

        public VirtualScrollView(float itemHeight = 20f, bool useVariableHeight = false)
        {
            _itemHeight = itemHeight;
            _useVariableHeight = useVariableHeight;
        }

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
                _visibleStartIndex = 0;
                _visibleEndIndex = totalItems - 1;

                float currentY = 0;
                for (int i = 0; i < totalItems; i++)
                {
                    float itemHeight = GetItemHeight(i);
                    if (currentY + itemHeight >= viewTop && _visibleStartIndex == 0)
                        _visibleStartIndex = i;

                    if (currentY > viewBottom)
                    {
                        _visibleEndIndex = i - 1;
                        break;
                    }
                    currentY += itemHeight;
                }
            }
            else
            {
                _visibleStartIndex = Mathf.Max(0, Mathf.FloorToInt(viewTop / _itemHeight) - 1);
                _visibleEndIndex = Mathf.Min(totalItems - 1, Mathf.CeilToInt(viewBottom / _itemHeight) + 1);
            }
        }
    }
}