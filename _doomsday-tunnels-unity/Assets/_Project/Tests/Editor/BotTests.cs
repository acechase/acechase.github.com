using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Tunnel;
using NUnit.Framework;

namespace DoomsdayTunnels.Tests
{
    /// <summary>The title's demo pilot: it steers, shoots at gates, boosts when the way is clear, and keeps a sector going.</summary>
    public class BotTests
    {
        [Test]
        public void TheBotFliesASectorOnItsOwn()
        {
            var tune = new Tune(); var sim = new FlightSim(); sim.StartSector(SectorGenerator.Make(2, tune), tune);
            var bot = new Bot(42); bool fired = false, boosted = false, steered = false; int steps = 0, respawns = 0;
            while (sim.Pz < sim.Sector.Length && steps++ < 60 * 150)
            {
                bot.Think(sim, 1f / 60f); var inp = bot.Input;
                Assert.LessOrEqual(System.Math.Abs(inp.SteerX), 1f); Assert.LessOrEqual(System.Math.Abs(inp.SteerY), 1f);
                fired |= inp.Fire; boosted |= inp.Boost; steered |= System.Math.Abs(inp.SteerX) > 0.5f;
                sim.Step(1f / 60f, inp); sim.Events.Clear();
                if (!sim.Playing) { if (sim.Shield <= 0) { sim.Respawn(); respawns++; } else break; }
            }
            Assert.IsTrue(fired, "it shoots"); Assert.IsTrue(boosted, "it boosts"); Assert.IsTrue(steered, "it steers");
            Assert.Greater(sim.Pz, 400f, "it gets a good way down the tunnel"); Assert.Less(respawns, 12, "and does not just crash into everything");
            var again = new Bot(42); var sim2 = new FlightSim(); sim2.StartSector(SectorGenerator.Make(2, tune), tune); sim2.Now = 0; sim.Now = 0;
            again.Think(sim2, 1f / 60f); var b2 = new Bot(42); b2.Think(sim2, 1f / 60f);
            Assert.AreEqual(again.X, b2.X, "the same seed thinks the same thoughts");
        }
    }
}
