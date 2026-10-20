using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoomsdayTunnels.Audio
{
    /// <summary>
    /// The web game's sounds, rendered to clips. The web game synthesises everything live with two WebAudio recipes:
    /// `tone` (an oscillator sliding from one frequency to another with a fading gain) and `noise` (white noise through
    /// a low-pass filter, fading). A Patch is a list of those; Render turns it into samples, so every effect, jingle
    /// and fanfare is the same recipe as on the web, with no sound files in the project.
    /// </summary>
    public static class Synth
    {
        public const int Rate = 44100;
        public const float Master = 0.5f;

        public enum Wave { Sine, Square, Triangle, Sawtooth }
        public struct Tone { public Wave W; public float F0, F1, Dur, Vol, Delay; }
        public struct Noise { public float Dur, Vol, Freq, Delay; }

        public sealed class Patch
        {
            public readonly List<Tone> Tones = new List<Tone>(); public readonly List<Noise> Noises = new List<Noise>();
            public Patch T(Wave w, float f0, float f1, float dur, float vol, float delay = 0) { Tones.Add(new Tone { W = w, F0 = f0, F1 = f1, Dur = dur, Vol = vol, Delay = delay }); return this; }
            public Patch N(float dur, float vol, float freq, float delay = 0) { Noises.Add(new Noise { Dur = dur, Vol = vol, Freq = freq, Delay = delay }); return this; }
            public float Length { get { float L = 0; foreach (var t in Tones) L = Math.Max(L, t.Delay + t.Dur); foreach (var n in Noises) L = Math.Max(L, n.Delay + n.Dur); return L + 0.03f; } }
        }

        static Patch P() => new Patch();
        static Patch Seq(Wave w, float[] f, float dur, float vol, float gap, int lastIdx = -1, float lastDur = 0, float delay = 0)
        {
            var p = P(); for (int i = 0; i < f.Length; i++) p.T(w, f[i], f[i], i == lastIdx ? lastDur : dur, vol, delay + i * gap); return p;
        }

        /// <summary>Every named sound: the in-game effects, the cut-scene stings and jingles, and the celebrations.</summary>
        public static Patch Named(string name)
        {
            switch (name)
            {
                case "laser": return P().T(Wave.Square, 900, 180, 0.09f, 0.07f);
                case "boom": return P().N(0.35f, 0.5f, 900).T(Wave.Sawtooth, 140, 40, 0.3f, 0.2f);
                case "hit": return P().N(0.25f, 0.6f, 500).T(Wave.Sawtooth, 110, 35, 0.25f, 0.3f);
                case "tink": return P().T(Wave.Triangle, 1400, 900, 0.05f, 0.12f);
                case "pick": return P().T(Wave.Sine, 660, 660, 0.08f, 0.2f).T(Wave.Sine, 990, 990, 0.12f, 0.2f, 0.08f);
                case "scrape": return P().N(0.08f, 0.2f, 2500);
                case "clear": return Seq(Wave.Square, new float[] { 523, 659, 784, 1047 }, 0.14f, 0.1f, 0.11f);
                case "beep": return P().T(Wave.Square, 1200, 1200, 0.06f, 0.09f);
                case "boostOn": return P().N(0.5f, 0.35f, 1800).T(Wave.Sawtooth, 90, 340, 0.45f, 0.16f);
                case "boostOff": return P().T(Wave.Sawtooth, 260, 80, 0.3f, 0.09f);
                case "boostDry": return P().T(Wave.Square, 150, 110, 0.07f, 0.1f).T(Wave.Square, 150, 110, 0.07f, 0.1f, 0.11f);
                case "refuel": return Seq(Wave.Triangle, new float[] { 440, 660, 880, 1320 }, 0.09f, 0.14f, 0.06f);
                // cut scenes
                case "static": return P().N(0.12f, 0.1f, 4000);
                case "thunder": return P().N(0.7f, 0.5f, 260).T(Wave.Sawtooth, 70, 40, 0.5f, 0.15f);
                case "sting": return P().T(Wave.Sawtooth, 196, 196, 0.16f, 0.1f).T(Wave.Sawtooth, 185, 185, 0.16f, 0.1f, 0.2f).T(Wave.Sawtooth, 147, 140, 0.7f, 0.13f, 0.4f);
                case "fanfare": return Seq(Wave.Square, new float[] { 392, 392, 523, 659, 784, 1047 }, 0.11f, 0.09f, 0.12f, 5, 0.5f);
                case "villain": return Seq(Wave.Sawtooth, new float[] { 147, 139, 131, 98 }, 0.15f, 0.09f, 0.17f, 3, 0.45f);
                case "hero": return Seq(Wave.Square, new float[] { 523, 659, 784 }, 0.1f, 0.07f, 0.1f);
                case "morse": { var p = P(); float[] d = { 0, 0.09f, 0.18f, 0.33f }; for (int i = 0; i < 4; i++) p.T(Wave.Sine, 880, 880, i == 3 ? 0.16f : 0.05f, 0.08f, d[i]); return p; }
                case "phone": { var p = P(); float[] d = { 0, 0.13f, 0.5f, 0.63f }; for (int i = 0; i < 4; i++) { float f = i % 2 == 1 ? 1700 : 1400; p.T(Wave.Sine, f, f, 0.11f, 0.07f, d[i]); } return p; }
                case "mother": return P().T(Wave.Triangle, 392, 392, 0.2f, 0.12f).T(Wave.Triangle, 311, 300, 0.45f, 0.13f, 0.22f);
                case "laugh": { var p = P(); for (int i = 0; i < 7; i++) p.T(Wave.Square, 250 - i * 9, 205 - i * 9, 0.07f, 0.06f, i * 0.11f); return p; }
                case "snip": return P().T(Wave.Triangle, 2400, 900, 0.05f, 0.2f).T(Wave.Triangle, 2400, 900, 0.05f, 0.2f, 0.12f).N(0.4f, 0.35f, 1200);
                // celebrations: a personal best gets the small one, a record the big one
                case "cheerSmall": return Seq(Wave.Triangle, new float[] { 784, 988, 1175, 1568 }, 0.09f, 0.14f, 0.08f, 3, 0.35f);
                case "cheerBig": return Cheer(false);
                case "cheerGrand": return Cheer(true);
                default: return null;
            }
        }

        static Patch Cheer(bool grand)
        {
            var p = Seq(Wave.Square, new float[] { 392, 392, 523, 659, 784, 1047 }, 0.11f, 0.09f, 0.12f, 5, 0.6f);      // the cut-scene fanfare,
            p.T(Wave.Sawtooth, 98, 98, 0.5f, 0.08f).T(Wave.Sawtooth, 131, 131, 0.6f, 0.1f, 0.6f);                      // with a bass under it,
            p.T(Wave.Square, 659, 659, 0.6f, 0.045f, 0.6f).T(Wave.Square, 784, 784, 0.6f, 0.045f, 0.6f);               // a chord on the last note,
            p.N(0.6f, 0.16f, 7000, 0.6f);                                                                             // a cymbal
            float[] sp = { 1568, 2093, 2637, 3136 }; for (int i = 0; i < 4; i++) p.T(Wave.Triangle, sp[i], sp[i], 0.12f, 0.07f, 0.78f + i * 0.06f);   // and a sparkle on top
            if (!grand) return p;
            float[] top = { 523, 659, 784, 1047, 1319, 1568 }; for (int i = 0; i < 6; i++) p.T(Wave.Square, top[i], top[i], i == 5 ? 0.8f : 0.09f, 0.09f, 1.3f + i * 0.09f);
            p.T(Wave.Sawtooth, 65, 65, 0.8f, 0.1f, 1.75f).T(Wave.Square, 1047, 1047, 0.8f, 0.045f, 1.75f).T(Wave.Square, 1319, 1319, 0.8f, 0.045f, 1.75f);
            p.N(0.8f, 0.18f, 7000, 1.75f);
            return p;
        }

        /// <summary>Mono samples for a patch, already at master volume and clipped.</summary>
        public static float[] Render(Patch p)
        {
            int n = Mathf.CeilToInt(p.Length * Rate); var buf = new float[n];
            foreach (var t in p.Tones) AddTone(buf, t);
            var rng = new System.Random(1234);
            foreach (var z in p.Noises) AddNoise(buf, z, rng);
            for (int i = 0; i < n; i++) buf[i] = Mathf.Clamp(buf[i] * Master, -1, 1);
            return buf;
        }

        // Frequency and gain both ramp exponentially, as WebAudio's exponentialRampToValueAtTime does.
        static void AddTone(float[] buf, Tone t)
        {
            int i0 = (int)(t.Delay * Rate), n = (int)(t.Dur * Rate); if (n <= 0) return;
            float f1 = Math.Max(1, t.F1), ratio = f1 / t.F0, gEnd = 0.0001f / t.Vol;
            double phase = 0;
            for (int i = 0; i < n && i0 + i < buf.Length; i++)
            {
                float u = (float)i / n;
                float f = t.F0 * Mathf.Pow(ratio, u), g = t.Vol * Mathf.Pow(gEnd, u);
                double dp = f / Rate;
                buf[i0 + i] += Osc(t.W, (float)phase, (float)dp) * g;
                phase += dp; if (phase >= 1) phase -= 1;
            }
        }

        // Square and sawtooth are smoothed at their steps (polyBLEP) so the high notes do not alias.
        static float Osc(Wave w, float ph, float dp)
        {
            switch (w)
            {
                case Wave.Sine: return Mathf.Sin(ph * 2 * Mathf.PI);
                case Wave.Triangle: return 4 * Mathf.Abs(ph - 0.5f) - 1;
                case Wave.Sawtooth: return 2 * ph - 1 - Blep(ph, dp);
                default: { float v = ph < 0.5f ? 1 : -1; v += Blep(ph, dp); v -= Blep((ph + 0.5f) % 1, dp); return v; }
            }
        }
        static float Blep(float t, float dt)
        {
            if (t < dt) { t /= dt; return t + t - t * t - 1; }
            if (t > 1 - dt) { t = (t - 1) / dt; return t * t + t + t + 1; }
            return 0;
        }

        // White noise through a two-pole low-pass (the cookbook biquad WebAudio uses), fading out exponentially.
        static void AddNoise(float[] buf, Noise z, System.Random rng)
        {
            int i0 = (int)(z.Delay * Rate), n = (int)(z.Dur * Rate); if (n <= 0) return;
            var lp = new Biquad(z.Freq, 1f); float gEnd = 0.0001f / z.Vol;
            for (int i = 0; i < n && i0 + i < buf.Length; i++)
            {
                float g = z.Vol * Mathf.Pow(gEnd, (float)i / n);
                buf[i0 + i] += lp.Next((float)(rng.NextDouble() * 2 - 1)) * g;
            }
        }

        public struct Biquad
        {
            float b0, b1, b2, a1, a2, x1, x2, y1, y2;
            public Biquad(float freq, float q)
            {
                float w = 2 * Mathf.PI * freq / Rate, cs = Mathf.Cos(w), sn = Mathf.Sin(w), al = sn / (2 * q), a0 = 1 + al;
                b0 = (1 - cs) / 2 / a0; b1 = (1 - cs) / a0; b2 = b0; a1 = -2 * cs / a0; a2 = (1 - al) / a0;
                x1 = x2 = y1 = y2 = 0;
            }
            public float Next(float x)
            {
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y; return y;
            }
        }

        /// <summary>The engine: a sawtooth through a 320 Hz low-pass, one second long so it loops without a seam, at HumBase Hz.</summary>
        public const float HumBase = 120f;
        public static float[] RenderHum()
        {
            int n = Rate; var lp = new Biquad(320, 1f); var buf = new float[n];
            double phase = 0, dp = HumBase / Rate;
            for (int i = 0; i < n * 2; i++)                                 // the first second settles the filter and is thrown away
            {
                float v = lp.Next(Osc(Wave.Sawtooth, (float)phase, (float)dp));
                if (i >= n) buf[i - n] = Mathf.Clamp(v * Master, -1, 1);
                phase += dp; if (phase >= 1) phase -= 1;
            }
            return buf;
        }

        public static AudioClip Clip(string name, float[] samples)
        {
            var c = AudioClip.Create(name, samples.Length, 1, Rate, false);
            c.SetData(samples, 0);
            return c;
        }
    }
}
