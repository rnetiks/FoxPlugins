using System;
using System.Linq;
using UnityEngine;

namespace Compositor.KK.Utils
{
    public static class Extensions
    {
        #region RECT::Resize

        /// Resizes the given Rect by setting its width and height to the specified values.
        /// <param name="rect">The Rect to be resized.</param>
        /// <param name="width">The new width for the Rect.</param>
        /// <param name="height">The new height for the Rect.</param>
        /// <returns>The resized Rect with updated width and height.</returns>
        public static Rect Resize(this Rect rect, float width, float height)
        {
            rect.width = width;
            rect.height = height;
            return rect;
        }

        public static Rect ResizeCopy(this Rect rect, float width, float height)
        {
            return new Rect(rect.x, rect.y, width, height);
        }

        public static Rect Resize(this Rect rect, Vector2 size)
        {
            rect.width = size.x;
            rect.height = size.y;
            return rect;
        }

        /// Creates a resized copy of the given Rect with the specified width and height values, without altering the original Rect.
        /// <param name="rect">The original Rect to resize.</param>
        /// <param name="width">The desired width for the new Rect.</param>
        /// <param name="height">The desired height for the new Rect.</param>
        /// <returns>A new Rect instance with the updated width and height.</returns>
        public static Rect ResizeCopy(this Rect rect, Vector2 size)
        {
            return new Rect(rect.x, rect.y, size.x, size.y);
        }

        public static Rect ResizeX(this Rect rect, float width, AlignmentX alignment = AlignmentX.Left)
        {
            var oldWidth = rect.width;

            switch (alignment)
            {

                case AlignmentX.Left:
                    rect.width = width;
                    break;
                case AlignmentX.Center:
                    rect.width = width;
                    rect.x += (oldWidth - width) / 2;
                    break;
                case AlignmentX.Right:
                    rect.width = width;
                    rect.x += oldWidth - width;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);
            }

            return rect;
        }

        public static Rect ResizeXCopy(this Rect rect, float width, AlignmentX alignment = AlignmentX.Left)
        {
            var oldWidth = rect.width;

            switch (alignment)
            {
                case AlignmentX.Left:
                    return new Rect(rect.x, rect.y, width, rect.height);
                case AlignmentX.Center:
                    return new Rect(rect.x + (oldWidth - width) / 2, rect.y, width, rect.height);
                case AlignmentX.Right:
                    return new Rect(rect.x + oldWidth - width, rect.y, width, rect.height);
                default:
                    throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);
            }
        }

        /// <summary>
        /// Resizes the height of the given rect and adjusts its position based on the specified vertical alignment.
        /// </summary>
        /// <param name="rect">The rectangular area to resize.</param>
        /// <param name="height">The new height to apply to the rect.</param>
        /// <param name="alignment">Specifies how to align the rect vertically with respect to the original height. Defaults to top-aligned.</param>
        /// <returns>A <see cref="Rect"/> object with the updated height and position based on the specified alignment.</returns>
        public static Rect ResizeY(this Rect rect, float height, AlignmentY alignment = AlignmentY.Top)
        {
            var oldHeight = rect.height;

            switch (alignment)
            {
                case AlignmentY.Top:
                    rect.height = height;
                    break;
                case AlignmentY.Center:
                    rect.height = height;
                    rect.y += (oldHeight - height) / 2;
                    break;
                case AlignmentY.Bottom:
                    rect.height = height;
                    rect.y += oldHeight - height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);
            }

            return rect;
        }

        public static Rect ResizeYCopy(this Rect rect, float height, AlignmentY alignment = AlignmentY.Top)
        {
            var oldHeight = rect.height;

            switch (alignment)
            {
                case AlignmentY.Top:
                    return new Rect(rect.x, rect.y, rect.width, height);
                case AlignmentY.Center:
                    return new Rect(rect.x, rect.y + (oldHeight - height) / 2, rect.width, height);
                case AlignmentY.Bottom:
                    return new Rect(rect.x, rect.y + oldHeight - height, rect.width, height);
                default:
                    throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);
            }
        }

        #endregion

        #region RECT::Move

        public static Rect Move(this Rect rect, Vector2 offset)
        {
            rect.x += offset.x;
            rect.y += offset.y;
            return rect;
        }

        /// <summary>
        /// Creates a new <c>Rect</c> by moving the specified rectangle by the given offset.
        /// </summary>
        /// <param name="rect">The rectangle to be moved.</param>
        /// <param name="offset">The offset by which the rectangle is to be moved.</param>
        /// <returns>A new <c>Rect</c> that is the result of moving the original rectangle by the specified offset.</returns>
        public static Rect MoveCopy(this Rect rect, Vector2 offset)
        {
            return new Rect(rect.x + offset.x, rect.y + offset.y, rect.width, rect.height);
        }

        #endregion

        #region Vect2::Move

        public static Vector2 Move(this Vector2 vector, Vector2 offset)
        {
            vector.x += offset.x;
            vector.y += offset.y;
            return vector;
        }
        
        /// <summary>
        /// Moves the specified Vector2 by applying a specified offset
        /// </summary>
        /// <param name="vector">The <see cref="Vector2"/> to move</param>
        /// <param name="offset">A <see cref="Vector2"/> specified offset to apply to the object</param>
        /// <returns>A new <see cref="Vector2"/> object with the new position applied</returns>
        public static Vector2 MoveCopy(this Vector2 vector, Vector2 offset)
        {
            return new Vector2(vector.x + offset.x, vector.y + offset.y);
        }
        
        public static Vector2 Move(this Vector2 vector, float x, float y)
        {
            vector.x += x;
            vector.y += y;
            return vector;
        }

        #endregion

        #region STRING

        /// <summary>
        /// Determines whether the specified string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to check.</param>
        /// <returns>True if the string is null, empty, or contains only white-space characters; otherwise, false.</returns>
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrEmpty(value) || value.All(char.IsWhiteSpace);
        }

        #endregion

        #region ALIGNMENT

        /// <summary>
        /// Specifies horizontal alignment options.
        /// </summary>
        public enum AlignmentX
        {
            Left,
            Center,
            /// <summary>
            /// Specifies that the alignment should
            Right
        }

        /// <summary>
        /// Specifies vertical alignment options.
        /// </summary>
        public enum AlignmentY
        {
            Top,
            Center,
            /// <summary>
            /// Represents
            Bottom
        }

        #endregion
    }
}