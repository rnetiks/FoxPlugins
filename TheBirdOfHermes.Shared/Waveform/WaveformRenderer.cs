using UnityEngine;

namespace TheBirdOfHermes.Waveform
{
    public class WaveformRenderer
    {
        private float[] _monoSamples;
        private float[] _waveformMin;
        private float[] _waveformMax;
        private float[] _waveformRms;
        private int _mipSamplesPerBucket = 256;
        private int _sampleRate;

        private Texture2D _waveformTex;
        private Color32[] _pixelBuffer;
        private int _cachedWidth;
        private float _cachedStartTime;
        private float _cachedVisibleDuration;
        private WaveformMode _cachedNormMode;
        private float _cachedFadeIn;
        private float _cachedFadeOut;
        private float _cachedVolumeScale;

        public Color WaveColor = new Color(0.3f, 0.6f, 1f);
        public Color WaveColorRms = new Color(0.5f, 0.8f, 1f);

        public Texture2D Texture => _waveformTex;

        public void SetSamples(float[] monoSamples, int sampleRate)
        {
            _monoSamples = monoSamples;
            _sampleRate = sampleRate;
            BuildMips();

            if (_waveformTex != null)
            {
                Object.Destroy(_waveformTex);
                _waveformTex = null;
            }
            _pixelBuffer = null;
        }

        public void Clear()
        {
            if (_waveformTex != null)
            {
                Object.Destroy(_waveformTex);
                _waveformTex = null;
            }
            _monoSamples = null;
            _waveformMin = null;
            _waveformMax = null;
            _waveformRms = null;
            _pixelBuffer = null;
        }

        public bool NeedsRebuild(int width, float startTime, float visibleDuration, WaveformMode normMode = WaveformMode.Normal, float fadeInDuration = 0f, float fadeOutDuration = 0f, float volumeScale = 1f)
        {
            return _waveformTex == null || _cachedWidth != width ||
                   Mathf.Abs(_cachedStartTime - startTime) > 0.0001f ||
                   Mathf.Abs(_cachedVisibleDuration - visibleDuration) > 0.0001f ||
                   _cachedNormMode != normMode ||
                   Mathf.Abs(_cachedFadeIn - fadeInDuration) > 0.0001f ||
                   Mathf.Abs(_cachedFadeOut - fadeOutDuration) > 0.0001f ||
                   (normMode == WaveformMode.Volume && Mathf.Abs(_cachedVolumeScale - volumeScale) > 0.001f);
        }

