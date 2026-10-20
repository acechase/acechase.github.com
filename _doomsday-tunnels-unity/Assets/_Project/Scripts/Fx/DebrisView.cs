using System.Collections.Generic;
using DoomsdayTunnels.Tunnel;
using UnityEngine;

namespace DoomsdayTunnels.Fx
{
    /// <summary>Draws the DebrisSim's chunks as pooled slabs: a dark fill in the thing's colour, one in four glowing.</summary>
    public sealed class DebrisView : MonoBehaviour
    {
        public readonly DebrisSim Sim = new DebrisSim();
        readonly List<Transform> pool = new List<Transform>();
        readonly List<Renderer> rends = new List<Renderer>();
        readonly List<MaterialPropertyBlock> blocks = new List<MaterialPropertyBlock>();
        Material fill, bright;
        static readonly int TintId = Shader.PropertyToID("_Tint"), IntensityId = Shader.PropertyToID("_Intensity"), HazeId = Shader.PropertyToID("_Haze");
        Color haze;

        public void Init(Material unlit)
        {
            fill = new Material(unlit); fill.SetFloat(IntensityId, 1f);
            bright = new Material(unlit); bright.SetFloat(IntensityId, 2.2f);
        }
        public void SetHaze(Color h) { haze = h; fill.SetColor(HazeId, h); bright.SetColor(HazeId, h); }

        Transform Make()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            rends.Add(r); blocks.Add(new MaterialPropertyBlock());
            return go.transform;
        }

        public void Sync(TunnelPath path)
        {
            var chunks = Sim.Chunks;
            while (pool.Count < chunks.Count) pool.Add(Make());
            for (int i = 0; i < pool.Count; i++)
            {
                bool on = i < chunks.Count;
                if (pool[i].gameObject.activeSelf != on) pool[i].gameObject.SetActive(on);
                if (!on) continue;
                var c = chunks[i];
                float fade = Mathf.Clamp01(c.Life / 0.3f);                                   // shrinks away at the end
                pool[i].position = path.World(c.X, c.Y, c.Z, c.Branch);
                pool[i].rotation = Quaternion.AngleAxis(c.Angle, c.Axis);
                pool[i].localScale = new Vector3(c.Size, c.Size * 0.6f, c.Size * 0.25f) * fade;
                rends[i].sharedMaterial = c.Bright ? bright : fill;
                var b = blocks[i]; b.SetColor(TintId, c.Bright ? c.Col : Color.Lerp(c.Col, Core.Palette.Void, 0.55f)); rends[i].SetPropertyBlock(b);
            }
        }
    }
}
