using System.Collections.Generic;
using UnityEngine;

namespace Neon7
{
    /// <summary>
    /// Процедурный звук — точный порт src/lib/audio.ts (осцилляторы + шум, lowpass 6 кГц).
    /// Клипы генерируются один раз и кэшируются.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx I { get; private set; }

        [SerializeField] private AudioSource source;
        [SerializeField] private bool muted;

        private const int Rate = 44100;
        private static readonly float[] Scale =
        { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.5f };

        private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            I = this;
            if (!source) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
        }

        public bool Muted
        {
            get => muted;
            set => muted = value;
        }

        public enum Wave { Sine, Triangle, Saw }

        // ---------- публичные события ----------

        public void Move() => Tone(320f, 0.05f, Wave.Triangle, 0.05f);

        public void Drop()
        {
            Tone(180f, 0.12f, Wave.Sine, 0.18f);
            Noise(0.12f, 0.08f, 500f);
        }

        public void Pop(int wave, int index)
        {
            float f = Scale[Mathf.Min(Scale.Length - 1, wave - 1)] * (1f + index * 0.03f);
            float delay = index * Metrics.PopSoundStep;
            Tone(f, 0.35f, Wave.Sine, 0.16f, delay);
            Tone(f * 2f, 0.18f, Wave.Triangle, 0.05f, delay);
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
            for (int i = 0; i < 6; i++) Tone(Scale[i] * 2f, 0.5f, Wave.Sine, 0.12f, i * 0.07f);
        }

        public void Over()
        {
            float[] f = { 440f, 350f, 260f, 180f };
            for (int i = 0; i < f.Length; i++) Tone(f[i], 0.5f, Wave.Saw, 0.12f, i * 0.13f);
        }

        // ---------- синтез ----------

        private void Tone(float freq, float dur, Wave type, float gain, float delay = 0f)
        {
            if (muted) return;
            string key = $"t{freq:F1}_{dur:F3}_{type}_{gain:F3}";
            if (!_cache.TryGetValue(key, out var clip))
            {
                int len = Mathf.CeilToInt(Rate * (dur + 0.05f));
                var data = new float[len];
                float prev = 0f;
                float rc = Mathf.Exp(-2f * Mathf.PI * 6000f / Rate); // lowpass 6 кГц
                for (int i = 0; i < len; i++)
                {
                    float t = i / (float)Rate;
                    float phase = freq * t;
                    float s = type switch
                    {
                        Wave.Sine => Mathf.Sin(phase * 2f * Mathf.PI),
                        Wave.Triangle => 2f * Mathf.Abs(2f * (phase - Mathf.Floor(phase + 0.5f))) - 1f,
                        _ => 2f * (phase - Mathf.Floor(phase + 0.5f)),
                    };
                    s *= Envelope(t, dur, gain);
                    prev = s * (1f - rc) + prev * rc;
                    data[i] = prev;
                }
                clip = AudioClip.Create(key, len, 1, Rate, false);
                clip.SetData(data, 0);
                _cache[key] = clip;
            }
            PlayDelayed(clip, delay);
        }

        private void Noise(float dur, float gain, float freq, float delay = 0f)
        {
            if (muted) return;
            int len = Mathf.CeilToInt(Rate * dur);
            var data = new float[len];
            float lp = 0f, hp = 0f, prev = 0f;
            float aLp = Mathf.Exp(-2f * Mathf.PI * (freq * 1.5f) / Rate);
            float aHp = Mathf.Exp(-2f * Mathf.PI * (freq * 0.5f) / Rate);
            for (int i = 0; i < len; i++)
            {
                float decay = Mathf.Pow(1f - i / (float)len, 2f); // (1 - i/len)^2 как в прототипе
                float s = (Random.value * 2f - 1f) * decay;
                lp = s * (1f - aLp) + lp * aLp;                    // bandpass ~ LP - HP
                hp = s * (1f - aHp) + hp * aHp;
                prev = (lp - hp) * gain;
                data[i] = prev;
            }
            var clip = AudioClip.Create("noise", len, 1, Rate, false);
            clip.SetData(data, 0);
            PlayDelayed(clip, delay);
        }

        /// <summary>Атака 12 мс до gain, дальше экспоненциальный спад до 0.0001 за dur.</summary>
        private static float Envelope(float t, float dur, float gain)
        {
            const float attack = 0.012f;
            if (t < attack) return Mathf.Lerp(0.0001f, gain, t / attack);
            float k = Mathf.Clamp01((t - attack) / Mathf.Max(0.0001f, dur - attack));
            return gain * Mathf.Pow(0.0001f / Mathf.Max(gain, 0.0001f), k);
        }

        private void PlayDelayed(AudioClip clip, float delay)
        {
            if (delay <= 0f) { source.PlayOneShot(clip); return; }
            StartCoroutine(DelayedRoutine(clip, delay));
        }

        private System.Collections.IEnumerator DelayedRoutine(AudioClip clip, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            source.PlayOneShot(clip);
        }

        // ---------- haptics ----------

        public static void Haptic(params long[] pattern)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var vib = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (pattern.Length == 1) vib.Call("vibrate", pattern[0]);
                else
                {
                    var full = new long[pattern.Length + 1];
                    System.Array.Copy(pattern, 0, full, 1, pattern.Length);
                    vib.Call("vibrate", full, -1);
                }
            }
            catch { /* noop */ }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
