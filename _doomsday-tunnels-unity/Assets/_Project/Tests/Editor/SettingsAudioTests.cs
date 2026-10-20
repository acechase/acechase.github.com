using System.Collections.Generic;
using DoomsdayTunnels.Audio;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Input;
using NUnit.Framework;
using UnityEngine;

namespace DoomsdayTunnels.Tests
{
    /// <summary>Settings persistence, the tilt maths, the synthesised sounds and the Part Two palette.</summary>
    public class SettingsAudioTests
    {
        sealed class MemoryStore : Settings.IStore
        {
            public readonly Dictionary<string, float> F = new Dictionary<string, float>(); public readonly Dictionary<string, int> I = new Dictionary<string, int>();
            public int Saves;
            public float GetFloat(string k, float d) => F.TryGetValue(k, out var v) ? v : d;
            public void SetFloat(string k, float v) => F[k] = v;
            public int GetInt(string k, int d) => I.TryGetValue(k, out var v) ? v : d;
            public void SetInt(string k, int v) => I[k] = v;
            public readonly Dictionary<string, string> S = new Dictionary<string, string>();
            public string GetString(string k, string d) => S.TryGetValue(k, out var v) ? v : d;
            public void SetString(string k, string v) => S[k] = v;
            public bool Has(string k) => F.ContainsKey(k) || I.ContainsKey(k) || S.ContainsKey(k);
            public void Save() => Saves++;
        }

        [Test]
        public void DefaultsMatchTheWebGame()
        {
            var s = new Settings(new MemoryStore());
            Assert.AreEqual(2.6f, s.Tune.Steer, 1e-5f); Assert.AreEqual(4.5f, s.Tune.Snap, 1e-5f); Assert.AreEqual(1f, s.Tune.Speed, 1e-5f);
            Assert.AreEqual(0.14f, s.Tune.Pull, 1e-5f); Assert.AreEqual(0.7f, s.Tune.Twist, 1e-5f); Assert.AreEqual(0.4f, s.Tune.Sway, 1e-5f); Assert.AreEqual(18f, s.Tune.Tilt, 1e-5f);
            Assert.AreEqual(SteerMode.Stick, s.Steer); Assert.IsTrue(s.Sound); Assert.IsTrue(s.CountPlays); Assert.IsFalse(s.ReduceMotion); Assert.IsFalse(s.InvertTilt);
            Assert.IsTrue(s.Tune.FairSpeed);
        }

        [Test]
        public void SettingsRoundTrip()
        {
            var store = new MemoryStore();
            var a = new Settings(store);
            a.Tune.Steer = 3.3f; a.Tune.Speed = 1.2f; a.Steer = SteerMode.Tilt; a.InvertTilt = true; a.Sound = false; a.CountPlays = false; a.ReduceMotion = true; a.Best = 4242; a.Name = "ACE"; a.LastDay = "2026-10-19";
            a.Save();
            var b = new Settings(store);
            Assert.AreEqual(3.3f, b.Tune.Steer, 1e-5f); Assert.AreEqual(1.2f, b.Tune.Speed, 1e-5f); Assert.IsFalse(b.Tune.FairSpeed);
            Assert.AreEqual(SteerMode.Tilt, b.Steer); Assert.IsTrue(b.InvertTilt); Assert.IsFalse(b.Sound); Assert.IsFalse(b.CountPlays); Assert.IsTrue(b.ReduceMotion); Assert.AreEqual(4242, b.Best); Assert.AreEqual("ACE", b.Name); Assert.AreEqual("2026-10-19", b.LastDay);
            Assert.AreEqual(1, store.Saves);
            b.ResetTune(); Assert.AreEqual(2.6f, b.Tune.Steer, 1e-5f); Assert.AreEqual(1f, b.Tune.Speed, 1e-5f);
        }

        [Test]
        public void StoredValuesAreClampedAndSnapped()
        {
            var store = new MemoryStore(); store.F["tune.steer"] = 99f; store.F["tune.pull"] = -1f;
            var s = new Settings(store);
            Assert.AreEqual(4.4f, s.Tune.Steer, 1e-5f); Assert.AreEqual(0f, s.Tune.Pull, 1e-5f);
            var snap = Settings.Defs[1];                                   // snap: 2..12 in halves
            Assert.AreEqual(4.5f, Settings.Snap(snap, 4.6f), 1e-5f); Assert.AreEqual(12f, Settings.Snap(snap, 40f), 1e-5f); Assert.AreEqual("4.5", snap.Format(4.5f));
            Assert.AreEqual("18", Settings.Defs[6].Format(18f)); Assert.AreEqual("0.14", Settings.Defs[3].Format(0.14f));
        }

