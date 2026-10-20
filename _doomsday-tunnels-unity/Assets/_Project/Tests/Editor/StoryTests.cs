using System.IO;
using DoomsdayTunnels.Story;
using NUnit.Framework;
using UnityEngine;

namespace DoomsdayTunnels.Tests
{
    public class StoryTests
    {
        static System.Collections.Generic.IEnumerable<(int n, Shot[] shots)> AllScenes()
        {
            yield return (1, Script.Intro());
            for (int n = 2; n <= 19; n++) yield return (n, Script.For(n));
        }

        [Test]
        public void TheScriptCoversEverySector()
        {
            Assert.AreEqual(5, Script.Intro().Length);
            Assert.IsNull(Script.For(1), "sector 1 has the intro instead");
            for (int n = 2; n <= 11; n++) Assert.IsNotNull(Script.For(n), "sector " + n);
            Assert.AreEqual(Script.For(12)[0].Bg, Script.For(19)[0].Bg, "the generic scenes rotate every seven sectors");
            Assert.AreNotEqual(Script.For(12)[0].Bg, Script.For(13)[0].Bg);
            StringAssert.Contains("device number 9", Script.For(13)[0].Lines[0].Text);
            foreach (var (n, shots) in AllScenes())
                foreach (var sh in shots)
                {
                    Assert.Contains(sh.Bg, Sets.Names, "sector " + n + " uses an unknown set");
                    Assert.Greater(sh.Lines.Length, 0);
                    foreach (var ln in sh.Lines) { Assert.IsTrue(Script.Who.ContainsKey(ln.Who), ln.Who); Assert.IsNotEmpty(ln.Text); }
                }
        }

        [Test]
        public void ATapFinishesTheSentenceThenMovesOn()
        {
            var p = new ScenePlayer(); bool done = false;
            var shots = Script.For(3);                                   // two shots, two and three lines
            p.Play(shots, () => done = true);
            Assert.IsTrue(p.Typing); p.Update(0.1f); Assert.Less(p.Shown, p.Cur.Text.Length);
            p.Advance(); Assert.IsFalse(p.Typing); Assert.AreEqual(0, p.LineNo);   // the first tap only finishes the line
            p.Advance(); Assert.AreEqual(1, p.LineNo); Assert.IsTrue(p.Typing);
            p.Advance(); p.Advance(); Assert.AreEqual(1, p.I); Assert.AreEqual(0, p.LineNo);   // next shot
            for (int i = 0; i < 6; i++) p.Advance();
            Assert.IsTrue(done); Assert.IsFalse(p.Active);
            p.Advance();                                                  // harmless once over
        }

        [Test]
        public void SkipEndsTheSceneAtOnce()
        {
            var p = new ScenePlayer(); bool done = false;
            p.Play(Script.Intro(), () => done = true); p.Update(1f); p.End();
            Assert.IsTrue(done); Assert.IsFalse(p.Active);
        }

        // Every set, in every variant the script uses, at a few moments in time: the painter must not throw or draw
        // out of bounds, and must produce a picture rather than a flat fill. With DT3D_SCENE_SHOTS set to a folder,
        // the frames are also written there as PNGs to be looked at.
        [Test]
        public void EverySetPaints()
        {
            string folder = System.Environment.GetEnvironmentVariable("DT3D_SCENE_SHOTS");
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            var g = new PixelCanvas(140); var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var (n, shots) in AllScenes())
                for (int si = 0; si < shots.Length; si++)
                {
                    var sh = shots[si]; string key = sh.Bg + (sh.Moon ? "-moon" : "") + (sh.Mother ? "-mother" : "") + (sh.NoBaron ? "-nobaron" : "") + (sh.Cut ? "-cut" : "") + (sh.Dot ? "-dot" : "") + (sh.Title != null ? "-title" : "");
                    bool first = seen.Add(key);
                    foreach (var ln in sh.Lines)
                        foreach (var t in new[] { 0.1f, 1.3f, 2.7f })
                        {
                            Sets.Paint(g, sh.Bg, t, ln.Who, true, sh, ln, out var title);
                            if (sh.Title != null) Assert.AreEqual(sh.Title, title.Text);
                            var colours = new System.Collections.Generic.HashSet<uint>();
                            foreach (var c in g.Buffer) colours.Add((uint)(c.r << 16 | c.g << 8 | c.b));
                            Assert.Greater(colours.Count, 4, key + " at t=" + t + " looks like a flat fill");
                            if (first && folder != null && t == 1.3f && ln == sh.Lines[0]) { g.Upload(); File.WriteAllBytes(Path.Combine(folder, key + ".png"), g.Texture.EncodeToPNG()); }
                        }
                }
        }

        [Test]
        public void TheCanvasDrawsWhereItIsTold()
        {
            var g = new PixelCanvas(100);
            g.Bx(0, 0, 320, 100, K.Black);
            g.Bx(10, 20, 5, 3, K.White);
            Assert.AreEqual((Color32)K.White, g.Buffer[(99 - 20) * 320 + 10]); Assert.AreEqual((Color32)K.White, g.Buffer[(99 - 22) * 320 + 14]);
            Assert.AreEqual((Color32)K.Black, g.Buffer[(99 - 23) * 320 + 10]); Assert.AreEqual((Color32)K.Black, g.Buffer[(99 - 20) * 320 + 15]);
            g.Ov(100, 50, 10, 6, K.Red);
            Assert.AreEqual((Color32)K.Red, g.Buffer[(99 - 50) * 320 + 100]); Assert.AreEqual((Color32)K.Black, g.Buffer[(99 - 50) * 320 + 112]);
            g.Pg(K.Cyan, 200, 10, 240, 10, 220, 40);
            Assert.AreEqual((Color32)K.Cyan, g.Buffer[(99 - 15) * 320 + 220]); Assert.AreEqual((Color32)K.Black, g.Buffer[(99 - 38) * 320 + 205]);
            g.Save(); g.Translate(300, 90); g.Scale(-1, 1); g.Bx(0, 0, 4, 2, K.Green); g.Restore();
            Assert.AreEqual((Color32)K.Green, g.Buffer[(99 - 90) * 320 + 297], "a flipped rect grows to the left");
            g.Alpha = 0.5f; g.Bx(0, 0, 1, 1, K.White); g.Alpha = 1;
            var mid = g.Buffer[99 * 320]; Assert.Greater(mid.r, 100); Assert.Less(mid.r, 160);
            g.ClipCircle(50, 80, 5); g.Bx(40, 70, 20, 20, K.Yellow); g.ClipNone();
            Assert.AreEqual((Color32)K.Yellow, g.Buffer[(99 - 80) * 320 + 50]); Assert.AreEqual((Color32)K.Black, g.Buffer[(99 - 72) * 320 + 42]);
        }
    }
}
