using System.Collections.Generic;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Tunnel;
using NUnit.Framework;
using UnityEngine;

namespace DoomsdayTunnels.Tests
{
    /// <summary>The 3D course: features where they belong, a frame that stays orthonormal, loops that loop, and a tunnel that never runs into itself.</summary>
    public class PathTests
    {
        [Test]
        public void WithoutACourseTheTunnelRunsStraightDownZ()
        {
            var tune = new Tune(); var lv = SectorGenerator.Make(1, tune); lv.Course = new Course();
            var path = new TunnelPath(lv, tune);
            foreach (float s in new[] { 0f, 10f, 333f, 900f })
            {
                var w = path.World(0.3f, -0.2f, s, -1);
                Assert.AreEqual(path.BaseX(s) + 0.3f, w.x, 1e-3); Assert.AreEqual(path.Cy(s) - 0.2f, w.y, 1e-3); Assert.AreEqual(s, w.z, 1e-3);
                Assert.AreEqual(1f, Vector3.Dot(path.Tangent(s), Vector3.forward), 1e-4); Assert.AreEqual(1f, Vector3.Dot(path.Up(s), Vector3.up), 1e-4);
            }
        }

        [Test]
        public void FeaturesSitInTheOpenStretchesAndGrowWithTheSector()
        {
            bool anyLoop = false, anyScrew = false;
            for (int n = 1; n <= 12; n++)
            {
                var lv = SectorGenerator.Make(n, new Tune()); var c = lv.Course;
                Assert.Greater(c.Features.Count, 0, "sector " + n);
                float last = 0;
                foreach (var f in c.Features)
                {
                    Assert.GreaterOrEqual(f.S0, last + 40, "features keep a gap, sector " + n); last = f.S1;
                    Assert.GreaterOrEqual(f.S0, Course.Margin); Assert.LessOrEqual(f.S1, lv.Length - 90);
                    Assert.IsTrue(f.S1 <= lv.Fork.Z0 - 50 || f.S0 >= lv.Fork.Z1 + G.ForkSpread + 30, "nothing across the fork, sector " + n);
                    if (n == 1) Assert.IsTrue(f.Kind == FeatureKind.Climb || f.Kind == FeatureKind.Dive, "sector 1 is gentle");
                    if (n < 3) Assert.AreNotEqual(FeatureKind.Loop, f.Kind); if (n < 4) Assert.AreNotEqual(FeatureKind.Corkscrew, f.Kind);
                    anyLoop |= f.Kind == FeatureKind.Loop; anyScrew |= f.Kind == FeatureKind.Corkscrew;
                }
                Assert.AreEqual(c.Features.Count, Course.For(lv).Features.Count, "deterministic");
            }
            Assert.IsTrue(anyLoop); Assert.IsTrue(anyScrew);
        }

        static Feature Find(FeatureKind k, out Sector lv, out TunnelPath path)
        {
            for (int n = 1; n <= 16; n++)
            {
                var tune = new Tune(); lv = SectorGenerator.Make(n, tune); path = new TunnelPath(lv, tune);
                foreach (var f in lv.Course.Features) if (f.Kind == k) return f;
            }
            lv = null; path = null; return null;
        }

        [Test]
        public void TheFrameStaysOrthonormal()
        {
            var tune = new Tune(); var lv = SectorGenerator.Make(6, tune); var path = new TunnelPath(lv, tune);
            for (float s = 0; s < lv.Length + 100; s += 3.7f)
            {
                var t = path.Tangent(s); var u = path.Up(s);
                Assert.AreEqual(1f, t.magnitude, 1e-3); Assert.AreEqual(1f, u.magnitude, 1e-3); Assert.AreEqual(0f, Vector3.Dot(t, u), 1e-3, "at " + s);
                Assert.AreEqual(1f, (path.World(0, 0, s + 1) - path.World(0, 0, s)).magnitude, 0.15f, "a metre along is a metre, at " + s);
            }
        }

        [Test]
        public void ALoopGoesOverTheTopAndComesOutBesideItsEntry()
        {
            var f = Find(FeatureKind.Loop, out var lv, out var path); Assert.IsNotNull(f);
            var t0 = path.Tangent(f.S0); var u0 = path.Up(f.S0);
            Assert.Greater(Vector3.Dot(path.Tangent(f.S0 + f.Length * 0.25f), u0), 0.8f, "climbing a quarter of the way round");
            Assert.Less(Vector3.Dot(path.Tangent(f.S0 + f.Length * 0.5f), t0), -0.9f, "heading back at the top");
            Assert.Less(Vector3.Dot(path.Up(f.S0 + f.Length * 0.5f), u0), -0.8f, "upside down at the top");
            Assert.Greater(Vector3.Dot(path.Tangent(f.S1), t0), 0.98f, "straight again at the end"); Assert.Greater(Vector3.Dot(path.Up(f.S1), u0), 0.98f);
            float apart = (path.World(0, 0, f.S1) - path.World(0, 0, f.S0)).magnitude;
            Assert.Greater(apart, 2 * G.HW + 2f, "the exit clears the entry");
        }

        [Test]
        public void AClimbPitchesUpAndLevelsOff()
        {
            var f = Find(FeatureKind.Climb, out var lv, out var path); Assert.IsNotNull(f);
            var t0 = path.Tangent(f.S0); var u0 = path.Up(f.S0);
            float mid = Vector3.Dot(path.Tangent(f.S0 + f.Length * 0.5f), u0);
            Assert.AreEqual(Mathf.Sin(f.Angle), mid, 0.05f, "holding the climb angle in the middle");
            Assert.Greater(Vector3.Dot(path.Tangent(f.S1), t0), 0.98f, "level again");
            Assert.Greater(Vector3.Dot(path.World(0, 0, f.S1) - path.World(0, 0, f.S0), u0), 20f, "and higher up");
        }

        [Test]
        public void ACorkscrewRollsTheTunnelRightRound()
        {
            var f = Find(FeatureKind.Corkscrew, out var lv, out var path); Assert.IsNotNull(f);
            var u0 = path.Up(f.S0);
            Assert.Less(Vector3.Dot(path.Up(f.S0 + f.Length * 0.5f), u0), -0.7f, "upside down half way");
            Assert.Greater(Vector3.Dot(path.Up(f.S1), u0), 0.9f, "the right way up again");
            Assert.Greater(Vector3.Dot(path.Tangent(f.S1), path.Tangent(f.S0)), 0.95f);
        }

        [Test]
        public void TheTunnelNeverRunsIntoItself()
        {
            for (int n = 1; n <= 12; n++)
            {
                var tune = new Tune(); var lv = SectorGenerator.Make(n, tune); var path = new TunnelPath(lv, tune);
                var pts = new List<(float s, Vector3 p)>();
                for (float s = 0; s <= lv.Length; s += 2)
                {
                    if (s < lv.Fork.Z1) pts.Add((s, path.World(0, 0, s, -1)));
                    else { pts.Add((s, path.World(0, 0, s, 0))); pts.Add((s, path.World(0, 0, s, 1))); }
                }
                float clear = 2 * G.HW + 0.6f;
                for (int i = 0; i < pts.Count; i++) for (int j = i + 1; j < pts.Count; j++)
                {
                    if (pts[j].s - pts[i].s < 14) continue;
                    Assert.Greater((pts[i].p - pts[j].p).magnitude, clear, "sector " + n + ": " + pts[i].s + " m and " + pts[j].s + " m come too close");
                }
            }
        }
    }
}
