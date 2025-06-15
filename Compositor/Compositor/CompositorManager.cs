using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK
{
    /// <summary>
    /// A manager that oversees and handles the core functionalities of the compositor system,
    /// including maintaining the state, managing nodes, and facilitating node connections.
    /// </summary>
    public class CompositorManager : IDisposable
    {
        private List<ICompositorNode> _nodes = new List<ICompositorNode>();
        private ICompositorNode _selectedNode;
        private bool _isDragging;
        private Vector2 _dragOffset;

        /// <summary>
        /// Represents the current state of the compositor system, encompassing viewport offsets,
        /// zoom levels, and visualization settings.
        /// </summary>
        /// <remarks>
        /// This property is responsible for tracking and adjusting various parameters
        /// related to the visual representation and interaction within the compositor interface.
        /// It includes details such as panning offsets, zoom scaling, and display options like gridlines and connections.
        /// </remarks>
        public CompositorState State { get; private set; }

        /// <summary>
        /// Represents the collection of compositor nodes managed by the compositor manager.
        /// </summary>
        /// <remarks>
        /// This property provides access to the internal list of compositor nodes used within
        /// the compositor system. It serves as a central repository for managing all nodes,
        /// enabling operations such as iteration, rendering, and individual node manipulation.
        /// </remarks>
        public List<ICompositorNode> Nodes => _nodes;
        /// <summary>
        /// Represents the currently selected compositing node in the manager.
        /// </summary>
        /// <remarks>
        /// This property holds a reference to the active or current node in the compositing graph.
        /// It is used to determine which node is being interacted with or modified at any given time.
        /// Changes or operations related to node-specific functionalities may rely on the value of this property.
        /// </remarks>
        public ICompositorNode SelectedNode => _selectedNode;


        public CompositorManager()
        {
            State = new CompositorState();
        }

        /// <summary>
        /// Creates and initializes the default nodes within the compositor workspace,
        /// setting up basic connections and positioning for an initial setup.
        /// This method is typically used to initialize a workspace with input and output nodes pre-connected.
        /// </summary>
        public void CreateDefaultNodes()
        {
            var inputNode = new InputNode();
            var outputNode = new OutputNode();
            var rgb2hsv = new RGB2HSVNode();

            inputNode.Position = new Vector2(200, 200);
            outputNode.Position = new Vector2(800, 800);
            rgb2hsv.Position = new Vector2(400, 400);

            inputNode.ConnectTo(outputNode, 0, 0);

            AddNode(inputNode);
            AddNode(rgb2hsv);
            AddNode(outputNode);
        }

        /// <summary>
        /// Adds a new node to the compositor workspace. The added node becomes part of the
        /// list of active nodes within the compositor.
        /// </summary>
        /// <param name="node">The node to be added. This node must implement the ICompositorNode interface and will be added to the current node collection.</param>
        public void AddNode(ICompositorNode node)
        {
            _nodes.Add(node);
        }

        /// <summary>
        /// Removes a node from the compositor workspace. If the node is currently selected, deselects it,
        /// disposes of the node, and removes it from the list of active nodes.
        /// </summary>
        /// <param name="node">The node to be removed. The node will be disposed of and cannot be reused.</param>
        public void RemoveNode(ICompositorNode node)
        {
            if (_selectedNode == node)
                _selectedNode = null;

            node.Dispose();
            _nodes.Remove(node);
        }

        /// <summary>
        /// Selects a node within the compositor workspace. Deselects any currently selected node
        /// and updates the state of the newly selected node.
        /// </summary>
        /// <param name="node">The node to be selected. If null, any currently selected node will be deselected.</param>
        public void SelectNode(ICompositorNode node)
        {
            if (_selectedNode != null)
                _selectedNode.IsSelected = false;

            _selectedNode = node;
            if (_selectedNode != null)
                _selectedNode.IsSelected = true;
        }

        /// <summary>
        /// Updates the state of the compositor by handling user input, updating all nodes in the workspace,
        /// and processing the nodes as required. Ensures that each node's state is refreshed and any necessary
        /// processing logic is executed.
        /// </summary>
        public void Update()
        {
            HandleInput();

            foreach (var node in _nodes)
            {
                node.Update();
            }

            ProcessNodes();
        }

        /// <summary>
        /// Handles user input to manipulate and interact with the nodes within the compositor workspace.
        /// This includes selecting, dragging, panning, and zooming operations based on mouse activity.
        /// </summary>
        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                var clickedNode = GetNodeAtPosition(mousePos);
                if (clickedNode != null)
                {
                    SelectNode(clickedNode);
                    _isDragging = true;
                    _dragOffset = mousePos - clickedNode.Position;
                }
                else
                {
                    _isDragging = true;
                    SelectNode(null);
                }
            }

            if (_isDragging)
            {
                if (_selectedNode == null)
                {
                    State.OffsetX += Input.GetAxis("Mouse X") * 12f;
                    State.OffsetY -= Input.GetAxis("Mouse Y") * 12f;
                }

                if (_selectedNode != null)
                {
                    Vector2 mousePos = Input.mousePosition;
                    mousePos.y = Screen.height - mousePos.y;
                    _selectedNode.Position = mousePos - _dragOffset;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            State.Zoom += scroll;
            State.Zoom = Mathf.Clamp(State.Zoom, 0.3f, 4f);
        }

        /// <summary>
        /// Retrieves the compositing node located at the specified position within the graphical workspace.
        /// The position is transformed based on the current state of the compositor, including zoom and offset,
        /// to properly map input coordinates to the underlying nodes.
        /// </summary>
        /// <param name="position">The screen-space position to check for a node, typically derived from user input such as mouse clicks.</param>
        /// <returns>The <see cref="ICompositorNode"/> at the specified position if one exists, or null if no node is found.</returns>
        private ICompositorNode GetNodeAtPosition(Vector2 position)
        {
            Vector2 transformedPos = (position - new Vector2(State.OffsetX, State.OffsetY)) / State.Zoom;

            return _nodes.FirstOrDefault(node =>
            {
                var rect = new Rect(node.Position, node.Size);
                return rect.Contains(transformedPos);
            });
        }

        /// <summary>
        /// Processes all compositor nodes in the current graph by traversing the connections between them
        /// and invoking the processing logic for each node. Ensures that nodes are processed in a manner
        /// that respects their dependencies and prevents duplicate processing.
        /// </summary>
        private void ProcessNodes()
        {
            var processedNodes = new HashSet<ICompositorNode>();
            var processingQueue = new Queue<ICompositorNode>();

            foreach (var node in _nodes)
            {
                if (ShouldProcessNode(node, processedNodes))
                {
                    processingQueue.Enqueue(node);
                }
            }

            while (processingQueue.Count > 0)
            {
                var node = processingQueue.Dequeue();
                if (processedNodes.Contains(node))
                    continue;

                node.Process();
                processedNodes.Add(node);

                foreach (var output in node.Outputs)
                {
                    foreach (var connection in output.Connections)
                    {
                        if (ShouldProcessNode(connection.InputNode, processedNodes))
                        {
                            processingQueue.Enqueue(connection.InputNode);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified node should be processed based on its connections
        /// and whether it has already been processed.
        /// </summary>
        /// <param name="node">The node to be checked for processing eligibility.</param>
        /// <param name="processedNodes">A set containing nodes that have already been processed.</param>
        /// <returns>
        /// True if the node should be processed, otherwise false.
        /// </returns>
        private bool ShouldProcessNode(ICompositorNode node, HashSet<ICompositorNode> processedNodes)
        {
            if (processedNodes.Contains(node))
                return false;

            foreach (var input in node.Inputs)
            {
                if (input.IsConnected && !processedNodes.Contains(input.ConnectedNode))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Resets the state of the compositor to its default values.
        /// </summary>
        public void Reset()
        {
            State.OffsetX = 0;
            State.OffsetY = 0;
            State.Zoom = 1;
        }

        /// <summary>
        /// Saves the current state of the compositor.
        /// </summary>
        public void Save()
        {
            // TODO
            Entry.Logger.LogDebug("Compositor state saved");
        }

        /// <summary>
        /// Connects the output of one compositor node to the input of another if the connection is allowed.
        /// </summary>
        /// <param name="outputNode">The node providing the output connection.</param>
        /// <param name="outputIndex">The index of the output from the <paramref name="outputNode"/>.</param>
        /// <param name="inputNode">The node receiving the input connection.</param>
        /// <param name="inputIndex">The index of the input on the <paramref name="inputNode"/>.</param>
        public void ConnectNodes(ICompositorNode outputNode, int outputIndex, ICompositorNode inputNode, int inputIndex)
        {
            if (outputNode.CanConnectTo(inputNode, outputIndex, inputIndex))
            {
                outputNode.ConnectTo(inputNode, outputIndex, inputIndex);
            }
        }

        public void Dispose()
        {
            foreach (var node in _nodes)
            {
                node.Dispose();
            }
            _nodes.Clear();
            _selectedNode = null;
        }

        /// <summary>
        /// Represents the state of the compositor application, including properties
        /// for managing the viewport and visualization settings.
        /// </summary>
        public class CompositorState
        {
            /// <summary>
            /// Represents the horizontal offset used in the viewport to translate or pan the content.
            /// </summary>
            /// <remarks>
            /// This property adjusts the horizontal position of the elements within the compositing view,
            /// allowing for navigation and alignment of visual components. It is primarily manipulated
            /// during user interactions such as dragging or panning with input devices like a mouse.
            /// </remarks>
            public float OffsetX { get; set; }
            /// Represents the vertical offset of the compositor's state.
            /// This property is used to translate the y-axis position of elements in the compositor
            /// relative to the user's input and current zoom level.
            /// Adjustments to this property are typically performed in response to user interactions,
            /// such as dragging the viewport, and it influences rendering coordinates for compositor
            /// elements.
            /// Modifying this property will affect the vertical positioning of all nodes, grids,
            /// and other visual elements rendered by the compositor.
            public float OffsetY { get; set; }
            /// Represents the zoom level of the compositor system.
            /// The value determines the scale at which nodes and grid elements are displayed.
            /// Restrictions:
            /// - The zoom level is clamped between 0.3 (minimum) and 4 (maximum).
            /// Default Value:
            /// - The default zoom value is 1, representing a 1:1 scale.
            public float Zoom { get; set; } = 1f;
            /// Represents a property that determines whether the grid is displayed in the compositor view.
            /// When set to true, the grid is visible, aiding in alignment and positioning of nodes.
            /// If set to false, the grid is hidden.
            /// Default value: true.
            public bool ShowGrid { get; set; } = true;
            /// Gets or sets a value indicating whether connections between nodes are displayed.
            /// This property determines whether the connections (links) between compositor nodes
            /// should be visually represented in the user interface. When set to true, all connections
            /// between nodes will be rendered. When set to false, these connections will be hidden.
            public bool ShowConnections { get; set; } = true;
        }
    }
}