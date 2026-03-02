using System;
using Compositor.KK;
using UnityEngine;

namespace DefaultNamespace
{
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
}