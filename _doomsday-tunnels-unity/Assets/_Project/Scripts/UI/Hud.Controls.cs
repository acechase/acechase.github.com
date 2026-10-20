using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Input;
using UnityEngine;

namespace DoomsdayTunnels.UI
{
    /// <summary>
    /// The thumb controls, as on the web: the stick, and the two buttons that are their own gauges. A button is a
    /// bright disc with an icon (two chevrons for boost, a gun sight for fire) and the bright part drains downwards
    /// as the reserve is used or the guns heat up. Spent, it goes dark and hollow with a broken outline; its ring
    /// blinks red just before, and again if pressed anyway, and a ring bursts outwards when it comes back. With the
    /// buttons saying all this, touch screens do without the BOOST and GUNS bars.
    /// </summary>
    public sealed partial class Hud
    {
        float gunHotAt = -9, gunReadyAt = -9, boostDryAt = -9, boostFillAt = -9; bool wasHot, wasDry;
        public float BoostPressedDryAt = -9;

        void DrawControls(FlightSim sim, InputReader input, GameState state, float now)
        {
            if (!input.TouchUI || state != GameState.Play) return;
            float R = input.StickRadius; bool on = input.StickDown;
            var a = on ? Gui(input.StickAnchor) : new Vector2(112 * u, H - 104 * u);
            Ring(a, R, Palette.Void, 0.28f); Ring(a, R, Palette.Ink, on ? 0.35f : 0.18f);
            Ring(a + new Vector2(input.Stick.x, -input.Stick.y) * R, 22 * u, Palette.Ink, on ? 0.55f : 0.22f);
            if (!on) Text(input.TiltActive ? "AIM" : "STEER", a.x, a.y, 11 * u, Palette.Ink, TextAnchor.MiddleCenter);
            // the moments the gauges react to
            if (sim.GunHot != wasHot) { wasHot = sim.GunHot; if (sim.GunHot) gunHotAt = now; else gunReadyAt = now; }
            bool dry = sim.BoostFuel <= 0; if (dry != wasDry) { wasDry = dry; if (dry) boostDryAt = now; else boostFillAt = now; }
            if (input.BoostDown && dry && BoostPressedDryAt < boostDryAt) BoostPressedDryAt = now;
            bool shooting = input.FireDown && !sim.GunHot;
            Gauge(Gui(input.FireCentre), input.FireRadius, 1 - sim.GunHeat, sim.GunHot, shooting, shooting && sim.GunHeat > 0.7f, sim.GunHot ? "HOT" : "FIRE", false, now - Mathf.Max(gunHotAt, input.FireDown && sim.GunHot ? now - 0.01f : -9), now - gunReadyAt, now);
            Gauge(Gui(input.BoostCentre), input.BoostRadius, sim.BoostFuel, dry, sim.Boosting, sim.Boosting && sim.BoostFuel < 0.2f, dry ? "EMPTY" : "BOOST", true, now - Mathf.Max(boostDryAt, BoostPressedDryAt), now - boostFillAt, now);
        }

        // f: how much is left (0..1). dead: spent. active: in use. warn: nearly spent and still being used.
        // out: seconds since it gave out (or was pressed while spent). got: seconds since it came back.
        void Gauge(Vector2 c, float r, float f, bool dead, bool active, bool warn, string label, bool chevrons, float outT, float gotT, float now)
        {
            f = Mathf.Clamp01(f); float rr = r + 6 * u, level = c.y + r - 2 * r * f;
            bool flick = warn && !ReduceMotion && (now * 6) % 1 < 0.5f;
            Text(label, c.x, c.y - rr - 12 * u, 10 * u, dead ? Palette.Dim : Palette.Ink, TextAnchor.MiddleCenter);
            Ring(c, r, Palette.Void, 0.45f);
            if (dead)
            {
                if (f > 0) Fill(c, r, level, Palette.Ink, 0.2f);
                Icon(c, r, chevrons, y => Palette.Ink, 0.38f);
                DashedRing(c, r, Palette.Ink, 0.55f);
            }
            else
            {
                float lit = flick ? 0.35f : active ? 0.95f : 0.8f;
                Fill(c, r, level, Palette.Ink, lit);
                Icon(c, r, chevrons, y => y > level ? Palette.Void : Palette.Ink, 1f);     // dark where the disc is lit, light where it is not
                DashedRing(c, r, Palette.Ink, 0.9f, 0);
            }
            // what is left again, as a ring round the button
            Arc(c, rr, 0, 1, Palette.Void, 0.5f, 4 * u);
            if (f > 0) Arc(c, rr, 0, f, Palette.Ink, dead ? 0.3f : flick ? 0.35f : active && !ReduceMotion ? 0.7f + 0.3f * Mathf.Sin(now * 20) : 0.9f, 4 * u);
            if (warn) Arc(c, rr, 0, 1, Palette.Warn, ReduceMotion ? 0.6f : flick ? 0.9f : 0.15f, 5 * u);                           // nearly spent and still going
            if (dead && outT < 0.7f) Arc(c, rr, 0, 1, Palette.Warn, (1 - outT / 0.7f) * (ReduceMotion || (outT * 8) % 1 < 0.6f ? 1 : 0.2f), 5 * u);   // just gave out, or pressed while spent
            if (gotT < 0.5f && !dead && !ReduceMotion) { float t = gotT / 0.5f; Arc(c, rr + t * 26 * u, 0, 1, Palette.Ink, 1 - t, (1 + 3 * (1 - t)) * u); }   // back again: a ring bursts outwards
        }

