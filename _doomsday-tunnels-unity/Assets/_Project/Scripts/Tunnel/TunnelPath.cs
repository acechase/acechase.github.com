using System;
using DoomsdayTunnels.Core;
using UnityEngine;

namespace DoomsdayTunnels.Tunnel
{
    /// <summary>
    /// Where the tunnel is at depth z. The tunnel runs along +z; its centre wanders sideways (x) and up and down (y)
    /// as a sum of two sines per axis, easing in over the first 80 metres. World position = (cx + x, cy + y, z).
    /// </summary>
    public sealed class TunnelPath
    {
        readonly Sector lv; readonly float twist;
        public TunnelPath(Sector lv, Tune tune) { this.lv = lv; twist = tune.Twist; }

        static float Env(float z) { if (z <= 0) return 0; if (z >= 80) return 1; float t = z / 80; return t * t * (3 - 2 * t); }
        float Wave(float[] p, float z) => twist * Env(z) * (p[0] * Mathf.Sin(z * p[1] + p[2]) + p[3] * Mathf.Sin(z * p[4] + p[5]));

        public float Cx(float z) => Wave(lv.Px, z);
        public float Cy(float z) => Wave(lv.Py, z);
        public Vector3 Centre(float z) => new Vector3(Cx(z), Cy(z), z);
        public Vector3 World(float x, float y, float z) => new Vector3(Cx(z) + x, Cy(z) + y, z);
        /// <summary>Direction the tunnel is heading at z, for a camera that looks down the tunnel rather than straight ahead.</summary>
        public Vector3 Tangent(float z) => (Centre(z + 1f) - Centre(z - 1f)).normalized;
        public float HalfWidth(float z) => G.HW;     // widens at a fork in a later milestone
    }
}
