using UnityEngine;

namespace Prototype.UIElements.Layout
{
    /// <summary>
    /// Represents a UI layout system that arranges elements in a grid format.
    /// </summary>
    public class GridLayout
    {
        private int _columns;
        private float _cellWidth, _cellHeight;
        private float _spacing;

        public GridLayout(int columns, float cellWidth, float cellHeight, float spacing = 2f)
        {
            _columns = columns;
            _cellWidth = cellWidth;
            _cellHeight = cellHeight;
            _spacing = spacing;
        }

        public Rect GetCellRect(Rect containerRect, int index)
        {
            int row = index / _columns;
            int col = index % _columns;

            float x = containerRect.x + col * (_cellWidth + _spacing);
            float y = containerRect.y + row * (_cellHeight + _spacing);

            return new Rect(x, y, _cellWidth, _cellHeight);
        }

        public Vector2 GetRequiredSize(int itemCount)
        {
            int rows = Mathf.CeilToInt((float)itemCount / _columns);
            float width = _columns * _cellWidth + (_columns - 1) * _spacing;
            float height = rows * _cellHeight + (rows - 1) * _spacing;
            return new Vector2(width, height);
        }
    }
}