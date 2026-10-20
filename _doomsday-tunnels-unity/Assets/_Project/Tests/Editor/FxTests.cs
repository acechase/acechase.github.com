using DoomsdayTunnels.Core;
using DoomsdayTunnels.Fx;
using NUnit.Framework;
using UnityEngine;

namespace DoomsdayTunnels.Tests
{
    /// <summary>The cosmetic systems that have engine-free logic: debris, the light selection and the quality tiers.</summary>
    public class FxTests
    {
        [Test]
        public void DebrisStaysInsideTheTunnelAndDies()
        {
            var d = new DebrisSim { Cap = 50 };
            d.Burst(0.3f, 0.2f, 100f, -1, 40, Color.yellow, 0.16f, 7f);
            Assert.AreEqual(40, d.Chunks.Count);
            float hw = G.HW;
            for (int step = 0; step < 240; step++)                                       // four seconds at 60 Hz
            {
                d.Tick(1f / 60f, (z, br) => hw);
                foreach (var c in d.Chunks)
                {
                    Assert.LessOrEqual(Mathf.Abs(c.X), hw + 1e-3f, "x at step " + step);
                    Assert.LessOrEqual(Mathf.Abs(c.Y), G.HH + 1e-3f, "y at step " + step);
                    Assert.GreaterOrEqual(c.Z, 100f, "chunks fly forward, never back");
                }
            }
            Assert.AreEqual(0, d.Chunks.Count, "every chunk has burnt out");
        }

        [Test]
        public void DebrisCapDropsTheOldest()
        {
            var d = new DebrisSim { Cap = 30 };
            d.Burst(0, 0, 10, -1, 20, Color.red, 0.1f, 1f); d.Chunks[0].Z = -1;             // mark the first
            d.Burst(0, 0, 20, 1, 20, Color.red, 0.1f, 1f);
            Assert.AreEqual(30, d.Chunks.Count);
            Assert.AreNotEqual(-1f, d.Chunks[0].Z, "the oldest chunk went first");
            d.DropBranch(0); Assert.AreEqual(10, d.Chunks.Count, "branch 1's chunks were dropped, the trunk's kept");
        }

        [Test]
        public void LightsNearestAndBrightestWinTheSlots()
        {
            var L = new DynamicLights { Budget = 3 };
            var cam = new Vector3(0, 0, 100);
            L.Flash(new Vector3(0, 0, 105), Color.red, 5, 3, 1);        // close and bright
            L.Flash(new Vector3(0, 0, 170), Color.green, 5, 3, 1);      // far
            L.Flash(new Vector3(0, 0, 90), Color.blue, 5, 3, 1);        // behind the camera: never chosen
            L.Flash(new Vector3(0, 0, 110), Color.white, 1, 0.2f, 1);   // dim
            L.Flash(new Vector3(0, 0, 120), Color.yellow, 6, 2, 1);
            var p = new Vector4[DynamicLights.Max]; var c = new Vector4[DynamicLights.Max];
            int n = L.Select(cam, p, c);
            Assert.AreEqual(3, n);
            Assert.AreEqual(105f, p[0].z, 1e-4f); Assert.AreEqual(120f, p[1].z, 1e-4f); Assert.AreEqual(170f, p[2].z, 1e-4f);
            for (int i = n; i < DynamicLights.Max; i++) Assert.AreEqual(0f, c[i].w, "unused slots are dark");
            L.Tick(0.5f); L.Select(cam, p, c);
            Assert.AreEqual(1.5f, c[0].w, 1e-4f, "a flash fades with its life");
            L.Tick(0.6f); Assert.AreEqual(0, L.Lights.Count);
            L.BeginFrame(); L.Hold(new Vector3(0, 0, 101), Color.cyan, 3, 1); Assert.AreEqual(1, L.Select(cam, p, c));
            L.BeginFrame(); Assert.AreEqual(0, L.Select(cam, p, c), "held lights last one frame");
        }

        [Test]
        public void QualityTiersFollowTheDevice()
        {
            Assert.AreEqual(QualityTier.High, Quality.TierFor("MacBookPro18,1", 32000, false));
            Assert.AreEqual(QualityTier.High, Quality.TierFor("iPhone19,2", 8000, true));      // iPhone 18 Pro
            Assert.AreEqual(QualityTier.High, Quality.TierFor("iPhone12,1", 4000, true));      // iPhone 11
            Assert.AreEqual(QualityTier.Mid, Quality.TierFor("iPhone11,8", 3000, true));       // iPhone XR
            Assert.AreEqual(QualityTier.Mid, Quality.TierFor("iPhone10,1", 2000, true));       // iPhone 8
            Assert.AreEqual(QualityTier.Low, Quality.TierFor("iPhone9,3", 2000, true));        // iPhone 7
            Assert.AreEqual(QualityTier.High, Quality.TierFor("iPad13,4", 8000, true));
            Assert.AreEqual(QualityTier.Mid, Quality.TierFor("iPhone", 2000, true), "an unreadable model string lands in the middle");
            Assert.AreEqual(QualityTier.High, Quality.TierFor("Pixel 8", 8000, true));
            Assert.AreEqual(QualityTier.Mid, Quality.TierFor("SM-A525F", 4000, true));
            Assert.AreEqual(QualityTier.Low, Quality.TierFor("Redmi 9A", 2000, true));
            var q = Quality.For(QualityTier.Low);
            Assert.LessOrEqual(q.Lights, DynamicLights.Max); Assert.Less(q.DebrisCap, Quality.For(QualityTier.High).DebrisCap); Assert.Less(q.RenderScale, 1f);
        }
    }
}
