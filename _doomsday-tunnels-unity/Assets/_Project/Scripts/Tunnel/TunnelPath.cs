using DoomsdayTunnels.Core;
using UnityEngine;

namespace DoomsdayTunnels.Tunnel
{
    /// <summary>
    /// Where the tunnel is at depth z. The tunnel runs along +z; its centre wanders sideways (x) and up and down (y)
    /// as a sum of two sines per axis, easing in over the first 80 metres. World position = (cx + x, cy + y, z).
    /// Past the fork there are two centrelines, one per branch, each the trunk's line plus an offset of its own.
    /// </summary>
    public sealed class TunnelPath
    {
        readonly Sector lv; readonly float twist; readonly Fork fork;
        public TunnelPath(Sector lv, Tune tune) { this.lv = lv; twist = tune.Twist; fork = lv.Fork; }

        /// <summary>The branch the ship has taken at the fork, or -1 while it is still in the trunk.</summary>
        public int Chosen = -1;
        public Fork Fork => fork;

        static float Env(float z) { if (z <= 0) return 0; if (z >= 80) return 1; float t = z / 80; return t * t * (3 - 2 * t); }
        float Wave(float[] p, float z) => twist * Env(z) * (p[0] * Mathf.Sin(z * p[1] + p[2]) + p[3] * Mathf.Sin(z * p[4] + p[5]));

        /// <summary>The trunk's centreline.</summary>
        public float BaseX(float z) => Wave(lv.Px, z);
        public float Cy(float z) => Wave(lv.Py, z);

        /// <summary>How far a branch's centre sits from the trunk's line: one tunnel-width at the split, easing further apart, with a wander of its own.</summary>
        public float BranchDX(int br, float z)
        {
            float sgn = br == 1 ? 1 : -1, t = Mathf.Clamp01((z - fork.Z1) / G.ForkSpread), sm = t * t * (3 - 2 * t); var w = fork.Wig[br];
            return sgn * (G.HW + G.ForkSep * sm) + sm * w[0] * Mathf.Sin((z - fork.Z1) * w[1]);
        }

        /// <summary>
        /// Centre of the trunk (br = -1) or of a branch at depth z. A branch's line runs straight back past the split,
        /// which is what a ship that has just chosen measures itself from.
        /// </summary>
        public float Cx(float z, int br) { float x = BaseX(z); if (fork != null && br >= 0) x += BranchDX(br, Mathf.Max(z, fork.Z1)); return x; }
        /// <summary>Centre of the tunnel the ship is in: the trunk until the fork, the chosen branch after.</summary>
        public float Cx(float z) => Cx(z, Chosen);
        public Vector3 Centre(float z) => new Vector3(Cx(z), Cy(z), z);
        public Vector3 World(float x, float y, float z) => new Vector3(Cx(z) + x, Cy(z) + y, z);
        public Vector3 World(float x, float y, float z, int br) => new Vector3(Cx(z, br) + x, Cy(z) + y, z);
        /// <summary>Direction the ship's tunnel is heading at z, for a camera that looks down the tunnel rather than straight ahead.</summary>
        public Vector3 Tangent(float z) => (Centre(z + 1f) - Centre(z - 1f)).normalized;

        /// <summary>Half the trunk's width as built: it doubles across the widening just before the split.</summary>
        public float BuiltHalfWidth(float z)
        {
            if (fork == null || z <= fork.Z0) return G.HW;
            return z >= fork.Z1 ? G.HW * 2 : G.HW * (1 + (z - fork.Z0) / (fork.Z1 - fork.Z0));
        }
        /// <summary>Half the width around the ship: the widening counts only while the fork is still untaken; a branch is a full tunnel again.</summary>
        public float HalfWidth(float z) => Chosen >= 0 ? G.HW : BuiltHalfWidth(z);
    }
}
