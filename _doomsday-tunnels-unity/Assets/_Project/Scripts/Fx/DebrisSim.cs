using System.Collections.Generic;
using DoomsdayTunnels.Core;
using UnityEngine;

namespace DoomsdayTunnels.Fx
{
    /// <summary>
    /// Chunks of shattered gate, mine and wall, flying in tunnel coordinates: they tumble, fall, and bounce off the
    /// floor, ceiling and walls of the tunnel they are in, which is a box the sim already knows, so no colliders are
    /// needed and the result is the same on every device. The view draws each chunk as a small slab. Cosmetic only:
    /// nothing here touches the flight model.
    /// </summary>
    public sealed class DebrisSim
    {
        public sealed class Chunk
        {
            public float X, Y, Z, Vx, Vy, Vz; public int Branch;
            public Vector3 Axis; public float Spin, Angle, Size, Life, Life0;
            public Color Col; public bool Bright;
        }
        public readonly List<Chunk> Chunks = new List<Chunk>();
        public int Cap = 320;
        public const float Gravity = 7f, Bounce = 0.45f, Drag = 0.6f;
        readonly System.Random rng = new System.Random(7);
        float R() => (float)rng.NextDouble();

        /// <summary>A burst of `n` chunks at a point, thrown forward (the shot's momentum) and outward.</summary>
        public void Burst(float x, float y, float z, int br, int n, Color col, float size, float throwZ)
        {
            for (int i = 0; i < n; i++)
            {
                if (Chunks.Count >= Cap) Chunks.RemoveAt(0);
                float a = R() * Mathf.PI * 2, sp = 1.2f + R() * 2.6f;
                Chunks.Add(new Chunk
                {
                    X = x + (R() - 0.5f) * 0.2f, Y = y + (R() - 0.5f) * 0.2f, Z = z, Branch = br,
                    Vx = Mathf.Cos(a) * sp, Vy = Mathf.Sin(a) * sp + 1.5f, Vz = throwZ * (0.5f + R()),
                    Axis = new Vector3(R() - 0.5f, R() - 0.5f, R() - 0.5f).normalized, Spin = (R() - 0.5f) * 900f, Angle = R() * 360,
                    Size = size * (0.5f + R()), Life0 = 1.4f + R() * 0.9f, Life = 0, Col = col, Bright = i % 4 == 0
                });
                Chunks[Chunks.Count - 1].Life = Chunks[Chunks.Count - 1].Life0;
            }
        }

        /// <summary>Steps every chunk. `halfWidth` gives the tunnel's half-width at a depth for a branch (-1 the trunk).</summary>
        public void Tick(float dt, System.Func<float, int, float> halfWidth)
        {
            for (int i = Chunks.Count - 1; i >= 0; i--)
            {
                var c = Chunks[i];
                c.Life -= dt; if (c.Life <= 0) { Chunks.RemoveAt(i); continue; }
                c.Vy -= Gravity * dt;
                float k = Mathf.Max(0, 1 - Drag * dt); c.Vx *= k; c.Vz *= k;
                c.X += c.Vx * dt; c.Y += c.Vy * dt; c.Z += c.Vz * dt; c.Angle += c.Spin * dt;
                float hw = halfWidth(c.Z, c.Branch) - c.Size * 0.5f, hh = G.HH - c.Size * 0.5f;
                if (c.Y < -hh) { c.Y = -hh; c.Vy = Mathf.Abs(c.Vy) * Bounce; c.Vx *= 0.8f; c.Vz *= 0.8f; c.Spin *= 0.6f; if (c.Vy < 0.3f) c.Vy = 0; }
                else if (c.Y > hh) { c.Y = hh; c.Vy = -Mathf.Abs(c.Vy) * Bounce; }
                if (c.X < -hw) { c.X = -hw; c.Vx = Mathf.Abs(c.Vx) * Bounce; }
                else if (c.X > hw) { c.X = hw; c.Vx = -Mathf.Abs(c.Vx) * Bounce; }
            }
        }

        /// <summary>Everything in the branch the ship did not take, or from an earlier sector, is dropped.</summary>
        public void Clear() => Chunks.Clear();
        public void DropBranch(int keep) => Chunks.RemoveAll(c => c.Branch >= 0 && c.Branch != keep);
    }
}
