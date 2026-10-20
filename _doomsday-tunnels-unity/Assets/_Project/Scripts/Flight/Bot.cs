using System;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Tunnel;

namespace DoomsdayTunnels.Flight
{
    /// <summary>
    /// The title screen's demo pilot, ported from the web game. A few times a second it picks a point to head for: the
    /// gap in the next wall, a hole already shot in the next gate, or the nearest pickup when the way is clear. It
    /// fires in bursts at gates and at mines in front of it, keeps clear of mines, picks a side at a fork, and boosts
    /// when nothing is coming. It is not good at any of this. Engine-free; its randomness comes from its own stream.
    /// </summary>
    public sealed class Bot
    {
        public float X, Y; public bool Fire, Boost; public int Side;
        float hold; SectorRng rng;

        public Bot(int seed) { rng = new SectorRng(seed); Side = rng.Next() < 0.5 ? 0 : 1; }

        public SimInput Input => new SimInput { SteerX = X, SteerY = Y, Fire = Fire, Boost = Boost };

        public void Think(FlightSim sim, float dt)
        {
            hold -= dt; if (hold > 0) return;
            hold = 0.15f + rng.NextF() * 0.25f;
            var f = sim.Sector.Fork; bool open = f != null && sim.Path.Chosen < 0;
            float tx = 0.3f * (float)Math.Sin(sim.Now * 0.7), ty = 0.2f * (float)Math.Sin(sim.Now * 0.9), near = 99;
            SectorObject wall = null, goal = null, mine = null; bool shoot = false;
            foreach (var o in sim.Sector.Objs)
            {
                float dz = o.Z - sim.Pz; if (dz < 0.5f) continue; if (dz > 40) break;
                if (open && o.Branch >= 0) continue;                             // the branches are past the split
                if (!open && o.Branch >= 0 && o.Branch != sim.Path.Chosen) continue;
                if (o.Kind == ObjKind.Wall) { if (!o.Hit && wall == null) { wall = o; near = Math.Min(near, dz); } }
                else if (o.Kind == ObjKind.Gate) { if (wall == null) { wall = o; near = Math.Min(near, dz); } if (dz < G.GunRange) shoot = true; }
                else if (o.Kind == ObjKind.Mine) { if (o.Alive) { near = Math.Min(near, dz); if (mine == null && dz < 22) mine = o; if (dz < G.GunRange && Math.Abs(o.MineX(sim.Now) - sim.Rx) < 0.35f && Math.Abs(o.Y - sim.Ry) < 0.35f) shoot = true; } }
                else if (!o.Taken && goal == null && dz < 32) goal = o;
            }
            if (open && sim.Pz > f.Z0 - 25 && sim.Pz < f.Z1) { tx = Side == 1 ? 0.9f : -0.9f; ty = 0; }      // heading for the chosen branch
            else if (goal != null && (wall == null || goal.Z < wall.Z)) { tx = goal.X; ty = goal.Y; }
            else if (wall != null && wall.Kind == ObjKind.Wall)
            {   // the nearest point of a grid that is clear of every slab
                float best = 1e9f;
                for (float gx = -1.05f; gx <= 1.06f; gx += 0.25f) for (float gy = -0.6f; gy <= 0.61f; gy += 0.2f)
                {
                    bool blocked = false; foreach (var r in wall.Rects) if (r.Contains(gx, gy, 0.17f)) { blocked = true; break; }
                    if (blocked) continue;
                    float d = (float)Math.Sqrt((gx - sim.Rx) * (gx - sim.Rx) + (gy - sim.Ry) * (gy - sim.Ry)) + rng.NextF() * 0.1f;
                    if (d < best) { best = d; tx = gx; ty = gy; }
                }
            }
            else if (wall != null)
            {   // a gate: through a hole if one has been shot, otherwise straight at it, firing
                GateBlock hole = null; foreach (var b in wall.Blocks) if (b.Hp <= 0) { hole = b; break; }
                if (hole != null) { tx = hole.R.Cx; ty = hole.R.Cy; } else { tx = sim.Rx; ty = sim.Ry; }
            }
            if (mine != null) { float mx = mine.MineX(sim.Now); if (Math.Abs(mx - tx) < 0.45f && Math.Abs(mine.Y - ty) < 0.45f) tx = Clamp(tx + (tx < mx ? -0.55f : 0.55f), -1, 1); }
            tx += (rng.NextF() - 0.5f) * 0.12f; ty += (rng.NextF() - 0.5f) * 0.1f;
            X = Clamp((tx - sim.Rx) * 3, -1, 1); Y = Clamp((ty - sim.Ry) * 3, -1, 1);
            Fire = shoot && !sim.GunHot && sim.GunHeat < 0.8f;
            Boost = near > 28 && sim.BoostFuel > 0.25f && sim.Shield > 40 && (Boost || rng.Next() < 0.35);
        }

        static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
    }
}
