using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace TexFac
{
    /// <summary>
    /// Converts SVG files to PNG format with color manipulation capabilities
    /// </summary>
    public class Svg
    {
        #region Fields

        private Dictionary<string, string> colorReplacements;
        private XmlDocument svgDoc;
        private float scaleX = 1f;
        private float scaleY = 1f;

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Converts SVG content string to a Bitmap with optional color replacements
        /// </summary>
        /// <param name="svgContent">SVG content as string</param>
        /// <param name="width">Output width in pixels (0 to use original SVG width)</param>
        /// <param name="height">Output height in pixels (0 to use original SVG height)</param>
        /// <param name="colorReplacements">Dictionary of color replacements (original -> replacement)</param>
        /// <returns>Bitmap object (caller is responsible for disposing)</returns>
        public static Bitmap ConvertSVG(
            string svgContent,
            int width = 0,
            int height = 0,
            Dictionary<string, string> colorReplacements = null)
        {
            Svg svg = new Svg();
            svg.colorReplacements = colorReplacements ?? new Dictionary<string, string>();

            svg.svgDoc = new XmlDocument();
            svg.svgDoc.LoadXml(svgContent);

            XmlElement svgElement = svg.svgDoc.DocumentElement;
            int svgWidth = width;
            int svgHeight = height;

            int originalWidth = 100;
            int originalHeight = 100;
            float viewBoxX = 0, viewBoxY = 0, viewBoxWidth = 0, viewBoxHeight = 0;

            string viewBox = svgElement.GetAttribute("viewBox");
            if (!string.IsNullOrEmpty(viewBox))
            {
                float[] values = svg.ParseFloatArray(viewBox);
                if (values.Length == 4)
                {
                    viewBoxX = values[0];
                    viewBoxY = values[1];
                    viewBoxWidth = values[2];
                    viewBoxHeight = values[3];
                    originalWidth = (int)viewBoxWidth;
                    originalHeight = (int)viewBoxHeight;
                }
            }
            else
            {
                originalWidth = svg.GetDimension(svgElement, "width", 100);
                originalHeight = svg.GetDimension(svgElement, "height", 100);
            }

            if (width == 0 || height == 0)
            {
                svgWidth = originalWidth;
                svgHeight = originalHeight;

                if (width > 0 && height == 0)
                {
                    svgHeight = (int)(svgHeight * ((float)width / svgWidth));
                    svgWidth = width;
                }
                else if (height > 0 && width == 0)
                {
                    svgWidth = (int)(svgWidth * ((float)height / svgHeight));
                    svgHeight = height;
                }
            }

            svg.scaleX = (float)svgWidth / originalWidth;
            svg.scaleY = (float)svgHeight / originalHeight;

            Bitmap bitmap = new Bitmap(svgWidth, svgHeight);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                if (!string.IsNullOrEmpty(viewBox) && viewBoxWidth > 0)
                {
                    g.TranslateTransform(-viewBoxX * svg.scaleX, -viewBoxY * svg.scaleY);
                }

                svg.RenderElement(g, svgElement);
            }

            return bitmap;
        }

        /// <summary>
        /// Converts SVG content string to PNG byte array with optional color replacements
        /// </summary>
        /// <param name="svgContent">SVG content as string</param>
        /// <param name="width">Output width in pixels (0 to use original SVG width)</param>
        /// <param name="height">Output height in pixels (0 to use original SVG height)</param>
        /// <param name="colorReplacements">Dictionary of color replacements (original -> replacement)</param>
        /// <returns>PNG file as a byte array</returns>
        public static byte[] ConvertSVG(
            string svgContent,
            ImageFormat format,
            int width = 0,
            int height = 0,
            Dictionary<string, string> colorReplacements = null)
        {
            using (Bitmap bitmap = ConvertSVG(svgContent, width, height, colorReplacements))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, format);
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Same as <seealso cref="SvgContentToPngBytes"/> but with added ability to select image format
        /// </summary>
        /// <returns>Image file as byte array</returns>
        public static byte[] SvgToImage(string svgContent,  ImageFormat format, int width = 0, int height = 0, Dictionary<string, string> colorReplacements = null)
        {
            using (Bitmap bitmap = ConvertSVG(svgContent, width, height, colorReplacements))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, format);
                    return ms.ToArray();
                }
            }
        }

        #endregion

        #region Private Rendering Methods

        /// <summary>
        /// Renders an SVG element and its children to the graphics context
        /// </summary>
        /// <param name="g">Graphics context</param>
        /// <param name="element">XML element to render</param>
        private void RenderElement(Graphics g, XmlElement element)
        {
            if (element == null) return;

            switch (element.LocalName.ToLower())
            {
                case "svg":
                case "g":
                    foreach (XmlNode child in element.ChildNodes)
                    {
                        if (child is XmlElement)
                            RenderElement(g, (XmlElement)child);
                    }
                    break;

                case "rect":
                    RenderRect(g, element);
                    break;

                case "circle":
                    RenderCircle(g, element);
                    break;

                case "ellipse":
                    RenderEllipse(g, element);
                    break;

                case "line":
                    RenderLine(g, element);
                    break;

                case "polyline":
                case "polygon":
                    RenderPolygon(g, element, element.LocalName.ToLower() == "polygon");
                    break;

                case "path":
                    RenderPath(g, element);
                    break;
                case "text":
                    RenderText(g, element);
                    break;
            }
        }

        // TODO Add support for tspan
        private void RenderText(Graphics g, XmlElement element)
        {
            float x = ParseFloat(element.GetAttribute("x")) * scaleX;
            float y = ParseFloat(element.GetAttribute("y")) * scaleY;

            string fontFamily = GetStyleAttribute(element, "font-family", "Arial").Trim().Trim('\'', '"');
            float fontSize = ParseFontSize(GetStyleAttribute(element, "font-size", "12"));
            string weightStr = GetStyleAttribute(element, "font-weight", "normal");
            string styleStr = GetStyleAttribute(element, "font-style", "normal");

            FontStyle fontStyle = FontStyle.Regular;
            if (weightStr == "bold" || weightStr == "700" || int.TryParse(weightStr, out int w) && w >= 700)
                fontStyle |= FontStyle.Bold;
            if (styleStr == "italic" || styleStr == "oblique")
                fontStyle |= FontStyle.Italic;

            string decoraton = GetStyleAttribute(element, "text-decoration", "none");
            if (decoraton.Contains("underline"))
                fontStyle |= FontStyle.Underline;
            if (decoraton.Contains("line-through"))
                fontStyle |= FontStyle.Strikeout;

            string anchor = GetStyleAttribute(element, "text-anchor", "start");

            string text = element.InnerText;

            using (Font font = new Font(fontFamily, fontSize, fontStyle, GraphicsUnit.Pixel))
            {
                float drawX = x;
                if (anchor == "middle" || anchor == "end")
                {
                    SizeF size = g.MeasureString(text, font);
                    drawX = anchor == "middle" ? x - size.Width / 2f : x - size.Width;
                }

                Brush fillBrush = GetFillBrush(element);
                bool ownsBrush = fillBrush != null;
                if (!ownsBrush) fillBrush = Brushes.Black;

                float ascentOffset = font.Size * font.FontFamily.GetCellAscent(fontStyle) / font.FontFamily.GetEmHeight(fontStyle);

                g.DrawString(text, font, fillBrush, drawX, y - ascentOffset);
                if (ownsBrush) fillBrush.Dispose();
            }
        }
        private float ParseFontSize(string value)
        {
            if (string.IsNullOrEmpty(value)) return 12f;
            value = value.Trim();

            switch (value)
            {
                case "xx-small":
                    return 7f;
                case "x-small":
                    return 9f;
                case "small":
                    return 10f;
                case "medium":
                    return 12f;
                case "large":
                    return 14f;
                case "x-large":
                    return 18f;
                case "xx-large":
                    return 24f;
                default:
                    switch (value)
                    {
                        case var _ when value.EndsWith("px"):
                            return float.Parse(value.Substring(0, value.Length - 2));
                        case var _ when value.EndsWith("pt"):
                            return float.Parse(value.Substring(0, value.Length - 2)) * 1.333f;
                        case var _ when value.EndsWith("rem"):
                            return float.Parse(value.Substring(0, value.Length - 3)) * 12f;
                        case var _ when value.EndsWith("em"):
                            return float.Parse(value.Substring(0, value.Length - 2)) * 12f; // relative to parent, simplified
                        case var _ when value.EndsWith("%"):
                            return float.Parse(value.Substring(0, value.Length - 1)) / 100f * 12f;
                        default:
                            return float.TryParse(value, out float f) ? f : 12f;
                    }
            }
        }

        /// <summary>
        /// Renders an SVG circle element
        /// </summary>
        /// <param name="g">Graphics context</param>
        /// <param name="element">Circle element</param>
        private void RenderCircle(Graphics g, XmlElement element)
        {
            float cx = ParseFloat(element.GetAttribute("cx")) * scaleX;
            float cy = ParseFloat(element.GetAttribute("cy")) * scaleY;
            float r = ParseFloat(element.GetAttribute("r")) * scaleX;

            Brush fillBrush = GetFillBrush(element);
            Pen strokePen = GetStrokePen(element);

            if (fillBrush != null)
                g.FillEllipse(fillBrush, cx - r, cy - r, r * 2, r * 2);
            if (strokePen != null)
                g.DrawEllipse(strokePen, cx - r, cy - r, r * 2, r * 2);

            if (fillBrush != null) fillBrush.Dispose();
            if (strokePen != null) strokePen.Dispose();
        }

        /// <summary>
        /// Renders an SVG ellipse element
        /// </summary>
        /// <param name="g">Graphics context</param>
        /// <param name="element">Ellipse element</param>
        private void RenderEllipse(Graphics g, XmlElement element)
        {
            float cx = ParseFloat(element.GetAttribute("cx")) * scaleX;
            float cy = ParseFloat(element.GetAttribute("cy")) * scaleY;
            float rx = ParseFloat(element.GetAttribute("rx")) * scaleX;
            float ry = ParseFloat(element.GetAttribute("ry")) * scaleY;

            Brush fillBrush = GetFillBrush(element);
            Pen strokePen = GetStrokePen(element);

            if (fillBrush != null)
                g.FillEllipse(fillBrush, cx - rx, cy - ry, rx * 2, ry * 2);
            if (strokePen != null)
                g.DrawEllipse(strokePen, cx - rx, cy - ry, rx * 2, ry * 2);

            if (fillBrush != null) fillBrush.Dispose();
            if (strokePen != null) strokePen.Dispose();
        }

        /// <summary>
        /// Renders an SVG line element
        /// </summary>
        /// <param name="g">Graphics context</param>
        /// <param name="element">Line element</param>
        private void RenderLine(Graphics g, XmlElement element)
        {
            float x1 = ParseFloat(element.GetAttribute("x1")) * scaleX;
            float y1 = ParseFloat(element.GetAttribute("y1")) * scaleY;
            float x2 = ParseFloat(element.GetAttribute("x2")) * scaleX;
            float y2 = ParseFloat(element.GetAttribute("y2")) * scaleY;

            Pen strokePen = GetStrokePen(element);
            if (strokePen != null)
            {
                g.DrawLine(strokePen, x1, y1, x2, y2);
                strokePen.Dispose();
            }
        }

        /// <summary>
        /// Renders an SVG path element
        /// </summary>
        /// <param name="g">Graphics context</param>
        /// <param name="element">Path element</param>
        private void RenderPath(Graphics g, XmlElement element)
        {
            string d = element.GetAttribute("d");
            if (string.IsNullOrEmpty(d)) return;

            using (GraphicsPath path = ParsePathData(d))
            {
                if (path == null) return;

                Brush fillBrush = GetFillBrush(element);
                Pen strokePen = GetStrokePen(element);

                if (fillBrush != null)
                    g.FillPath(fillBrush, path);
                if (strokePen != null)
                    g.DrawPath(strokePen, path);

                if (fillBrush != null) fillBrush.Dispose();
                if (strokePen != null) strokePen.Dispose();
            }
        }

        /// <summary>
        /// Renders an SVG polygon or polyline element
        /// </summary>
        /// <param name="g">Graphics context</param>
        /// <param name="element">Polygon or polyline element</param>
        /// <param name="closePath">True for polygon, false for polyline</param>
        private void RenderPolygon(Graphics g, XmlElement element, bool closePath)
        {
            string pointsStr = element.GetAttribute("points");
            if (string.IsNullOrEmpty(pointsStr)) return;

            List<PointF> points = ParsePoints(pointsStr);
            if (points.Count < 2) return;

            PointF[] pointArray = points.ToArray();

            Brush fillBrush = GetFillBrush(element);
            Pen strokePen = GetStrokePen(element);

            if (closePath && fillBrush != null)
                g.FillPolygon(fillBrush, pointArray);

            if (strokePen != null)
            {
                if (closePath)
                    g.DrawPolygon(strokePen, pointArray);
                else
                    g.DrawLines(strokePen, pointArray);
            }

            if (fillBrush != null) fillBrush.Dispose();
            if (strokePen != null) strokePen.Dispose();
        }

        /// <summary>
        /// Renders an SVG rectangle element
        /// </summary>
        /// <param name="g">Graphics context</param>
        /// <param name="element">Rectangle element</param>
        private void RenderRect(Graphics g, XmlElement element)
        {
            float x = ParseFloat(element.GetAttribute("x")) * scaleX;
            float y = ParseFloat(element.GetAttribute("y")) * scaleY;
            float width = ParseFloat(element.GetAttribute("width")) * scaleX;
            float height = ParseFloat(element.GetAttribute("height")) * scaleY;
            float rx = ParseFloat(element.GetAttribute("rx")) * scaleX;
            float ry = ParseFloat(element.GetAttribute("ry")) * scaleY;

            if (ry == 0) ry = rx;
            if (rx == 0) rx = ry;

            Brush fillBrush = GetFillBrush(element);
            Pen strokePen = GetStrokePen(element);

            if (rx > 0 || ry > 0)
            {
                using (GraphicsPath path = CreateRoundedRectPath(x, y, width, height, rx, ry))
                {
                    if (fillBrush != null)
                        g.FillPath(fillBrush, path);
                    if (strokePen != null)
                        g.DrawPath(strokePen, path);
                }
            }
            else
            {
                if (fillBrush != null)
                    g.FillRectangle(fillBrush, x, y, width, height);
                if (strokePen != null)
                    g.DrawRectangle(strokePen, x, y, width, height);
            }

            if (fillBrush != null) fillBrush.Dispose();
            if (strokePen != null) strokePen.Dispose();
        }

        #endregion

        #region Path Parsing Methods

        /// <summary>
        /// Adds an elliptical arc to a graphics path using Bezier curve approximation
        /// </summary>
        /// <param name="path">Graphics path to add arc to</param>
        /// <param name="start">Start point of arc</param>
        /// <param name="end">End point of arc</param>
        /// <param name="rx">X radius</param>
        /// <param name="ry">Y radius</param>
        /// <param name="xAxisRotation">X-axis rotation in degrees</param>
        /// <param name="largeArc">Large arc flag</param>
        /// <param name="sweep">Sweep direction flag</param>
        private void AddArcToBezier(GraphicsPath path, PointF start, PointF end,
            float rx, float ry, float xAxisRotation, bool largeArc, bool sweep)
        {
            if (start.X == end.X && start.Y == end.Y)
                return;

            if (rx == 0 || ry == 0)
            {
                path.AddLine(start, end);
                return;
            }

            rx = Math.Abs(rx);
            ry = Math.Abs(ry);

            double phi = xAxisRotation * Math.PI / 180.0;
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);

            double dx = (start.X - end.X) / 2.0;
            double dy = (start.Y - end.Y) / 2.0;
            double x1p = cosPhi * dx + sinPhi * dy;
            double y1p = -sinPhi * dx + cosPhi * dy;

            double lambda = (x1p * x1p) / (rx * rx) + (y1p * y1p) / (ry * ry);
            if (lambda > 1)
            {
                rx *= (float)Math.Sqrt(lambda);
                ry *= (float)Math.Sqrt(lambda);
            }

            double sq = Math.Max(0, (rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p) /
                                    (rx * rx * y1p * y1p + ry * ry * x1p * x1p));
            double coPrime = Math.Sqrt(sq);
            if (largeArc == sweep)
                coPrime = -coPrime;

            double cxp = coPrime * rx * y1p / ry;
            double cyp = -coPrime * ry * x1p / rx;

            double cx = cosPhi * cxp - sinPhi * cyp + (start.X + end.X) / 2.0;
            double cy = sinPhi * cxp + cosPhi * cyp + (start.Y + end.Y) / 2.0;

            double theta1 = Math.Atan2((y1p - cyp) / ry, (x1p - cxp) / rx);
            double theta2 = Math.Atan2((-y1p - cyp) / ry, (-x1p - cxp) / rx);
            double dtheta = theta2 - theta1;

            if (sweep && dtheta < 0)
                dtheta += 2 * Math.PI;
            else if (!sweep && dtheta > 0)
                dtheta -= 2 * Math.PI;

            int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(dtheta) / (Math.PI / 2)));
            double dthetaSegment = dtheta / segments;

            double alpha = Math.Sin(dthetaSegment) * (Math.Sqrt(4 + 3 * Math.Tan(dthetaSegment / 2) * Math.Tan(dthetaSegment / 2)) - 1) / 3;

            PointF currentPt = start;
            double currentTheta = theta1;

            for (int i = 0; i < segments; i++)
            {
                double nextTheta = currentTheta + dthetaSegment;

                double cosStart = Math.Cos(currentTheta);
                double sinStart = Math.Sin(currentTheta);
                double cosEnd = Math.Cos(nextTheta);
                double sinEnd = Math.Sin(nextTheta);

                PointF p1 = new PointF(
                    (float)(cosPhi * rx * cosStart - sinPhi * ry * sinStart + cx),
                    (float)(sinPhi * rx * cosStart + cosPhi * ry * sinStart + cy)
                );

                PointF p2 = new PointF(
                    (float)(cosPhi * rx * cosEnd - sinPhi * ry * sinEnd + cx),
                    (float)(sinPhi * rx * cosEnd + cosPhi * ry * sinEnd + cy)
                );

                PointF q1 = new PointF(
                    (float)(p1.X + alpha * (-cosPhi * rx * sinStart - sinPhi * ry * cosStart)),
                    (float)(p1.Y + alpha * (-sinPhi * rx * sinStart + cosPhi * ry * cosStart))
                );

                PointF q2 = new PointF(
                    (float)(p2.X - alpha * (-cosPhi * rx * sinEnd - sinPhi * ry * cosEnd)),
                    (float)(p2.Y - alpha * (-sinPhi * rx * sinEnd + cosPhi * ry * cosEnd))
                );

                path.AddBezier(currentPt, q1, q2, p2);
                currentPt = p2;
                currentTheta = nextTheta;
            }
        }

        /// <summary>
        /// Parses SVG path data string into a GraphicsPath
        /// </summary>
        /// <param name="d">Path data string</param>
        /// <returns>GraphicsPath representing the path</returns>
        private GraphicsPath ParsePathData(string d)
        {
            GraphicsPath path = new GraphicsPath();

            Regex commandRegex = new Regex(@"([MmLlHhVvCcSsQqTtAaZz])\s*([^MmLlHhVvCcSsQqTtAaZz]*)");
            MatchCollection matches = commandRegex.Matches(d);

            PointF currentPoint = new PointF(0, 0);
            PointF startPoint = new PointF(0, 0);
            PointF lastControlPoint = new PointF(0, 0);
            bool hasLastControlPoint = false;

            foreach (Match match in matches)
            {
                char command = match.Groups[1].Value[0];
                string paramStr = match.Groups[2].Value.Trim();
                float[] parameters = ParseFloatArray(paramStr);

                bool isRelative = char.IsLower(command);
                char upperCommand = char.ToUpper(command);

                switch (upperCommand)
                {
                    case 'M':
                        for (int i = 0; i < parameters.Length; i += 2)
                        {
                            if (i + 1 >= parameters.Length) break;

                            float x = parameters[i];
                            float y = parameters[i + 1];

                            PointF newPoint;

                            if (i == 0)
                            {
                                if (isRelative)
                                {
                                    newPoint = new PointF(
                                        currentPoint.X + x * scaleX,
                                        currentPoint.Y + y * scaleY
                                    );
                                }
                                else
                                {
                                    newPoint = new PointF(x * scaleX, y * scaleY);
                                }

                                currentPoint = newPoint;
                                startPoint = currentPoint;
                                path.StartFigure();
                            }
                            else
                            {
                                if (isRelative)
                                {
                                    newPoint = new PointF(
                                        currentPoint.X + x * scaleX,
                                        currentPoint.Y + y * scaleY
                                    );
                                }
                                else
                                {
                                    newPoint = new PointF(x * scaleX, y * scaleY);
                                }

                                path.AddLine(currentPoint, newPoint);
                                currentPoint = newPoint;
                            }
                        }
                        hasLastControlPoint = false;
                        break;

                    case 'L':
                        for (int i = 0; i < parameters.Length; i += 2)
                        {
                            if (i + 1 >= parameters.Length) break;

                            float x = parameters[i];
                            float y = parameters[i + 1];

                            PointF target = isRelative
                                ? new PointF(currentPoint.X + x * scaleX, currentPoint.Y + y * scaleY)
                                : new PointF(x * scaleX, y * scaleY);

                            path.AddLine(currentPoint, target);
                            currentPoint = target;
                        }
                        hasLastControlPoint = false;
                        break;

                    case 'H':
                        foreach (float x in parameters)
                        {
                            float targetX = isRelative ? currentPoint.X + x * scaleX : x * scaleX;

                            path.AddLine(currentPoint, new PointF(targetX, currentPoint.Y));
                            currentPoint.X = targetX;
                        }
                        hasLastControlPoint = false;
                        break;

                    case 'V':
                        foreach (float y in parameters)
                        {
                            float targetY = isRelative ? currentPoint.Y + y * scaleY : y * scaleY;

                            path.AddLine(currentPoint, new PointF(currentPoint.X, targetY));
                            currentPoint.Y = targetY;
                        }
                        hasLastControlPoint = false;
                        break;

                    case 'C':
                        for (int i = 0; i < parameters.Length; i += 6)
                        {
                            if (i + 5 >= parameters.Length) break;

                            float x1 = parameters[i];
                            float y1 = parameters[i + 1];
                            float x2 = parameters[i + 2];
                            float y2 = parameters[i + 3];
                            float x = parameters[i + 4];
                            float y = parameters[i + 5];

                            PointF cp1, cp2, endPoint;

                            if (isRelative)
                            {
                                cp1 = new PointF(currentPoint.X + x1 * scaleX, currentPoint.Y + y1 * scaleY);
                                cp2 = new PointF(currentPoint.X + x2 * scaleX, currentPoint.Y + y2 * scaleY);
                                endPoint = new PointF(currentPoint.X + x * scaleX, currentPoint.Y + y * scaleY);
                            }
                            else
                            {
                                cp1 = new PointF(x1 * scaleX, y1 * scaleY);
                                cp2 = new PointF(x2 * scaleX, y2 * scaleY);
                                endPoint = new PointF(x * scaleX, y * scaleY);
                            }

                            path.AddBezier(currentPoint, cp1, cp2, endPoint);
                            lastControlPoint = cp2;
                            hasLastControlPoint = true;
                            currentPoint = endPoint;
                        }
                        break;

                    case 'S':
                        for (int i = 0; i < parameters.Length; i += 4)
                        {
                            if (i + 3 >= parameters.Length) break;

                            PointF cp1;
                            if (hasLastControlPoint)
                            {
                                cp1 = new PointF(
                                    2 * currentPoint.X - lastControlPoint.X,
                                    2 * currentPoint.Y - lastControlPoint.Y
                                );
                            }
                            else
                            {
                                cp1 = currentPoint;
                            }

                            float x2 = parameters[i];
                            float y2 = parameters[i + 1];
                            float x = parameters[i + 2];
                            float y = parameters[i + 3];

                            PointF cp2, endPoint;

                            if (isRelative)
                            {
                                cp2 = new PointF(currentPoint.X + x2 * scaleX, currentPoint.Y + y2 * scaleY);
                                endPoint = new PointF(currentPoint.X + x * scaleX, currentPoint.Y + y * scaleY);
                            }
                            else
                            {
                                cp2 = new PointF(x2 * scaleX, y2 * scaleY);
                                endPoint = new PointF(x * scaleX, y * scaleY);
                            }

                            path.AddBezier(currentPoint, cp1, cp2, endPoint);
                            lastControlPoint = cp2;
                            hasLastControlPoint = true;
                            currentPoint = endPoint;
                        }
                        break;

                    case 'Q':
                        for (int i = 0; i < parameters.Length; i += 4)
                        {
                            if (i + 3 >= parameters.Length) break;

                            float x1 = parameters[i];
                            float y1 = parameters[i + 1];
                            float x = parameters[i + 2];
                            float y = parameters[i + 3];

                            PointF cp1, endPoint;

                            if (isRelative)
                            {
                                cp1 = new PointF(currentPoint.X + x1 * scaleX, currentPoint.Y + y1 * scaleY);
                                endPoint = new PointF(currentPoint.X + x * scaleX, currentPoint.Y + y * scaleY);
                            }
                            else
                            {
                                cp1 = new PointF(x1 * scaleX, y1 * scaleY);
                                endPoint = new PointF(x * scaleX, y * scaleY);
                            }

                            float cx1 = currentPoint.X + 2f / 3f * (cp1.X - currentPoint.X);
                            float cy1 = currentPoint.Y + 2f / 3f * (cp1.Y - currentPoint.Y);
                            float cx2 = endPoint.X + 2f / 3f * (cp1.X - endPoint.X);
                            float cy2 = endPoint.Y + 2f / 3f * (cp1.Y - endPoint.Y);

                            path.AddBezier(currentPoint, new PointF(cx1, cy1), new PointF(cx2, cy2), endPoint);
                            lastControlPoint = cp1;
                            hasLastControlPoint = true;
                            currentPoint = endPoint;
                        }
                        break;

                    case 'A':
                        for (int i = 0; i < parameters.Length; i += 7)
                        {
                            if (i + 6 >= parameters.Length) break;

                            float rx = parameters[i];
                            float ry = parameters[i + 1];
                            float xAxisRotation = parameters[i + 2];
                            float largeArcFlag = parameters[i + 3];
                            float sweepFlag = parameters[i + 4];
                            float x = parameters[i + 5];
                            float y = parameters[i + 6];

                            PointF endPoint;
                            if (isRelative)
                            {
                                endPoint = new PointF(
                                    currentPoint.X + x * scaleX,
                                    currentPoint.Y + y * scaleY
                                );
                            }
                            else
                            {
                                endPoint = new PointF(x * scaleX, y * scaleY);
                            }

                            AddArcToBezier(path, currentPoint, endPoint,
                                rx * scaleX, ry * scaleY, xAxisRotation,
                                largeArcFlag != 0, sweepFlag != 0);

                            currentPoint = endPoint;
                        }
                        hasLastControlPoint = false;
                        break;

                    case 'Z':
                        if (path.PointCount > 0)
                        {
                            path.CloseFigure();
                        }
                        currentPoint = startPoint;
                        hasLastControlPoint = false;
                        break;
                }
            }

            return path;
        }

        #endregion

        #region Style and Attribute Methods

        /// <summary>
        /// Gets the fill brush for an element
        /// </summary>
        /// <param name="element">XML element</param>
        /// <returns>Brush for fill or null if no fill</returns>
        private Brush GetFillBrush(XmlElement element)
        {
            string fill = GetStyleAttribute(element, "fill");

            if (fill == "none")
                return null;

            if (string.IsNullOrEmpty(fill))
            {
                string stroke = GetStyleAttribute(element, "stroke");
                if (!string.IsNullOrEmpty(stroke) && stroke != "none")
                {
                    return null;
                }
                fill = "black";
            }

            Color color = ParseColor(fill);

            string opacity = GetStyleAttribute(element, "fill-opacity");
            if (!string.IsNullOrEmpty(opacity))
            {
                float alpha = ParseFloat(opacity);
                color = Color.FromArgb((int)(alpha * 255), color);
            }

            return new SolidBrush(color);
        }

        /// <summary>
        /// Gets the stroke pen for an element
        /// </summary>
        /// <param name="element">XML element</param>
        /// <returns>Pen for stroke or null if no stroke</returns>
        private Pen GetStrokePen(XmlElement element)
        {
            string stroke = GetStyleAttribute(element, "stroke");

            if (stroke == "none")
                return null;

            if (string.IsNullOrEmpty(stroke))
                return null;

            if (stroke == "currentColor")
                stroke = "black";

            Color color = ParseColor(stroke);

            string opacity = GetStyleAttribute(element, "stroke-opacity");
            if (!string.IsNullOrEmpty(opacity))
            {
                float alpha = ParseFloat(opacity);
                color = Color.FromArgb((int)(alpha * 255), color);
            }

            float width = ParseFloat(GetStyleAttribute(element, "stroke-width", "1")) * scaleX;

            Pen pen = new Pen(color, width);

            string linecap = GetStyleAttribute(element, "stroke-linecap");
            if (linecap == "round")
                pen.StartCap = pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            else if (linecap == "square")
                pen.StartCap = pen.EndCap = System.Drawing.Drawing2D.LineCap.Square;

            string linejoin = GetStyleAttribute(element, "stroke-linejoin");
            if (linejoin == "round")
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
            else if (linejoin == "bevel")
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel;
            else if (linejoin == "miter")
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;

            return pen;
        }

        /// <summary>
        /// Gets a style attribute from an element, checking direct attributes, style attribute, and parent inheritance
        /// </summary>
        /// <param name="element">XML element</param>
        /// <param name="attributeName">Attribute name to look for</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Attribute value</returns>
        private string GetStyleAttribute(XmlElement element, string attributeName, string defaultValue = "")
        {
            string value = element.GetAttribute(attributeName);
            if (!string.IsNullOrEmpty(value))
            {
                if ((attributeName == "fill" || attributeName == "stroke") && colorReplacements != null)
                {
                    string normalized = NormalizeColorForSearch(value);
                    foreach (var replacement in colorReplacements)
                    {
                        string oldNormalized = NormalizeColorForSearch(replacement.Key);
                        if (normalized == oldNormalized)
                            return replacement.Value;
                    }
                }
                return value;
            }

            string style = element.GetAttribute("style");
            if (!string.IsNullOrEmpty(style))
            {
                string[] parts = style.Split(';');
                foreach (string part in parts)
                {
                    string[] keyValue = part.Split(':');
                    if (keyValue.Length == 2 && keyValue[0].Trim() == attributeName)
                    {
                        value = keyValue[1].Trim();

                        if ((attributeName == "fill" || attributeName == "stroke") && colorReplacements != null)
                        {
                            string normalized = NormalizeColorForSearch(value);
                            foreach (var replacement in colorReplacements)
                            {
                                string oldNormalized = NormalizeColorForSearch(replacement.Key);
                                if (normalized == oldNormalized)
                                    return replacement.Value;
                            }
                        }
                        return value;
                    }
                }
            }

            if (IsInheritableAttribute(attributeName))
            {
                XmlNode parent = element.ParentNode;
                while (parent != null && parent is XmlElement)
                {
                    XmlElement parentElement = (XmlElement)parent;

                    value = parentElement.GetAttribute(attributeName);
                    if (!string.IsNullOrEmpty(value))
                    {
                        if ((attributeName == "fill" || attributeName == "stroke") && colorReplacements != null)
                        {
                            string normalized = NormalizeColorForSearch(value);
                            foreach (var replacement in colorReplacements)
                            {
                                string oldNormalized = NormalizeColorForSearch(replacement.Key);
                                if (normalized == oldNormalized)
                                    return replacement.Value;
                            }
                        }
                        return value;
                    }

                    style = parentElement.GetAttribute("style");
                    if (!string.IsNullOrEmpty(style))
                    {
                        string[] parts = style.Split(';');
                        foreach (string part in parts)
                        {
                            string[] keyValue = part.Split(':');
                            if (keyValue.Length == 2 && keyValue[0].Trim() == attributeName)
                            {
                                value = keyValue[1].Trim();

                                if ((attributeName == "fill" || attributeName == "stroke") && colorReplacements != null)
                                {
                                    string normalized = NormalizeColorForSearch(value);
                                    foreach (var replacement in colorReplacements)
                                    {
                                        string oldNormalized = NormalizeColorForSearch(replacement.Key);
                                        if (normalized == oldNormalized)
                                            return replacement.Value;
                                    }
                                }
                                return value;
                            }
                        }
                    }

                    parent = parent.ParentNode;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Determines if an attribute inherits from parent elements in SVG
        /// </summary>
        /// <param name="attributeName">Attribute name</param>
        /// <returns>True if inheritable</returns>
        private bool IsInheritableAttribute(string attributeName)
        {
            switch (attributeName)
            {
                case "fill":
                case "fill-opacity":
                case "stroke":
                case "stroke-opacity":
                case "stroke-width":
                case "stroke-linecap":
                case "stroke-linejoin":
                case "opacity":
                case "font-family":
                case "font-size":
                case "font-weight":
                case "font-style":
                case "text-anchor":
                case "text-decoration":
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a rounded rectangle path
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <param name="rx">X radius</param>
        /// <param name="ry">Y radius</param>
        /// <returns>GraphicsPath representing rounded rectangle</returns>
        private GraphicsPath CreateRoundedRectPath(float x, float y, float width, float height, float rx, float ry)
        {
            GraphicsPath path = new GraphicsPath();

            if (rx <= 0 || ry <= 0)
            {
                path.AddRectangle(new RectangleF(x, y, width, height));
                return path;
            }

            rx = Math.Min(rx, width / 2);
            ry = Math.Min(ry, height / 2);

            float diameter = rx * 2;
            RectangleF arc = new RectangleF(x, y, diameter, diameter);

            path.AddArc(arc, 180, 90);

            arc.X = x + width - diameter;
            path.AddArc(arc, 270, 90);

            arc.Y = y + height - diameter;
            path.AddArc(arc, 0, 90);

            arc.X = x;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Gets a dimension value from an element attribute
        /// </summary>
        /// <param name="element">XML element</param>
        /// <param name="attributeName">Attribute name</param>
        /// <param name="defaultValue">Default value if not found or invalid</param>
        /// <returns>Dimension as integer</returns>
        private int GetDimension(XmlElement element, string attributeName, int defaultValue)
        {
            string value = element.GetAttribute(attributeName);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            value = Regex.Replace(value, @"[a-z]+$", "", RegexOptions.IgnoreCase);

            float result;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                return (int)result;

            return defaultValue;
        }

        /// <summary>
        /// Normalizes a color value for searching and replacement
        /// </summary>
        /// <param name="color">Color string</param>
        /// <returns>Normalized hex color</returns>
        private static string NormalizeColorForSearch(string color)
        {
            if (color.StartsWith("#"))
                return color.ToLower();

            Color c = ParseColor(color);
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B).ToLower();
        }

        /// <summary>
        /// Parses a color string into a Color object
        /// </summary>
        /// <param name="color">Color string (hex, rgb, or named)</param>
        /// <returns>Color object</returns>
        private static Color ParseColor(string color)
        {
            if (string.IsNullOrEmpty(color) || color == "none")
                return Color.Transparent;

            color = color.Trim();

            if (color.StartsWith("#"))
            {
                color = color.Substring(1);
                if (color.Length == 3)
                {
                    color = "" + color[0] + color[0] + color[1] + color[1] + color[2] + color[2];
                }

                int r = int.Parse(color.Substring(0, 2), NumberStyles.HexNumber);
                int g = int.Parse(color.Substring(2, 2), NumberStyles.HexNumber);
                int b = int.Parse(color.Substring(4, 2), NumberStyles.HexNumber);
                return Color.FromArgb(r, g, b);
            }

            if (color.StartsWith("rgb("))
            {
                string values = color.Substring(4, color.Length - 5);
                string[] parts = values.Split(',');
                int r = int.Parse(parts[0].Trim());
                int g = int.Parse(parts[1].Trim());
                int b = int.Parse(parts[2].Trim());
                return Color.FromArgb(r, g, b);
            }

            return Color.FromName(color);
        }

        /// <summary>
        /// Parses a float value from a string
        /// </summary>
        /// <param name="value">String value</param>
        /// <param name="defaultValue">Default value if parsing fails</param>
        /// <returns>Parsed float value</returns>
        private float ParseFloat(string value, float defaultValue = 0)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            value = Regex.Replace(value, @"[a-z]+$", "", RegexOptions.IgnoreCase).Trim();

            float result;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                return result;

            return defaultValue;
        }

        /// <summary>
        /// Parses a string into an array of float values
        /// </summary>
        /// <param name="str">String containing numbers</param>
        /// <returns>Array of parsed floats</returns>
        private float[] ParseFloatArray(string str)
        {
            if (string.IsNullOrEmpty(str))
                return new float[0];

            str = str.Replace(',', ' ');

            List<string> tokens = new List<string>();
            string current = "";
            bool inNumber = false;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current);
                        current = "";
                        inNumber = false;
                    }
                }
                else if (c == '-')
                {
                    if (inNumber && current.Length > 0)
                    {
                        tokens.Add(current);
                        current = "-";
                        inNumber = true;
                    }
                    else
                    {
                        current = "-";
                        inNumber = true;
                    }
                }
                else if (c == '.')
                {
                    if (current.Contains("."))
                    {
                        tokens.Add(current);
                        current = ".";
                        inNumber = true;
                    }
                    else
                    {
                        current += c;
                        inNumber = true;
                    }
                }
                else if (char.IsDigit(c))
                {
                    current += c;
                    inNumber = true;
                }
                else
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current);
                        current = "";
                        inNumber = false;
                    }
                }
            }

            if (current.Length > 0)
                tokens.Add(current);

            List<float> result = new List<float>();
            foreach (string token in tokens)
            {
                if (!string.IsNullOrEmpty(token) && token != "-" && token != ".")
                {
                    result.Add(ParseFloat(token));
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Parses a points string into a list of PointF objects
        /// </summary>
        /// <param name="pointsStr">Points string</param>
        /// <returns>List of parsed points</returns>
        private List<PointF> ParsePoints(string pointsStr)
        {
            float[] coords = ParseFloatArray(pointsStr);
            List<PointF> points = new List<PointF>();

            for (int i = 0; i < coords.Length - 1; i += 2)
            {
                points.Add(new PointF(coords[i] * scaleX, coords[i + 1] * scaleY));
            }

            return points;
        }

        #endregion
    }
}