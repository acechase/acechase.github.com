using DoomsdayTunnels.Core;
using UnityEngine;

namespace DoomsdayTunnels.Tunnel
{
    /// <summary>
    /// Where the tunnel is at depth s. Two things are combined here. First, the tunnel's own 3D course: a centreline
    /// integrated from the sector's features (climbs, dives, bends, loops, corkscrews), carrying a frame (forward, up,
    /// right) that is parallel-transported so it never twists on its own; a straight tunnel runs along +z. Second,
    /// inside that frame, the web game's lateral wander: the centre moves sideways (x) and up and down (y) as a sum
    /// of two sines per axis, and past the fork each branch has its own line. The flight model only ever sees the
    /// second part, in tunnel coordinates, so the course is cosmetic and sector times stay comparable.
    /// </summary>
    public sealed class TunnelPath
    {
        readonly Sector lv; readonly float twist; readonly Fork fork;
        public const float Step = 1f;                 // metres between frame samples
        readonly Vector3[] pos, tan, up; readonly int count;

        public TunnelPath(Sector lv, Tune tune) { this.lv = lv; twist = tune.Twist; fork = lv.Fork; count = Mathf.CeilToInt((lv.Length + 160) / Step) + 1; pos = new Vector3[count]; tan = new Vector3[count]; up = new Vector3[count]; Integrate(); }

        /// <summary>The branch the ship has taken at the fork, or -1 while it is still in the trunk.</summary>
        public int Chosen = -1;
        public Fork Fork => fork;

        /* ---------- the course ---------- */
        void Integrate()
        {
            var P = Vector3.zero; var T = Vector3.forward; var U = Vector3.up;
            Feature active = null; Vector3 axis = Vector3.right; float rollPer = 0;
            for (int i = 0; i < count; i++)
            {
                pos[i] = P; tan[i] = T; up[i] = U;
                float s = i * Step;
                var f = lv.Course?.At(s);
                if (f != active)
                {   // entering a feature: fix the axes it turns about from the frame as it stands now
                    active = f; rollPer = 0;
                    if (f != null)
                    {
                        var R0 = Vector3.Cross(U, T);
                        switch (f.Kind)
                        {
                            case FeatureKind.Loop:
                                axis = (Mathf.Cos(f.Tilt) * R0 + Mathf.Sin(f.Tilt) * T).normalized;
                                if (Vector3.Dot(Quaternion.AngleAxis(20, axis) * T, U) < 0) axis = -axis;      // a loop climbs first
                                break;
                            case FeatureKind.Corkscrew:
                                axis = (Mathf.Cos(0.35f) * T + Mathf.Sin(0.35f) * R0 * f.Sign).normalized;    // a helix around a line near straight ahead
                                rollPer = f.Sign * 360f / f.Length;                                             // and the tunnel rolls once around you
                                break;
                        }
                        if (f.Kind == FeatureKind.Loop || f.Kind == FeatureKind.Corkscrew)
                        {   // carrying a frame round a helix leaves it rolled a little (the geometry's holonomy): find out by how
                            // much with a dry run, and spread the opposite roll over the feature so it comes out the right way up
                            var Td = T; var Ud = U;
                            for (float sd = s; sd < f.S1; sd += Step) Advance(f, sd, axis, rollPer, ref Td, ref Ud);
                            float drift = Vector3.SignedAngle(Vector3.ProjectOnPlane(U, Td), Ud, Td);
                            rollPer -= drift / Mathf.Max(1f, f.Length);
                        }
                    }
                }
                var Tn = T; var Un = U;
                if (f != null) Advance(f, s, axis, rollPer, ref Tn, ref Un);
                P += (T + Tn) * (0.5f * Step); T = Tn; U = Un;
            }
        }