        /// <summary>
        /// Rebuilds the waveform texture based on the given parameters.
        /// This function initializes or recreates the texture and pixel buffer if needed,
        /// clears the pixel buffer, and updates the waveform image based on audio data.
        /// The visual representation is influenced by parameters like start time, visible duration,
        /// normalization mode, and volume scaling options.
        /// </summary>
        /// <param name="width">The width of the waveform texture in pixels.</param>
        /// <param name="height">The height of the waveform texture in pixels.</param>
        /// <param name="startTime">The time position in the audio at which the waveform starts, in seconds.</param>
        /// <param name="visibleDuration">The duration of audio visible in the waveform, in seconds.</param>
        /// <param name="audioOffset">The offset in time to translate the audio start position, in seconds.</param>
        /// <param name="normMode">The normalization mode which determines how the waveform amplitude is scaled.</param>
        /// <param name="fadeInDuration">The duration of fade-in applied to the waveform, in seconds.</param>
        /// <param name="fadeOutDuration">The duration of fade-out applied to the waveform, in seconds.</param>
        /// <param name="volumeScale">The scale factor applied to the audio amplitude, affecting the waveform's appearance.</param>
        public void Rebuild(int width, int height, float startTime, float visibleDuration, float audioOffset, WaveformMode normMode = WaveformMode.Normal, float fadeInDuration = 0f, float fadeOutDuration = 0f, float volumeScale = 1f)
        {
            if (_waveformTex == null || _waveformTex.width != width || _waveformTex.height != height)
            {
                if (_waveformTex != null) Object.Destroy(_waveformTex);
                _waveformTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                _waveformTex.filterMode = FilterMode.Point;
                _pixelBuffer = null;
            }

            int pixelCount = width * height;
            if (_pixelBuffer == null || _pixelBuffer.Length != pixelCount)
                _pixelBuffer = new Color32[pixelCount];

            var clear = new Color32(0, 0, 0, 0);
            int unrollFactor = 4;
            int unrolledLength = pixelCount / unrollFactor * unrollFactor;

            for (int i = 0; i < unrolledLength; i += unrollFactor)
            {
                _pixelBuffer[i] = clear;
                _pixelBuffer[i + 1] = clear;
                _pixelBuffer[i + 2] = clear;
                _pixelBuffer[i + 3] = clear;
            }

            for (int i = unrolledLength; i < pixelCount; i++)
                _pixelBuffer[i] = clear;
            
            var waveCol = (Color32)WaveColor;
            var rmsCol = (Color32)WaveColorRms;

            float audioStartTime = startTime - audioOffset;
            float startSample = audioStartTime * _sampleRate;
            float endSample = (audioStartTime + visibleDuration) * _sampleRate;
            float visibleSamples = endSample - startSample;

            if (_monoSamples == null || visibleSamples <= 0)
            {
                _waveformTex.SetPixels32(_pixelBuffer);
                _waveformTex.Apply();
                CacheState(width, startTime, visibleDuration, normMode, fadeInDuration, fadeOutDuration, volumeScale);
                return;
            }

            float globalPeak = 1f;
            if (normMode == WaveformMode.Max)
            {
                globalPeak = FindGlobalPeak();
                if (globalPeak < 0.001f) globalPeak = 1f;
            }

            float samplesPerPixel = visibleSamples / width;
            bool useMips = _waveformMin != null && samplesPerPixel > _mipSamplesPerBucket;

            for (int x = 0; x < width; x++)
            {
                float min, max, rms;

                if (useMips)
                {
                    int bucketStart = (int)((startSample + x * samplesPerPixel) / _mipSamplesPerBucket);
                    int bucketEnd = (int)((startSample + (x + 1) * samplesPerPixel) / _mipSamplesPerBucket);

                    bucketStart = Mathf.Clamp(bucketStart, 0, _waveformMin.Length - 1);
                    bucketEnd = Mathf.Clamp(bucketEnd, 0, _waveformMin.Length);

                    if (bucketStart >= bucketEnd) continue;

                    min = float.MaxValue;
                    max = float.MinValue;
                    float sumRmsSq = 0;

                    for (int b = bucketStart; b < bucketEnd; b++)
                    {
                        if (_waveformMin[b] < min) min = _waveformMin[b];
                        if (_waveformMax[b] > max) max = _waveformMax[b];
                        sumRmsSq += _waveformRms[b] * _waveformRms[b];
                    }
                    rms = Mathf.Sqrt(sumRmsSq / (bucketEnd - bucketStart));
                }
                else
                {
                    int sampleStart = (int)(startSample + x * samplesPerPixel);
                    int sampleEnd = (int)(startSample + (x + 1) * samplesPerPixel);

                    sampleStart = Mathf.Clamp(sampleStart, 0, _monoSamples.Length - 1);
                    sampleEnd = Mathf.Clamp(sampleEnd, 0, _monoSamples.Length);

                    if (sampleStart >= sampleEnd) continue;

                    min = float.MaxValue;
                    max = float.MinValue;
                    float sumSq = 0;

                    for (int i = sampleStart; i < sampleEnd; i++)
                    {
                        float s = _monoSamples[i];
                        if (s < min) min = s;
                        if (s > max) max = s;
                        sumSq += s * s;
                    }
                    rms = Mathf.Sqrt(sumSq / (sampleEnd - sampleStart));
                }

                float normScale = 1f;
                switch (normMode)
                {
                    case WaveformMode.Max:
                        normScale = 1f / globalPeak;
                        break;
                    case WaveformMode.Volume:
                        normScale = volumeScale;
                        break;
                }

                min *= normScale;
                max *= normScale;
                rms *= normScale;

                float fadeScale = GetFadeScale(x, width, visibleDuration, fadeInDuration, fadeOutDuration, audioStartTime);
                min *= fadeScale;
                max *= fadeScale;
                rms *= fadeScale;

                min = Mathf.Clamp(min, -1f, 1f);
                max = Mathf.Clamp(max, -1f, 1f);
                rms = Mathf.Clamp(rms, 0f, 1f);

                int yMin = (int)((min * 0.5f + 0.5f) * (height - 1));
                int yMax = (int)((max * 0.5f + 0.5f) * (height - 1));
                int yRmsLow = (int)((0.5f - rms * 0.5f) * (height - 1));
                int yRmsHigh = (int)((0.5f + rms * 0.5f) * (height - 1));

                for (int y = yMin; y <= yMax; y++)
                    _pixelBuffer[y * width + x] = waveCol;

                for (int y = yRmsLow; y <= yRmsHigh; y++)
                    _pixelBuffer[y * width + x] = rmsCol;
            }

            int centerY = height / 2;
            var centerCol = new Color32(255, 255, 255, 51);
            for (int x = 0; x < width; x++)
                _pixelBuffer[centerY * width + x] = centerCol;

            _waveformTex.SetPixels32(_pixelBuffer);
            _waveformTex.Apply();

            CacheState(width, startTime, visibleDuration, normMode, fadeInDuration, fadeOutDuration, volumeScale);
        }
        
