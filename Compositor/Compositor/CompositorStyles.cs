using Compositor.KK;
using UnityEngine;
using UIBuilder;

namespace DefaultNamespace.Compositor
{
    public static class CompositorStyles
    {

        private static GUIStyle _headerButtonPrimary;
        private static GUIStyle _headerButtonSecondary;
        private static GUIStyle _nodeWindow;
        private static GUIStyle _nodeWindowSelected;
        private static GUIStyle _nodeTitle;
        private static GUIStyle _nodeContent;
        private static GUIStyle _portLabel;
        private static GUIStyle _statusLabel;
        private static GUIStyle _filterButton;
        private static GUIStyle _filterButtonSelected;
        private static GUIStyle _exportButton;

        public static GUIStyle HeaderButtonPrimary
        {
            get
            {
                if (_headerButtonPrimary == null)
                {
                    _headerButtonPrimary = GUIStyleBuilder.Create()
                        .AsButton()
                        .WithNormalState(
                            textColor: GUIUtils.Colors.TextPrimary,
                            background: GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonPrimary))
                        .WithHoverState(
                            textColor: Color.white,
                            background: GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonPrimaryHover))
                        .WithActiveState(
                            textColor: Color.white,
                            background: GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonPrimary))
                        .WithFontSize(11)
                        .WithFontStyle(FontStyle.Bold)
                        .WithAlignment(TextAnchor.MiddleCenter)
                        .WithPadding(8, 8, 4, 4)
                        .Build();
                }
                return _headerButtonPrimary;
            }
        }

        public static GUIStyle HeaderButtonSecondary
        {
            get
            {
                if (_headerButtonSecondary == null)
                {
                    _headerButtonSecondary = GUIStyleBuilder.Create()
                        .AsButton()
                        .WithNormalState(
                            textColor: GUIUtils.Colors.TextPrimary,
                            background: GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonSecondary))
                        .WithHoverState(
                            textColor: Color.white,
                            background: GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonSecondaryHover))
                        .WithActiveState(
                            textColor: Color.white,
                            background: GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonSecondary))
                        .WithFontSize(11)
                        .WithAlignment(TextAnchor.MiddleCenter)
                        .WithPadding(8, 8, 4, 4)
                        .Build();
                }
                return _headerButtonSecondary;
            }
        }

        public static GUIStyle NodeWindow
        {
            get
            {
                if (_nodeWindow == null)
                {
                    _nodeWindow = GUIStyleBuilder.Create()
                        .WithNormalState(background: GUIUtils.GetColorTexture(GUIUtils.Colors.NodeBackground))
                        .WithBorder(1)
                        .WithPadding(0)
                        .Build();
                }
                return _nodeWindow;
            }
        }

        public static GUIStyle NodeWindowSelected
        {
            get
            {
                if (_nodeWindowSelected == null)
                {
                    _nodeWindowSelected = GUIStyleBuilder.Create()
                        .AsBox()
                        .WithNormalState(background: GUIUtils.GetColorTexture(GUIUtils.Colors.HeaderAccent))
                        .WithBorder(2)
                        .WithPadding(0)
                        .Build();
                }
                return _nodeWindowSelected;
            }
        }

        public static GUIStyle NodeTitle
        {
            get
            {
                if (_nodeTitle == null)
                {
                    _nodeTitle = GUIStyleBuilder.Create()
                        .AsLabel()
                        .WithNormalState(textColor: GUIUtils.Colors.TextPrimary)
                        .WithFontSize(12)
                        .WithFontStyle(FontStyle.Bold)
                        .WithAlignment(TextAnchor.MiddleCenter)
                        .WithPadding(4)
                        .Build();
                }
                return _nodeTitle;
            }
        }

        public static GUIStyle NodeContent
        {
            get
            {
                if (_nodeContent == null)
                {
                    _nodeContent = GUIStyleBuilder.Create()
                        .AsLabel()
                        .WithNormalState(textColor: GUIUtils.Colors.TextSecondary)
                        .WithFontSize(10)
                        .WithAlignment(TextAnchor.MiddleCenter)
                        .WithWordWrap(true)
                        .Build();
                }
                return _nodeContent;
            }
        }

        public static GUIStyle PortLabel
        {
            get
            {
                if (_portLabel == null)
                {
                    _portLabel = GUIStyleBuilder.Create()
                        .AsLabel()
                        .WithNormalState(textColor: GUIUtils.Colors.TextSecondary)
                        .WithFontSize(8)
                        .WithAlignment(TextAnchor.MiddleLeft)
                        .Build();
                }
                return _portLabel;
            }
        }

        public static GUIStyle StatusLabel
        {
            get
            {
                if (_statusLabel == null)
                {
                    _statusLabel = GUIStyleBuilder.Create()
                        .AsLabel()
                        .WithNormalState(textColor: GUIUtils.Colors.TextAccent)
                        .WithFontSize(10)
                        .WithAlignment(TextAnchor.MiddleLeft)
                        .Build();
                }
                return _statusLabel;
            }
        }

        public static GUIStyle FilterButton
        {
            get
            {
                if (_filterButton == null)
                {
                    _filterButton = GUIStyleBuilder.Create()
                        .AsButton()
                        .WithNormalState(
                            textColor: GUIUtils.Colors.TextSecondary,
                            background: GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonSecondary))
                        .WithHoverState(
                            textColor: GUIUtils.Colors.TextPrimary,
                            background: GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonSecondaryHover))
                        .WithFontSize(9)
                        .WithAlignment(TextAnchor.MiddleCenter)
                        .WithPadding(2)
                        .Build();
                }
                return _filterButton;
            }
        }

        public static GUIStyle FilterButtonSelected
        {
            get
            {
                if (_filterButtonSelected == null)
                {
                    _filterButtonSelected = GUIStyleBuilder.Create()
                        .AsButton()
                        .WithNormalState(
                            textColor: Color.white,
                            background: GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonPrimary))
                        .WithHoverState(
                            textColor: Color.white,
                            background: GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonPrimaryHover))
                        .WithFontSize(9)
                        .WithFontStyle(FontStyle.Bold)
                        .WithAlignment(TextAnchor.MiddleCenter)
                        .WithPadding(2)
                        .Build();
                }
                return _filterButtonSelected;
            }
        }

        public static GUIStyle ExportButton
        {
            get
            {
                if (_exportButton == null)
                {
                    _exportButton = GUIStyleBuilder.Create()
                        .AsButton()
                        .WithNormalState(
                            textColor: Color.white,
                            background: GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonSuccess))
                        .WithHoverState(
                            textColor: Color.white,
                            background: GUIUtils.GetColorTexture(new Color(0.25f, 0.7f, 0.35f, 1f)))
                        .WithFontSize(10)
                        .WithFontStyle(FontStyle.Bold)
                        .WithAlignment(TextAnchor.MiddleCenter)
                        .WithPadding(4)
                        .Build();
                }
                return _exportButton;
            }
        }

        /// <summary>
        /// Draws a grid on the specified area with an adjustable zoom level and offset.
        /// </summary>
        /// <param name="area">The rectangular area where the grid will be drawn.</param>
        /// <param name="zoom">The zoom level of the grid.</param>
        /// <param name="offset">The offset of the grid in the area.</param>
        public static void DrawGrid(Rect area, float zoom, Vector2 offset)
        {
            float gridSize = 20f * zoom;
            float startX = area.x + (offset.x % gridSize);
            float startY = area.y + (offset.y % gridSize);

            for (float x = startX; x < area.x + area.width; x += gridSize)
            {
                GUIUtils.DrawLine(
                    new Vector2(x, area.y),
                    new Vector2(x, area.y + area.height),
                    new Color(GUIUtils.Colors.BackgroundGrid.r, GUIUtils.Colors.BackgroundGrid.g, GUIUtils.Colors.BackgroundGrid.b, 0.3f),
                    1f
                );
            }

            for (float y = startY; y < area.y + area.height; y += gridSize)
            {
                GUIUtils.DrawLine(
                    new Vector2(area.x, y),
                    new Vector2(area.x + area.width, y),
                    new Color(GUIUtils.Colors.BackgroundGrid.r, GUIUtils.Colors.BackgroundGrid.g, GUIUtils.Colors.BackgroundGrid.b, 0.3f),
                    1f
                );
            }
        }

        /// <summary>
        /// Draws a port representation on the UI.
        /// </summary>
        /// <param name="portRect">The rectangle specifying the location and size of the port.</param>
        /// <param name="isConnected">Indicates whether the port is connected.</param>
        /// <param name="isInput">Indicates whether the port is an input port.</param>
        /// <param name="isHovered">Indicates whether the port is currently being hovered over by the cursor. Default is false.</param>
        public static void DrawPort(Rect portRect, bool isConnected, bool isInput, bool isHovered = false)
        {
            Color portColor;
            
            if (isConnected)
            {
                portColor = GUIUtils.Colors.PortConnected;
            }
            else if (isInput)
            {
                portColor = GUIUtils.Colors.NodeInput;
            }
            else
            {
                portColor = GUIUtils.Colors.NodeOutput;
            }

            if (isHovered)
            {
                portColor = Color.Lerp(portColor, Color.white, 0.3f);
            }

            var bgRect = new Rect(portRect.x - 1, portRect.y - 1, portRect.width + 2, portRect.height + 2);
            GUI.DrawTexture(bgRect, GUIUtils.GetColorTexture(new Color(0, 0, 0, 0.5f)));

            GUI.DrawTexture(portRect, GUIUtils.GetColorTexture(portColor));

            if (isConnected)
            {
                var innerRect = new Rect(portRect.x + 2, portRect.y + 2, portRect.width - 4, portRect.height - 4);
                GUI.DrawTexture(innerRect, GUIUtils.GetColorTexture(new Color(1, 1, 1, 0.4f)));
            }
        }
    }
}