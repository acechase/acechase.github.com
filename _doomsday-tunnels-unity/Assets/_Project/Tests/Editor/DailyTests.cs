using DoomsdayTunnels.Core;
using DoomsdayTunnels.Online;
using DoomsdayTunnels.Tunnel;
using NUnit.Framework;

namespace DoomsdayTunnels.Tests
{
    /// <summary>The daily sector comes from the date alone, by formulas the scoreboard server shares.</summary>
    public class DailyTests
    {
        [Test]
        public void TheDateFixesTheSector()
        {
            Assert.AreEqual(0, Daily.DayIndex("2026-01-01")); Assert.AreEqual(292, Daily.DayIndex("2026-10-20")); Assert.AreEqual(365, Daily.DayIndex("2027-01-01"));
            Assert.AreEqual(4, Daily.Difficulty(0)); Assert.AreEqual(7, Daily.Difficulty(3)); Assert.AreEqual(4, Daily.Difficulty(4)); Assert.AreEqual(4, Daily.Difficulty(292));
            Assert.AreEqual(5, Daily.Difficulty(-3), "a date before the epoch still lands in 4..7");
            var tune = new Tune();
            var a = Daily.Make("2026-10-20", tune); var b = Daily.Make("2026-10-20", tune); var c = Daily.Make("2026-10-21", tune);
            Assert.AreEqual("2026-10-20", a.Daily); Assert.AreEqual(4, a.N); Assert.AreEqual(5, c.N);
            Assert.AreEqual(a.Objs.Count, b.Objs.Count); for (int i = 0; i < a.Objs.Count; i++) { Assert.AreEqual(a.Objs[i].Z, b.Objs[i].Z); Assert.AreEqual(a.Objs[i].Kind, b.Objs[i].Kind); }
            Assert.AreEqual(a.Course.Features.Count, b.Course.Features.Count);
            var plain = SectorGenerator.Make(4, tune);
            Assert.AreNotEqual(plain.Px[1], a.Px[1], "the daily sector is not sector 4's layout"); Assert.IsNull(plain.Daily);
            Assert.AreEqual(plain.Length, a.Length); Assert.AreEqual(plain.V0, a.V0); Assert.AreEqual(plain.Limit, a.Limit, "but it has sector 4's speed, length and clock");
            Assert.IsNotNull(a.Fork); Assert.Greater(a.Course.Features.Count, 0);
            StringAssert.IsMatch(@"^\d{4}-\d{2}-\d{2}$", Daily.Today());
        }

        [Test]
        public void DailyBoardJsonParses()
        {
            var b = DailyBoard.Parse("{\"game\":\"app3d\",\"day\":\"2026-10-20\",\"difficulty\":4,\"top\":[{\"name\":\"ACE\",\"t\":512}]}");
            Assert.AreEqual(4, b.difficulty); Assert.AreEqual(1, b.top.Length); Assert.AreEqual(512, b.top[0].t); Assert.IsFalse(b.ok);
            var r = DailyBoard.Parse("{\"ok\":true,\"name\":\"ACE\",\"best\":512,\"rank\":2,\"game\":\"app3d\",\"day\":\"2026-10-20\",\"difficulty\":4,\"top\":[]}");
            Assert.IsTrue(r.ok); Assert.AreEqual(2, r.rank); Assert.AreEqual(0, r.top.Length);
            Assert.IsNull(DailyBoard.Parse("<html>"));
        }
    }
}
