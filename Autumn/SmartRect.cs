using Autumn.Attributes;
using UnityEngine;

namespace Autumn
{
    public class SmartRect
    {
        private const float DefaultOffsetX = 20;
        private const float DefaultOffsetY = 5f;

        public readonly float DefaultHeight;
        public readonly float DefaultWidth;
        public readonly float DefaultX;
        public readonly float DefaultY;

        private float moveX;
        private float moveY;
        private float offsetX;
        private float offsetY;
        private Rect source;

        public float height
        {
            get
            {
                return source.height;
            }
            set
            {
                source.height = value;
                moveY = value + offsetY;
            }
        }

        public float width
        {
            get
            {
                return source.width;
            }
            set
            {
                source.width = value;
                moveX = value + offsetX;
            }
        }

        public float x
        {
            get
            {
                return source.x;
            }
            set
            {
                source.x = value;
            }
        }
        
        public float y
        {
            get
            {
                return source.y;
            }
            set
            {
                source.y = value;
            }
        }

        public SmartRect(Rect src) : this(src, DefaultOffsetX, DefaultOffsetY)
        {
        }

        public SmartRect(Rect src, float offX, float offY)
        {
            source = new Rect(src.x, src.y, src.width, src.height);
            offsetX = offX;
            offsetY = offY;
            moveX = source.width + offsetX;
            moveY = source.height + offsetY;
            DefaultHeight = src.height;
            DefaultWidth = src.width;
            DefaultX = src.x;
            DefaultY = src.y;
        }

        public SmartRect(float x, float y, float width, float height) : this(new Rect(x, y, width, height))
        {
        }

        public SmartRect(float x, float y, float width, float height, float offX, float offY) : this(new Rect(x, y, width, height), offX, offY)
        {
        }

        public void BeginHorizontal(int elementCount)
        {
            width = (width - (offsetX * (elementCount - 1))) / elementCount;
        }

        public SmartRect Move(Vector2 vec)
        {
            source.x += vec.x;
            source.y += vec.y;
            return this;
        }

        public SmartRect Move(int x, int y)
        {
            source.x += x;
            source.y += y;
            return this;
        }

        public void MoveOffsetX(float off)
        {
            source.x += off;
            source.width -= off;
        }

        public void MoveOffsetY(float off)
        {
            source.y += off;
            source.height -= off;
        }

        public void MoveToEndX(Rect box, float width)
        {
            source.x += box.x + Style.WindowSideOffset + box.width - Style.WindowSideOffset * 2 - source.x - width;
        }

        public void MoveToEndY(Rect box, float height)
        {
            source.y += box.y + Style.WindowTopOffset + box.height - (Style.WindowTopOffset + Style.WindowBottomOffset) - source.y - height;
        }

        public SmartRect MoveX()
        {
            source.x += moveX;
            return this;
        }

        public SmartRect MoveX(float off, bool wid = false)
        {
            source.x += off;
            if (wid)
            {
                source.x += source.width;
            }

            return this;
        }

        /// <summary>
        /// Moves the <seealso cref="SmartRect"/> by it's own height.
        /// </summary>
        public void MoveY()
        {
            source.y += moveY;
        }

        /// <summary>
        /// Moves the <seealso cref="SmartRect"/> by a specified offset.
        /// </summary>
        /// <param name="offset">The amount to move the <seealso cref="SmartRect"/> by.</param>
        /// <param name="considerHeight">If true will also move the <seealso cref="SmartRect"/> by it's own height, else only by <paramref name="offset"/></param>
        public void MoveY(float offset, bool considerHeight = false)
        {
            source.y += offset;
            if (considerHeight)
            {
                source.y += source.height;
            }
        }

        public SmartRect NextColumn()
        {
            MoveX();
            return this;
        }

        public SmartRect NextRow(bool resetColumn = true)
        {
            if (resetColumn)
                source.x = DefaultX;
            MoveY();
            return this;
        }

        public void Reset()
        {
            source.x = DefaultX;
            source.y = DefaultY;
            height = DefaultHeight;
            width = DefaultWidth;
        }

        public void ResetX(bool includeWidth = true)
        {
            source.x = DefaultX;
            if (includeWidth)
            {
                source.width = DefaultWidth;
            }
        }

        public void ResetY(bool includeHeight = false)
        {
            source.y = DefaultY;
            if (includeHeight)
            {
                source.height = DefaultHeight;
            }
        }

        public Rect ToRect()
        {
            return source;
        }

        public static implicit operator Rect(SmartRect r)
        {
            return r.source;
        }
    }
}