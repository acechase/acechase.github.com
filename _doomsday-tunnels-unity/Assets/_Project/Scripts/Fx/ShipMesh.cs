using System.Collections.Generic;
using UnityEngine;

namespace DoomsdayTunnels.Fx
{
    /// <summary>
    /// The player's ship, built from code: a lofted fuselage with a canopy, swept wings with tip fins, a tail fin, and
    /// twin engine nacelles ending in nozzle rings. Two meshes come out: the hull as triangles, and its edges as
    /// lines (the ship's neon outline, the only part of a ghost). Nose along +z, up along +y, about a metre long.
    /// </summary>
    public static class ShipMesh
    {
        sealed class B
        {
            public readonly List<Vector3> V = new List<Vector3>(); public readonly List<int> T = new List<int>();
            public readonly List<Vector3> LV = new List<Vector3>(); public readonly List<int> L = new List<int>();
            public void Tri(Vector3 a, Vector3 b, Vector3 c) { int i = V.Count; V.Add(a); V.Add(b); V.Add(c); T.Add(i); T.Add(i + 1); T.Add(i + 2); }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { Tri(a, b, c); Tri(a, c, d); }
            public void Line(Vector3 a, Vector3 b) { int i = LV.Count; LV.Add(a); LV.Add(b); L.Add(i); L.Add(i + 1); }
        }

        /// <summary>A tube through the given rings (each a closed loop of points), capped at both ends, with its seams and a few hoops as lines.</summary>
        static void Loft(B b, List<Vector3[]> rings, bool capFront, bool capBack, int seamEvery = 1, bool hoops = true)
        {
            for (int r = 0; r + 1 < rings.Count; r++)
            {
                var A = rings[r]; var C = rings[r + 1]; int n = A.Length;
                for (int i = 0; i < n; i++) { int j = (i + 1) % n; b.Quad(A[i], A[j], C[j], C[i]); if (i % seamEvery == 0) b.Line(A[i], C[i]); }
            }
            if (hoops) foreach (var R in new[] { rings[0], rings[rings.Count - 1] }) for (int i = 0; i < R.Length; i++) b.Line(R[i], R[(i + 1) % R.Length]);
            if (capFront) { var R = rings[0]; var c = Centre(R); for (int i = 0; i < R.Length; i++) b.Tri(c, R[(i + 1) % R.Length], R[i]); }
            if (capBack) { var R = rings[rings.Count - 1]; var c = Centre(R); for (int i = 0; i < R.Length; i++) b.Tri(c, R[i], R[(i + 1) % R.Length]); }
        }
        static Vector3 Centre(Vector3[] r) { var c = Vector3.zero; foreach (var p in r) c += p; return c / r.Length; }

        /// <summary>A ring of `n` points around (x, y, z), `w` wide and `h` tall, squashed toward a diamond by `k`.</summary>
        static Vector3[] Ring(float x, float y, float z, float w, float h, int n, float k = 0.25f)
        {
            var r = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n + Mathf.PI / n, c = Mathf.Cos(a), s = Mathf.Sin(a);
                float f = 1 - k * Mathf.Abs(Mathf.Sin(2 * a));                   // pinch the diagonals: a softened diamond
                r[i] = new Vector3(x + c * w * f, y + s * h * f, z);
            }
            return r;
        }

