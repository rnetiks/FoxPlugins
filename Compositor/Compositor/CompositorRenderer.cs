using System;
using DefaultNamespace.Compositor;
using UIBuilder;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Compositor.KK
{
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

        public void DrawCompositor()
        {
            DrawBackground();
            DrawHeader();
            DrawNodes();
            DrawConnections();
            DrawStatusBar();
        }

        private void DrawBackground()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), GUIUtils.GetColorTexture(GUIUtils.Colors.Background));
            var gridArea = new Rect(0, 30, Screen.width, Screen.height - 60);
            CompositorStyles.DrawGrid(gridArea, _manager.State.Zoom, new Vector2(_manager.State.OffsetX, _manager.State.OffsetY));
        }

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
            
            if (GUI.Button(new Rect(buttonX, 4, buttonWidth + 20, buttonHeight), "➕ Filter", CompositorStyles.HeaderButtonSecondary))
            {
                AddFilterNode();
            }
            buttonX += buttonWidth + 20 + buttonSpacing;

            if (GUI.Button(new Rect(buttonX, 4, buttonWidth + 20, buttonHeight), "🔧 Transform", CompositorStyles.HeaderButtonSecondary))
            {
                AddTransformNode();
            }

            var statsText = $"Nodes: {_manager.Nodes.Count} | Textures: {TextureCache.Count} | Zoom: {_manager.State.Zoom:F1}x";
            var statsSize = GUI.skin.label.CalcSize(new GUIContent(statsText));
            var statsRect = new Rect(Screen.width - statsSize.x - 15, 4, statsSize.x, buttonHeight);
            GUI.Label(statsRect, statsText, CompositorStyles.StatusLabel);
        }

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
                
                var titleStyle = node.IsSelected ? 
                    GUIStyleBuilder.CreateFrom(CompositorStyles.NodeTitle).WithNormalState(textColor: GUIUtils.Colors.TextAccent) :
                    CompositorStyles.NodeTitle;
                GUI.Label(titleRect, node.Title, titleStyle);
            }
        }

        private void DrawNodeWindow(ICompositorNode node)
        {
            var contentRect = new Rect(2, 22, node.Size.x - 4, node.Size.y - 24);
            
            GUI.DrawTexture(new Rect(0, 20, node.Size.x, node.Size.y - 20), GUIUtils.GetColorTexture(new Color(GUIUtils.Colors.NodeBackground.r, GUIUtils.Colors.NodeBackground.g, GUIUtils.Colors.NodeBackground.b, 0.8f)));
            node.DrawContent(contentRect);
            
            for (var i = 0; i < node.Inputs.Count; i++)
            {
                var input = node.Inputs[i];
                var portRect = new Rect(input.LocalPosition.x - 6, input.LocalPosition.y - 6, 12, 12);

                bool isHovered = portRect.Contains(Event.current.mousePosition);
                CompositorStyles.DrawPort(portRect, input.IsConnected, true, isHovered);
                var labelRect = new Rect(portRect.x + 18, portRect.y - 2, 100, 16);
                GUI.Label(labelRect, input.Name, CompositorStyles.PortLabel);

                if (input.IsConnected)
                {
                    var indicatorRect = new Rect(portRect.x + 18, portRect.y + 12, 8, 2);
                    GUI.DrawTexture(indicatorRect, GUIUtils.GetColorTexture(GUIUtils.Colors.PortConnected));
                }
            }
            
            for (var i = 0; i < node.Outputs.Count; i++)
            {
                var output = node.Outputs[i];
                var portRect = new Rect(output.LocalPosition.x - 6, output.LocalPosition.y - 6, 12, 12);
                bool isHovered = portRect.Contains(Event.current.mousePosition);
                CompositorStyles.DrawPort(portRect, output.Connections.Count > 0, false, isHovered);
                var labelRect = new Rect(portRect.x - 85, portRect.y - 2, 80, 16);
                var labelStyle = GUIStyleBuilder.CreateFrom(CompositorStyles.PortLabel).WithAlignment(TextAnchor.MiddleRight);
                GUI.Label(labelRect, output.Name, labelStyle);

                if (output.Connections.Count > 0)
                {
                    var countRect = new Rect(portRect.x - 90, portRect.y + 12, 20, 12);
                    var countStyle = GUIStyleBuilder.CreateFrom(CompositorStyles.PortLabel)
                        .WithAlignment(TextAnchor.MiddleRight)
                        .WithFontSize(7)
                        .WithNormalState(textColor: GUIUtils.Colors.TextSuccess);
                    GUI.Label(countRect, $"x{output.Connections.Count}", countStyle);
                }
            }
        }

        private void DrawConnections()
        {
            var state = _manager.State;

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

        private Vector2 GetPortWorldPosition(ICompositorNode node, Vector2 localPosition)
        {
            var state = _manager.State;
            var nodeWorldPos = GUIUtils.ScaleVector2(node.Position, state.Zoom, new Vector2(state.OffsetX, state.OffsetY));

            nodeWorldPos.y += 30;

            return nodeWorldPos + localPosition * state.Zoom;
        }

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

        private void AddFilterNode()
        {
            var filterNode = new FilterNode();
            filterNode.Position = new Vector2(400 + Random.Range(-50, 50), 250 + Random.Range(-50, 50));
            _manager.AddNode(filterNode);
        }

        private void AddTransformNode()
        {
            var transformNode = new InputNode.TransformNode();
            transformNode.Position = new Vector2(600 + Random.Range(-50, 50), 250 + Random.Range(-50, 50));
            _manager.AddNode(transformNode);
        }
    }
}