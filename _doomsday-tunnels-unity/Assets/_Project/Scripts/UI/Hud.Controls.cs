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
                Circle(c, r, 1.5f * u, Palette.Ink, 0.9f);
            }
            // what is left again, as a ring round the button
            Arc(c, rr, 0, 1, Palette.Void, 0.5f, 4 * u);
            if (f > 0) Arc(c, rr, 0, f, Palette.Ink, dead ? 0.3f : flick ? 0.35f : active && !ReduceMotion ? 0.7f + 0.3f * Mathf.Sin(now * 20) : 0.9f, 4 * u);
            if (warn) Arc(c, rr, 0, 1, Palette.Warn, ReduceMotion ? 0.6f : flick ? 0.9f : 0.15f, 5 * u);                           // nearly spent and still going
            if (dead && outT < 0.7f) Arc(c, rr, 0, 1, Palette.Warn, (1 - outT / 0.7f) * (ReduceMotion || (outT * 8) % 1 < 0.6f ? 1 : 0.2f), 5 * u);   // just gave out, or pressed while spent
            if (gotT < 0.5f && !dead && !ReduceMotion) { float t = gotT / 0.5f; Arc(c, rr + t * 26 * u, 0, 1, Palette.Ink, 1 - t, (1 + 3 * (1 - t)) * u); }   // back again: a ring bursts outwards
        }

        /// <summary>The part of a disc below `level`: the disc, clipped to a band.</summary>
        void Fill(Vector2 c, float r, float level, Color col, float alpha)
        {
            float top = Mathf.Max(level, c.y - r), h = c.y + r - top; if (h <= 0) return;
            GUI.BeginGroup(new Rect(c.x - r, top, 2 * r, h)); Disc(new Vector2(r, c.y - top), r, col, alpha); GUI.EndGroup();
        }

        /// <summary>A stroke from fraction `from` to `to` of a turn (clockwise from the top): a full turn is an outline, less is a run of overlapping round dots.</summary>
        void Arc(Vector2 c, float r, float from, float to, Color col, float alpha, float w)
        {
            if (to - from >= 0.999f) { Circle(c, r, w, col, alpha); return; }
            int n = Mathf.Max(2, Mathf.CeilToInt(2 * Mathf.PI * r * (to - from) / (w * 0.4f)));
            for (int i = 0; i <= n; i++)
            {
                float t = from + (to - from) * i / n, ang = -Mathf.PI / 2 + t * 2 * Mathf.PI;
                Disc(new Vector2(c.x + Mathf.Cos(ang) * r, c.y + Mathf.Sin(ang) * r), w / 2, col, alpha);
            }
        }
        void DashedRing(Vector2 c, float r, Color col, float alpha, int gap = 1)
        {
            if (gap == 0) { Circle(c, r, 1.5f * u, col, alpha); return; }
            int n = Mathf.Max(24, Mathf.RoundToInt(r / (2.2f * u))); float w = 1.6f * u;
            for (int i = 0; i < n; i++) { if ((i / 3) % 2 == 1) continue; float ang = i * 2 * Mathf.PI / n; Disc(new Vector2(c.x + Mathf.Cos(ang) * r, c.y + Mathf.Sin(ang) * r), w, col, alpha); }
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
                float lw = 2.5f * k;
                {   // the sight's ring, in two halves so each can take its own colour
                    float cut = Mathf.Clamp(colAt(c.y - 100) == colAt(c.y + 100) ? c.y + r : LevelFor(colAt, c.y - 6 * k, c.y + 6 * k), c.y - 6 * k, c.y + 6 * k);
                    GUI.BeginGroup(new Rect(c.x - 7 * k, c.y - 7 * k, 14 * k, cut - (c.y - 7 * k))); Circle(new Vector2(7 * k, 7 * k), 6 * k, lw, colAt(c.y - 7 * k), alpha); GUI.EndGroup();
                    GUI.BeginGroup(new Rect(c.x - 7 * k, cut, 14 * k, c.y + 7 * k - cut)); Circle(new Vector2(7 * k, c.y - cut), 6 * k, lw, colAt(c.y + 7 * k), alpha); GUI.EndGroup();
                }
                foreach (var (dx, dy) in new[] { (1f, 0f), (-1f, 0f), (0f, 1f), (0f, -1f) })
                    for (int j = 0; j < 3; j++) { float t0 = 3 + 2.5f * j, t1 = t0 + 2.5f; var a = new Vector2(c.x + dx * t0 * k, c.y + dy * t0 * k); var b = new Vector2(c.x + dx * t1 * k, c.y + dy * t1 * k); Bar(a, b, lw, colAt((a.y + b.y) / 2), alpha); }
            }
        }

        // Where the colour changes between two heights (the lit level), by bisection.
        static float LevelFor(System.Func<float, Color> colAt, float a, float b) { var ca = colAt(a); for (int i = 0; i < 12; i++) { float m = (a + b) / 2; if (colAt(m) == ca) a = m; else b = m; } return (a + b) / 2; }

        /// <summary>Rotates what is drawn next about a point, in the HUD's own coordinates (Unity's helper ignores the safe-area shift).</summary>
        static void Rot(float deg, Vector2 pivot) => GUI.matrix = GUI.matrix * Matrix4x4.TRS(pivot, Quaternion.Euler(0, 0, deg), Vector3.one) * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);

        /// <summary>A thick line between two points, as a rotated box.</summary>
        void Bar(Vector2 a, Vector2 b, float w, Color col, float alpha)
        {
            var d = b - a; float len = d.magnitude; if (len < 0.01f) return;
            var m = GUI.matrix; Rot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a);
            RoundBox(new Rect(a.x - w / 2, a.y - w / 2, len + w, w), w / 2, col, alpha);      // round caps
            GUI.matrix = m;
        }
    }
}
