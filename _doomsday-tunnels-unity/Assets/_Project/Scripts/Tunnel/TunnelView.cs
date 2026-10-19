using System.Collections.Generic;
using DoomsdayTunnels.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DoomsdayTunnels.Tunnel
{
    /// <summary>Owns the chunk meshes of the current sector: the trunk, then both branches of the fork, each in its own palette.</summary>
    public sealed class TunnelView : MonoBehaviour
    {
        readonly List<GameObject> chunks = new List<GameObject>();
        readonly List<Material> materials = new List<Material>();

        public void Build(Sector lv, TunnelPath path, Material unlitBase)
        {
            Clear();
            var f = lv.Fork;
            Sweep(path, lv.Pal, unlitBase, 0, f != null ? f.Z1 : lv.Length, -1);
            if (f != null) for (int b = 0; b < 2; b++) Sweep(path, f.Pal[b], unlitBase, f.Z1, lv.Length, b);
        }

        void Sweep(TunnelPath path, Palette P, Material unlitBase, float zFrom, float zTo, int br)
        {
            var surface = Mat(unlitBase, P, "Surface", 1f); surface.SetFloat("_Cull", (float)CullMode.Back);
            var lines = Mat(unlitBase, P, "Lines", P.Neon ? 3.2f : 2.4f);    // over the bloom threshold, so the lines glow
            int segs = Mathf.CeilToInt(zTo / G.Seg);
            for (int first = Mathf.FloorToInt(zFrom / G.Seg); first < segs; first += TunnelMeshBuilder.ChunkSegments)
            {
                var mesh = TunnelMeshBuilder.BuildChunk(path, P, first, Mathf.Min(segs, first + TunnelMeshBuilder.ChunkSegments), zTo, br);
                var go = new GameObject(mesh.name);
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[] { surface, lines };
                mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
                chunks.Add(go);
            }
        }

        Material Mat(Material unlitBase, Palette P, string what, float intensity)
        {
            var m = new Material(unlitBase) { name = "Tunnel " + what };
            m.SetColor("_Haze", P.Haze); m.SetFloat("_Intensity", intensity);
            materials.Add(m);
            return m;
        }

        public void Clear()
        {
            foreach (var c in chunks) { if (c) { Destroy(c.GetComponent<MeshFilter>().sharedMesh); Destroy(c); } }
            chunks.Clear();
            foreach (var m in materials) if (m) Destroy(m);
            materials.Clear();
        }
    }
}
