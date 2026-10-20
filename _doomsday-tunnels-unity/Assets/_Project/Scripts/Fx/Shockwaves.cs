using System.Collections.Generic;
using UnityEngine;

namespace DoomsdayTunnels.Fx
{
    /// <summary>Expanding rings in the tunnel's cross-section plane where something blew up or the ship hit something.</summary>
    public sealed class Shockwaves : MonoBehaviour
    {
        struct Wave { public Vector3 At; public Quaternion Rot; public Color Col; public float T, Dur, Size; }
        readonly List<Wave> waves = new List<Wave>();
        readonly List<Transform> pool = new List<Transform>();
        readonly List<Renderer> rends = new List<Renderer>();
        readonly List<MaterialPropertyBlock> blocks = new List<MaterialPropertyBlock>();
        Material mat; Mesh quad; public bool Enabled = true;
        static readonly int TintId = Shader.PropertyToID("_Tint");

        public void Init(Material additive)
        {
            var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64, 64)) / 64f;      // a soft ring at 0.8 of the radius
                float a = Mathf.Clamp01(1 - Mathf.Abs(d - 0.8f) / 0.12f); a *= a;
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            mat = new Material(additive) { mainTexture = tex }; mat.SetFloat("_Intensity", 1.2f);
            quad = new Mesh { name = "Ring" };
            quad.SetVertices(new List<Vector3> { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) });
            quad.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            quad.SetColors(new List<Color> { Color.white, Color.white, Color.white, Color.white });
            quad.SetIndices(new[] { 0, 1, 2, 0, 2, 3 }, MeshTopology.Triangles, 0);
        }

        public void Add(Vector3 at, Quaternion rot, Color col, float size = 2.6f, float dur = 0.45f) { if (Enabled) waves.Add(new Wave { At = at, Rot = rot, Col = col, Size = size, Dur = dur }); }

        public void Tick(float dt)
        {
            for (int i = waves.Count - 1; i >= 0; i--) { var w = waves[i]; w.T += dt; if (w.T >= w.Dur) waves.RemoveAt(i); else waves[i] = w; }
            while (pool.Count < waves.Count)
            {
                var go = new GameObject("Shock"); go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = quad;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pool.Add(go.transform); rends.Add(r); blocks.Add(new MaterialPropertyBlock());
            }
            for (int i = 0; i < pool.Count; i++)
            {
                bool on = i < waves.Count;
                if (pool[i].gameObject.activeSelf != on) pool[i].gameObject.SetActive(on);
                if (!on) continue;
                var w = waves[i]; float t = w.T / w.Dur, ease = 1 - (1 - t) * (1 - t);
                pool[i].position = w.At; pool[i].rotation = w.Rot; pool[i].localScale = Vector3.one * Mathf.Lerp(0.3f, w.Size, ease);
                var c = w.Col; c.a = (1 - t) * 0.9f;
                blocks[i].SetColor(TintId, c); rends[i].SetPropertyBlock(blocks[i]);
            }
        }
    }
}
