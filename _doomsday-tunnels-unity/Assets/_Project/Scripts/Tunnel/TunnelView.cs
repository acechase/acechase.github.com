using System.Collections.Generic;
using DoomsdayTunnels.Core;
using UnityEngine;

namespace DoomsdayTunnels.Tunnel
{
    /// <summary>Owns the chunk meshes of the current sector and the two materials that draw them.</summary>
    public sealed class TunnelView : MonoBehaviour
    {
        readonly List<GameObject> chunks = new List<GameObject>();
        public Material Surface, Lines;

        public void Build(Sector lv, TunnelPath path, Material unlitBase)
        {
            Clear();
            Surface = new Material(unlitBase) { name = "Tunnel Surface" };
            Lines = new Material(unlitBase) { name = "Tunnel Lines" };
            SetPalette(lv.Pal);
            int segs = Mathf.CeilToInt(lv.Length / G.Seg);
            for (int first = 0; first < segs; first += TunnelMeshBuilder.ChunkSegments)
            {
                var mesh = TunnelMeshBuilder.BuildChunk(path, lv.Pal, first, Mathf.Min(segs, first + TunnelMeshBuilder.ChunkSegments), lv.Length);
                var go = new GameObject("Chunk " + first);
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[] { Surface, Lines };
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                chunks.Add(go);
            }
        }

        public void SetPalette(Palette P)
        {
            Surface.SetColor("_Haze", P.Haze); Surface.SetFloat("_Intensity", 1f);
            Lines.SetColor("_Haze", P.Haze); Lines.SetFloat("_Intensity", P.Neon ? 3.2f : 2.4f);   // over the bloom threshold, so the lines glow
        }

        public void Clear()
        {
            foreach (var c in chunks) { if (c) { Destroy(c.GetComponent<MeshFilter>().sharedMesh); Destroy(c); } }
            chunks.Clear();
        }
    }
}
