using System;
using Compositor.KK.Utils;
using UnityEngine;

namespace Compositor.KK
{
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
        public SocketType AcceptedType { get; set; }
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
        public NodeInput(string name, SocketType acceptedType, Vector2 localPosition)
        {
            Name = name;
            AcceptedType = acceptedType;
            LocalPosition = localPosition;
        }

        public int PixelComponents
        {
            get
            {
                switch (AcceptedType)
                {

                    case SocketType.RGBA:
                        return 4;
                    case SocketType.Alpha:
                        return 1;
                    case SocketType.Vector:
                        return 3;
                    case SocketType.UV:
                    case SocketType.Text:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
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
                if (output.OutputType != AcceptedType)
                {
                    var convertedValue = Converter.FastConvert(output.OutputType, AcceptedType, output.Value as float[]);
                    return convertedValue as T;
                }
                return output.Value as T;
            }

            return Value as T;
        }
    }
}