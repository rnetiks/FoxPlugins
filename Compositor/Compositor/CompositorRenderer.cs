using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DefaultNamespace;
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
        public static CompositorRenderer Instance { get; private set; }
        private CompositorManager _manager;
        private readonly int _headerWindowId = 1000;
        private readonly int _nodeWindowIdStart = 2000;

        private Vector2 _lastMousePosition;
        private bool _isHoveringPort;
        private ICompositorNode _hoveredNode;

        public CompositorRenderer(CompositorManager manager)
        {
            Instance = this;
            _manager = manager;
            _scrollPosition = Vector2.zero;
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
            if (CompositorManager.IsSearchMenuVisible)
                DrawSearchMenu();
        }

        public static string searchText = "";
        private Vector2 _scrollPosition;
        private string _searchText = "";
        private string _currentGroup = null;
        private Dictionary<string, List<Type>> _nodeGroups = new Dictionary<string, List<Type>>();

        private void DrawSearchMenu()
        {
            var menuWidth = 320f;
            var menuHeight = 450f;
            var clientRect = new Rect(CompositorManager.SearchMenuPosition, new Vector2(menuWidth, menuHeight));
            clientRect.x = Mathf.Clamp(clientRect.x, 10, Screen.width - menuWidth - 10);
            clientRect.y = Mathf.Clamp(clientRect.y, 10, Screen.height - menuHeight - 10);

            var currentMousePosition = Event.current.mousePosition;
            if (Event.current.type == EventType.MouseDown && !clientRect.Contains(currentMousePosition) && GUI.GetNameOfFocusedControl() != "SearchField")
            {
                _manager.HideSearchMenu();
                return;
            }
            if (_nodeGroups.Count == 0)
                BuildNodeGroups();

            GUI.Window(32908, clientRect, DrawSearchWindow, "", CompositorStyles.NodeWindow);
            var titleRect = new Rect(clientRect.x, clientRect.y, clientRect.width, 25);
            GUI.DrawTexture(titleRect, GUIUtils.GetColorTexture(GUIUtils.Colors.NodeHeader));

            var titleText = "Add Node";
            if (!string.IsNullOrEmpty(_currentGroup))
                titleText = _currentGroup;

            GUI.Label(titleRect, titleText, CompositorStyles.NodeTitle);
        }

        private void DrawSearchWindow(int windowId)
        {
            var clientRect = new Rect(0, 0, 320, 450);
            if (!string.IsNullOrEmpty(_currentGroup) && string.IsNullOrEmpty(_searchText))
            {
                var backRect = new Rect(clientRect.x + 5, clientRect.y + 2, 50, 21);
                if (GUI.Button(backRect, "Back", CompositorStyles.FilterButton))
                {
                    _currentGroup = null;
                }
            }
            GUI.SetNextControlName("SearchField");
            var searchRect = new Rect(5, 30, clientRect.width - 10, 22);
            var newSearchText = GUI.TextField(searchRect, _searchText, CompositorStyles.NodeContent);

            if (newSearchText != _searchText)
            {
                _searchText = newSearchText;
                _currentGroup = null;
            }

            var contentRect = new Rect(5, 60, clientRect.width - 10, clientRect.height - 65);

            if (string.IsNullOrEmpty(_searchText))
            {
                if (string.IsNullOrEmpty(_currentGroup))
                {
                    DrawGroups(contentRect);
                }
                else
                {
                    DrawGroupItems(contentRect, _currentGroup);
                }
            }
            else
            {
                DrawSearchResults(contentRect);
            }
        }

        private void BuildNodeGroups()
        {
            _nodeGroups.Clear();

            foreach (var nodeType in Entry.AvailableNodes)
            {
                var group = GetNodeGroup(nodeType);

                if (!_nodeGroups.ContainsKey(group))
                    _nodeGroups[group] = new List<Type>();

                _nodeGroups[group].Add(nodeType);
            }
            var sortedGroups = new Dictionary<string, List<Type>>();
            foreach (var kvp in _nodeGroups.OrderBy(x => x.Key))
            {
                kvp.Value.Sort((a, b) => GetNodeDisplayName(a).CompareTo(GetNodeDisplayName(b)));
                sortedGroups[kvp.Key] = kvp.Value;
            }
            _nodeGroups = sortedGroups;
        }

        private string GetNodeGroup(Type nodeType)
        {
            var groupProperty = nodeType.GetProperty("Group", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (groupProperty != null && groupProperty.PropertyType == typeof(string))
            {
                return (string)groupProperty.GetValue(this, null) ?? "General";
            }

            var groupField = nodeType.GetField("Group", BindingFlags.Public | BindingFlags.Static);
            if (groupField != null && groupField.FieldType == typeof(string))
            {
                return (string)groupField.GetValue(null) ?? "General";
            }

            var typeName = "Undefined";

            return typeName;
        }

        private void DrawGroups(Rect contentRect)
        {
            var itemHeight = 28f;
            var totalHeight = _nodeGroups.Count * itemHeight;

            _scrollPosition = GUI.BeginScrollView(contentRect, _scrollPosition, new Rect(0, 0, contentRect.width - 20, totalHeight));

            int index = 0;
            foreach (var group in _nodeGroups.Keys)
            {
                var itemRect = new Rect(0, index * itemHeight, contentRect.width - 20, itemHeight - 2);
                var isHovered = itemRect.Contains(Event.current.mousePosition);

                if (isHovered)
                {
                    GUI.DrawTexture(itemRect, GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonSecondaryHover));
                }
                else
                {
                    GUI.DrawTexture(itemRect, GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonSecondary));
                }
                var nodeCount = _nodeGroups[group].Count;
                var labelText = $"{group} ({nodeCount})";

                var labelStyle = GUIStyleBuilder.Create()
                    .AsLabel()
                    .WithNormalState(textColor: isHovered ? Color.white : GUIUtils.Colors.TextPrimary)
                    .WithFontSize(12)
                    .WithAlignment(TextAnchor.MiddleLeft)
                    .WithPadding(10, 10, 6, 6);

                if (GUI.Button(itemRect, "", GUIStyle.none))
                {
                    _currentGroup = group;
                    _scrollPosition = Vector2.zero;
                }

                GUI.Label(itemRect, labelText, labelStyle);
                var arrowRect = new Rect(itemRect.x + itemRect.width - 25, itemRect.y + 6, 16, 16);
                GUI.Label(arrowRect, "►", GUIStyleBuilder.Create().AsLabel().WithNormalState(textColor: GUIUtils.Colors.TextSecondary).WithAlignment(TextAnchor.MiddleCenter));

                index++;
            }

            GUI.EndScrollView();
        }

        private void DrawGroupItems(Rect contentRect, string groupName)
        {
            if (!_nodeGroups.ContainsKey(groupName)) return;

            var nodes = _nodeGroups[groupName];
            var itemHeight = 25f;
            var totalHeight = nodes.Count * itemHeight;

            _scrollPosition = GUI.BeginScrollView(contentRect, _scrollPosition, new Rect(0, 0, contentRect.width - 20, totalHeight));

            for (int i = 0; i < nodes.Count; i++)
            {
                var nodeType = nodes[i];
                var itemRect = new Rect(0, i * itemHeight, contentRect.width - 20, itemHeight - 1);

                DrawNodeItem(itemRect, nodeType);
            }

            GUI.EndScrollView();
        }

        private void DrawSearchResults(Rect contentRect)
        {
            var matchingNodes = new List<Type>();
            var searchLower = _searchText.ToLower();

            foreach (var nodeType in Entry.AvailableNodes)
            {
                var nodeName = GetNodeDisplayName(nodeType);
                if (nodeName.ToLower().Contains(searchLower))
                {
                    matchingNodes.Add(nodeType);
                }
            }
            matchingNodes.Sort((a, b) =>
            {
                var nameA = GetNodeDisplayName(a).ToLower();
                var nameB = GetNodeDisplayName(b).ToLower();

                bool aExact = nameA == searchLower;
                bool bExact = nameB == searchLower;
                if (aExact != bExact) return bExact.CompareTo(aExact);

                bool aStarts = nameA.StartsWith(searchLower);
                bool bStarts = nameB.StartsWith(searchLower);
                if (aStarts != bStarts) return bStarts.CompareTo(aStarts);

                return nameA.CompareTo(nameB);
            });

            var itemHeight = 25f;
            var totalHeight = matchingNodes.Count * itemHeight;

            _scrollPosition = GUI.BeginScrollView(contentRect, _scrollPosition, new Rect(0, 0, contentRect.width - 20, totalHeight));

            for (int i = 0; i < matchingNodes.Count; i++)
            {
                var nodeType = matchingNodes[i];
                var itemRect = new Rect(0, i * itemHeight, contentRect.width - 20, itemHeight - 1);

                DrawNodeItem(itemRect, nodeType);
            }

            GUI.EndScrollView();
        }

        private void DrawNodeItem(Rect itemRect, Type nodeType)
        {
            var isHovered = itemRect.Contains(Event.current.mousePosition);
            if (isHovered)
            {
                GUI.DrawTexture(itemRect, GUIUtils.GetColorTexture(GUIUtils.Colors.ButtonPrimaryHover));
            }
            var nodeName = GetNodeDisplayName(nodeType);
            var labelStyle = GUIStyleBuilder.Create()
                .AsLabel()
                .WithNormalState(textColor: isHovered ? Color.white : GUIUtils.Colors.TextPrimary)
                .WithFontSize(11)
                .WithAlignment(TextAnchor.MiddleLeft)
                .WithPadding(8, 8, 4, 4);

            if (GUI.Button(itemRect, "", GUIStyle.none))
            {
                CreateNodeAtCursor(nodeType);
            }

            GUI.Label(itemRect, nodeName, labelStyle);
        }

        private string GetNodeDisplayName(Type nodeType)
        {
            var name = nodeType.Name;
            if (name.EndsWith("Node"))
                name = name.Substring(0, name.Length - 4);
            return name;
        }

        private void CreateNodeAtCursor(Type nodeType)
        {
            if (nodeType.IsSubclassOf(typeof(BaseCompositorNode)))
            {
                var node = (BaseCompositorNode)Activator.CreateInstance(nodeType);
                var worldPos = (CompositorManager.SearchMenuPosition / _manager.State.Zoom) - new Vector2(_manager.State.OffsetX, _manager.State.OffsetY);
                worldPos.y -= 30;

                node.Position = worldPos;
                _manager.AddNode(node);
            }
            _currentGroup = null;
            searchText = "";
            _scrollPosition = Vector2.zero;
            _manager.HideSearchMenu();
        }

        CurveDrawer _curveDrawer = new CurveDrawer();

        /// <summary>
        /// Renders the background of the compositor interface. Fills the entire screen area with the designated background color
        /// and displays a grid overlay scaled and offset based on the current zoom level and positional offset from the state.
        /// </summary>
        private void DrawBackground()
        {
            var background = GUIUtils.Colors.Background;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), GUIUtils.GetColorTexture(background));
            var gridArea = new Rect(0, 30, Screen.width, Screen.height - 60);
            // _curveDrawer.DrawGrid(gridArea, 10, 10, new Color(background.r, background.g, background.b, 0.3f));
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

            if (GUI.Button(new Rect(buttonX, 4, buttonWidth + 20, buttonHeight), "Render", CompositorStyles.HeaderButtonSecondary))
            {
                _manager.ProcessNodes();
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
                var titleRect = new Rect(nodeRect.x, nodeRect.y - 20, nodeRect.width, 20);
                // GUI.DrawTexture(titleRect, GUIUtils.GetColorTexture(GUIUtils.Colors.NodeHeader));

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
            var state = _manager.State;
            var contentRect = new Rect(2, 22, node.Size.x * state.Zoom - 4, node.Size.y * state.Zoom - 24);

            GUI.DrawTexture(new Rect(0, 20, node.Size.x * state.Zoom, node.Size.y * state.Zoom - 20), GUIUtils.GetColorTexture(new Color(GUIUtils.Colors.NodeBackground.r, GUIUtils.Colors.NodeBackground.g, GUIUtils.Colors.NodeBackground.b, 0.8f)));
            node.DrawContent(contentRect);

            for (var i = 0; i < node.Inputs.Count; i++)
            {
                var input = node.Inputs[i];
                var portRect = new Rect(input.LocalPosition.x * state.Zoom - 6, input.LocalPosition.y * state.Zoom - 6, 12, 12);

                bool isHovered = portRect.Contains(Event.current.mousePosition);
                CompositorStyles.DrawPort(portRect, input.IsConnected, true, input.AcceptedType, isHovered);
                var labelRect = new Rect(portRect.x + 18, portRect.y - 2, 100, 16);
                GUI.Label(labelRect, input.Name, CompositorStyles.PortLabel);
            }

            for (var i = 0; i < node.Outputs.Count; i++)
            {
                var output = node.Outputs[i];
                Rect portRect;

                if (output.PortMode == NodeOutput.PortPositioning.Fixed)
                    portRect = new Rect(output.LocalPosition.x * state.Zoom - 6, output.LocalPosition.y * state.Zoom - 6, 12, 12);
                else
                    portRect = new Rect(node.Size.x * state.Zoom - output.LocalPosition.x - 6, output.LocalPosition.y * state.Zoom - 6, 12, 12);

                bool isHovered = portRect.Contains(Event.current.mousePosition);
                CompositorStyles.DrawPort(portRect, output.Connections.Count > 0, false, output.OutputType, isHovered);
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
                        Vector2 startPos;
                        if (output.PortMode == NodeOutput.PortPositioning.Fixed)
                            startPos = GetPortWorldPosition(node, output.LocalPosition);
                        else
                            startPos = GetPortWorldPosition(node, new Vector2(node.Size.x - output.LocalPosition.x, output.LocalPosition.y));
                        var endPos = GetPortWorldPosition(connection.InputNode, connection.InputNode.Inputs[connection.InputIndex].LocalPosition);
                        var halfxdiff = (endPos - startPos) / 2;
                        var connectionColor = GetConnectionColor(output.OutputType);
                        var pulseIntensity = Mathf.Sin(Time.time * 3f) * 0.1f + 0.9f;
                        connectionColor = Color.Lerp(connectionColor, GUIUtils.Colors.ConnectionHighlight, pulseIntensity * 0.3f);
                        GUIUtils.DrawBezierCurve(startPos, endPos, connectionColor, 3f, Entry._segments.Value, halfxdiff.x);
                    }
                }
            }

            if (_manager._isConnecting && _manager._connectionStartOutput != null)
            {
                Vector2 startPos;
                if (_manager._connectionStartOutput.PortMode == NodeOutput.PortPositioning.Fixed)
                    startPos = GetPortWorldPosition(_manager._connectionStartNode, _manager._connectionStartOutput.LocalPosition);
                else
                    startPos = GetPortWorldPosition(_manager._connectionStartNode, new Vector2(_manager._connectionStartNode.Size.x - _manager._connectionStartOutput.LocalPosition.x, _manager._connectionStartOutput.LocalPosition.y));
                var endPos = Event.current.mousePosition;
                var halfxdiff = (endPos - startPos) / 2;

                var dragColor = Color.Lerp(GUIUtils.Colors.ConnectionHighlight, Color.white, 0.3f);
                dragColor.a = 0.8f;

                GUIUtils.DrawBezierCurve(startPos, endPos, dragColor, 4f, Entry._segments.Value, halfxdiff.x);
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

            var perfText = $"FPS: {(1f / Time.deltaTime):F0} | Memory: {(GC.GetTotalMemory(false) / 1024f / 1024f):F1} MB";
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
        public Vector2 GetPortWorldPosition(ICompositorNode node, Vector2 localPosition)
        {
            var state = _manager.State;
            var nodeWorldPos = GUIUtils.ScaleVector2(node.Position, state.Zoom, new Vector2(state.OffsetX, state.OffsetY));

            return nodeWorldPos + localPosition * state.Zoom;
        }

        public Vector2 GetPortScaledPosition(Vector2 localPosition)
        {
            var state = _manager.State;
            return localPosition * state.Zoom;
        }

        /// <summary>
        /// Determines the color used for rendering the connection lines in the compositor graph,
        /// based on the type of data being transmitted through the connection.
        /// </summary>
        /// <param name="dataType">The type of the data being transmitted through the connection, such as Texture2D, float, or Color.</param>
        /// <returns>A Color instance representing the visual style of the connection based on the data type.</returns>
        private Color GetConnectionColor(SocketType socketType)
        {
            var baseColor = GUIUtils.Colors.SocketColors.GetSocketColor(socketType);
            return Color.Lerp(baseColor, GUIUtils.Colors.Connection, 0.3f);
        }
    }
}