        public static void Build(out Mesh hull, out Mesh edges, out Vector3[] nozzles)
        {
            var b = new B();
            // fuselage: a long body, widest a third back, flattened, with a dropped keel
            var fus = new List<Vector3[]>();
            foreach (var (z, w, h, dy) in new[] { (0.62f, 0.012f, 0.012f, 0.02f), (0.5f, 0.05f, 0.04f, 0.01f), (0.3f, 0.1f, 0.075f, 0f), (0.05f, 0.125f, 0.085f, -0.005f), (-0.2f, 0.11f, 0.08f, 0f), (-0.4f, 0.075f, 0.06f, 0.01f), (-0.5f, 0.05f, 0.045f, 0.015f) })
                fus.Add(Ring(0, dy, z, w, h, 8, 0.3f));
            Loft(b, fus, true, true, 2);
            // canopy: a low bubble on top, from a fifth back to half way
            var can = new List<Vector3[]>();
            foreach (var (z, w, h) in new[] { (0.36f, 0.015f, 0.01f), (0.26f, 0.045f, 0.045f), (0.12f, 0.055f, 0.06f), (-0.02f, 0.05f, 0.05f), (-0.1f, 0.03f, 0.02f) })
                can.Add(Ring(0, 0.075f, z, w, h, 6, 0.1f));
            Loft(b, can, true, true, 1, false);
            // wings: swept back, thin, with a dihedral, as two faces each (top and bottom) and an outline
            foreach (int s in new[] { -1, 1 })
            {
                var root0 = new Vector3(s * 0.1f, -0.01f, 0.2f); var root1 = new Vector3(s * 0.1f, -0.01f, -0.32f);
                var tip0 = new Vector3(s * 0.46f, 0.05f, -0.2f); var tip1 = new Vector3(s * 0.46f, 0.05f, -0.4f);
                var up = new Vector3(0, 0.012f, 0);
                b.Quad(root0 + up, tip0 + up, tip1 + up, root1 + up); b.Quad(root0 - up, root1 - up, tip1 - up, tip0 - up);
                b.Quad(root0 + up, root0 - up, tip0 - up, tip0 + up); b.Quad(tip1 + up, tip1 - up, root1 - up, root1 + up);  // leading and trailing edges
                b.Quad(tip0 + up, tip0 - up, tip1 - up, tip1 + up);
                if (s < 0) { b.Line(root0, tip0); b.Line(tip0, tip1); b.Line(tip1, root1); } else { b.Line(root0, tip0); b.Line(tip0, tip1); b.Line(tip1, root1); }
                // a tip fin, standing up from the wing's end
                var finA = tip0 + new Vector3(0, 0, -0.02f); var finB = tip1; var finC = new Vector3(s * 0.46f, 0.17f, -0.36f);
                b.Tri(finA, finB, finC); b.Tri(finA, finC, finB); b.Line(finA, finC); b.Line(finC, finB);
                // an engine nacelle under the wing root: a short tube with a flared nozzle ring at the back
                var nac = new List<Vector3[]>();
                foreach (var (z, rr) in new[] { (0.05f, 0.03f), (0.0f, 0.052f), (-0.3f, 0.052f), (-0.42f, 0.046f), (-0.47f, 0.058f), (-0.5f, 0.062f) })
                    nac.Add(Ring(s * 0.17f, -0.045f, z, rr, rr, 8, 0f));
                Loft(b, nac, true, false, 2);
                var nz = nac[nac.Count - 1]; for (int i = 0; i < nz.Length; i++) b.Line(nz[i], nz[(i + 1) % nz.Length]);     // the bright nozzle lip
                var inner = Ring(s * 0.17f, -0.045f, -0.5f, 0.04f, 0.04f, 8, 0f); for (int i = 0; i < inner.Length; i++) { b.Line(inner[i], inner[(i + 1) % inner.Length]); b.Tri(nz[i], nz[(i + 1) % nz.Length], inner[i]); b.Tri(inner[i], nz[(i + 1) % nz.Length], inner[(i + 1) % inner.Length]); }
            }
            // tail fin
            { var a = new Vector3(0, 0.06f, -0.15f); var c = new Vector3(0, 0.07f, -0.5f); var t = new Vector3(0, 0.24f, -0.46f); var t2 = new Vector3(0, 0.22f, -0.36f);
              b.Quad(a, t2, t, c); b.Quad(a, c, t, t2); b.Line(a, t2); b.Line(t2, t); b.Line(t, c); }
            // spine line and keel line, for the neon read
            b.Line(new Vector3(0, 0.02f, 0.62f), new Vector3(0, 0.085f, 0.3f)); b.Line(new Vector3(0, -0.085f, 0.05f), new Vector3(0, -0.045f, -0.5f));

            hull = new Mesh { name = "ShipHull" }; hull.SetVertices(b.V); hull.SetIndices(b.T.ToArray(), MeshTopology.Triangles, 0);
            var cols = new List<Color>(); for (int i = 0; i < b.V.Count; i++) cols.Add(Color.white); hull.SetColors(cols); hull.RecalculateNormals(); hull.RecalculateBounds();
            edges = new Mesh { name = "ShipEdges" }; var lv = new List<Vector3>(); foreach (var p in b.LV) lv.Add(p * 1.01f); edges.SetVertices(lv); edges.SetIndices(b.L.ToArray(), MeshTopology.Lines, 0);
            var lc = new List<Color>(); for (int i = 0; i < lv.Count; i++) lc.Add(Color.white); edges.SetColors(lc); edges.RecalculateBounds();
            nozzles = new[] { new Vector3(-0.17f, -0.045f, -0.5f), new Vector3(0.17f, -0.045f, -0.5f) };
        }
    }
}
