using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartRectV0
{
    /// <summary>
    /// Represents a smart rectangle that provides advanced manipulation of a rectangle's dimensions and position.
    /// </summary>
    public class SmartRect
    {
        // Default offset values
        public static float DefaultOffsetX => 20;
        public static float DefaultOffsetY => 5f;

        // Default values for this instance
        public readonly float DefaultHeight;
        public readonly float DefaultWidth;
        public readonly float DefaultX;
        public readonly float DefaultY;

        // Core rect and offset properties
        private Rect _source;
        private readonly float _offsetX;
        private readonly float _offsetY;
        private float _moveX;
        private float _moveY;

        // Property getters and setters
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

        public float TotalWidth => Width + _offsetX;
        public float TotalHeight => Height + _offsetY;

        // Accessor for internal rectangle
        public Rect Rect => _source;

        /// <summary>
        /// Initializes a new instance of the SmartRect class using a specified rectangle.
        /// </summary>
        /// <param name="src">The default <see cref="Rect"/> to use.</param>
        public SmartRect(Rect src) : this(src, DefaultOffsetX, DefaultOffsetY)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SmartRect class using a specified rectangle and offsets.
        /// </summary>
        /// <param name="src">The default <see cref="Rect"/> to use.</param>
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

        /// <summary>
        /// Initializes a new instance of the SmartRect class using specified coordinates and dimensions.
        /// </summary>
        /// <param name="x">The x-coordinate of the rectangle.</param>
        /// <param name="y">The y-coordinate of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public SmartRect(float x, float y, float width, float height)
            : this(new Rect(x, y, width, height))
        {
        }

        /// <summary>
        /// Initializes a new instance of the SmartRect class using specified coordinates, dimensions, and offsets.
        /// </summary>
        /// <param name="x">The x-coordinate of the rectangle.</param>
        /// <param name="y">The y-coordinate of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="offX">The offset in pixels towards the X coordinate.</param>
        /// <param name="offY">The offset in pixels towards the Y coordinate.</param>
        public SmartRect(float x, float y, float width, float height, float offX, float offY)
            : this(new Rect(x, y, width, height), offX, offY)
        {
        }

        /// <summary>
        /// Creates a new animation controller for this SmartRect.
        /// </summary>
        /// <returns>A new animation controller for this SmartRect.</returns>
        public SmartRectAnimator CreateAnimator()
        {
            return new SmartRectAnimator(this);
        }

        /// <summary>
        /// Divides the current width of the rectangle into equal segments for horizontal layout.
        /// Adjusts the width of each segment based on the total number of elements and specified horizontal offsets.
        /// </summary>
        /// <param name="elementCount">The number of elements to divide the rectangle horizontally into.</param>
        public SmartRect BeginHorizontal(int elementCount)
        {
            Width = (Width - _offsetX * (elementCount - 1)) / elementCount;
            return this;
        }

        /// <summary>
        /// Synonymous to <see cref="ResetX"/>
        /// </summary>
        public SmartRect EndHorizontal()
        {
            return ResetX();
        }

        /// <summary>
        /// Moves the SmartRect by the specified vector values.
        /// </summary>
        /// <param name="vec">A <see cref="Vector2"/> specifying the change in X and Y coordinates.</param>
        /// <returns>The updated <see cref="SmartRect"/> after applying the movement.</returns>
        public SmartRect Move(Vector2 vec)
        {
            _source.x += vec.x;
            _source.y += vec.y;
            return this;
        }

        /// <summary>
        /// Moves the rectangle by the specified x and y offsets and returns the updated <see cref="SmartRect"/>.
        /// </summary>
        /// <param name="x">The offset to move the rectangle along the x-axis.</param>
        /// <param name="y">The offset to move the rectangle along the y-axis.</param>
        /// <returns>The updated <see cref="SmartRect"/> after being moved.</returns>
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
        public SmartRect MoveOffsetX(float off)
        {
            _source.x += off;
            _source.width -= off;
            return this;
        }

        /// <summary>
        /// Adjusts the Y position and height of the smart rectangle by the specified offset.
        /// </summary>
        /// <param name="off">The offset to apply to the Y position and height.</param>
        public SmartRect MoveOffsetY(float off)
        {
            _source.y += off;
            _source.height -= off;
            return this;
        }

        /// <summary>
        /// Adjusts the X position of the current rectangle to align with the right edge of the specified rectangle, taking into account the given width.
        /// </summary>
        /// <param name="box">The rectangle to align with.</param>
        /// <param name="width">The width to use for alignment.</param>
        public SmartRect MoveToEndX(Rect box, float width)
        {
            _source.x += box.x + box.width - _source.x - width;
            return this;
        }

        /// <summary>
        /// Moves the Y position of the rectangle represented by the current <see cref="SmartRect"/> instance
        /// to the bottom of the specified 'box' plus the specified 'height'.
        /// </summary>
        /// <param name="box">The reference rectangle used to determine the new Y position.</param>
        /// <param name="height">The height to be considered when moving to the end.</param>
        public SmartRect MoveToEndY(Rect box, float height)
        {
            _source.y += box.y + box.height - _source.y - height;
            return this;
        }

        /// <summary>
        /// Moves the rectangle horizontally by a predefined offset and returns the updated <see cref="SmartRect"/> instance.
        /// </summary>
        /// <returns>The updated <see cref="SmartRect"/> instance.</returns>
        public SmartRect MoveX()
        {
            _source.x += _moveX;
            return this;
        }

        /// <summary>
        /// Moves the rectangle along the X-axis by the specified offset and optionally considers its width during the move.
        /// </summary>
        /// <param name="off">The offset by which to move the rectangle along the X-axis.</param>
        /// <param name="considerWidth">Determines whether the rectangle's width should be added to the offset.</param>
        /// <returns>The current instance of <see cref="SmartRect"/> after applying the movement.</returns>
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
        /// Moves the <see cref="SmartRect"/> by it's own height.
        /// </summary>
        public SmartRect MoveY()
        {
            _source.y += _moveY;
            return this;
        }

        /// <summary>
        /// Moves the <see cref="SmartRect"/> by a specified offset.
        /// </summary>
        /// <param name="offset">The amount to move the <see cref="SmartRect"/> by.</param>
        /// <param name="considerHeight">If true will also move the <see cref="SmartRect"/> by its own height, else only by <paramref name="offset"/></param>
        public SmartRect MoveY(float offset, bool considerHeight = false)
        {
            _source.y += offset;
            if (considerHeight)
            {
                _source.y += _source.height;
            }

            return this;
        }

        /// <summary>
        /// Sets the width of the rectangle.
        /// </summary>
        /// <param name="width">The new width value.</param>
        /// <returns>The current instance for method chaining.</returns>
        public SmartRect SetWidth(float width)
        {
            _source.width = width;
            return this;
        }

        /// <summary>
        /// Sets the height of the rectangle.
        /// </summary>
        /// <param name="height">The new height value.</param>
        /// <returns>The current instance for method chaining.</returns>
        public SmartRect SetHeight(float height)
        {
            _source.height = height;
            return this;
        }

        /// <summary>
        /// Sets the width of the rectangle such that its right edge aligns with the specified x-coordinate.
        /// </summary>
        /// <param name="x">The x-coordinate to which the right edge of the rectangle should align.</param>
        /// <returns>The current instance of <see cref="SmartRect"/> to allow method chaining.</returns>
        public SmartRect WidthToEnd(float x)
        {
            _source.width = x - _source.x;
            return this;
        }

        /// <summary>
        /// Sets the height of the rectangle such that its bottom edge aligns with the specified y-coordinate.
        /// </summary>
        /// <param name="y">The y-coordinate to which the bottom edge of the rectangle should align.</param>
        /// <returns>The current instance of <see cref="SmartRect"/> to allow method chaining.</returns>
        public SmartRect HeightToEnd(float y)
        {
            _source.height = y - _source.y;
            return this;
        }

        /// <summary>
        /// Moves to the next column by shifting the rectangle horizontally by a predefined offset.
        /// </summary>
        /// <returns>An updated instance of <see cref="SmartRect"/> representing the next column.</returns>
        public SmartRect NextColumn()
        {
            MoveX();
            return this;
        }

        /// <summary>
        /// Creates a new SmartRect representing a column at the specified index.
        /// </summary>
        /// <param name="col">The column index.</param>
        /// <returns>A new SmartRect positioned at the specified column.</returns>
        public SmartRect Col(int col) =>
            new SmartRect(_source.x + _moveX * col, _source.y, _source.width, _source.height);

        /// <summary>
        /// Creates a new SmartRect representing a row at the specified index.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <returns>A new SmartRect positioned at the specified row.</returns>
        public SmartRect Row(int row) =>
            new SmartRect(_source.x, _source.y + _moveY * row, _source.width, _source.height);

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
        /// Resets the <see cref="SmartRect"/> to its default position and dimensions.
        /// </summary>
        public SmartRect Reset()
        {
            _source.x = DefaultX;
            _source.y = DefaultY;
            Height = DefaultHeight;
            Width = DefaultWidth;
            return this;
        }

        /// <summary>
        /// Resets the x-coordinate of the rectangle to its default value.
        /// Optionally resets the width to its default value.
        /// </summary>
        /// <param name="includeWidth">If true, the width will also be reset to its default value.</param>
        public SmartRect ResetX(bool includeWidth = true)
        {
            _source.x = DefaultX;
            if (includeWidth)
            {
                _source.width = DefaultWidth;
            }

            return this;
        }

        /// <summary>
        /// Resets the y-coordinate of the rectangle to its default value.
        /// Optionally resets the height to its default value.
        /// </summary>
        /// <param name="includeHeight">If true, the height will also be reset to its default value.</param>
        public SmartRect ResetY(bool includeHeight = false)
        {
            _source.y = DefaultY;
            if (includeHeight)
            {
                _source.height = DefaultHeight;
            }

            return this;
        }

        /// <summary>
        /// Converts the current <see cref="SmartRect"/> instance into a Rect object.
        /// </summary>
        /// <returns>A Rect object representing the current <see cref="SmartRect"/>.</returns>
        public Rect ToRect()
        {
            return _source;
        }

        /// <summary>
        /// Updates the internal rectangle with a new one.
        /// </summary>
        /// <param name="rect">The new rectangle to use.</param>
        internal void UpdateRect(Rect rect)
        {
            _source = rect;
        }

        /// <summary>
        /// Defines an implicit conversion from a <see cref="SmartRect"/> instance to a UnityEngine.Rect instance.
        /// </summary>
        /// <param name="r">The <see cref="SmartRect"/> instance to convert.</param>
        /// <returns>A Rect instance representing the same dimensions and position as the <see cref="SmartRect"/>.</returns>
        public static implicit operator Rect(SmartRect r)
        {
            return r._source;
        }
    }

    /// <summary>
    /// Provides animation capabilities for SmartRect objects.
    /// </summary>
    public class SmartRectAnimator
    {
        private readonly SmartRect _target;
        private Rect _animateFrom;
        private Rect _animateTo;
        private float _animationDuration;
        private float _elapsedTime;

        /// <summary>
        /// Gets whether the animator is currently animating.
        /// </summary>
        public bool IsAnimating => _animationDuration > 0;

        /// <summary>
        /// Initializes a new instance of the SmartRectAnimator class.
        /// </summary>
        /// <param name="target">The SmartRect to animate.</param>
        public SmartRectAnimator(SmartRect target)
        {
            _target = target;
        }

        /// <summary>
        /// Sets the starting and target rectangles, along with the duration for an animation.
        /// </summary>
        /// <param name="from">The starting <see cref="Rect"/> of the animation.</param>
        /// <param name="to">The target <see cref="Rect"/> to animate to.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>The current instance to allow method chaining.</returns>
        public SmartRectAnimator SetAnimation(Rect from, Rect to, float duration)
        {
            _elapsedTime = 0f;
            _animateFrom = from;
            _animateTo = to;
            _target.UpdateRect(from);
            _animationDuration = duration;
            return this;
        }

        /// <summary>
        /// Sets the target rectangle and duration for an animation, using the current rectangle as the starting point.
        /// </summary>
        /// <param name="to">The target <see cref="Rect"/> to animate to.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>The current instance to allow method chaining.</returns>
        public SmartRectAnimator AnimateTo(Rect to, float duration)
        {
            _elapsedTime = 0f;
            _animateFrom = _target.Rect;
            _animateTo = to;
            _animationDuration = duration;
            return this;
        }

        /// <summary>
        /// Updates the animation using a Bézier curve.
        /// </summary>
        /// <param name="bezier">The <see cref="BezierTemplate"/> to use for the animation curve.</param>
        /// <returns>
        /// A boolean indicating whether the animation is still in progress.
        /// Returns <c>true</c> if the animation is ongoing, <c>false</c> if the animation has completed.
        /// </returns>
        public bool Update(Beziers bezier)
        {
            if (_animationDuration <= 0)
                return false;

            var currentRect = _target.Rect;
            var xDiff = _animateTo.x - currentRect.x;
            var yDiff = _animateTo.y - currentRect.y;
            var widthDiff = _animateTo.width - currentRect.width;
            var heightDiff = _animateTo.height - currentRect.height;

            if (Math.Abs(xDiff) <= 0.01f && Math.Abs(yDiff) <= 0.01f &&
                Math.Abs(widthDiff) <= 0.01f && Math.Abs(heightDiff) <= 0.01f)
            {
                _target.UpdateRect(_animateTo);
                _animationDuration = 0;
                _elapsedTime = 0;
                return false;
            }

            // Expecting to be called 50 times per second
            var progress = Mathf.Clamp01(_elapsedTime / _animationDuration);
            var f = Beziers.Vector3(bezier, progress).y;

            var newRect = new Rect
            {
                x = Mathf.Ceil(f * (_animateTo.x - _animateFrom.x) + _animateFrom.x),
                y = Mathf.Ceil(f * (_animateTo.y - _animateFrom.y) + _animateFrom.y),
                width = Mathf.Ceil(f * (_animateTo.width - _animateFrom.width) + _animateFrom.width),
                height = Mathf.Ceil(f * (_animateTo.height - _animateFrom.height) + _animateFrom.height)
            };

            _target.UpdateRect(newRect);
            _elapsedTime += Time.deltaTime;

            var updateAnimation = progress < 1f;
            if (!updateAnimation)
            {
                _target.UpdateRect(_animateTo);
                _elapsedTime = 0;
                _animationDuration = 0;
            }

            return updateAnimation;
        }
    }

    /// <summary>
    /// Provides utility methods and predefined templates for Bezier curve calculations.
    /// </summary>
    public class Beziers
    {
        /// <summary>
        /// The starting point of the Bézier curve.
        /// </summary>
        public readonly Vector3 Start;

        /// <summary>
        /// The ending point of the Bézier curve.
        /// </summary>
        public readonly Vector3 End;

        /// <summary>
        /// The first control point that influences the curve's shape.
        /// </summary>
        public readonly Vector3 Control1;

        /// <summary>
        /// The second control point that influences the curve's shape.
        /// </summary>
        public readonly Vector3 Control2;

        public Beziers(Vector3 start, Vector3 control1, Vector3 control2, Vector3 end)
        {
            Start = start;
            Control1 = control1;
            Control2 = control2;
            End = end;
        }

        /// <summary>
        /// Calculates a point on a cubic Bézier curve at the specified normalized time.
        /// </summary>
        /// <param name="template">The Bézier curve template to use.</param>
        /// <param name="t">Normalized time parameter (0.0 to 1.0).</param>
        /// <returns>The interpolated point on the curve.</returns>
        public static Vector3 Vector3(Beziers template, float t)
        {
            return Vector3(template.Start, template.Control1, template.Control2, template.End, t);
        }

        /// <summary>
        /// Calculates a point on a cubic Bezier curve with the given control points at the specified normalized time.
        /// </summary>
        /// <param name="p0">The starting point.</param>
        /// <param name="p1">The first control point.</param>
        /// <param name="p2">The second control point.</param>
        /// <param name="p3">The ending point.</param>
        /// <param name="t">Normalized time parameter (0.0 to 1.0).</param>
        /// <returns>The interpolated point on the curve.</returns>
        public static Vector3 Vector3(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            float oneMinusTSquared = oneMinusT * oneMinusT;
            float oneMinusTCubed = oneMinusTSquared * oneMinusT;
            float tSquared = t * t;
            float tCubed = tSquared * t;

            return oneMinusTCubed * p0 +
                   3f * oneMinusTSquared * t * p1 +
                   3f * oneMinusT * tSquared * p2 +
                   tCubed * p3;
        }

        /// <summary>
        /// Converts the Bézier curve into a polygonal approximation with the specified number of segments.
        /// </summary>
        /// <param name="segments">The number of segments to divide the Bézier curve into.</param>
        /// <returns>A list of <see cref="UnityEngine.Vector3"/> points representing the polygonal approximation of the curve.</returns>
        public IList<Vector3> ToPoly(int segments)
        {
            Vector3[] points = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                points[i] = Vector3(Start, Control1, Control2, End, t);
            }

            return points;
        }

        /// <summary>
        /// Converts a polyline into a sequence of cubic Bézier curves that approximate the original shape.
        /// </summary>
        /// <param name="poly">The polyline to convert, represented as a list of Vector3 points. Must contain at least 3 points.</param>
        /// <returns>A list of Bézier objects representing the cubic Bézier curve segments.</returns>
        public static IList<Beziers> ToBezier(IList<Vector3> poly)
        {
            if (poly == null || poly.Count < 3)
                throw new ArgumentException("Polyline must contain at least 3 points", nameof(poly));

            List<Beziers> bezierCurves = new List<Beziers>();
            Vector3[] controlPoints = CalculateControlPoints(poly);

            for (int i = 0; i < poly.Count - 1; i++)
            {
                Vector3 p0 = poly[i];
                Vector3 p3 = poly[i + 1];
                Vector3 p1 = controlPoints[i * 2];
                Vector3 p2 = controlPoints[i * 2 + 1];

                bezierCurves.Add(new Beziers(p0, p1, p2, p3));
            }

            return bezierCurves;
        }

        /// <summary>
        /// Calculates control points for a sequence of cubic Bézier curves that pass through all points in the polyline.
        /// </summary>
        /// <param name="points">The polyline points.</param>
        /// <returns>An array of control points (two for each segment).</returns>
        private static Vector3[] CalculateControlPoints(IList<Vector3> points)
        {
            int n = points.Count - 1;
            Vector3[] result = new Vector3[n * 2];

            if (n == 1)
            {
                Vector3 direction = (points[1] - points[0]) / 3f;
                result[0] = points[0] + direction;
                result[1] = points[1] - direction;
                return result;
            }

            Vector3[] tangents = new Vector3[n + 1];

            for (int i = 1; i < n; i++)
            {
                tangents[i] = (points[i + 1] - points[i - 1]).normalized;
            }

            tangents[0] = (points[1] - points[0]).normalized;
            tangents[n] = (points[n] - points[n - 1]).normalized;

            for (int i = 0; i < n; i++)
            {
                float segmentLength = UnityEngine.Vector3.Distance(points[i], points[i + 1]) / 3f;
                result[i * 2] = points[i] + tangents[i] * segmentLength;
                result[i * 2 + 1] = points[i + 1] - tangents[i + 1] * segmentLength;
            }

            return result;
        }

        /// <summary>
        /// Reduces a given polygon's number of vertices to the specified number of segments.
        /// </summary>
        /// <param name="poly">The input list of vertices representing the polygon.</param>
        /// <param name="segments">The number of vertices to reduce the polygon to.</param>
        /// <returns>A new list of vertices representing the reduced polygon.</returns>
        public IList<Vector3> ReducePoly(IList<Vector3> poly, int segments)
        {
            if (poly.Count <= segments)
                return poly;

            Vector3[] result = new Vector3[segments];
            float step = (poly.Count - 1) / (float)(segments - 1);

            for (int i = 0; i < segments; i++)
            {
                float index = i * step;
                int floor = Mathf.FloorToInt(index);
                float t = index - floor;

                if (floor >= poly.Count - 1)
                {
                    result[i] = poly[poly.Count - 1];
                }
                else
                {
                    result[i] = UnityEngine.Vector3.Lerp(poly[floor], poly[floor + 1], t);
                }
            }

            return result;
        }

        /// <summary>
        /// Increases the number of points in the given polyline to match the specified number of segments, optionally smoothing the result.
        /// </summary>
        /// <param name="poly">The original polyline represented as a list of <see cref="UnityEngine.Vector3"/>.</param>
        /// <param name="segments">The desired number of segments in the resulting polyline. Must be greater than or equal to the number of points in the original polyline.</param>
        /// <param name="smooth">Determines whether smooth interpolation is applied to the new points in the polyline. Defaults to false.</param>
        /// <returns>
        /// A new polyline with the specified number of segments, represented as a list of <see cref="UnityEngine.Vector3"/>.
        /// </returns>
        public IList<Vector3> IncreasePoly(IList<Vector3> poly, int segments, bool smooth = false)
        {
            if (poly.Count >= segments)
                return poly;

            Vector3[] result = new Vector3[segments];
            float step = (poly.Count - 1) / (float)(segments - 1);

            for (int i = 0; i < segments; i++)
            {
                float index = i * step;
                int floor = Mathf.FloorToInt(index);
                float t = index - floor;

                if (floor >= poly.Count - 1)
                {
                    result[i] = poly[poly.Count - 1];
                }
                else if (smooth && floor > 0 && floor < poly.Count - 2)
                {
                    Vector3 p0 = poly[floor - 1];
                    Vector3 p1 = poly[floor];
                    Vector3 p2 = poly[floor + 1];
                    Vector3 p3 = poly[floor + 2];

                    float tSquared = t * t;
                    float tCubed = tSquared * t;

                    float q1 = -tCubed + 2.0f * tSquared - t;
                    float q2 = 3.0f * tCubed - 5.0f * tSquared + 2.0f;
                    float q3 = -3.0f * tCubed + 4.0f * tSquared + t;
                    float q4 = tCubed - tSquared;

                    result[i] = 0.5f * (q1 * p0 + q2 * p1 + q3 * p2 + q4 * p3);
                }
                else
                {
                    result[i] = UnityEngine.Vector3.Lerp(poly[floor], poly[floor + 1], t);
                }
            }

            return result;
        }

        /// <summary>
        /// A linear Bézier curve template with a constant rate of change.
        /// </summary>
        public static Beziers LinearTemplate { get; } = new Beziers(
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.one
        );

        /// <summary>
        /// A general-purpose ease curve with a slight initial acceleration.
        /// </summary>
        public static Beziers EaseTemplate { get; } = new Beziers(
            Vector2.zero,
            Vector2.one,
            new Vector2(0.25f, 0.1f),
            new Vector2(0.25f, 1f)
        );

        /// <summary>
        /// An ease-in curve that starts slow and accelerates.
        /// </summary>
        public static Beziers EaseInTemplate { get; } = new Beziers(
            Vector2.zero,
            Vector2.one,
            new Vector2(0.42f, 0f),
            Vector2.one
        );

        /// <summary>
        /// An ease-out curve that starts fast and decelerates.
        /// </summary>
        public static Beziers EaseOutTemplate { get; } = new Beziers(
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            new Vector2(0.58f, 1f)
        );

        /// <summary>
        /// An ease-in-out curve that accelerates in the middle and decelerates at the end.
        /// </summary>
        public static Beziers EaseInOutTemplate { get; } = new Beziers(
            Vector2.zero,
            Vector2.one,
            new Vector2(0.42f, 0),
            new Vector2(0.58f, 1)
        );
    }
}