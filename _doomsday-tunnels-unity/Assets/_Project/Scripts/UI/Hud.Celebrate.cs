using System.Collections.Generic;
using DoomsdayTunnels.Core;
using UnityEngine;

namespace DoomsdayTunnels.UI
{
    /// <summary>
    /// Celebrations, as on the web: confetti thrown from the bottom corners (or raining from the top for the big
    /// ones), cut in the shapes on Debbie's office wall, and a headline stamped on with the five sector colours
    /// running through its letters and a star at each end. Only on the screens between sectors and after a run,
    /// and never with reduced motion. Plus the two small icons the title uses: a speaker and a gear.
    /// </summary>
    public sealed partial class Hud
    {
        struct Piece { public float X, Y, Vx, Vy, K, Vt, T0, Life, S, Ph, Vr, Wf; public int Kind; public Color Col; }
        readonly List<Piece> confetti = new List<Piece>();

        /// <summary>`n` pieces thrown from the corners after `delay` seconds, and `rain` more dropped from the top.</summary>
        public void Party(int n, float delay, int rain, float now)
        {
            if (ReduceMotion) return;
            var cols = new List<Color>(Palette.Sectors) { Palette.Gate, Palette.Ink };
            float t0 = now + delay;
            for (int i = 0; i < n + rain; i++)
            {
                bool fall = i >= n; int side = i % 2 == 1 ? 1 : -1;
                float a = (35 + Random.value * 45) * Mathf.Deg2Rad, sp = (700 + Random.value * 800) * u, vt = (110 + Random.value * 90) * u;
                confetti.Add(new Piece
                {
                    X = fall ? Random.value * W : side < 0 ? 0 : W, Y = fall ? -12 * u : H + 6 * u,
                    Vx = fall ? (Random.value - 0.5f) * 120 * u : -side * Mathf.Cos(a) * sp, Vy = fall ? 0 : -Mathf.Sin(a) * sp,
                    K = 1.8f + Random.value * 0.9f, Vt = vt, T0 = t0 + (fall ? 0.3f + Random.value * 1.1f : Random.value * 0.25f), Life = fall ? Mathf.Min(5, (H + 40 * u) / vt) : 3 + Random.value * 0.8f,
                    Kind = i % 4, Col = cols[Random.Range(0, cols.Count)], S = (5 + Random.value * 4) * u, Ph = Random.value * 6.283f, Vr = (Random.value - 0.5f) * 9, Wf = 4 + Random.value * 4
                });
            }
        }
        public void ClearParty() => confetti.Clear();

        void DrawConfetti(float now)
        {
            for (int i = confetti.Count - 1; i >= 0; i--)
            {
                var p = confetti[i]; float t = now - p.T0, s = p.S;
                if (t < 0) continue;
                if (t > p.Life) { confetti.RemoveAt(i); continue; }
                float e = 1 - Mathf.Exp(-p.K * t), x = p.X + p.Vx * e / p.K + Mathf.Sin(t * p.Wf + p.Ph) * 10 * u * e, y = p.Y + (p.Vy - p.Vt) * e / p.K + p.Vt * t;
                float alpha = Mathf.Min(1, (p.Life - t) / 0.6f), rot = (p.Ph + p.Vr * t) * Mathf.Rad2Deg;
                var m = GUI.matrix; Rot(rot, new Vector2(x, y));
                if (p.Kind == 2) Ring(new Vector2(x, y), s * 0.38f, p.Col, alpha);
                else if (p.Kind == 3) { float lw = Mathf.Max(1.5f, s * 0.26f); Bar(new Vector2(x - s, y + s * 0.3f), new Vector2(x - s * 0.33f, y - s * 0.3f), lw, p.Col, alpha); Bar(new Vector2(x - s * 0.33f, y - s * 0.3f), new Vector2(x + s * 0.33f, y + s * 0.3f), lw, p.Col, alpha); Bar(new Vector2(x + s * 0.33f, y + s * 0.3f), new Vector2(x + s, y - s * 0.3f), lw, p.Col, alpha); }
                else
                {   // bars and triangles tumble end over end: their height follows a cosine
                    float sq = Mathf.Abs(Mathf.Cos(t * p.Wf * 1.4f + p.Ph));
                    if (p.Kind == 0) Box(x - s * 0.7f, y - s * 0.28f * sq, s * 1.4f, Mathf.Max(1, s * 0.56f * sq), p.Col, alpha);
                    else for (int j = 0; j < 5; j++) { float f = (j + 0.5f) / 5; Box(x - s * 0.6f * f, y + s * (0.5f - 1.1f * (1 - f)) * sq, s * 1.2f * f, Mathf.Max(1, s * 0.22f * sq), p.Col, alpha); }
                }
                GUI.matrix = m;
            }
        }

