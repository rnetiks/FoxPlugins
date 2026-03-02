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

        public Color WaveColor = new Color(0.3f, 0.6f, 1f);
        public Color WaveColorRms = new Color(0.5f, 0.8f, 1f);

        public Texture2D Texture => _waveformTex;

        public void SetSamples(float[] monoSamples, int sampleRate)
        {
            _monoSamples = monoSamples;
            _sampleRate = sampleRate;
            BuildMips();
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

        public bool NeedsRebuild(int width, float startTime, float visibleDuration)
        {
            return _waveformTex == null || _cachedWidth != width ||
                   Mathf.Abs(_cachedStartTime - startTime) > 0.0001f ||
                   Mathf.Abs(_cachedVisibleDuration - visibleDuration) > 0.0001f;
        }

        public void Rebuild(int width, int height, float startTime, float visibleDuration, float audioOffset)
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
            for (int i = 0; i < pixelCount; i++)
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
                CacheState(width, startTime, visibleDuration);
                return;
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
            CacheState(width, startTime, visibleDuration);
        }

        private void CacheState(int width, float startTime, float visibleDuration)
        {
            _cachedWidth = width;
            _cachedStartTime = startTime;
            _cachedVisibleDuration = visibleDuration;
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