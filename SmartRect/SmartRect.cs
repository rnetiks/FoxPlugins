using UnityEngine;

namespace SmartRectV0
{
    /// <summary>
    /// Represents a smart rectangle that provides advanced manipulation of a rectangle's dimensions and position.
    /// </summary>
    public class SmartRect
    {
        private const float DefaultOffsetX = 20;
        private const float DefaultOffsetY = 5f;

        public readonly float DefaultHeight;
        public readonly float DefaultWidth;
        public readonly float DefaultX;
        public readonly float DefaultY;

        private float _moveX;
        private float _moveY;
        private readonly float _offsetX;
        private readonly float _offsetY;
        private Rect _source;

        public float Height
        {
            get => _source.height;
            set
            {
                _source.height = value;
                _moveY = value + _offsetY;
            }
        }

        public float Width
        {
            get => _source.width;
            set
            {
                _source.width = value;
                _moveX = value + _offsetX;
            }
        }

        public float X
        {
            get => _source.x;
            set => _source.x = value;
        }

        public float Y
        {
            get => _source.y;
            set => _source.y = value;
        }

        /// <summary>
        /// Represents a smart rectangle that provides advanced manipulation of a rectangle's dimensions and position.
        /// </summary>
        /// <param name="src">The default <seealso cref="Rect"/> to use.</param>
        public SmartRect(Rect src) : this(src, DefaultOffsetX, DefaultOffsetY)
        {
        }


        /// <summary>
        /// Represents a smart rectangle that provides advanced manipulation of a rectangle's dimensions and position.
        /// </summary>
        /// <param name="src">The default <seealso cref="Rect"/> to use.</param>
        /// <param name="offX">The offset in pixels towards the X coordinate.</param>
        /// <param name="offY">The offset in pixels towards the Y coordinate.</param>
        public SmartRect(Rect src, float offX, float offY)
        {
            _source = new Rect(src.x, src.y, src.width, src.height);
            _offsetX = offX;
            _offsetY = offY;
            _moveX = _source.width + _offsetX;
            _moveY = _source.height + _offsetY;
            DefaultHeight = src.height;
            DefaultWidth = src.width;
            DefaultX = src.x;
            DefaultY = src.y;
        }

        public SmartRect(float x, float y, float width, float height) : this(new Rect(x, y, width, height))
        {
        }

        public SmartRect(float x, float y, float width, float height, float offX, float offY) : this(
            new Rect(x, y, width, height), offX, offY)
        {
        }

        public void BeginHorizontal(int elementCount)
        {
            Width = (Width - (_offsetX * (elementCount - 1))) / elementCount;
        }

        public SmartRect Move(Vector2 vec)
        {
            _source.x += vec.x;
            _source.y += vec.y;
            return this;
        }

        public SmartRect Move(int x, int y)
        {
            _source.x += x;
            _source.y += y;
            return this;
        }

        /// <summary>
        /// Moves the rectangle's X position by the specified offset and adjusts its width accordingly.
        /// </summary>
        /// <param name="off">The offset value to move the rectangle's X position.</param>
        public void MoveOffsetX(float off)
        {
            _source.x += off;
            _source.width -= off;
        }

        /// <summary>
        /// Adjusts the Y position and height of the smart rectangle by the specified offset.
        /// </summary>
        /// <param name="off">The offset to apply to the Y position and height.</param>
        public void MoveOffsetY(float off)
        {
            _source.y += off;
            _source.height -= off;
        }

        /// <summary>
        /// Adjusts the X position of the current rectangle to align with the right edge of the specified rectangle, taking into account the given width.
        /// </summary>
        /// <param name="box">The rectangle to align with.</param>
        /// <param name="width">The width to use for alignment.</param>
        public void MoveToEndX(Rect box, float width)
        {
            _source.x += box.x + box.width - _source.x - width;
        }

        /// <summary>
        /// Moves the Y position of the rectangle represented by the current SmartRect instance
        /// to the bottom of the specified 'box' plus the specified 'height'.
        /// </summary>
        /// <param name="box">The reference rectangle used to determine the new Y position.</param>
        /// <param name="height">The height to be considered when moving to the end.</param>
        public void MoveToEndY(Rect box, float height)
        {
            _source.y += box.y + box.height - _source.y - height;
        }

        /// <summary>
        /// Moves the rectangle horizontally by a predefined offset and returns the updated SmartRect instance.
        /// </summary>
        /// <returns>The updated SmartRect instance.</returns>
        public SmartRect MoveX()
        {
            _source.x += _moveX;
            return this;
        }

        public SmartRect MoveX(float off, bool considerWidth = false)
        {
            _source.x += off;
            if (considerWidth)
            {
                _source.x += _source.width;
            }

            return this;
        }

        /// <summary>
        /// Moves the <seealso cref="SmartRect"/> by it's own height.
        /// </summary>
        public void MoveY()
        {
            _source.y += _moveY;
        }

        /// <summary>
        /// Moves the <seealso cref="SmartRect"/> by a specified offset.
        /// </summary>
        /// <param name="offset">The amount to move the <seealso cref="SmartRect"/> by.</param>
        /// <param name="considerHeight">If true will also move the <seealso cref="SmartRect"/> by its own height, else only by <paramref name="offset"/></param>
        public void MoveY(float offset, bool considerHeight = false)
        {
            _source.y += offset;
            if (considerHeight)
            {
                _source.y += _source.height;
            }
        }

        public SmartRect NextColumn()
        {
            MoveX();
            return this;
        }

        /// <summary>
        /// Moves the rectangle to the next row, optionally resetting the column position.
        /// </summary>
        /// <param name="resetColumn">Indicates whether the column position should be reset to the default X position.</param>
        /// <returns>The updated instance of <see cref="SmartRect"/> after moving to the next row.</returns>
        public SmartRect NextRow(bool resetColumn = true)
        {
            if (resetColumn)
                _source.x = DefaultX;
            MoveY();
            return this;
        }

        /// <summary>
        /// Resets the SmartRect to its default position and dimensions.
        /// </summary>
        public void Reset()
        {
            _source.x = DefaultX;
            _source.y = DefaultY;
            Height = DefaultHeight;
            Width = DefaultWidth;
        }

        /// <summary>
        /// Resets the x-coordinate of the rectangle to its default value.
        /// Optionally resets the width to its default value.
        /// </summary>
        /// <param name="includeWidth">If true, the width will also be reset to its default value.</param>
        public void ResetX(bool includeWidth = true)
        {
            _source.x = DefaultX;
            if (includeWidth)
            {
                _source.width = DefaultWidth;
            }
        }

        /// <summary>
        /// Resets the y-coordinate of the rectangle to its default value.
        /// Optionally resets the height to its default value.
        /// </summary>
        /// <param name="includeHeight">If true, the height will also be reset to its default value.</param>
        public void ResetY(bool includeHeight = false)
        {
            _source.y = DefaultY;
            if (includeHeight)
            {
                _source.height = DefaultHeight;
            }
        }


        /// <summary>
        /// Converts the current SmartRect instance into a Rect object.
        /// </summary>
        /// <returns>A Rect object representing the current SmartRect.</returns>
        public Rect ToRect()
        {
            return _source;
        }

        /// <summary>
        /// Defines an implicit conversion from a SmartRect instance to a UnityEngine.Rect instance.
        /// </summary>
        /// <param name="r">The SmartRect instance to convert.</param>
        /// <returns>A Rect instance representing the same dimensions and position as the SmartRect.</returns>
        public static implicit operator Rect(SmartRect r)
        {
            return r._source;
        }
    }
}