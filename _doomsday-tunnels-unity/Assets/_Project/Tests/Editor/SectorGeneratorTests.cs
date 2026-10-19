using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Tunnel;
using NUnit.Framework;

namespace DoomsdayTunnels.Tests
{
    public class SectorGeneratorTests
    {
        // Reference values from the web game's mulberry32 (node: mulberry(n * 7919 + 13)) for seeds of sectors 1 and 3.
        [Test]
        public void RngMatchesTheWebGame()
        {
            var r1 = new SectorRng(1 * 7919 + 13);
            foreach (var v in new[] { 0.569124038, 0.576201165, 0.786746451, 0.859323077, 0.107661083 }) Assert.AreEqual(v, r1.Next(), 1e-9);
            var r3 = new SectorRng(3 * 7919 + 13);
            foreach (var v in new[] { 0.784171950, 0.404360025, 0.628869999, 0.162941983, 0.468758177 }) Assert.AreEqual(v, r3.Next(), 1e-9);
        }

        [Test]
        public void SameSectorSameLayout()
        {
            var tune = new Tune();
            var a = SectorGenerator.Make(4, tune); var b = SectorGenerator.Make(4, tune);
            Assert.AreEqual(a.Objs.Count, b.Objs.Count);
            Assert.Greater(a.Objs.Count, 10);
            for (int i = 0; i < a.Objs.Count; i++)
            {
                Assert.AreEqual(a.Objs[i].Kind, b.Objs[i].Kind);
                Assert.AreEqual(a.Objs[i].Z, b.Objs[i].Z); Assert.AreEqual(a.Objs[i].X, b.Objs[i].X); Assert.AreEqual(a.Objs[i].Y, b.Objs[i].Y);
            }
            for (int i = 0; i < 6; i++) { Assert.AreEqual(a.Px[i], b.Px[i]); Assert.AreEqual(a.Py[i], b.Py[i]); }
        }

        [Test]
        public void DifferentSectorsDiffer()
        {
            var tune = new Tune();
            var a = SectorGenerator.Make(1, tune); var b = SectorGenerator.Make(2, tune);
            Assert.AreNotEqual(a.Px[1], b.Px[1]);
            Assert.AreEqual(1050f, a.Length); Assert.AreEqual(22f, a.V0);
            Assert.AreEqual(1200f, b.Length); Assert.AreEqual(24f, b.V0);
        }

        [Test]
        public void ObjectsStayInsideTheTunnelAndInOrder()
        {
            var lv = SectorGenerator.Make(7, new Tune());
            float lastZ = -1;
            foreach (var o in lv.Objs)
            {
                Assert.GreaterOrEqual(o.Z, lastZ); lastZ = o.Z;
                Assert.GreaterOrEqual(o.Z, 90f); Assert.LessOrEqual(o.Z, lv.Length - 50 + 6);
                if (o.Kind != ObjKind.Wall && o.Kind != ObjKind.Gate) { Assert.LessOrEqual(System.Math.Abs(o.X), G.HW - 0.45f); Assert.LessOrEqual(System.Math.Abs(o.Y), G.HH - 0.32f); }
            }
        }

        [Test]
        public void FlightSimIsDeterministic()
        {
            float RunScore()
            {
                var sim = new FlightSim(); var tune = new Tune();
                sim.StartSector(SectorGenerator.Make(2, tune), tune);
                var inp = new SimInput { Fire = true, Boost = true };
                for (int i = 0; i < 60 * 40; i++) { inp.SteerX = (i / 90) % 2 == 0 ? 0.7f : -0.7f; inp.SteerY = (i / 130) % 2 == 0 ? 0.4f : -0.4f; sim.Step(1f / 60f, inp); sim.Events.Clear(); if (!sim.Playing) break; }
                return sim.Score * 1000 + sim.Pz;
            }
            Assert.AreEqual(RunScore(), RunScore());
        }

        [Test]
        public void StraightFlightReachesTheExitInTime()
        {
            var sim = new FlightSim(); var tune = new Tune();
            sim.StartSector(SectorGenerator.Make(1, tune), tune);
            sim.Invuln = 1e9f;                                            // ignore the walls; this checks the clock, not the piloting
            int steps = 0;
            while (sim.Playing && steps++ < 60 * 120) { sim.Step(1f / 60f, new SimInput()); sim.Events.Clear(); }
            Assert.IsFalse(sim.Playing);
            Assert.GreaterOrEqual(sim.Pz, sim.Sector.Length, "the ship should reach the exit");
            Assert.Greater(sim.Time, 0, "with time to spare at the standard speed");
        }
    }
}
