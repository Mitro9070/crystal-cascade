using System.Collections.Generic;
using NeonSeven.PrototypePort.Neon7;
using UnityEngine;

namespace NeonSeven.Infrastructure.Services
{
    public sealed class AudioService
    {
        private readonly AudioSource[] _sources;
        private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        private int _cursor;
        private const int SampleRate = 44100;
        private static readonly float[] Scale =
        {
            523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.5f
        };

        public AudioService(GameObject root, int poolSize)
        {
            _sources = new AudioSource[Mathf.Max(1, poolSize)];
            for (int i = 0; i < _sources.Length; i++)
                _sources[i] = root.AddComponent<AudioSource>();
        }

        public bool IsMuted { get; private set; }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
        }

        public void PlayOneShot(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || IsMuted)
                return;

            var source = _sources[_cursor];
            _cursor = (_cursor + 1) % _sources.Length;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        public void Move() => Tone(320f, 0.05f, Wave.Triangle, 0.05f);

        public void Drop()
        {
            Tone(180f, 0.12f, Wave.Sine, 0.18f);
            Noise(0.12f, 0.08f, 500f);
        }

        public void Pop(int wave, int index)
        {
            float frequency = Scale[Mathf.Min(Scale.Length - 1, wave - 1)] * (1f + index * 0.03f);
            float delay = index * PrototypeMetrics.PopSoundStep;
            Tone(frequency, 0.35f, Wave.Sine, 0.16f, delay);
            Tone(frequency * 2f, 0.18f, Wave.Triangle, 0.05f, delay);
            Noise(0.18f, 0.05f, 2400f, delay);
        }

        public void Crack()
        {
            Noise(0.2f, 0.12f, 900f);
            Tone(140f, 0.2f, Wave.Saw, 0.06f);
        }

        public void Rise()
        {
            Tone(90f, 0.5f, Wave.Saw, 0.10f);
            Noise(0.4f, 0.10f, 300f);
        }

        public void Clear()
        {
            for (int i = 0; i < 6; i++)
                Tone(Scale[i] * 2f, 0.5f, Wave.Sine, 0.12f, i * 0.07f);
        }

        public void Over()
        {
            float[] frequencies = { 440f, 350f, 260f, 180f };
            for (int i = 0; i < frequencies.Length; i++)
                Tone(frequencies[i], 0.5f, Wave.Saw, 0.12f, i * 0.13f);
        }

        private enum Wave
        {
            Sine,
            Triangle,
            Saw
        }

        private void Tone(float frequency, float duration, Wave wave, float gain, float delay = 0f)
        {
            if (IsMuted)
                return;

            string key = $"tone_{frequency:F1}_{duration:F3}_{wave}_{gain:F3}";
            if (!_cache.TryGetValue(key, out AudioClip clip))
            {
                int length = Mathf.CeilToInt(SampleRate * (duration + 0.05f));
                var samples = new float[length];
                float previous = 0f;
                float lowPass = Mathf.Exp(-2f * Mathf.PI * 6000f / SampleRate);
                for (int i = 0; i < length; i++)
                {
                    float time = i / (float)SampleRate;
                    float phase = frequency * time;
                    float sample;
                    switch (wave)
                    {
                        case Wave.Sine:
                            sample = Mathf.Sin(phase * 2f * Mathf.PI);
                            break;
                        case Wave.Triangle:
                            sample = 2f * Mathf.Abs(2f * (phase - Mathf.Floor(phase + 0.5f))) - 1f;
                            break;
                        default:
                            sample = 2f * (phase - Mathf.Floor(phase + 0.5f));
                            break;
                    }

                    sample *= Envelope(time, duration, gain);
                    previous = sample * (1f - lowPass) + previous * lowPass;
                    samples[i] = previous;
                }

                clip = AudioClip.Create(key, length, 1, SampleRate, false);
                clip.SetData(samples, 0);
                _cache[key] = clip;
            }

            PlayClip(clip, delay);
        }

        private void Noise(float duration, float gain, float frequency, float delay = 0f)
        {
            if (IsMuted)
                return;

            int length = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[length];
            float low = 0f;
            float high = 0f;
            float lowCoefficient = Mathf.Exp(-2f * Mathf.PI * (frequency * 1.5f) / SampleRate);
            float highCoefficient = Mathf.Exp(-2f * Mathf.PI * (frequency * 0.5f) / SampleRate);
            for (int i = 0; i < length; i++)
            {
                float decay = Mathf.Pow(1f - i / (float)length, 2f);
                float sample = (Random.value * 2f - 1f) * decay;
                low = sample * (1f - lowCoefficient) + low * lowCoefficient;
                high = sample * (1f - highCoefficient) + high * highCoefficient;
                samples[i] = (low - high) * gain;
            }

            var clip = AudioClip.Create("noise", length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            PlayClip(clip, delay);
        }

        private void PlayClip(AudioClip clip, float delay)
        {
            var source = _sources[_cursor];
            _cursor = (_cursor + 1) % _sources.Length;
            source.clip = clip;
            source.volume = 1f;
            source.pitch = 1f;
            source.PlayDelayed(delay);
        }

        private static float Envelope(float time, float duration, float gain)
        {
            const float attack = 0.012f;
            if (time < attack)
                return Mathf.Lerp(0.0001f, gain, time / attack);

            float progress = Mathf.Clamp01((time - attack) / Mathf.Max(0.0001f, duration - attack));
            return gain * Mathf.Pow(0.0001f / Mathf.Max(gain, 0.0001f), progress);
        }
    }
}
