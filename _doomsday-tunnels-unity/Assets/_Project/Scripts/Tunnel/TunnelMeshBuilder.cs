using System.Collections.Generic;
using DoomsdayTunnels.Core;
using UnityEngine;

namespace DoomsdayTunnels.Tunnel
{
    /// <summary>
    /// Sweeps the rectangular cross-section along a tunnel line and returns one mesh per chunk: submesh 0 is the
    /// floor, ceiling and walls as banded flat colour (the web game's alternating shades and lit ribs every eighth
    /// segment), submesh 1 is the wireframe as line primitives: the rails along the tunnel and a rib every fourth
    /// segment. Colours are baked per vertex, so one material draws a whole chunk. Every face points inward, so the
    /// surface material can cull back faces: where the two branches of a fork share a dividing wall, each side then
    /// sees only its own.
    /// </summary>
    public static class TunnelMeshBuilder
    {
        public const int ChunkSegments = 50;    // 100 metres of tunnel per mesh

        /// <summary>Segments firstSeg..lastSeg of the trunk (br = -1, widening into the fork) or of a branch, cut off at zEnd.</summary>
        public static Mesh BuildChunk(TunnelPath path, Palette P, int firstSeg, int lastSeg, float zEnd, int br)
        {
            var verts = new List<Vector3>(); var cols = new List<Color32>(); var tris = new List<int>(); var lines = new List<int>();
            float HH = G.HH;
            for (int k = firstSeg; k < lastSeg; k++)
            {
                float zf = k * G.Seg, zn = Mathf.Min((k + 1) * G.Seg, zEnd);
                if (zf >= zEnd) break;
                float fw = br < 0 ? path.BuiltHalfWidth(zf) : G.HW, nw = br < 0 ? path.BuiltHalfWidth(zn) : G.HW;
                Vector3 F(float x, float y) => path.World(x, y, zf, br); Vector3 N(float x, float y) => path.World(x, y, zn, br);
                bool band = ((k >> 1) & 1) == 1, lit = k % 8 == 0;
                Color floor = lit ? P.Light : band ? P.FloorA : P.FloorB, ceil = lit ? P.Light : band ? P.CeilA : P.CeilB, wall = lit ? P.LightWall : band ? P.WallB : P.WallA;
                // Corners of the near ring (f) and the far ring (n): bottom-left, bottom-right, top-right, top-left.
                const float e = 0.012f;                                                        // lines sit a hair inside the surface they belong to
                var f0 = F(-fw, -HH); var f1 = F(fw, -HH); var f2 = F(fw, HH); var f3 = F(-fw, HH);
                var n0 = N(-nw, -HH); var n1 = N(nw, -HH); var n2 = N(nw, HH); var n3 = N(-nw, HH);
                var centre = path.World(0, 0, (zf + zn) * 0.5f, br);
                Quad(verts, cols, tris, f0, f1, n1, n0, floor, centre);
                Quad(verts, cols, tris, f3, f2, n2, n3, ceil, centre);
                Quad(verts, cols, tris, f0, f3, n3, n0, wall, centre);
                Quad(verts, cols, tris, f1, f2, n2, n1, wall, centre);
                // Rails: seven along the floor and the ceiling, three up each wall, from this ring to the next.
                const int n = 6;
                for (int i = 0; i <= n; i++)
                {
                    float t = (float)i / n;
                    Line(verts, cols, lines, F(Mathf.Lerp(-fw, fw, t), -HH + e), N(Mathf.Lerp(-nw, nw, t), -HH + e), P.Line);
                    Line(verts, cols, lines, F(Mathf.Lerp(-fw, fw, t), HH - e), N(Mathf.Lerp(-nw, nw, t), HH - e), P.Line);
                }
                for (int j = 1; j <= 3; j++)
                {
                    float y = -HH + j * HH / 2;
                    Line(verts, cols, lines, F(-fw + e, y), N(-nw + e, y), P.Line);
                    Line(verts, cols, lines, F(fw - e, y), N(nw - e, y), P.Line);
                }
                // Ribs: a bright rectangle every fourth segment, a plain one on the others.
                Color rib = k % 4 == 0 ? P.Bright : P.Line;
                var r0 = F(-fw + e, -HH + e); var r1 = F(fw - e, -HH + e); var r2 = F(fw - e, HH - e); var r3 = F(-fw + e, HH - e);
                Line(verts, cols, lines, r0, r1, rib); Line(verts, cols, lines, r1, r2, rib); Line(verts, cols, lines, r2, r3, rib); Line(verts, cols, lines, r3, r0, rib);
            }
            var mesh = new Mesh { name = (br < 0 ? "Trunk " : "Branch " + br + " ") + firstSeg, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetColors(cols);
            mesh.subMeshCount = 2;
            mesh.SetIndices(tris, MeshTopology.Triangles, 0);
            mesh.SetIndices(lines, MeshTopology.Lines, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        // A quad wound so that its front face looks at `centre`, the inside of the tunnel.
        static void Quad(List<Vector3> v, List<Color32> c, List<int> t, Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Color col, Vector3 centre)
        {
            int i = v.Count; v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
            Color32 k = col.linear; c.Add(k); c.Add(k); c.Add(k); c.Add(k);
            bool inward = Vector3.Dot(Vector3.Cross(b - a, cc - a), centre - a) > 0;      // Unity's front face is the one its cross product points at
            if (inward) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
            else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
        }
        static void Line(List<Vector3> v, List<Color32> c, List<int> l, Vector3 a, Vector3 b, Color col)
        {
            int i = v.Count; v.Add(a); v.Add(b); Color32 k = col.linear; c.Add(k); c.Add(k); l.Add(i); l.Add(i + 1);
        }
    }
}
