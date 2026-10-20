using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Tunnel;
using NUnit.Framework;

namespace DoomsdayTunnels.Tests
{
    public class ForkTests
    {
        // Reference values from the web game's makeLevel (node, with its mulberry32) for sectors 1, 2 and 3.
        [Test]
        public void ForkMatchesTheWebGame()
        {
            var tune = new Tune();
            var a = SectorGenerator.Make(1, tune);
            Assert.AreEqual(420f, a.Fork.Z0); Assert.AreEqual(444f, a.Fork.Z1); Assert.AreEqual(1, a.Fork.Dark);
            Assert.AreEqual(0.6221508f, a.Fork.Wig[0][0], 1e-5); Assert.AreEqual(0.05970573f, a.Fork.Wig[0][1], 1e-6);
            Assert.AreEqual(0.7639247f, a.Fork.Wig[1][0], 1e-5); Assert.AreEqual(0.07801262f, a.Fork.Wig[1][1], 1e-6);
            Assert.AreEqual(1, SectorGenerator.Make(2, tune).Fork.Dark);
            Assert.AreEqual(0, SectorGenerator.Make(3, tune).Fork.Dark);
            Assert.AreEqual(ObjKind.Ship, a.ShipToken.Kind); Assert.AreEqual(a.Fork.Z1 + 22, a.ShipToken.Z); Assert.AreEqual(a.Fork.Dark, a.ShipToken.Branch);
            Assert.Contains(a.ShipToken, a.Objs);
        }

        [Test]
        public void ObjectsKeepClearOfTheSplit()
        {
            for (int n = 1; n <= 9; n += 4)
            {
                var lv = SectorGenerator.Make(n, new Tune()); int[] perBranch = new int[2];
                foreach (var o in lv.Objs)
                {
                    if (o.Branch < 0) Assert.LessOrEqual(o.Z, lv.Fork.Z0 - 14 + 6, "trunk objects stop before the widening");
                    else { Assert.GreaterOrEqual(o.Z, lv.Fork.Z1 + 22, "branch objects start past the split"); perBranch[o.Branch]++; }
                }
                Assert.Greater(perBranch[0], 3); Assert.Greater(perBranch[1], 3);
            }
        }

        [Test]
        public void TheTrunkWidensAndTheBranchesPart()
        {
            var lv = SectorGenerator.Make(1, new Tune()); var path = new TunnelPath(lv, new Tune()); var f = lv.Fork;
            Assert.AreEqual(G.HW, path.BuiltHalfWidth(f.Z0)); Assert.AreEqual(G.HW * 1.5f, path.BuiltHalfWidth(f.Z0 + G.ForkWiden / 2), 1e-5); Assert.AreEqual(G.HW * 2, path.BuiltHalfWidth(f.Z1));
            Assert.AreEqual(path.BaseX(f.Z1) - G.HW, path.Cx(f.Z1, 0), 1e-5, "at the split the branches sit either side of the trunk's line");
            Assert.AreEqual(path.BaseX(f.Z1) + G.HW, path.Cx(f.Z1, 1), 1e-5);
            Assert.AreEqual(path.Cx(f.Z1, 1) - path.Cx(f.Z1 - 50, 1), path.BaseX(f.Z1) - path.BaseX(f.Z1 - 50), 1e-5, "a branch's line runs straight back past the split, parallel to the trunk's");
            float far = f.Z1 + G.ForkSpread + 200;
            Assert.Greater(path.Cx(far, 1) - path.BaseX(far), G.HW + G.ForkSep - 2f); Assert.Less(path.Cx(far, 0) - path.BaseX(far), -(G.HW + G.ForkSep - 2f));
            Assert.AreEqual(G.HW * 2, path.HalfWidth(f.Z1 + 5)); path.Chosen = 0; Assert.AreEqual(G.HW, path.HalfWidth(f.Z1 + 5));
        }

