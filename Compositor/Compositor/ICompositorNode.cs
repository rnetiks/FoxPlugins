using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

namespace Compositor.KK
{
    /// <summary>
    /// Defines the interface for a compositing node within a visual graph system.
    /// Compositor nodes serve as building blocks for processing and transforming data,
    /// handling inputs, outputs, and interconnections in graphical or procedural workflows.
    /// This interface provides essential properties for node management, including position,
    /// size, selection state, and methods for updating, rendering, and connecting nodes.
    /// </summary>
    public interface ICompositorNode : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for a node within the compositing system.
        /// </summary>
        /// <remarks>
        /// The ID serves as a distinct identifier for each node, facilitating node management and ensuring integrity
        /// in operations such as connections, updates, and serialization.
        /// </remarks>
        int Id { get; }
        /// <summary>
        /// Represents the title of a compositing node within a graphical user interface.
        /// </summary>
        /// <remarks>
        /// The title serves as a textual identifier for the node in the user interface, providing clarity and context.
        /// It is displayed prominently, typically at the top of the node, to facilitate easy identification during node-based operations.
        /// </remarks>
        string Title { get; }
        /// <summary>
        /// Represents the position of a node within a compositing system.
        /// </summary>
        /// <remarks>
        /// This property defines the 2D coordinates of a node on the canvas, which determines its placement
        /// and is used for rendering, selection, and interaction. Updating this property allows nodes to
        /// be moved or arranged dynamically within the system.
        /// </remarks>
        Vector2 Position { get; set; }
        /// <summary>
        /// Defines the size of a node in a compositing system.
        /// </summary>
        /// <remarks>
        /// This property specifies the width and height of a node's visual representation within the editor or canvas.
        /// It is used to calculate the bounding rectangle for rendering, hit detection, and layout adjustments.
        /// The size determines the space the node occupies and affects the alignment of its inputs and outputs.
        /// </remarks>
        Vector2 Size { get; set; }
        /// <summary>
        /// Indicates whether the node is currently selected within the compositing system.
        /// </summary>
        /// <remarks>
        /// This property represents the selection state of the node, allowing the system
        /// to visually or functionally distinguish selected nodes from non-selected ones.
        /// It is primarily utilized when managing user interactions, rendering, and
        /// performing node-specific operations.
        /// </remarks>
        bool IsSelected { get; set; }

        /// <summary>
        /// Represents the input ports of a compositor node, allowing data or connections to be supplied to the node's processing logic.
        /// </summary>
        /// <remarks>
        /// This property provides access to a collection of inputs for the node. Each input encapsulates metadata such as its name, the data type it accepts,
        /// and information about any connected nodes. It enables the configuration and interconnection of nodes within the compositing system.
        /// </remarks>
        List<NodeInput> Inputs { get; }
        /// <summary>
        /// Represents the collection of outputs available from the node within a compositing system.
        /// </summary>
        /// <remarks>
        /// This property provides access to the outputs of the node, enabling connections to other nodes.
        /// It facilitates the flow of data or signals from this node to connected nodes, supporting the overall data processing
        /// and compositing workflows within the system.
        /// </remarks>
        List<NodeOutput> Outputs { get; }

        /// <summary>
        /// Updates the state or behavior of the node. This method is typically invoked within the compositor's update cycle
        /// to allow the node to perform necessary operations such as recalculations, data refreshes, or internal state updates.
        /// </summary>
        void Update();
        /// <summary>
        /// Renders the content of the node within a defined rectangular area.
        /// Generally used for drawing controls, text, or other visual elements specific to the node.
        /// </summary>
        /// <param name="contentRect">The rectangular area in which the node content will be drawn.</param>
        void DrawContent(Rect contentRect);
        /// <summary>
        /// Executes the primary processing logic of the node. This method handles
        /// data manipulation, computation, or transformation specific to the node's
        /// purpose within the composition, ensuring that the output data is prepared
        /// for downstream nodes in the processing pipeline.
        /// </summary>
        void Process();

