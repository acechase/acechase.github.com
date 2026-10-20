using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Tunnel;
using NUnit.Framework;

namespace DoomsdayTunnels.Tests
{
    public class MineShotTests
    {
        [Test]
        public void AShotFiredAtAMineBlowsItUp()
        {
            var tune = new Tune { Pull = 0, Twist = 0 }; var lv = SectorGenerator.Make(3, tune);
            SectorObject mine = null; foreach (var o in lv.Objs) if (o.Kind == ObjKind.Mine && o.Branch < 0) { mine = o; break; }
            Assert.IsNotNull(mine);
            var sim = new FlightSim(); sim.StartSector(lv, tune);
            sim.Pz = mine.Z - 20; sim.Speed = 22; int steps = 0; bool boom = false;
            while (steps++ < 240 && mine.Alive)
            {   // sit where the mine is swinging to and hold the trigger
                sim.Rx = mine.MineX(sim.Now + 0.17f); sim.Ry = mine.Y + 0.09f; sim.Vx = sim.Vy = 0;
                sim.Step(1f / 60f, new SimInput { Fire = true });
                foreach (var e in sim.Events) if (e.Kind == SimEventKind.Boom) boom = true;
                sim.Events.Clear();
                if (sim.Pz > mine.Z) break;
            }
            Assert.IsFalse(mine.Alive, "the mine dies"); Assert.IsTrue(boom, "with a bang");
        }
    }
}
