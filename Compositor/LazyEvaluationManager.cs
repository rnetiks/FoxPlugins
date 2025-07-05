using System;
using System.Collections.Generic;
using Compositor.KK;
using Compositor.KK.Compositor;
using UnityEngine;

namespace DefaultNamespace
{
    public class LazyEvaluationManager
    {
        private Dictionary<ICompositorNode, NodeEvaluationState> _nodeStates = new Dictionary<ICompositorNode, NodeEvaluationState>();
        private HashSet<ICompositorNode> _dirtyNodes = new HashSet<ICompositorNode>();
        private int _currentEvaluationFrame = 0;

        public void RegisterNode(ICompositorNode node)
        {
            if (!_nodeStates.ContainsKey(node))
            {
                _nodeStates[node] = new NodeEvaluationState();
            }
        }
        
        public void UnregisterNode(ICompositorNode node)
        {
            if (node == null)
                return;
            _nodeStates.Remove(node);
            _dirtyNodes.Remove(node);
        }

        public void MarkNodeDirty(ICompositorNode node)
        {
            if(!_nodeStates.ContainsKey(node))
                RegisterNode(node);

            _dirtyNodes.Add(node);
            _nodeStates[node].LastModifiedFrame = Time.frameCount;

            MarkDownstreamNodesDirty(node);
        }

        private void MarkDownstreamNodesDirty(ICompositorNode node)
        {
            foreach (var output in node.Outputs)
            {
                foreach (var connection in output.Connections)
                {
                    if (!_dirtyNodes.Contains(connection.InputNode))
                    {
                        _dirtyNodes.Add(connection.InputNode);
                        _nodeStates[connection.InputNode].LastModifiedFrame = Time.frameCount;
                        MarkDownstreamNodesDirty(connection.InputNode);
                    }
                }
            }
        }

        public bool ShouldEvaluateNode(ICompositorNode node)
        {
            if(!_nodeStates.ContainsKey(node))
                RegisterNode(node);

            var state = _nodeStates[node];

            if (_dirtyNodes.Contains(node))
                return true;
            
            foreach (var input in node.Inputs)
            {
                if (input.IsConnected)
                {
                    var connectedState = _nodeStates[input.ConnectedNode];
                    if (connectedState.LastEvaluatedFrame > state.LastEvaluatedFrame)
                        return true;
                }
            }

            return false;
        }

        public void MarkNodeEvaluated(ICompositorNode node)
        {
            if (!_nodeStates.ContainsKey(node))
                RegisterNode(node);

            _nodeStates[node].LastEvaluatedFrame = _currentEvaluationFrame;
            _dirtyNodes.Remove(node);
        }

        public void BeginEvaluation()
        {
            _currentEvaluationFrame = Time.frameCount;
        }

        public EvaluationStats GetEvaluationStats()
        {
            return new EvaluationStats
            {
                TotalNodes = _nodeStates.Count,
                DirtyNodes = _dirtyNodes.Count,
                CurrentFrame = _currentEvaluationFrame
            };
        }
    }

    public class NodeEvaluationState
    {
        public int LastEvaluatedFrame = -1;
        public int LastModifiedFrame = -1;
        public long LastModifiedTime = 0;
    }

    public struct EvaluationStats
    {
        public int TotalNodes;
        public int DirtyNodes;
        public int CurrentFrame;
    }

    public abstract class LazyCompositorNode : BaseCompositorNode
    {
        protected LazyEvaluationManager _evaluationManager;
        private bool _hasBeenProcessed = false;
        private int _lastProcessFrame = -1;

        public virtual void SetEvaluationManager(LazyEvaluationManager manager)
        {
            _evaluationManager = manager;
            _evaluationManager.RegisterNode(this);
        }

        public override void Process()
        {
            if (_evaluationManager != null)
            {
                if (!_evaluationManager.ShouldEvaluateNode(this))
                {
                    return;
                }
            }
            else
            {
                if (_lastProcessFrame == Time.frameCount && _hasBeenProcessed)
                    return;
            }

            try
            {
                ProcessInternal();
                _hasBeenProcessed = true;
                _lastProcessFrame = Time.frameCount;

                _evaluationManager?.MarkNodeEvaluated(this);
            }
            catch (Exception e)
            {
                Entry.Logger.LogError($"Error processing node {Title}: {e.Message}");
            }
        }

        protected abstract void ProcessInternal();

        internal protected void NotifyOutputChanged()
        {
            _evaluationManager?.MarkNodeDirty(this);
        }

        public override void Disconnect(int outputIndex)
        {
            _evaluationManager?.UnregisterNode(this);
        }
    }

    public class MemoryMonitor
    {
        private Queue<float> _memoryHistory = new Queue<float>();
        private const int HISTORY_SIZE = 100;
        private float _lastGCTime = 0;
        private long _lastGCMemory = 0;

        public MemorySnapshot GetCurrentSnapshot()
        {
            var totalMemory = GC.GetTotalMemory(false);
            var arraymanagerStats = ArrayMemoryManager.GetMemoryStats();

            var snapshot = new MemorySnapshot
            {
                TotalManagedMemory = totalMemory,
                ArrayManagerMemory = arraymanagerStats.TotalAllocatedBytes,
                MemoryPressure = arraymanagerStats.MemoryPressure,
                PooledArrays = arraymanagerStats.PooledArrayCount,
                Timestamp = Time.time
            };
            
            _memoryHistory.Enqueue((float)totalMemory / 1024 / 1024);
            while (_memoryHistory.Count > HISTORY_SIZE)
                _memoryHistory.Dequeue();

            return snapshot;
        }

        public void CheckMemoryPressure()
        {
            var stats = ArrayMemoryManager.GetMemoryStats();

            if (stats.MemoryPressure > 0.9f)
            {
                Entry.Logger.LogWarning("High memory pressure detected, triggering cleanup");
                ArrayMemoryManager.ForceCleanup();
            }

            if (Time.time - _lastGCTime > 30f  && stats.MemoryPressure > 0.7f)
            {
                var beforeGC = GC.GetTotalMemory(false);
                GC.Collect();
                var afterGC = GC.GetTotalMemory(true);
                
                _lastGCTime = Time.time;
                _lastGCMemory = beforeGC - afterGC;
                
                Entry.Logger.LogInfo($"Forced GC, freed {_lastGCTime / 1024 / 1024:F1}MB");
            }
        }

        public float[] GetMemoryHistory()
        {
            return _memoryHistory.ToArray();
        }

        public bool IsMemoryPressureHigh()
        {
            var stats = ArrayMemoryManager.GetMemoryStats();
            return stats.MemoryPressure > 0.8f;
        }
    }

    public struct MemorySnapshot
    {
        public long TotalManagedMemory;
        public long ArrayManagerMemory;
        public float MemoryPressure;
        public int PooledArrays;
        public float Timestamp;
    }
}