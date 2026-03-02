using System;
using UnityEngine;

namespace TheBirdOfHermes
{
    /// <summary>
    /// Handles variable-speed audio playback for scratching/scrubbing effect
    /// </summary>
    public class ScratchAudio : MonoBehaviour
    {
        private float[] _samples;
        private int _channels;
        private int _sampleRate;
        private int _totalSamples;

        private double _position;
        private float _speed;
        private float _targetSpeed;
        private float _volume = 0.8f;
        private bool _isActive;

        private const float SpeedSmoothing = 0.3f;

        public void SetSamples(float[] samples, int channels, int sampleRate)
        {
            _samples = samples;
            _channels = channels;
            _sampleRate = sampleRate;
            _totalSamples = samples.Length / channels;
            _position = 0;
            _speed = 0;
            _targetSpeed = 0;
        }

        public void SetPosition(float timeInSeconds)
        {
            _position = timeInSeconds * _sampleRate;
            _position = Math.Max(0, Math.Min(_position, _totalSamples - 1));
        }

        public void SetSpeed(float speed)
        {
            _targetSpeed = speed;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            if (!active)
            {
                _speed = 0;
                _targetSpeed = 0;
            }
        }

        private void Update()
        {
            _speed = Mathf.Lerp(_speed, _targetSpeed, SpeedSmoothing);
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_isActive || _samples == null || Math.Abs(_speed) < 0.01f)
            {
                return;
            }

            int outputSamples = data.Length / channels;

            for (int i = 0; i < outputSamples; i++)
            {
                double pos = _position;

                if (pos < 0 || pos >= _totalSamples - 1)
                {
                    for (int c = 0; c < channels; c++)
                        data[i * channels + c] = 0;
                }
                else
                {
                    int idx0 = (int)pos;
                    int idx1 = idx0 + 1;
                    float frac = (float)(pos - idx0);

                    for (int c = 0; c < channels; c++)
                    {
                        int srcChannel = c % _channels;
                        float s0 = _samples[idx0 * _channels + srcChannel];
                        float s1 = _samples[idx1 * _channels + srcChannel];
                        float sample = s0 + (s1 - s0) * frac;

                        data[i * channels + c] = sample * _volume;
                    }
                }

                double advance = _speed * _sampleRate / AudioSettings.outputSampleRate;
                _position += advance;
            }
        }
    }
}