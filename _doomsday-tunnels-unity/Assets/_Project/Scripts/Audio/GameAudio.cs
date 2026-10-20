using System.Collections.Generic;
using UnityEngine;

namespace DoomsdayTunnels.Audio
{
    /// <summary>
    /// Plays the synthesised sounds: one source for the one-shot effects, one looping source for the engine hum whose
    /// pitch and volume follow the ship's speed and boost. Muting silences both without stopping the hum.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly List<(float at, string name)> queued = new List<(float, string)>();
        AudioSource fx, hum; bool muted; float humGain;

        public bool Muted { get => muted; set { muted = value; if (fx) fx.mute = value; if (hum) hum.mute = value; } }

        void Awake()
        {
            fx = gameObject.AddComponent<AudioSource>(); fx.playOnAwake = false; fx.spatialBlend = 0; fx.volume = 1;
            hum = gameObject.AddComponent<AudioSource>(); hum.playOnAwake = false; hum.spatialBlend = 0; hum.loop = true; hum.volume = 0;
            hum.clip = Synth.Clip("hum", Synth.RenderHum()); hum.Play();
        }

        AudioClip Get(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            var p = Synth.Named(name);
            c = p == null ? null : Synth.Clip(name, Synth.Render(p));
            clips[name] = c;
            return c;
        }

        /// <summary>Plays a named sound now, or after `delay` seconds.</summary>
        public void Play(string name, float delay = 0)
        {
            if (name == null) return;
            if (delay > 0) { queued.Add((Time.unscaledTime + delay, name)); return; }
            var c = Get(name); if (c != null && fx) fx.PlayOneShot(c);
        }

        /// <summary>The engine: audible while flying, louder and higher under boost. `speed` is metres per second.</summary>
        public void Engine(bool live, float speed, float boostEase, float dt)
        {
            float target = live ? 0.07f + 0.09f * boostEase : 0;
            humGain += (target - humGain) * Mathf.Min(1, dt * 12);
            float f = 50 + speed * 2.2f + 50 * boostEase;
            hum.pitch += (f / Synth.HumBase - hum.pitch) * Mathf.Min(1, dt * 10);
            hum.volume = humGain;                                             // the clip already carries the master level
        }

        void Update()
        {
            for (int i = queued.Count - 1; i >= 0; i--) if (Time.unscaledTime >= queued[i].at) { var q = queued[i]; queued.RemoveAt(i); Play(q.name); }
        }

        public void CancelQueued() => queued.Clear();
    }
}