        [Test]
        public void SectorTimesKeepTheFastestFairRun()
        {
            var s = new Settings(new MemoryStore());
            Assert.IsFalse(s.RecordTime(1, 40f, true)); Assert.AreEqual(40f, s.BestTime(1), 1e-5f);      // a first time is kept but is not a "best"
            Assert.IsFalse(s.RecordTime(1, 45f, true)); Assert.AreEqual(40f, s.BestTime(1), 1e-5f);      // slower: ignored
            Assert.IsTrue(s.RecordTime(1, 38f, true)); Assert.AreEqual(38f, s.BestTime(1), 1e-5f);       // faster: a personal best
            Assert.IsFalse(s.RecordTime(1, 20f, false)); Assert.AreEqual(38f, s.BestTime(1), 1e-5f);     // not at the standard speed: never counts
            Assert.AreEqual(0f, s.BestTime(2), 1e-5f);
        }

        [Test]
        public void DailyBestKeepsOnlyTheLatestDay()
        {
            var s = new Settings(new MemoryStore());
            Assert.AreEqual(0f, s.DailyBest("2026-10-20"));
            Assert.IsFalse(s.RecordDaily("2026-10-20", 50f), "a first time is kept but is not a best");
            Assert.IsFalse(s.RecordDaily("2026-10-20", 55f)); Assert.AreEqual(50f, s.DailyBest("2026-10-20"));
            Assert.IsTrue(s.RecordDaily("2026-10-20", 48f)); Assert.AreEqual(48f, s.DailyBest("2026-10-20"));
            Assert.IsFalse(s.RecordDaily("2026-10-21", 60f)); Assert.AreEqual(60f, s.DailyBest("2026-10-21")); Assert.AreEqual(0f, s.DailyBest("2026-10-20"), "yesterday is gone");
        }

        [Test]
        public void TiltAnglesComeFromTheUpVector()
        {
            TiltReader.Measure(new Vector3(0, 1, 0), out var roll, out var pitch);              // held upright, square on
            Assert.AreEqual(0, roll, 1e-5f); Assert.AreEqual(0, pitch, 1e-5f);
            float a = 20 * Mathf.Deg2Rad;
            TiltReader.Measure(new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0), out roll, out pitch);     // right side dipped 20 degrees
            Assert.AreEqual(a, roll, 1e-4f); Assert.AreEqual(0, pitch, 1e-5f);
            TiltReader.Measure(new Vector3(0, 0, 1), out roll, out pitch);                      // lying flat, screen up
            Assert.AreEqual(0, roll, 1e-5f); Assert.AreEqual(Mathf.PI / 2, pitch, 1e-5f);
            // A phone held landscape with its top edge to the left reports "up" along its own +x; in picture terms that is straight up.
            var pic = TiltReader.ToPicture(new Vector3(1, 0, 0), ScreenOrientation.LandscapeLeft);
            Assert.AreEqual(0, pic.x, 1e-5f); Assert.AreEqual(1, pic.y, 1e-5f);
            pic = TiltReader.ToPicture(new Vector3(-1, 0, 0), ScreenOrientation.LandscapeRight);
            Assert.AreEqual(0, pic.x, 1e-5f); Assert.AreEqual(1, pic.y, 1e-5f);
        }

        [Test]
        public void TiltSteeringHasADeadZoneAndAFullRange()
        {
            float range = 18;
            TiltReader.Steer(0, 0, 0, 0, range, false, out var tx, out var ty); Assert.AreEqual(0, tx); Assert.AreEqual(0, ty);
            TiltReader.Steer(0.5f * Mathf.Deg2Rad, 0, 0, 0, range, false, out tx, out ty); Assert.AreEqual(0, tx);         // inside the dead zone
            TiltReader.Steer(range * Mathf.Deg2Rad, 0, 0, 0, range, false, out tx, out ty); Assert.AreEqual(1, tx, 1e-4f); Assert.AreEqual(0, ty, 1e-5f);
            TiltReader.Steer(0, -range * 0.8f * Mathf.Deg2Rad, 0, 0, range, false, out tx, out ty); Assert.AreEqual(1, ty, 1e-4f);   // top edge tipped toward you: climb
            TiltReader.Steer(0, -range * 0.8f * Mathf.Deg2Rad, 0, 0, range, true, out tx, out ty); Assert.AreEqual(-1, ty, 1e-4f);   // inverted
            TiltReader.Steer(0.1f, 0.2f, 0.1f, 0.2f, range, false, out tx, out ty); Assert.AreEqual(0, tx); Assert.AreEqual(0, ty);   // the calibrated hold is centre
            TiltReader.Steer(0, 3.1f, 0, -3.1f, range, false, out tx, out ty); Assert.Less(Mathf.Abs(ty), 1f + 1e-4f);           // pitch wraps, no wild values
        }

