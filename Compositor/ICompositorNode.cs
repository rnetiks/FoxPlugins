using System;
using System.Collections.Generic;
using UnityEngine;

namespace Compositor.KK
{
    public interface ICompositorNode : IDisposable
    {
        int Id { get; }
        string Title { get; }
        Vector2 Position { get; set; }
        Vector2 Size { get; set; }
        bool IsSelected { get; set; }

        List<NodeInput> Inputs { get; }
        List<NodeOutput> Outputs { get; }

        void Update();
        void DrawContent(Rect contentRect);
        void Process();

        bool CanConnectTo(ICompositorNode other, int outputIndex, int inputIndex);
        void ConnectTo(ICompositorNode other, int outputIndex, int inputIndex);
        void Disconnect(int outputIndex);
        void DisconnectInput(int inputIndex);
    }

    public class NodeInput
    {
        public string Name { get; set; }
        public Type AcceptedType { get; set; }
        public object Value { get; set; }
        public ICompositorNode ConnectedNode { get; set; }
        public int ConnectedOutputIndex { get; set; } = -1;
        public bool IsConnected => ConnectedNode != null;
        public Vector2 LocalPosition { get; set; }

        public NodeInput(string name, Type acceptedType, Vector2 localPosition)
        {
            Name = name;
            AcceptedType = acceptedType;
            LocalPosition = localPosition;
        }

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

    public class NodeOutput
    {
        public string Name { get; set; }
        public Type OutputType { get; set; }
        public object Value { get; set; }
        public List<NodeConnection> Connections { get; set; }
        public Vector2 LocalPosition { get; set; }

        public NodeOutput(string name, Type outputType, Vector2 localPosition)
        {
            Name = name;
            OutputType = outputType;
            LocalPosition = localPosition;
            Connections = new List<NodeConnection>();
        }

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

    public class NodeConnection
    {
        public ICompositorNode InputNode { get; set; }
        public int InputIndex { get; set; }

        public NodeConnection(ICompositorNode inputNode, int inputIndex)
        {
            InputNode = inputNode;
            InputIndex = inputIndex;
        }
    }

    public abstract class BaseCompositorNode : ICompositorNode
    {
        private static int _nextId = 1;
        
        public int Id { get; private set; }
        public abstract string Title { get; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; } = new Vector2(200, 150);
        public bool IsSelected { get; set; }
        
        internal List<NodeInput> _inputs { get; } = new List<NodeInput>();
        internal List<NodeOutput> _outputs { get; } = new List<NodeOutput>();
        
        public List<NodeInput> Inputs => _inputs;
        public List<NodeOutput> Outputs => _outputs;
        
        protected BaseCompositorNode()
        {
            Id = _nextId++;
            InitializePorts();
        }
        
        protected abstract void InitializePorts();
        
        public virtual void Update(){}
        public abstract void DrawContent(Rect contentRect);
        public abstract void Process();
        public virtual bool CanConnectTo(ICompositorNode other, int outputIndex, int inputIndex)
        {
            if (outputIndex >= Outputs.Count || inputIndex >= other.Inputs.Count)
                return false;
            
            var output = Outputs[outputIndex];
            var input = other.Inputs[inputIndex];

            return input.AcceptedType.IsAssignableFrom(output.OutputType);
        }

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