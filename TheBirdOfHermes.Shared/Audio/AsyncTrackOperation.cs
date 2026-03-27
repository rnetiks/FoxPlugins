using System;
using System.Threading;

namespace TheBirdOfHermes.Audio
{
    /// <summary>
    /// Represents an async operation (decode or filter) running on a background thread.
    /// Polled from the main thread via AudioTrack.PollAsyncCompletion().
    /// </summary>
    public class AsyncTrackOperation
    {
        public enum OperationType
        {
            Decode,
            Filter
        }

        public OperationType Type { get; }
        public string Description { get; }

        /// <summary>Progress 0..1, updated from background thread (volatile for thread safety).</summary>
        private volatile float _progress;
        public float Progress => _progress;

        /// <summary>True when the background work is done (success or failure).</summary>
        private volatile bool _isComplete;
        public bool IsComplete => _isComplete;

        /// <summary>If non-null, the operation failed with this exception.</summary>
        public Exception Error { get; private set; }

        /// <summary>The decoded AudioData result (for Decode operations).</summary>
        public AudioData Result { get; private set; }

        /// <summary>The filter that was applied (for Filter operations).</summary>
        public IAudioFilter AppliedFilter { get; }

        /// <summary>
        /// Creates and starts a decode operation on a background thread.
        /// </summary>
        public static AsyncTrackOperation StartDecode(byte[] bytes, string extension, string description)
        {
            var op = new AsyncTrackOperation(OperationType.Decode, description, null);
            op.Start(() => AudioLoader.LoadWithProgress(bytes, extension, p => op._progress = p));
            return op;
        }

        /// <summary>
        /// Creates and starts a filter operation on a background thread.
        /// </summary>
        public static AsyncTrackOperation StartFilter(AudioData data, AudioFilterBase filter, string description)
        {
            var op = new AsyncTrackOperation(OperationType.Filter, description, filter);
            op.Start(() =>
            {
                filter.ProgressCallback = p => op._progress = p;
                filter.Process(data);
                filter.ProgressCallback = null;
                return data;
            });
            return op;
        }

        private AsyncTrackOperation(OperationType type, string description, IAudioFilter filter)
        {
            Type = type;
            Description = description;
            AppliedFilter = filter;
        }

        private void Start(Func<AudioData> work)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    Result = work();
                }
                catch (Exception ex)
                {
                    Error = ex;
                }
                finally
                {
                    _isComplete = true;
                }
            });
            thread.IsBackground = true;
            thread.Name = $"TBOH_{Type}_{Description}";
            thread.Start();
        }
    }
}
