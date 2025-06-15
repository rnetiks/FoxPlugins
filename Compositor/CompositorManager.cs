using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK
{
    public class CompositorManager : IDisposable
    {
        private List<ICompositorNode> _nodes = new List<ICompositorNode>();
        private ICompositorNode _selectedNode;
        private bool _isDragging;
        private Vector2 _dragOffset;

        public CompositorState State { get; private set; }

        public List<ICompositorNode> Nodes => _nodes;
        public ICompositorNode SelectedNode => _selectedNode;


        public CompositorManager()
        {
            State = new CompositorState();
        }

        public void CreateDefaultNodes()
        {
            var inputNode = new InputNode();
            inputNode.Position = new Vector2(200, 200);
            AddNode(inputNode);

            var outputNode = new OutputNode();
            outputNode.Position = new Vector2(800, 200);
            AddNode(outputNode);
        }

        public void AddNode(ICompositorNode node)
        {
            _nodes.Add(node);
        }

        public void RemoveNode(ICompositorNode node)
        {
            if (_selectedNode == node)
                _selectedNode = null;

            node.Dispose();
            _nodes.Remove(node);
        }

        public void SelectNode(ICompositorNode node)
        {
            if (_selectedNode != null)
                _selectedNode.IsSelected = false;

            _selectedNode = node;
            if (_selectedNode != null)
                _selectedNode.IsSelected = true;
        }

        public void Update()
        {
            HandleInput();

            foreach (var node in _nodes)
            {
                node.Update();
            }

            ProcessNodes();
        }

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
                    SelectNode(null);
                }
            }

            if (_isDragging)
            {
                State.OffsetX += Input.GetAxis("Mouse X") * 12f;
                State.OffsetY += Input.GetAxis("Mouse Y") * 12f;

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

        private ICompositorNode GetNodeAtPosition(Vector2 position)
        {
            Vector2 transformedPos = (position - new Vector2(State.OffsetX, State.OffsetY)) / State.Zoom;

            return _nodes.FirstOrDefault(node =>
            {
                var rect = new Rect(node.Position, node.Size);
                return rect.Contains(transformedPos);
            });
        }

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

        public void Reset()
        {
            State.OffsetX = 0;
            State.OffsetY = 0;
            State.Zoom = 1;
        }

        public void Save()
        {
            // TODO
            Entry.Logger.LogDebug("Compositor state saved");
        }

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

        public class CompositorState
        {
            public float OffsetX { get; set; }
            public float OffsetY { get; set; }
            public float Zoom { get; set; } = 1f;
            public bool ShowGrid { get; set; } = true;
            public bool ShowConnections { get; set; } = true;
        }
    }
}