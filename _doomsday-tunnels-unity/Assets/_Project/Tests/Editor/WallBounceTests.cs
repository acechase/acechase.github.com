using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Tunnel;
using NUnit.Framework;

namespace DoomsdayTunnels.Tests
{
    /// <summary>Red walls are solid: the ship bounces off and is shoved toward the gap, paying shield once.</summary>
    public class WallBounceTests
    {
        static (FlightSim, SectorObject, Rect4) Setup()
        {
            var tune = new Tune { Pull = 0, Twist = 0 };                   // a straight tunnel, so the ship goes where it is put
            var lv = SectorGenerator.Make(1, tune);
            SectorObject wall = null; foreach (var o in lv.Objs) if (o.Kind == ObjKind.Wall && o.Branch < 0) { wall = o; break; }
            Assert.IsNotNull(wall);
            var sim = new FlightSim(); sim.StartSector(lv, tune);
            var r = wall.Rects[0];
            sim.Pz = wall.Z - 12; sim.Rx = r.Cx; sim.Ry = r.Cy; sim.Speed = 22;
            return (sim, wall, r);
        }

        [Test]
        public void TheShipIsThrownBackAndPaysOnce()
        {
            var (sim, wall, r) = Setup();
            float shield0 = sim.Shield; bool bounced = false; int steps = 0;
            while (steps++ < 60 * 8 && sim.Playing && sim.Pz < wall.Z + 1)
            {
                sim.Step(1f / 60f, default);
                if (wall.Hit && !bounced) { bounced = true; Assert.Less(sim.Pz, wall.Z - G.ShipNose + 0.01f, "held short of the wall by the length of the nose"); Assert.Less(sim.Speed, 0, "thrown back"); Assert.AreEqual(shield0 - G.WallHit, sim.Shield, 1e-3, "one hit's worth of shield"); }
                sim.Events.Clear();
            }
            Assert.IsTrue(bounced);
            Assert.Greater(sim.Pz, wall.Z, "shoved toward the gap, it gets past in the end");
            Assert.GreaterOrEqual(sim.Shield, shield0 - G.WallHit - 6, "a second bounce off the same wall costs nothing (a scrape may)");
            Assert.Greater(sim.Speed, 5, "speed is coming back");
        }

        [Test]
        public void BoostRecoversSpeedFaster()
        {
            float After(bool boost)
            {
                var (sim, wall, r) = Setup();
                for (int i = 0; i < 60 * 6; i++) { sim.Step(1f / 60f, new SimInput { Boost = boost && wall.Hit }); sim.Events.Clear(); }
                return sim.Pz;
            }
            Assert.Greater(After(true), After(false) + 10, "boosting after a bounce gets further down the tunnel");
        }

        [Test]
        public void TheShoveHeadsForTheGap()
        {
            var (sim, wall, r) = Setup();
            while (!wall.Hit) { sim.Step(1f / 60f, default); sim.Events.Clear(); }
            // the push is along one axis, toward an edge of the slab that opens onto the tunnel, not into the tunnel wall
            Assert.IsTrue((sim.Vx != 0) != (sim.Vy != 0), "one axis");
            if (sim.Vx < 0) Assert.Greater(r.X0, -G.HW + 0.01f); if (sim.Vx > 0) Assert.Less(r.X1, G.HW - 0.01f);
            if (sim.Vy < 0) Assert.Greater(r.Y0, -G.HH + 0.01f); if (sim.Vy > 0) Assert.Less(r.Y1, G.HH - 0.01f);
        }
    }
}