        static readonly string[] AllSounds =
        {
            "laser", "boom", "hit", "tink", "pick", "scrape", "clear", "beep", "boostOn", "boostOff", "boostDry", "refuel",
            "static", "thunder", "sting", "fanfare", "villain", "hero", "morse", "phone", "mother", "laugh", "snip", "cheerSmall", "cheerBig", "cheerGrand",
        };

        [Test]
        public void EverySoundRendersInRange()
        {
            foreach (var name in AllSounds)
            {
                var p = Synth.Named(name); Assert.IsNotNull(p, name);
                var buf = Synth.Render(p);
                Assert.AreEqual(Mathf.CeilToInt(p.Length * Synth.Rate), buf.Length, name);
                float peak = 0, sum = 0; foreach (var v in buf) { Assert.IsFalse(float.IsNaN(v), name); peak = Mathf.Max(peak, Mathf.Abs(v)); sum += Mathf.Abs(v); }
                Assert.LessOrEqual(peak, 1f, name); Assert.Greater(peak, 0.01f, name); Assert.Greater(sum / buf.Length, 1e-4f, name);
            }
            Assert.IsNull(Synth.Named("nope"));
            Assert.AreEqual(0.09f + 0.03f, Synth.Named("laser").Length, 1e-5f);
            Assert.Greater(Synth.Named("cheerGrand").Length, 2.5f);
        }

        [Test]
        public void ToneFadesAndSlidesAsOnTheWeb()
        {
            var buf = Synth.Render(new Synth.Patch().T(Synth.Wave.Sine, 440, 440, 0.5f, 0.4f));
            int n = Synth.Rate / 2;
            float early = 0, late = 0; for (int i = 0; i < n / 10; i++) early = Mathf.Max(early, Mathf.Abs(buf[i])); for (int i = n - n / 10; i < n; i++) late = Mathf.Max(late, Mathf.Abs(buf[i]));
            Assert.AreEqual(0.4f * Synth.Master, early, 0.02f); Assert.Less(late, early * 0.1f);         // exponential fade to nothing
            // A sliding square: count zero crossings in the first and last tenth to see the pitch fall.
            buf = Synth.Render(new Synth.Patch().T(Synth.Wave.Square, 900, 180, 1f, 0.5f));
            int Cross(int a, int b) { int c = 0; for (int i = a + 1; i < b; i++) if ((buf[i] >= 0) != (buf[i - 1] >= 0)) c++; return c; }
            Assert.Greater(Cross(0, Synth.Rate / 10), Cross(Synth.Rate * 9 / 10, Synth.Rate) * 2);
        }

        [Test]
        public void HumLoopsWithoutASeam()
        {
            var hum = Synth.RenderHum();
            Assert.AreEqual(Synth.Rate, hum.Length);
            float peak = 0; foreach (var v in hum) peak = Mathf.Max(peak, Mathf.Abs(v));
            Assert.Greater(peak, 0.05f); Assert.LessOrEqual(peak, 1f);
            // one second holds a whole number of cycles and the filter has settled, so the end runs straight into the start
            float stepSeam = Mathf.Abs(hum[0] - hum[hum.Length - 1]), stepTypical = 0;
            for (int i = 1; i < 2000; i++) stepTypical = Mathf.Max(stepTypical, Mathf.Abs(hum[i] - hum[i - 1]));
            Assert.LessOrEqual(stepSeam, stepTypical * 1.5f);
        }

        [Test]
        public void PartTwoIsNeonFromSectorSix()
        {
            Assert.IsFalse(Palette.For(5).Neon); Assert.IsTrue(Palette.For(6).Neon); Assert.IsTrue(Palette.For(11).Neon);
            var p = Palette.For(6);
            float Lum(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            Assert.Less(Lum(p.FloorA), 0.08f); Assert.Less(Lum(p.WallA), 0.1f);                  // surfaces near black
            Assert.Greater(Lum(p.Line), Lum(p.FloorA) * 5);                                         // the lines carry the colour
            Assert.AreEqual(Palette.HueFor(1), Palette.HueFor(6));                                  // the five hues come round again
        }
    }
}