        // Flies sector 1 until depth z, steering as `steer` says each step, with the red walls taken out (they are solid,
        // and a ship steered into one would bounce there for ever). The tunnel's bends pull the ship about, so a test
        // that wants a particular line has to hold it: `Hold` keeps the ship on the centreline.
        static FlightSim FlyTo(float z, System.Func<FlightSim, float> steer, FlightSim sim = null)
        {
            if (sim == null) { sim = new FlightSim(); var tune = new Tune(); var lv = SectorGenerator.Make(1, tune); lv.Objs.RemoveAll(o => o.Kind == ObjKind.Wall); sim.StartSector(lv, tune); sim.Invuln = 1e9f; }
            int steps = 0;
            while (sim.Playing && sim.Pz < z && steps++ < 60 * 120) sim.Step(1f / 60f, new SimInput { SteerX = steer(sim) });
            return sim;
        }
        static float Hold(FlightSim s) => UnityEngine.Mathf.Clamp(-s.Rx * 8f, -1f, 1f);

        [Test]
        public void SteeringLeftTakesTheLeftBranch()
        {
            var sim = FlyTo(SectorGenerator.Make(1, new Tune()).Fork.Z1 + 5, s => -1f);
            Assert.AreEqual(0, sim.Path.Chosen);
            Assert.Less(System.Math.Abs(sim.Rx), G.HW, "the ship is measured from the branch's centre now");
            Assert.IsTrue(sim.Events.Exists(e => e.Kind == SimEventKind.Fork && e.Count == 0));
            Assert.IsTrue(sim.Events.Exists(e => e.Kind == SimEventKind.Popup && e.Text.StartsWith("FORK AHEAD")));
            Assert.IsNull(sim.Sector.ShipToken, "sector 1's token is in the right-hand (dark) branch");
            Assert.IsTrue(sim.Events.Exists(e => e.Kind == SimEventKind.Popup && e.Text.StartsWith("LIGHT TUNNEL")));
        }

        [Test]
        public void FlyingAtTheDividerClipsItAndStillPicksASide()
        {
            var lv = SectorGenerator.Make(1, new Tune());
            var sim = FlyTo(lv.Fork.Z1, Hold);                  // the step that reaches the split is the one that picks the side
            Assert.That(sim.Path.Chosen, Is.EqualTo(0).Or.EqualTo(1));
            Assert.AreEqual(G.HW - 0.22f, System.Math.Abs(sim.Rx), 1e-4, "pushed just clear of the wall, then re-based on the branch");
            Assert.IsTrue(sim.Events.Exists(e => e.Kind == SimEventKind.ScreenBurst), "the clip registers as a hit (shrugged off here: the test ship is invulnerable)");
            Assert.IsTrue(sim.Events.Exists(e => e.Kind == SimEventKind.Popup && (e.Text == "DARK TUNNEL" || e.Text.StartsWith("LIGHT TUNNEL"))));
        }

        [Test]
        public void TheTokenIsEarnedInTheDarkBranchAndMissedOtherwise()
        {
            var lv = SectorGenerator.Make(1, new Tune()); float toDark = lv.Fork.Dark == 1 ? 1f : -1f;
            var sim = FlyTo(lv.Fork.Z1 + 5, s => toDark);       // hug the dark side's wall into the split
            Assert.AreEqual(lv.Fork.Dark, sim.Path.Chosen); Assert.IsNotNull(sim.Sector.ShipToken);
            sim.Events.Clear();
            FlyTo(lv.Fork.Z1 + 30, Hold, sim);                  // then back to the centreline, where the token is
            Assert.IsTrue(sim.ShipEarned, "the token sits on the branch's centreline, 22 m past the split");
            var bonus = sim.SectorClear(); Assert.IsTrue(bonus.Ship); Assert.AreEqual(1, sim.Lives);

            var miss = FlyTo(lv.Fork.Z1 + 5, s => toDark); miss.Events.Clear();
            FlyTo(lv.Fork.Z1 + 30, s => toDark, miss);          // staying on the wall flies past it
            Assert.IsFalse(miss.ShipEarned);
            Assert.IsTrue(miss.Events.Exists(e => e.Kind == SimEventKind.Popup && e.Text == "SHIP TOKEN MISSED"));
        }
    }
}
