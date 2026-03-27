using System;

namespace TheBirdOfHermes.Audio
{
    public interface IAudioFilter
    {
        string Name { get; }
        string Group { get; }

        void Process(AudioData data);
    }

    public abstract class AudioFilterBase : IAudioFilter
    {
        public abstract string Name { get; set; }
        public abstract string Group { get; set; }
        public abstract void Process(AudioData data);

        /// <summary>
        /// Override to draw parameter UI in the filter config modal.
        /// If not overridden, the filter applies immediately with no modal.
        /// </summary>
        public virtual void OnDraw() { }

        /// <summary>
        /// Optional callback to report progress (0..1) during Process().
        /// Set by the async system before calling Process on a background thread.
        /// </summary>
        public Action<float> ProgressCallback { get; set; }

        /// <summary>
        /// Helper to report progress from within Process().
        /// </summary>
        protected void ReportProgress(float progress)
        {
            ProgressCallback?.Invoke(progress);
        }
    }
}