        /// <summary>A celebration headline: stamped on, the sector colours running through its letters, a star at each end. `t` is the time since it appeared.</summary>
        void Cheer(string s, float cx, float cy, float size, float t, float now)
        {
            if (t <= 0) return;
            float pop = ReduceMotion || t > 0.3f ? 1 : 1 + 0.8f * Mathf.Pow(1 - t / 0.3f, 2);
            float fs = size * pop; display.fontSize = Mathf.RoundToInt(fs); display.alignment = TextAnchor.MiddleCenter;
            float w = display.CalcSize(new GUIContent(s)).x, h = fs * 1.6f, x0 = cx - w / 2, off = ReduceMotion ? 0 : (now * 0.3f) % 1;
            var prev = GUI.color; GUI.color = new Color(1, 1, 1, ReduceMotion ? 1 : Mathf.Min(1, t / 0.12f));
            // dark edge first, then the letters, one colour band at a time
            foreach (var (dx, dy) in new[] { (1f, 0f), (-1f, 0f), (0f, 1f), (0f, -1f), (1f, 1f), (-1f, -1f) }) { display.normal.textColor = Palette.Void; GUI.Label(new Rect(x0 + dx * 2 * u, cy - h / 2 + dy * 2 * u, w, h), s, display); }
            int bands = 10;
            for (int i = 0; i <= bands; i++)
            {   // one band more than fits, since the whole run slides along by `off`
                float bw = w / bands, bx0 = x0 + (i - off) * bw;
                var r = new Rect(bx0, cy - h / 2, bw, h); if (r.xMax < x0 || r.x > x0 + w) continue;
                r.x = Mathf.Max(r.x, x0); r.width = Mathf.Min(bw, x0 + w - r.x);
                GUI.BeginGroup(r); display.normal.textColor = Palette.Sectors[((i % 5) + 5) % 5]; GUI.Label(new Rect(x0 - r.x, 0, w, h), s, display); GUI.EndGroup();
            }
            GUI.color = prev;
            Star(cx - w / 2 - size * 1.2f, cy, size * 0.75f, 0, now); Star(cx + w / 2 + size * 1.2f, cy, size * 0.75f, 2, now);
        }
        void Star(float x, float y, float r, float ph, float now)
        {
            float a = r * (ReduceMotion ? 0.85f : 0.65f + 0.35f * Mathf.Sin(now * 7 + ph)), b = a * 0.24f;
            Box(x - a, y - b / 2, 2 * a, b, Palette.Gate); Box(x - b / 2, y - a, b, 2 * a, Palette.Gate);
            var m = GUI.matrix; Rot(45, new Vector2(x, y)); Box(x - a * 0.55f, y - b / 2, a * 1.1f, b, Palette.Gate); Box(x - b / 2, y - a * 0.55f, b, a * 1.1f, Palette.Gate); GUI.matrix = m;
        }

        // Sound: a speaker with sound waves on a lit button, or with a line through it on a dark one when muted.
        void SpeakerIcon(Vector2 c, float r, bool off)
        {
            float k = r / 20, x = c.x - (off ? 0 : 2.5f * k), y = c.y; var col = off ? Palette.Ink : Palette.Void;
            Box(x - 7 * k, y - 3.2f * k, 4 * k, 6.4f * k, col);
            for (int i = 0; i < 6; i++) { float f = i / 5f; float hh = Mathf.Lerp(3.2f, 7.5f, f) * k; Box(x - 3 * k + f * 5 * k, y - hh, 1.1f * k, 2 * hh, col); }
            if (!off) { Arc(new Vector2(x + 2 * k, y), 4.2f * k, 0.11f, 0.39f, col, 1, 1.8f * k); Arc(new Vector2(x + 2 * k, y), 8 * k, 0.12f, 0.38f, col, 1, 1.8f * k); }
            else { Bar(new Vector2(c.x - 9 * k, c.y - 9 * k), new Vector2(c.x + 9 * k, c.y + 9 * k), 5.5f * k, Palette.Void, 1); Bar(new Vector2(c.x - 9 * k, c.y - 9 * k), new Vector2(c.x + 9 * k, c.y + 9 * k), 2.2f * k, Palette.Ink, 1); }
        }
        // Settings: a gear.
        void GearIcon(Vector2 c, float r)
        {
            float k = r / 20; int n = 8;
            for (int i = 0; i < n; i++) { var m = GUI.matrix; Rot(i * 360f / n, c); Box(c.x - 2.2f * k, c.y - 10 * k, 4.4f * k, 20 * k, Palette.Ink); GUI.matrix = m; }
            Ring(c, 7.2f * k, Palette.Ink, 1); Ring(c, 3.6f * k, Palette.Void, 1);
        }
    }
}
