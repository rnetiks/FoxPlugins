namespace Compositor.KK
{
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
}