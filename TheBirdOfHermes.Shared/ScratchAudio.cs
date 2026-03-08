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

        /// <summary>
        /// Sets the audio samples, number of channels, and sample rate for the ScratchAudio component.
        /// </summary>
        /// <param name="samples">The array of audio sample data.</param>
        /// <param name="channels">The number of audio channels in the sample data.</param>
        /// <param name="sampleRate">The sample rate of the audio in Hz.</param>
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

        /// <summary>
        /// Sets the playback position for the ScratchAudio component based on the provided time in seconds.
        /// </summary>
        /// <param name="timeInSeconds">The desired playback position in seconds.</param>
        public void SetPosition(float timeInSeconds)
        {
            _position = timeInSeconds * _sampleRate;
            _position = Math.Max(0, Math.Min(_position, _totalSamples - 1));
        }

        /// <summary>
        /// Sets the playback speed for the ScratchAudio component.
        /// </summary>
        /// <param name="speed">The desired playback speed. A value of 1 represents normal speed,
        /// less than 1 for slower playback, and greater than 1 for faster playback.</param>
        public void SetSpeed(float speed)
        {
            _targetSpeed = speed;
        }

        /// <summary>
        /// Activates or deactivates the ScratchAudio component, resetting playback speed and target speed when deactivated.
        /// </summary>
        /// <param name="active">Indicates whether the component should be active. Pass true to activate or false to deactivate.</param>
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

        /// <summary>
        /// Processes audio data in real-time and applies scratching or scrubbing effects based on the playback position and speed.
        /// </summary>
        /// <param name="data">The buffer array of audio samples to be filled with processed audio data.</param>
        /// <param name="channels">The number of audio output channels.</param>
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