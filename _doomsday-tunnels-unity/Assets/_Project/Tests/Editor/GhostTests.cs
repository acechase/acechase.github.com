using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Tunnel;
using NUnit.Framework;

namespace DoomsdayTunnels.Tests
{
    /// <summary>A run is its inputs: recorded, saved as text, and flown again step for step by the ghost.</summary>
    public class GhostTests
    {
        static SimInput Script(int i) => new SimInput { SteerX = (float)System.Math.Sin(i * 0.013), SteerY = (float)System.Math.Cos(i * 0.021) * 0.4f, Fire = i % 7 < 4, Boost = i % 300 < 90, AimX = 0.3f, AimY = -0.2f };

        [Test]
        public void LogRoundTripsThroughText()
        {
            var tune = new Tune { Steer = 3.1f, Snap = 5f, Speed = 1f, Pull = 0.1f, Twist = 0.9f };
            var sim = new FlightSim(); sim.StartSector(SectorGenerator.Make(2, tune), tune); sim.Now = 12.5f; sim.Shield = 64f; sim.Invuln = 1.5f;
            var log = new InputLog(sim, tune);
            for (int i = 0; i < 500; i++) log.Record(InputLog.Quantise(Script(i)), i == 123);
            var back = InputLog.Parse(log.Serialize());
            Assert.IsNotNull(back); Assert.AreEqual(500, back.Steps);
            Assert.AreEqual(12.5f, back.Now0); Assert.AreEqual(64f, back.Shield0); Assert.AreEqual(1.5f, back.Invuln0); Assert.AreEqual(3.1f, back.Steer); Assert.AreEqual(0.9f, back.Twist);
            for (int i = 0; i < 500; i++)
            {
                var a = InputLog.Quantise(Script(i)); var b = back.Read(i, out bool respawn);
                Assert.AreEqual(a.SteerX, b.SteerX); Assert.AreEqual(a.SteerY, b.SteerY); Assert.AreEqual(a.AimX, b.AimX); Assert.AreEqual(a.Fire, b.Fire); Assert.AreEqual(a.Boost, b.Boost);
                Assert.AreEqual(i == 123, respawn);
            }
            Assert.IsNull(InputLog.Parse("nonsense")); Assert.IsNull(InputLog.Parse(""));
            var q = InputLog.Quantise(new SimInput { SteerX = 2f, SteerY = -0.003f }); Assert.AreEqual(1f, q.SteerX); Assert.AreEqual(0f, q.SteerY, 1e-6);
        }

        [Test]
        public void TheGhostFliesTheRecordedRunExactly()
        {
            var tune = new Tune { Steer = 2.9f, Twist = 0.8f };
            var sim = new FlightSim(); sim.StartSector(SectorGenerator.Make(3, tune), tune); sim.Now = 40.25f; sim.Shield = 80f; sim.Invuln = 0.5f;
            var log = new InputLog(sim, tune);
            var trace = new System.Collections.Generic.List<(float, float, float, int)>();
            bool respawnNext = false;
            int lost = 0;
            for (int i = 0; i < 60 * 25; i++)
            {
                if (!sim.Playing) { Assert.LessOrEqual(sim.Shield, 0, "only a lost ship stops this run"); sim.Respawn(); respawnNext = true; lost++; }   // a ship lost and replaced, as the game does
                var q = InputLog.Quantise(Script(i));
                sim.Step(1f / 60f, q); log.Record(q, respawnNext); respawnNext = false; sim.Events.Clear();
                trace.Add((sim.Pz, sim.Rx, sim.Ry, sim.Path.Chosen));
            }
            Assert.AreEqual(60 * 25, trace.Count); Assert.Greater(lost, 0, "the scripted run loses at least one ship, so a respawn is replayed too");
            var ghost = GhostRunner.From(log.Serialize(), 3); Assert.IsNotNull(ghost);
            Assert.AreEqual(trace.Count / 60f, ghost.Seconds, 1e-4);
            for (int i = 0; i < trace.Count; i++)
            {
                ghost.Advance(1f / 60f);
                Assert.AreEqual(trace[i].Item1, ghost.Sim.Pz, "z at step " + i); Assert.AreEqual(trace[i].Item2, ghost.Sim.Rx, "x at step " + i);
                Assert.AreEqual(trace[i].Item3, ghost.Sim.Ry, "y at step " + i); Assert.AreEqual(trace[i].Item4, ghost.Sim.Path.Chosen, "branch at step " + i);
            }
            Assert.IsTrue(ghost.Done); ghost.Advance(1f / 60f); Assert.IsTrue(ghost.Done);
            Assert.AreEqual(trace[trace.Count - 1].Item1, ghost.Sim.Pz, "advancing a finished ghost does nothing");
        }

        [Test]
        public void ARunNeverCarriesStateFromTheSectorBefore()
        {
            var tune = new Tune();
            var fresh = new FlightSim(); fresh.StartSector(SectorGenerator.Make(2, tune), tune);
            var used = new FlightSim(); used.StartSector(SectorGenerator.Make(1, tune), tune);
            for (int i = 0; i < 200; i++) { used.Step(1f / 60f, new SimInput { Fire = true, Boost = true, SteerX = 1 }); used.Events.Clear(); }
            used.StartSector(SectorGenerator.Make(2, tune), tune); used.Now = fresh.Now; used.Shield = fresh.Shield;
            for (int i = 0; i < 300; i++) { var q = Script(i); fresh.Step(1f / 60f, q); used.Step(1f / 60f, q); fresh.Events.Clear(); used.Events.Clear(); Assert.AreEqual(fresh.Pz, used.Pz, "step " + i); Assert.AreEqual(fresh.Bullets.Count, used.Bullets.Count, "shots at step " + i); }
        }
    }
}
