using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK
{
    /// <summary>
    /// Serves as the foundational class for all compositing nodes in the system.
    /// Provides shared properties and methods for managing node connections,
    /// layout, identification, and core behaviors within the visual compositing framework.
    /// </summary>
    public abstract class BaseCompositorNode : ICompositorNode
    {
        private static int _nextId = 1;

        /// <summary>
        /// Represents a unique identifier for the node within the compositing system.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned during the node's instantiation and ensures each node can be distinctly identified.
        /// It is typically used for internal management, tracking, and referencing nodes within the compositing framework.
        /// </remarks>
        public int Id { get; private set; }
        /// <summary>
        /// Defines a descriptive title for the node, indicating its specific type or functionality within the compositor system.
        /// </summary>
        /// <remarks>
        /// This property serves as a human-readable identifier to convey a node's purpose or role within the context of a larger
        /// node graph or workflow. It facilitates user interaction by providing clarity and distinguishing between various node types.
        /// </remarks>
        public abstract string Title { get; }
        /// <summary>
        /// Determines the position of the node within the visual interface or layout space.
        /// </summary>
        /// <remarks>
        /// This property is essential for defining or updating the coordinates of the node in a 2D space,
        /// typically influencing its rendered location and organization in the context of a compositor graph.
        /// </remarks>
        public Vector2 Position { get; set; }
        /// <summary>
        /// Defines the width and height of the node in the compositor graph.
        /// </summary>
        /// <remarks>
        /// This property is used to specify or retrieve the size of the node's visual representation,
        /// typically affecting layout and positioning of connected inputs and outputs.
        /// </remarks>
        /// <value>Defaults to Vector2(200, 150)</value>
        public Vector2 Size { get; set; } = new Vector2(200, 150);
        /// <summary>
        /// Indicates whether the node is currently selected.
        /// </summary>
        /// <remarks>
        /// This property is used to track the selection state of a compositor node.
        /// It can be set or queried to determine or modify the current selection status.
        /// </remarks>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Retrieves the value of an input node by its name if it exists within the node's input collection.
        /// </summary>
        /// <param name="name">The name of the input for which the value is being retrieved.</param>
        /// <returns>The value associated with the specified input name if found, or null if no such input exists.</returns>
        public NodeInput TryGetInput(string name) => _inputs.FirstOrDefault(input => input.Name == name);

        /// <summary>
        /// Represents the collection of input ports for the node.
        /// </summary>
        /// <remarks>
        /// This property contains a list of input connections for the node. It is used to define
        /// and store all data or signals that can be received by the node during processing.
        /// Input ports allow nodes to be connected in a graph-like structure, enabling
        /// information flow between nodes.
        /// </remarks>
        internal List<NodeInput> _inputs { get; } = new List<NodeInput>();
        /// <summary>
        /// Stores the collection of output ports for the compositing node.
        /// </summary>
        /// <remarks>
        /// This property contains the list of outputs that can emit data or results
        /// from the node. It provides mechanisms to define, access, and manage the outputs
        /// that other nodes can connect to for data processing within the compositing graph.
        /// </remarks>
        internal List<NodeOutput> _outputs { get; } = new List<NodeOutput>();

        /// <summary>
        /// Represents a collection of inputs that allow the node to receive data or values from other nodes in the compositing system.
        /// </summary>
        /// <remarks>
        /// Each input is defined as a <see cref="NodeInput"/> object, which specifies properties such as the accepted data type,
        /// the value being received, and the connection to a specific output of another node.
        /// Inputs enable the node to establish dependencies and process its functionality based on the incoming data or connections.
        /// </remarks>
        public List<NodeInput> Inputs => _inputs;
        /// <summary>
        /// Represents the collection of outputs for the compositor node.
        /// </summary>
        /// <remarks>
        /// Each output in this collection provides data or signals that can be connected to
        /// the inputs of other nodes. Outputs define the result produced by the node and
        /// its potential propagation throughout the compositing system.
        /// </remarks>
        public List<NodeOutput> Outputs => _outputs;

        public void AddOutput(string name, SocketType type)
        {
            _outputs.Add(new NodeOutput(name, type, new Vector2(Size.x, 40 * (_outputs.Count + 1))));
        }

        /// <summary>
        /// Serves as the foundational class for all compositing nodes in the system.
        /// Provides shared properties and methods for managing node connections,
        /// layout, identification, and core behaviors within the visual compositing framework.
        /// </summary>
        protected BaseCompositorNode()
        {
            Id = _nextId++;
            Initialize();
            InitializePorts();
        }

        /// <summary>
        /// Initializes the input and output ports for the node. This method is responsible for defining
        /// the connections a node can establish with other nodes by adding appropriate input and output ports.
        /// It is called during the node's construction to set up its port structure.
        /// </summary>
        protected abstract void InitializePorts();

        /// <summary>
        /// Performs general node initialization. This method is called during node construction
        /// and provides an override point for custom initialization logic beyond port setup.
        /// </summary>
        /// <remarks>
        /// This method is called before <see cref="InitializePorts()"/> during node instantiation.
        /// Override this method to perform additional setup such as setting default values,
        /// initializing internal state, or configuring node-specific properties.
        /// </remarks>
        protected virtual void Initialize() { }

        /// <summary>
        /// Updates the state or internal logic of the node. This method is called during the compositor's update cycle
        /// and can be used to perform operations such as recalculating values, refreshing internal data, or handling other necessary changes.
        /// </summary>
        public virtual void Update() { }
        /// <summary>
        /// Renders the visual content of the node within the specified rectangular area.
        /// This method is responsible for drawing custom UI or content relevant to the node's function.
        /// </summary>
        /// <param name="contentRect">The rectangle defining the area where the node's content should be drawn.</param>
        public abstract void DrawContent(Rect contentRect);
        /// <summary>
        /// Executes the core processing logic of the node. This method is responsible for
        /// performing any node-specific operations and transformations on the input data,
        /// generating the output results that downstream nodes may depend upon. It should be
        /// implemented by derived classes to define the specific behavior of the node during
        /// the compositor's processing cycle.
        /// </summary>
        public abstract void Process();
        /// <summary>
        /// Determines whether a connection can be established between the specified output of this node and the input of another node.
        /// </summary>
        /// <param name="other">The target node to evaluate a potential connection with.</param>
        /// <param name="outputIndex">The index of the output on this node to test for connection.</param>
        /// <param name="inputIndex">The index of the input on the target node to test for connection.</param>
        /// <returns>True if the connection is allowed based on type compatibility and index validity; otherwise, false.</returns>
        public virtual bool CanConnectTo(ICompositorNode other, int outputIndex, int inputIndex)
        {
            if (outputIndex >= Outputs.Count || inputIndex >= other.Inputs.Count)
                return false;

            var output = Outputs[outputIndex];
            var input = other.Inputs[inputIndex];

            if (output.OutputType == input.AcceptedType)
                return true;

            return IsConversionAllowed(output.OutputType, input.AcceptedType);
        }

        private bool IsConversionAllowed(SocketType from, SocketType to)
        {
            if (from == SocketType.RGBA)
                return true;
            if (from == SocketType.Alpha && (to == SocketType.RGBA || to == SocketType.Vector))
                return true;
            if (from == SocketType.Vector && (to == SocketType.RGBA || to == SocketType.Alpha))
                return true;
            return false;
        }

        /// <summary>
        /// Establishes a connection between the specified output of this node and the input of another node.
        /// </summary>
        /// <param name="other">The target node to connect to.</param>
        /// <param name="outputIndex">The index of the output in the current node to create the connection from.</param>
        /// <param name="inputIndex">The index of the input in the target node to connect to.</param>
        public virtual void ConnectTo(ICompositorNode other, int outputIndex, int inputIndex)
        {
            if (!CanConnectTo(other, outputIndex, inputIndex))
                return;

            var output = Outputs[outputIndex];
            var input = other.Inputs[inputIndex];

            if (input.IsConnected)
            {
                other.DisconnectInput(inputIndex);
            }

            var connection = new NodeConnection(other, inputIndex);
            output.Connections.Add(connection);

            input.ConnectedNode = this;
            input.ConnectedOutputIndex = outputIndex;
            input.Value = output.Value;

            if (other is LazyCompositorNode lazy)
                lazy.NotifyOutputChanged();
        }

        /// <summary>
        /// Disconnects the node output at the specified index, removing all its connections.
        /// </summary>
        /// <param name="outputIndex">The index of the output to be disconnected.</param>
        public virtual void Disconnect(int outputIndex)
        {
            if (outputIndex >= _outputs.Count)
                return;

            var output = _outputs[outputIndex];
            foreach (var connection in output.Connections)
            {
                var input = connection.InputNode.Inputs[connection.InputIndex];
                input.ConnectedNode = null;
                input.ConnectedOutputIndex = -1;
                input.Value = null;
            }

            output.Connections.Clear();
        }

        /// <summary>
        /// Disconnects the node input at the specified index, removing all its connections.
        /// </summary>
        /// <param name="inputIndex">The index of the input to be disconnected.</param>
        public virtual void DisconnectInput(int inputIndex)
        {
            if (inputIndex >= _inputs.Count)
                return;

            var input = _inputs[inputIndex];
            if (input.IsConnected)
            {
                var connectedOutput = input.ConnectedNode.Outputs[input.ConnectedOutputIndex];
                connectedOutput.Connections.RemoveAll(e => e.InputNode == this && e.InputIndex == inputIndex);

                input.ConnectedNode = null;
                input.ConnectedOutputIndex = -1;
                input.Value = null;
            }
        }

        public virtual void Dispose()
        {
            for (int i = 0; i < _outputs.Count; i++)
            {
                this.Disconnect(i);
            }

            for (int i = 0; i < _inputs.Count; i++)
            {
                this.DisconnectInput(i);
            }
        }
    }
}