        // One step of a feature: turn the tangent as the feature says, carry the up vector round with it (parallel
        // transport: it turns with the tangent and no more), then add whatever roll the feature asks for.
        static void Advance(Feature f, float s, Vector3 axis, float rollPer, ref Vector3 T, ref Vector3 U)
        {
            float t = (s - f.S0) / f.Length, deg = 0; Vector3 a = axis;
            switch (f.Kind)
            {
                case FeatureKind.Climb: case FeatureKind.Dive:
                    {   // pitch ramps up over the first third, holds, and ramps back down
                        float rate = f.Angle * Mathf.Rad2Deg / (f.Length / 3);
                        deg = t < 1f / 3 ? rate : t > 2f / 3 ? -rate : 0; if (f.Kind == FeatureKind.Dive) deg = -deg;
                        a = Vector3.Cross(U, T);                                                      // about the current right: a level climb
                        if (Vector3.Dot(Quaternion.AngleAxis(10, a) * T, U) < 0) a = -a;             // positive is up
                        break;
                    }
                case FeatureKind.Bend:
                    deg = f.Sign * f.Angle * Mathf.Rad2Deg / f.Length * (1 - Mathf.Cos(t * 2 * Mathf.PI));   // eases in and out
                    a = U; break;
                case FeatureKind.Loop: case FeatureKind.Corkscrew:
                    deg = 360f / f.Length; break;
            }
            var Tn = deg != 0 ? (Quaternion.AngleAxis(deg * Step, a) * T).normalized : T;
            var Un = Quaternion.FromToRotation(T, Tn) * U;
            if (rollPer != 0) Un = Quaternion.AngleAxis(rollPer * Step, Tn) * Un;
            T = Tn; U = (Un - Vector3.Dot(Un, Tn) * Tn).normalized;
        }

        void Frame(float s, out Vector3 p, out Vector3 t, out Vector3 u)
        {
            float f = Mathf.Clamp(s / Step, 0, count - 1.001f); int i = (int)f; float k = f - i;
            p = Vector3.LerpUnclamped(pos[i], pos[i + 1], k);
            t = Vector3.LerpUnclamped(tan[i], tan[i + 1], k).normalized;
            u = Vector3.LerpUnclamped(up[i], up[i + 1], k); u = (u - Vector3.Dot(u, t) * t).normalized;
            if (s > (count - 1) * Step) p += t * (s - (count - 1) * Step);       // straight on past the samples
        }

        /// <summary>The tunnel's heading at depth s.</summary>
        public Vector3 Tangent(float s) { Frame(s, out _, out var t, out _); return t; }
        public Vector3 Up(float s) { Frame(s, out _, out _, out var u); return u; }
        /// <summary>A rotation whose forward is the tunnel's heading and whose up is the tunnel's up.</summary>
        public Quaternion Rotation(float s) { Frame(s, out _, out var t, out var u); return Quaternion.LookRotation(t, u); }

        /* ---------- the wander within the frame (what the flight model sees) ---------- */
        static float Env(float z) { if (z <= 0) return 0; if (z >= 80) return 1; float t = z / 80; return t * t * (3 - 2 * t); }
        float Wave(float[] p, float z) => twist * Env(z) * (p[0] * Mathf.Sin(z * p[1] + p[2]) + p[3] * Mathf.Sin(z * p[4] + p[5]));

        /// <summary>The trunk's centreline, across the tunnel.</summary>
        public float BaseX(float z) => Wave(lv.Px, z);
        public float Cy(float z) => Wave(lv.Py, z);

        /// <summary>How far a branch's centre sits from the trunk's line: one tunnel-width at the split, easing further apart, with a wander of its own.</summary>
        public float BranchDX(int br, float z)
        {
            float sgn = br == 1 ? 1 : -1, t = Mathf.Clamp01((z - fork.Z1) / G.ForkSpread), sm = t * t * (3 - 2 * t); var w = fork.Wig[br];
            return sgn * (G.HW + G.ForkSep * sm) + sm * w[0] * Mathf.Sin((z - fork.Z1) * w[1]);
        }

        /// <summary>
        /// Centre of the trunk (br = -1) or of a branch at depth z, across the tunnel. A branch's line runs straight back
        /// past the split, which is what a ship that has just chosen measures itself from.
        /// </summary>
        public float Cx(float z, int br) { float x = BaseX(z); if (fork != null && br >= 0) x += BranchDX(br, Mathf.Max(z, fork.Z1)); return x; }
        /// <summary>Centre of the tunnel the ship is in: the trunk until the fork, the chosen branch after.</summary>
        public float Cx(float z) => Cx(z, Chosen);

        /* ---------- world positions ---------- */
        /// <summary>World position of a point x across, y up, at depth z in the ship's tunnel (the trunk, or the chosen branch).</summary>
        public Vector3 World(float x, float y, float z) => World(x, y, z, Chosen);
        public Vector3 World(float x, float y, float z, int br)
        {
            Frame(z, out var p, out var t, out var u); var r = Vector3.Cross(u, t);
            return p + r * (Cx(z, br) + x) + u * (Cy(z) + y);
        }
        public Vector3 Centre(float z) => World(0, 0, z);

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
