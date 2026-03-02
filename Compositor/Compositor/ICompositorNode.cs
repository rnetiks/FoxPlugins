using System;
using System.Collections.Generic;
using System.Net.Sockets;
using ADV.Commands.Object;
using JetBrains.Annotations;
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
}