        /// <summary>
        /// Calculates fade scale for a pixel. Fade is relative to the audio content visible in this render.
        /// fadeInDuration/fadeOutDuration are in seconds, relative to the audible region start/end.
        /// audioStartTime is the time in the audio file that the render starts at.
        /// </summary>
        private float GetFadeScale(int x, int width, float visibleDuration, float fadeInDuration, float fadeOutDuration, float audioStartTime)
        {
            if (fadeInDuration <= 0f && fadeOutDuration <= 0f) return 1f;

            float pixelTime = audioStartTime + ((float)x / width) * visibleDuration;

            if (fadeInDuration > 0f && pixelTime < fadeInDuration)
            {
                if (pixelTime < 0f) return 0f;
                return Mathf.Clamp01(pixelTime / fadeInDuration);
            }

            if (fadeOutDuration > 0f && _monoSamples != null)
            {
                float totalDuration = (float)_monoSamples.Length / _sampleRate;
                float fadeOutStart = totalDuration - fadeOutDuration;
                if (pixelTime > fadeOutStart)
                    return Mathf.Clamp01((totalDuration - pixelTime) / fadeOutDuration);
            }

            return 1f;
        }

        private float FindGlobalPeak()
        {
            if (_monoSamples == null) return 1f;

            float peak = 0f;
            foreach (float value in _monoSamples)
            {
                float abs = Mathf.Abs(value);
                if (abs > peak) peak = abs;
            }
            return peak;
        }

        private void CacheState(int width, float startTime, float visibleDuration,
            WaveformMode normMode, float fadeIn, float fadeOut, float volumeScale)
        {
            _cachedWidth = width;
            _cachedStartTime = startTime;
            _cachedVisibleDuration = visibleDuration;
            _cachedNormMode = normMode;
            _cachedFadeIn = fadeIn;
            _cachedFadeOut = fadeOut;
            _cachedVolumeScale = volumeScale;
        }

        private void BuildMips()
        {
            if (_monoSamples == null) return;

            int bucketCount = (_monoSamples.Length + _mipSamplesPerBucket - 1) / _mipSamplesPerBucket;
            _waveformMin = new float[bucketCount];
            _waveformMax = new float[bucketCount];
            _waveformRms = new float[bucketCount];

            for (int b = 0; b < bucketCount; b++)
            {
                int start = b * _mipSamplesPerBucket;
                int end = Mathf.Min(start + _mipSamplesPerBucket, _monoSamples.Length);

                float min = float.MaxValue, max = float.MinValue, sumSq = 0;
                for (int i = start; i < end; i++)
                {
                    float s = _monoSamples[i];
                    if (s < min) min = s;
                    if (s > max) max = s;
                    sumSq += s * s;
                }

                _waveformMin[b] = min;
                _waveformMax[b] = max;
                _waveformRms[b] = Mathf.Sqrt(sumSq / (end - start));
            }
        }
    }
}