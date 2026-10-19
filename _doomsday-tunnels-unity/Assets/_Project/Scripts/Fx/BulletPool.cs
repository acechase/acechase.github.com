using System.Collections.Generic;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Tunnel;
using UnityEngine;

namespace DoomsdayTunnels.Fx
{
    /// <summary>Draws the sim's shots as short glowing rods: a white core inside a wider streak in the sector's line colour.</summary>
    public sealed class BulletPool : MonoBehaviour
    {
        readonly List<Transform> rods = new List<Transform>();
        Material coreMat, glowMat;

        public void Init(Material unlit, Material additive, Color lineColor)
        {
            coreMat = new Material(unlit); coreMat.SetColor("_Tint", Color.white); coreMat.SetFloat("_Intensity", 3f); coreMat.SetFloat("_FogAmount", 0f);
            glowMat = new Material(additive); glowMat.SetColor("_Tint", lineColor); glowMat.SetFloat("_Intensity", 0.6f);
        }
        public void SetLineColor(Color c) { if (glowMat) glowMat.SetColor("_Tint", c); }

        Transform Make()
        {
            var go = new GameObject("Shot"); go.transform.SetParent(transform, false);
            var core = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(core.GetComponent<Collider>());
            core.transform.SetParent(go.transform, false); core.transform.localScale = new Vector3(0.08f, 0.08f, 2.6f); core.transform.localPosition = new Vector3(0, 0, 1.3f);
            core.GetComponent<Renderer>().sharedMaterial = coreMat;
            var glow = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(glow.GetComponent<Collider>());
            glow.transform.SetParent(go.transform, false); glow.transform.localScale = new Vector3(0.3f, 0.3f, 2.8f); glow.transform.localPosition = new Vector3(0, 0, 1.3f);
            glow.GetComponent<Renderer>().sharedMaterial = glowMat;
            return go.transform;
        }

        public void Sync(FlightSim sim, TunnelPath path)
        {
            while (rods.Count < sim.Bullets.Count) rods.Add(Make());
            for (int i = 0; i < rods.Count; i++)
            {
                bool on = i < sim.Bullets.Count;
                if (rods[i].gameObject.activeSelf != on) rods[i].gameObject.SetActive(on);
                if (!on) continue;
                var b = sim.Bullets[i];
                var from = path.World(b.X, b.Y, b.Z);
                var to = path.World(b.X + b.Sx * 2.6f, b.Y + b.Sy * 2.6f, b.Z + 2.6f);
                rods[i].position = from; rods[i].rotation = Quaternion.LookRotation(to - from);
            }
        }
    }
}
