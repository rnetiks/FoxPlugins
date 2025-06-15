using System;
using DefaultNamespace.Compositor;
using UIBuilder;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Compositor.KK
{
    /// <summary>
    /// Responsible for rendering elements of the compositor system, including background, header, nodes,
    /// connections, and status bar, ensuring proper sequencing and state consistency.
    /// </summary>
    public class CompositorRenderer
    {
        private CompositorManager _manager;
        private readonly int _headerWindowId = 1000;
        private readonly int _nodeWindowIdStart = 2000;

        private Vector2 _lastMousePosition;
        private bool _isHoveringPort;
        private ICompositorNode _hoveredNode;

        public CompositorRenderer(CompositorManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Handles the rendering of the entire compositor interface, including the background, header, nodes, connections, and status bar.
        /// It ensures the visual elements are drawn in the correct sequence, with respect to the state of the compositor manager.
        /// </summary>
        public void DrawCompositor()
        {
            DrawBackground();
            DrawHeader();
            DrawNodes();
            DrawConnections();
            DrawStatusBar();
        }

        /// <summary>
        /// Renders the background of the compositor interface. Fills the entire screen area with the designated background color
        /// and displays a grid overlay scaled and offset based on the current zoom level and positional offset from the state.
        /// </summary>
        private void DrawBackground()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), GUIUtils.GetColorTexture(GUIUtils.Colors.Background));
            var gridArea = new Rect(0, 30, Screen.width, Screen.height - 60);
            CompositorStyles.DrawGrid(gridArea, _manager.State.Zoom, new Vector2(_manager.State.OffsetX, _manager.State.OffsetY));
        }

        /// <summary>
        /// Renders the header section of the compositor interface. Includes visual elements such as background textures and
        /// accent lines. Displays interactive controls like buttons for saving, resetting, and adding specific nodes (e.g.,
        /// Filter and Transform). Additionally, shows statistics about the current compositor state, including node count,
        /// texture count, and zoom level, all dynamically positioned based on the screen width and style definitions.
        /// </summary>
        private void DrawHeader()
        {
            var headerRect = new Rect(0, 0, Screen.width, 30);
            GUI.DrawTexture(headerRect, GUIUtils.GetColorTexture(GUIUtils.Colors.Header));

            var accentRect = new Rect(0, 28, Screen.width, 2);
            GUI.DrawTexture(accentRect, GUIUtils.GetColorTexture(GUIUtils.Colors.HeaderAccent));

            float buttonX = 8;
            const float buttonWidth = 80;
            const float buttonHeight = 22;
            const float buttonSpacing = 8;

            if (GUI.Button(new Rect(buttonX, 4, buttonWidth, buttonHeight), "Save", CompositorStyles.HeaderButtonPrimary))
            {
                _manager.Save();
            }

            buttonX += buttonWidth + buttonSpacing;

            if (GUI.Button(new Rect(buttonX, 4, buttonWidth, buttonHeight), "Reset", CompositorStyles.HeaderButtonSecondary))
            {
                _manager.Reset();
            }

            buttonX += buttonWidth + buttonSpacing;

            if (GUI.Button(new Rect(buttonX, 4, buttonWidth + 20, buttonHeight), "Filter", CompositorStyles.HeaderButtonSecondary))
            {
                AddFilterNode();
            }
            buttonX += buttonWidth + 20 + buttonSpacing;

            if (GUI.Button(new Rect(buttonX, 4, buttonWidth + 20, buttonHeight), "Transform", CompositorStyles.HeaderButtonSecondary))
            {
                AddTransformNode();
            }

            var statsText = $"Nodes: {_manager.Nodes.Count} | Textures: {TextureCache.Count} | Zoom: {_manager.State.Zoom:F1}x";
            var statsSize = GUI.skin.label.CalcSize(new GUIContent(statsText));
            var statsRect = new Rect(Screen.width - statsSize.x - 15, 4, statsSize.x, buttonHeight);
            GUI.Label(statsRect, statsText, CompositorStyles.StatusLabel);
        }

        /// <summary>
        /// Iterates through all compositor nodes and renders them on the screen. Handles individual node positioning,
        /// size, and appearance based on their properties such as selection state. Also updates the hovered node based
        /// on the mouse position.
        /// </summary>
        private void DrawNodes()
        {
            var state = _manager.State;
            for (var i = 0; i < _manager.Nodes.Count; i++)
            {
                var node = _manager.Nodes[i];
                var windowId = _nodeWindowIdStart + i;

                var nodeRect = GUIUtils.ScaleRect(
                    new Rect(node.Position, node.Size), state.Zoom, new Vector2(state.OffsetX, state.OffsetY));

                nodeRect.y += 30;

                var mousePos = Event.current.mousePosition;
                bool isHovered = nodeRect.Contains(mousePos);
                _hoveredNode = isHovered ? node : _hoveredNode;

                var windowStyle = node.IsSelected ? CompositorStyles.NodeWindowSelected : CompositorStyles.NodeWindow;

                if (isHovered && !node.IsSelected)
                {
                    var hoverStyle = new GUIStyle(windowStyle);
                    hoverStyle.normal.background = GUIUtils.GetColorTexture(GUIUtils.Colors.NodeBackgroundHover);
                    windowStyle = hoverStyle;
                }

                var shadowRect = new Rect(nodeRect.x + 2, nodeRect.y + 2, nodeRect.width, nodeRect.height);
                GUI.DrawTexture(shadowRect, GUIUtils.GetColorTexture(new Color(0, 0, 0, 0.3f)));

                GUI.Window(windowId, nodeRect, (id) => DrawNodeWindow(node), "", windowStyle);

                var titleRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, 20);
                GUI.DrawTexture(titleRect, GUIUtils.GetColorTexture(GUIUtils.Colors.NodeHeader));

                var titleStyle = node.IsSelected ? GUIStyleBuilder.CreateFrom(CompositorStyles.NodeTitle).WithNormalState(textColor: GUIUtils.Colors.TextAccent) : CompositorStyles.NodeTitle;
                GUI.Label(titleRect, node.Title, titleStyle);
            }
        }

        /// <summary>
        /// Renders the visual representation of a compositor node, including its background, input ports,
        /// output ports, and content. Handles drawing with appropriate styles and scales based on current zoom
        /// and hover states.
        /// </summary>
        /// <param name="node">The node to be rendered. Responsible for providing size, content, input ports, and output ports information.</param>
        private void DrawNodeWindow(ICompositorNode node)
        {
            var contentRect = new Rect(2, 22, node.Size.x - 4, node.Size.y - 24);

            var state = _manager.State;
            GUI.DrawTexture(new Rect(0, 20, node.Size.x * state.Zoom, node.Size.y * state.Zoom - 20), GUIUtils.GetColorTexture(new Color(GUIUtils.Colors.NodeBackground.r, GUIUtils.Colors.NodeBackground.g, GUIUtils.Colors.NodeBackground.b, 0.8f)));
            node.DrawContent(contentRect);

            for (var i = 0; i < node.Inputs.Count; i++)
            {
                var input = node.Inputs[i];
                var portRect = new Rect(input.LocalPosition.x * state.Zoom - 6, input.LocalPosition.y * state.Zoom - 6, 12, 12);

                bool isHovered = portRect.Contains(Event.current.mousePosition);
                CompositorStyles.DrawPort(portRect, input.IsConnected, true, isHovered);
                var labelRect = new Rect(portRect.x + 18, portRect.y - 2, 100, 16);
                GUI.Label(labelRect, input.Name, CompositorStyles.PortLabel);
            }

            for (var i = 0; i < node.Outputs.Count; i++)
            {
                var output = node.Outputs[i];
                var portRect = new Rect(output.LocalPosition.x * state.Zoom - 6, output.LocalPosition.y * state.Zoom - 6, 12, 12);
                bool isHovered = portRect.Contains(Event.current.mousePosition);
                CompositorStyles.DrawPort(portRect, output.Connections.Count > 0, false, isHovered);
                var labelRect = new Rect(portRect.x - 85, portRect.y - 2, 80, 16);
                var labelStyle = GUIStyleBuilder.CreateFrom(CompositorStyles.PortLabel).WithAlignment(TextAnchor.MiddleRight);
                GUI.Label(labelRect, output.Name, labelStyle);
            }
        }

        /// <summary>
        /// Draws the visual connections between compositor nodes, representing data flow or relationships.
        /// Handles rendering of bezier curves connecting output ports to corresponding input ports, with
        /// dynamic styling including colors and pulsing effects. Additionally, arrows are drawn to indicate
        /// directionality of the connections.
        /// </summary>
        private void DrawConnections()
        {
            foreach (var node in _manager.Nodes)
            {
                for (var i = 0; i < node.Outputs.Count; i++)
                {
                    var output = node.Outputs[i];

                    foreach (var connection in output.Connections)
                    {
                        var startPos = GetPortWorldPosition(node, output.LocalPosition);
                        var endPos = GetPortWorldPosition(connection.InputNode, connection.InputNode.Inputs[connection.InputIndex].LocalPosition);
                        var connectionColor = GetConnectionColor(output.OutputType);
                        var pulseIntensity = Mathf.Sin(Time.time * 3f) * 0.1f + 0.9f;
                        connectionColor = Color.Lerp(connectionColor, GUIUtils.Colors.ConnectionHighlight, pulseIntensity * 0.3f);
                        GUIUtils.DrawBezierCurve(startPos, endPos, connectionColor, 3f, 1);
                        var midPoint = Vector2.Lerp(startPos, endPos, 0.5f);
                        var direction = (endPos - startPos).normalized;
                        var arrowStart = midPoint - direction * 5f;
                        var arrowEnd = midPoint + direction * 5f;
                        GUIUtils.DrawLine(arrowStart, arrowEnd, connectionColor, 2f);
                    }
                }
            }
        }

        /// <summary>
        /// Draws the status bar at the bottom of the compositor interface, displaying key information such as
        /// system performance metrics and the current processing status.
        /// </summary>
        /// <remarks>
        /// The status bar includes the status of the compositor (e.g., "Ready", "Processing", "Waiting for Input") and system performance metrics like
        /// frames per second (FPS) and memory usage in MB.
        /// </remarks>
        private void DrawStatusBar()
        {
            var statusRect = new Rect(0, Screen.height - 25, Screen.width, 25);
            GUI.DrawTexture(statusRect, GUIUtils.GetColorTexture(GUIUtils.Colors.Header));

            var processingText = "Ready";
            if (_manager.Nodes.Count > 0)
            {
                processingText = TextureCache.HasTextures() ? "Processing" : "Waiting for Input";
            }

            GUI.Label(new Rect(10, Screen.height - 22, 200, 20), $"Status: {processingText}", CompositorStyles.StatusLabel);

            var perfText = $"FPS: {(1f / Time.deltaTime):F0} | Memory: {(System.GC.GetTotalMemory(false) / 1024f / 1024f):F1} MB";
            var perfSize = GUI.skin.label.CalcSize(new GUIContent(perfText));
            GUI.Label(new Rect(Screen.width - perfSize.x - 10, Screen.height - 22, perfSize.x, 20), perfText, CompositorStyles.StatusLabel);
        }

        /// <summary>
        /// Calculates the world position of a port within a compositor node, considering the node's position,
        /// zoom level, and any offset applied in the compositor state.
        /// </summary>
        /// <param name="node">The compositor node containing the port.</param>
        /// <param name="localPosition">The local position of the port within the node.</param>
        /// <returns>The world position of the port as a <see cref="Vector2"/>.</returns>
        private Vector2 GetPortWorldPosition(ICompositorNode node, Vector2 localPosition)
        {
            var state = _manager.State;
            var nodeWorldPos = GUIUtils.ScaleVector2(node.Position, state.Zoom, new Vector2(state.OffsetX, state.OffsetY));

            nodeWorldPos.y += 30;

            return nodeWorldPos + localPosition * state.Zoom;
        }

        /// <summary>
        /// Determines the color used for rendering the connection lines in the compositor graph,
        /// based on the type of data being transmitted through the connection.
        /// </summary>
        /// <param name="dataType">The type of the data being transmitted through the connection, such as Texture2D, float, or Color.</param>
        /// <returns>A Color instance representing the visual style of the connection based on the data type.</returns>
        private Color GetConnectionColor(Type dataType)
        {
            if (dataType == typeof(Texture2D))
                return GUIUtils.Colors.Connection;
            if (dataType == typeof(float))
                return new Color(0.3f, 0.8f, 0.3f, 0.8f);
            if (dataType == typeof(Color))
                return new Color(0.8f, 0.3f, 0.8f, 0.8f);
            return GUIUtils.Colors.Connection;
        }

        /// <summary>
        /// Adds a new filter node to the compositor system. The filter node is initialized with a random position
        /// within the defined offset range and subsequently added to the node manager for rendering and processing.
        /// </summary>
        private void AddFilterNode()
        {
            var filterNode = new FilterNode();
            filterNode.Position = new Vector2(400 + Random.Range(-50, 50), 250 + Random.Range(-50, 50));
            _manager.AddNode(filterNode);
        }

        /// <summary>
        /// Adds a new TransformNode instance to the compositor, initializing its position with randomized
        /// offsets. The newly created node is then registered with the CompositorManager, making it
        /// part of the compositor workflow.
        /// </summary>
        private void AddTransformNode()
        {
            var transformNode = new TransformNode();
            transformNode.Position = new Vector2(600 + Random.Range(-50, 50), 250 + Random.Range(-50, 50));
            _manager.AddNode(transformNode);
        }

        /// <summary>
        /// Adds a new compositor node of the specified type to the composition system.
        /// The node's position is initialized based on the current mouse position,
        /// constrained by screen boundaries, and subsequently registered to the compositor manager.
        /// </summary>
        /// <param name="nodeType">The type of the node to be added. Must be a subclass of BaseCompositorNode.</param>
        private void AddNode(Type nodeType)
        {
            if (!nodeType.IsSubclassOf(typeof(BaseCompositorNode)))
                return;
            var node = (BaseCompositorNode) Activator.CreateInstance(nodeType);
            node.Position = Vector2.Min(Event.current.mousePosition, new Vector2(Screen.width, Screen.height));
            _manager.AddNode(node);
        }
    }
}