        /// <summary>
        /// Determines whether a connection can be established between the specified output of this node and the input of another node.
        /// </summary>
        /// <param name="other">The target node to evaluate a potential connection with.</param>
        /// <param name="outputIndex">The index of the output on this node to test for connection.</param>
        /// <param name="inputIndex">The index of the input on the target node to test for connection.</param>
        /// <returns>True if the connection is allowed based on type compatibility and index validity; otherwise, false.</returns>
        bool CanConnectTo(ICompositorNode other, int outputIndex, int inputIndex);
        /// <summary>
        /// Establishes a connection between the specified output of this node and the input of another node.
        /// </summary>
        /// <param name="other">The target node to connect to.</param>
        /// <param name="outputIndex">The zero-based index of the output in the current node to connect from.</param>
        /// <param name="inputIndex">The zero-based index of the input in the target node to connect to.</param>
        void ConnectTo(ICompositorNode other, int outputIndex, int inputIndex);
        /// <summary>
        /// Disconnects the node output at the specified index, removing all its connections.
        /// </summary>
        /// <param name="outputIndex">The zero-based index of the output to disconnect.</param>
        void Disconnect(int outputIndex);
        /// <summary>
        /// Disconnects the input at the specified index, effectively severing its connection to any output.
        /// </summary>
        /// <param name="inputIndex">The zero-based index of the input to disconnect.</param>
        void DisconnectInput(int inputIndex);
    }

    /// <summary>
    /// Represents an input within the compositing framework, providing details about the name,
    /// data type it accepts, current value, connection status, and its position in the visual graph.
    /// The input can be connected to an output of another node, facilitating data transfer within the system.
    /// </summary>
    public class NodeInput
    {
        /// <summary>
        /// Represents the name of a node input within a compositing system.
        /// </summary>
        /// <remarks>
        /// This property identifies the input port of a node, allowing it to be labeled and referenced in the user interface.
        /// It serves to distinguish one input from another and to associate connections with their corresponding inputs.
        /// </remarks>
        public string Name { get; set; }
        /// <summary>
        /// Represents the type of value that the node input can accept.
        /// </summary>
        /// <remarks>
        /// This property defines the specific type that is compatible with this node input.
        /// It is used to enforce type safety when establishing connections between nodes,
        /// ensuring that only values of this type or derived from it can be assigned to the input.
        /// </remarks>
        public Type AcceptedType { get; set; }
        /// <summary>
        /// Represents the current value of the node input.
        /// </summary>
        /// <remarks>
        /// This property holds the data assigned to the input port. The type of the value
        /// is determined by the accepted type of the node input. It is typically used to
        /// transfer processed or raw data between connected nodes in a compositing system.
        /// </remarks>
        public object Value { get; set; }
        /// <summary>
        /// Represents the node to which this input is connected.
        /// </summary>
        /// <remarks>
        /// This property provides a reference to the node that supplies data to the current input.
        /// It facilitates interaction and data flow between connected nodes within the graph.
        /// When a connection is established, this property is set to the source node of the connection.
        /// If no connection exists, it is set to null.
        /// </remarks>
        public ICompositorNode ConnectedNode { get; set; }
        /// <summary>
        /// Represents the index of the output port in the connected node.
        /// </summary>
        /// <remarks>
        /// This property specifies the zero-based index of the output in the connected node
        /// to which the current input is linked. It is used to determine the specific
        /// output that supplies data to this input. If no connection exists, the value is -1.
        /// </remarks>
        public int ConnectedOutputIndex { get; set; } = -1;
        /// <summary>
        /// Indicates whether the input is connected to another node's output.
        /// </summary>
        /// <remarks>
        /// This property returns true if the input has an associated connection with
        /// another node's output; otherwise, it returns false. It is used to determine
        /// the current linkage status of the input to validate connections and retrieve
        /// data from connected nodes.
        /// </remarks>
        public bool IsConnected => ConnectedNode != null;
        /// <summary>
        /// Represents the local position of the node input within its parent node.
        /// </summary>
        /// <remarks>
        /// This property defines the position of the input port relative to the node's coordinate system.
        /// It is used for rendering and calculating the input port's location during UI interactions
        /// such as drawing connections or detecting mouse events.
        /// </remarks>
        public Vector2 LocalPosition { get; set; }

        /// <summary>
        /// Represents an input node connection within a compositing system.
        /// This class includes details about the node input's name, supported type, current value,
        /// connected node, and its local position in the UI.
        /// </summary>
        public NodeInput(string name, Type acceptedType, Vector2 localPosition)
        {
            Name = name;
            AcceptedType = acceptedType;
            LocalPosition = localPosition;
        }

        /// <summary>
        /// Retrieves the value associated with the input, casting it to the specified type.
        /// If the input is connected to an output of another node, this method returns the value of that connection.
        /// Otherwise, it returns the local value of the input.
        /// </summary>
        /// <typeparam name="T">The expected type of the value to retrieve.</typeparam>
        /// <returns>The value cast to the specified type, or null if the cast is not possible or the value is unavailable.</returns>
        public T GetValue<T>() where T : class
        {
            if (IsConnected && ConnectedNode.Outputs.Count > ConnectedOutputIndex)
            {
                var output = ConnectedNode.Outputs[ConnectedOutputIndex];
                return output.Value as T;
            }

            return Value as T;
        }
    }

    /// <summary>
    /// Represents the output of a compositing node within a visual compositing framework.
    /// Encapsulates the output's name, data type, current value, local position relative to the node,
    /// and its connections to other nodes' input ports. This class allows the output to be linked
    /// to multiple other nodes and facilitates data propagation between connected nodes in the graph.
    /// </summary>
    public class NodeOutput
    {
        /// <summary>
        /// Represents the name of the node output.
        /// </summary>
        /// <remarks>
        /// This property specifies the identifier or label for the node output.
        /// It is used to display and distinguish output ports in the user interface
        /// and to associate connections with their corresponding outputs.
        /// </remarks>
        public string Name { get; set; }
        /// <summary>
        /// Specifies the data type of the output generated by the node.
        /// </summary>
        /// <remarks>
        /// This property determines the type of data that the output port can produce or transmit.
        /// It is utilized to ensure compatibility during connections between nodes by
        /// validating whether the receiving input port can accept the output's data type.
        /// </remarks>
        public Type OutputType { get; set; }
        /// <summary>
        /// Represents the value of a node output in the compositor graph.
        /// </summary>
        /// <remarks>
        /// This property defines the data or object being output by the node output.
        /// It can hold any object type and is used to transfer information from one
        /// node to another within the compositor system. Changes to this property
        /// should be propagated to connected input nodes to ensure synchronization
        /// across the graph.
        /// </remarks>
        public object Value { get; set; }
        /// <summary>
        /// Represents the collection of connections established by this output port to other nodes' input ports.
        /// </summary>
        /// <remarks>
        /// This property maintains a list of all the connections originating from the output port.
        /// Each connection links the output port to an input port of another node, facilitating data flow
        /// within the compositing system. Connections are dynamically updated as nodes are linked or unlinked.
        /// </remarks>
        public List<NodeConnection> Connections { get; set; }
        /// <summary>
        /// Represents the local position of an output port in the node's coordinate space.
        /// </summary>
        /// <remarks>
        /// This property defines the position of the output port relative to the node's origin.
        /// It is primarily used for graphical rendering and determining interaction points
        /// within the user interface when connecting nodes.
        /// </remarks>
        public Vector2 LocalPosition { get; set; }

        /// <summary>
        /// Represents the output of a compositing node, allowing connections to inputs of other nodes.
        /// Stores metadata about the output such as its name, data type, and positional information
        /// on the node it belongs to, as well as its connections to other nodes.
        /// </summary>
        public NodeOutput(string name, Type outputType, Vector2 localPosition)
        {
            Name = name;
            OutputType = outputType;
            LocalPosition = localPosition;
            Connections = new List<NodeConnection>();
        }

        /// <summary>
        /// Sets the value of the output and propagates the value to all connected inputs of other nodes.
        /// This method ensures that any changes to an output value are reflected in its connected nodes.
        /// </summary>
        /// <param name="value">The new value to be assigned to this output and propagated to the connected inputs.</param>
        public void SetValue(object value)
        {
            Value = value;

            foreach (var connection in Connections)
            {
                if (connection.InputNode.Inputs.Count > connection.InputIndex)
                {
                    connection.InputNode.Inputs[connection.InputIndex].Value = value;
                }
            }
        }
    }

    /// <summary>
    /// Represents a connection between a node's output port and another node's input port
    /// within a visual compositing framework. Encapsulates information about the destination node
    /// and the index of the input port being connected.
    /// </summary>
    public class NodeConnection
    {
        /// <summary>
        /// Represents the input node that a connection targets within the compositing system.
        /// </summary>
        /// <remarks>
        /// This property identifies the source node associated with a specific connection's input.
        /// It is used internally to determine the node providing data to this input.
        /// </remarks>
        public ICompositorNode InputNode { get; set; }
        /// <summary>
        /// Represents the index of the input slot in a target node within a connection.
        /// </summary>
        /// <remarks>
        /// This property identifies the specific input slot in a target node that a connection is linked to.
        /// It is primarily used to establish or manage connections between nodes in the compositing system.
        /// </remarks>
        public int InputIndex { get; set; }

        /// <summary>
        /// Represents a connection between the output of one node and the input of another node within a compositing framework.
        /// Provides details about the input node and the index of the input port it connects to.
        /// </summary>
        public NodeConnection(ICompositorNode inputNode, int inputIndex)
        {
            InputNode = inputNode;
            InputIndex = inputIndex;
        }
    }

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

        /// <summary>
        /// Serves as the foundational class for all compositing nodes in the system.
        /// Provides shared properties and methods for managing node connections,
        /// layout, identification, and core behaviors within the visual compositing framework.
        /// </summary>
        protected BaseCompositorNode()
        {
            Id = _nextId++;
            InitializePorts();
        }

        /// <summary>
        /// Initializes the input and output ports for the node. This method is responsible for defining
        /// the connections a node can establish with other nodes by adding appropriate input and output ports.
        /// It is called during the node's construction to set up its port structure.
        /// </summary>
        protected abstract void InitializePorts();

        /// <summary>
        /// Updates the state or internal logic of the node. This method is called during the compositor's update cycle
        /// and can be used to perform operations such as recalculating values, refreshing internal data, or handling other necessary changes.
        /// </summary>
        public virtual void Update(){}
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

            return input.AcceptedType.IsAssignableFrom(output.OutputType);
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
                Disconnect(i);
            }

            for (int i = 0; i < _inputs.Count; i++)
            {
                DisconnectInput(i);
            }
        }
    }
}