        /// <summary>The part of a disc below `level`, as horizontal slices.</summary>
        void Fill(Vector2 c, float r, float level, Color col, float alpha)
        {
            float step = Mathf.Max(1f, 1.5f * u);
            for (float y = Mathf.Max(level, c.y - r); y < c.y + r; y += step)
            {
                float dy = y + step / 2 - c.y, hw = Mathf.Sqrt(Mathf.Max(0, r * r - dy * dy));
                Box(c.x - hw, y, 2 * hw, Mathf.Min(step, c.y + r - y), col, alpha);
            }
        }

        /// <summary>A ring from fraction `from` to `to` of a turn (clockwise from the top), as a run of dots.</summary>
        void Arc(Vector2 c, float r, float from, float to, Color col, float alpha, float w)
        {
            int n = Mathf.Max(8, Mathf.RoundToInt(r * 0.9f)); float d = w * 1.1f;
            for (int i = 0; i <= n; i++)
            {
                float t = from + (to - from) * i / n; if (t > to + 1e-4f) break;
                float ang = -Mathf.PI / 2 + t * 2 * Mathf.PI;
                Box(c.x + Mathf.Cos(ang) * r - d / 2, c.y + Mathf.Sin(ang) * r - d / 2, d, d, col, alpha);
            }
        }
        void DashedRing(Vector2 c, float r, Color col, float alpha, int gap = 1)
        {
            int n = gap > 0 ? 40 : Mathf.Max(48, Mathf.RoundToInt(r * 2.2f)); float d = gap > 0 ? 1.6f * u : 2f * u;
            for (int i = 0; i < n; i++) { if (gap > 0 && (i / 3) % 2 == 1) continue; float ang = i * 2 * Mathf.PI / n; Box(c.x + Mathf.Cos(ang) * r - d / 2, c.y + Mathf.Sin(ang) * r - d / 2, d, d, col, alpha); }
        }

        // Boost: two stacked arrowheads pointing up the tunnel, as on the boost cell. Fire: a gun sight.
        void Icon(Vector2 c, float r, bool chevrons, System.Func<float, Color> colAt, float alpha)
        {
            float k = r / 20;
            if (chevrons)
            {
                float lw = 3.4f * k;
                foreach (float dy in new[] { -5f, 5f })
                {   // each arm in short pieces, so the colour can change where the lit level crosses it
                    for (int side = -1; side <= 1; side += 2) for (int j = 0; j < 4; j++)
                    {
                        float t0 = j / 4f, t1 = (j + 1) / 4f;
                        var a = new Vector2(c.x + side * 9 * k * t0, c.y + (dy - 4.5f + 9 * t0) * k); var b = new Vector2(c.x + side * 9 * k * t1, c.y + (dy - 4.5f + 9 * t1) * k);
                        Bar(a, b, lw, colAt((a.y + b.y) / 2), alpha);
                    }
                }
            }
            else
            {
                float lw = 2.5f * k; int n = 28;
                for (int i = 0; i < n; i++) { float ang = i * 2 * Mathf.PI / n, y = c.y + Mathf.Sin(ang) * 6 * k; Box(c.x + Mathf.Cos(ang) * 6 * k - lw / 2, y - lw / 2, lw, lw, colAt(y), alpha); }
                foreach (var (dx, dy) in new[] { (1f, 0f), (-1f, 0f), (0f, 1f), (0f, -1f) })
                    for (int j = 0; j < 3; j++) { float t0 = 3 + 2.5f * j, t1 = t0 + 2.5f; var a = new Vector2(c.x + dx * t0 * k, c.y + dy * t0 * k); var b = new Vector2(c.x + dx * t1 * k, c.y + dy * t1 * k); Bar(a, b, lw, colAt((a.y + b.y) / 2), alpha); }
            }
        }

        /// <summary>Rotates what is drawn next about a point, in the HUD's own coordinates (Unity's helper ignores the safe-area shift).</summary>
        static void Rot(float deg, Vector2 pivot) => GUI.matrix = GUI.matrix * Matrix4x4.TRS(pivot, Quaternion.Euler(0, 0, deg), Vector3.one) * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);

        /// <summary>A thick line between two points, as a rotated box.</summary>
        void Bar(Vector2 a, Vector2 b, float w, Color col, float alpha)
        {
            var d = b - a; float len = d.magnitude; if (len < 0.01f) return;
            var m = GUI.matrix; Rot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a);
            Box(a.x, a.y - w / 2, len, w, col, alpha);
            GUI.matrix = m;
        }
    }
}
