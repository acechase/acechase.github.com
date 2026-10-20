using DoomsdayTunnels.Online;
using NUnit.Framework;

namespace DoomsdayTunnels.Tests
{
    /// <summary>The scoreboard client's engine-free parts: the config file, the server's JSON, and the run log.</summary>
    public class OnlineTests
    {
        [Test]
        public void ConfigParsesAndIsOffWhenMissing()
        {
            var c = OnlineConfig.Parse("{ \"api\": \"https://example.workers.dev/\", \"appKey\": \"k123\" }");
            Assert.IsTrue(c.Enabled); Assert.AreEqual("https://example.workers.dev", c.api, "trailing slash dropped"); Assert.AreEqual("k123", c.appKey);
            Assert.IsFalse(OnlineConfig.Parse(null).Enabled); Assert.IsFalse(OnlineConfig.Parse("").Enabled); Assert.IsFalse(OnlineConfig.Parse("not json").Enabled);
            Assert.IsFalse(OnlineConfig.Parse("{ \"api\": \"https://x\" }").Enabled, "no key, no posting");
            Assert.AreEqual("app3d", OnlineConfig.Game);
        }

        const string SampleBoard = "{\"game\":\"app3d\",\"title\":\"Doomsday Tunnels 3D\",\"top\":[{\"name\":\"ACE\",\"score\":12345,\"sector\":7,\"day\":\"2026-10-19\"},{\"name\":\"Fluffles\",\"score\":900,\"sector\":2,\"day\":\"2026-10-18\"}],"
            + "\"times\":[{\"sector\":1,\"top\":[{\"name\":\"ACE\",\"t\":412,\"day\":\"2026-10-19\"},{\"name\":\"Fluffles\",\"t\":455,\"day\":\"2026-10-18\"}]},{\"sector\":2,\"top\":[{\"name\":\"ACE\",\"t\":480,\"day\":\"2026-10-19\"}]}]}";

        [Test]
        public void BoardJsonParses()
        {
            var b = BoardData.Parse(SampleBoard);
            Assert.IsNotNull(b); Assert.AreEqual("Doomsday Tunnels 3D", b.title);
            Assert.AreEqual(2, b.top.Length); Assert.AreEqual("ACE", b.top[0].name); Assert.AreEqual(12345, b.top[0].score); Assert.AreEqual(7, b.top[0].sector);
            Assert.AreEqual(2, b.times.Length); Assert.AreEqual(1, b.times[0].sector); Assert.AreEqual(2, b.times[0].top.Length); Assert.AreEqual(455, b.times[0].top[1].t);
            Assert.AreEqual("41.2", BoardFormat.Tenths(412)); Assert.AreEqual("5.0", BoardFormat.Tenths(50));
            var empty = BoardData.Parse("{\"game\":\"app3d\",\"top\":[],\"times\":[]}");
            Assert.AreEqual(0, empty.top.Length); Assert.AreEqual(0, empty.times.Length);
            Assert.IsNull(BoardData.Parse("<html>"));
        }

        [Test]
        public void PostResultCarriesRankOrRefusal()
        {
            var ok = PostResult.Parse("{\"ok\":true,\"name\":\"ACE\",\"best\":12345,\"rank\":1,\"top\":[{\"name\":\"ACE\",\"score\":12345,\"sector\":7,\"day\":\"2026-10-19\"}],\"times\":[]}");
            Assert.IsTrue(ok.ok); Assert.AreEqual(1, ok.rank); Assert.AreEqual(12345, ok.best); Assert.AreEqual(1, ok.AsBoard().top.Length);
            var no = PostResult.Parse("{\"error\":\"name\",\"message\":\"Please pick a different name.\"}");
            Assert.IsFalse(no.ok); Assert.AreEqual("name", no.error); Assert.AreEqual("Please pick a different name.", no.message);
            Assert.AreEqual("abc.def", TokenResult.Parse("{\"token\":\"abc.def\"}").token);
        }

        [Test]
        public void RunLogKeepsTimesForTheBoard()
        {
            var r = new RunLog(); r.Begin();
            Assert.AreEqual(1, r.Sector); Assert.AreEqual(0, r.Cleared); Assert.AreEqual(0, r.Times().Length);
            r.SectorCleared(1, 41.24f, true); r.SectorCleared(2, 48.0f, false); r.SectorCleared(3, 52.46f, true);
            Assert.AreEqual(3, r.Cleared); Assert.AreEqual(4, r.Sector);
            CollectionAssert.AreEqual(new[] { 412, 0, 525 }, r.Times(), "tenths of a second; nothing offered for an unfair sector");
            r.Begin(); Assert.AreEqual(0, r.Times().Length); Assert.IsNull(r.Token);
            RunLog.DayFlags("", "2026-10-19", out bool today, out bool ever); Assert.IsTrue(today); Assert.IsTrue(ever);
            RunLog.DayFlags("2026-10-18", "2026-10-19", out today, out ever); Assert.IsTrue(today); Assert.IsFalse(ever);
            RunLog.DayFlags("2026-10-19", "2026-10-19", out today, out ever); Assert.IsFalse(today); Assert.IsFalse(ever);
            StringAssert.IsMatch(@"^\d{4}-\d{2}-\d{2}$", RunLog.Today());
        }
    }
}
