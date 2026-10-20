using System;
using System.Collections.Generic;
using DoomsdayTunnels.Core;
using UnityEngine;

namespace DoomsdayTunnels.Tunnel
{
    public enum FeatureKind { Climb, Dive, Bend, Loop, Corkscrew }

    /// <summary>
    /// One stretch of the sector where the tunnel does something a 2D renderer never could: climbs or dives steeply,
    /// bends hard, loops the loop, or corkscrews. Measured along the tunnel in metres, like everything else.
    /// </summary>
    public sealed class Feature
    {
        public FeatureKind Kind;
        public float S0, S1;                 // where it starts and ends along the tunnel
        public float Angle;                  // climb/dive pitch, bend yaw (radians); loops and corkscrews are a full turn
        public float Tilt;                   // loop: how far its axis leans forward, which makes the loop a helix so it exits beside its entry
        public int Sign = 1;                 // bend: left or right; corkscrew: which way it winds
        public float Length => S1 - S0;
        public bool Holds(float s) => s >= S0 && s < S1;
    }

    /// <summary>
    /// The sector's 3D course: which features it has and where. Drawn from the sector's own second random stream, so
    /// the obstacle layout (which must match the web game draw for draw) is untouched. Sector 1 gets gentle climbs
    /// and dives; loops come in from sector 3, corkscrews from 4, and there are more of everything later on.
    /// </summary>
    public sealed class Course
    {
        public readonly List<Feature> Features = new List<Feature>();
        public Feature At(float s) { foreach (var f in Features) if (f.Holds(s)) return f; return null; }

        public const float Margin = 100f;             // no feature in the first stretch, nor near the fork, nor at the end
        public const float LoopShift = 26f;           // a loop exits this far to the side of its entry: clear of both branches of a fork, even just before it closes

        public static Course For(Sector lv) => For(lv, lv.N * 31337 + 7);
        public static Course For(Sector lv, int seed)
        {
            var R = new SectorRng(seed);
            var c = new Course(); int n = lv.N;
            int want = Math.Min(2 + n / 2, 6);
            // the stretches that may hold a feature: before the fork's widening and after the branches have parted
            var windows = new List<(float a, float b)>();
            if (lv.Fork != null) { windows.Add((Margin, lv.Fork.Z0 - 50)); windows.Add((lv.Fork.Z1 + G.ForkSpread + 30, lv.Length - 90)); }
            else windows.Add((Margin, lv.Length - 90));
            foreach (var (a, b) in windows)
            {
                float s = a + R.NextF() * 30;
                while (c.Features.Count < want)
                {
                    var f = Pick(ref R, n, out float len);
                    if (s + len > b) break;
                    f.S0 = s; f.S1 = s + len;
                    c.Features.Add(f);
                    s = f.S1 + 50 + R.NextF() * 90;
                }
            }
            return c;
        }

        static Feature Pick(ref SectorRng R, int n, out float len)
        {
            var f = new Feature();
            double k = R.Next();
            bool loops = n >= 3, screws = n >= 4;
            if (screws && k < 0.2) { f.Kind = FeatureKind.Corkscrew; len = 90 + R.NextF() * 40; f.Sign = R.Next() < 0.5 ? -1 : 1; }
            else if (loops && k < 0.45) { f.Kind = FeatureKind.Loop; len = 95 + R.NextF() * 35; f.Tilt = Mathf.Asin(Mathf.Min(0.35f, LoopShift / len)); }   // the helix advances LoopShift metres
            else if (n >= 2 && k < 0.65) { f.Kind = FeatureKind.Bend; len = 70 + R.NextF() * 50; f.Angle = (60 + R.NextF() * 70) * Mathf.Deg2Rad; f.Sign = R.Next() < 0.5 ? -1 : 1; }
            else { f.Kind = R.Next() < 0.5 ? FeatureKind.Climb : FeatureKind.Dive; len = 110 + R.NextF() * 60; f.Angle = (n == 1 ? 22 + R.NextF() * 12 : 35 + R.NextF() * 25) * Mathf.Deg2Rad; }
            return f;
        }
    }
}
