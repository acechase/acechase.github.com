using System.Collections.Generic;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Tunnel;
using UnityEngine;

namespace DoomsdayTunnels.Fx
{
    /// <summary>
    /// The player's ship, which the web game never showed: a dart with a dark hull and glowing edges, banking into its
    /// turns, with an engine flare that stretches under boost. It sits where the sim says the ship is; the camera rides
    /// just behind. Hull damage shows as the edges dimming from white to warning red as the shield drains, and sparks
    /// trail from the engine when it is nearly gone.
    /// </summary>
    public sealed class ShipView : MonoBehaviour
    {
        public bool Visible = true;
        Transform body; Renderer hull, edges, flare; Material hullMat, edgeMat, flareMat;
        float bank, pitch, yaw, sparkCd;
        static readonly int TintId = Shader.PropertyToID("_Tint");

        public void Init(Material unlit, Material additive)
        {
            body = new GameObject("Body").transform; body.SetParent(transform, false); body.localScale = Vector3.one * 0.8f;
            // The hull: a flat dart, nose forward. Vertices: nose, left tip, right tip, spine top, keel.
            var v = new List<Vector3> { new Vector3(0, 0, 0.42f), new Vector3(-0.34f, 0, -0.3f), new Vector3(0.34f, 0, -0.3f), new Vector3(0, 0.11f, -0.2f), new Vector3(0, -0.06f, -0.2f), new Vector3(0, 0.02f, -0.34f) };
            var tris = new[] { 0, 3, 1, 0, 2, 3, 0, 1, 4, 0, 4, 2, 1, 3, 5, 3, 2, 5, 1, 5, 4, 5, 2, 4 };
            var mesh = new Mesh { name = "Ship" }; mesh.SetVertices(v); mesh.SetIndices(tris, MeshTopology.Triangles, 0);
            var cols = new List<Color>(); for (int i = 0; i < v.Count; i++) cols.Add(Color.white); mesh.SetColors(cols);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            hull = Part(mesh, unlit, out hullMat); hullMat.SetColor(TintId, Color.Lerp(Palette.Ink, Palette.Void, 0.8f)); hullMat.SetFloat("_Intensity", 1f); hullMat.SetFloat("_Lit", 0.3f);
            // The edges: the same dart as lines, a hair outside the hull so they always show.
            var lines = new Mesh { name = "ShipEdges" }; var lv = new List<Vector3>(); foreach (var p in v) lv.Add(p * 1.02f); lines.SetVertices(lv); lines.SetColors(cols);
            lines.SetIndices(new[] { 0, 1, 0, 2, 1, 2, 0, 3, 3, 1, 3, 2, 0, 4, 4, 1, 4, 2, 3, 5, 1, 5, 2, 5 }, MeshTopology.Lines, 0);
            edges = Part(lines, unlit, out edgeMat); edgeMat.SetColor(TintId, Palette.Ink); edgeMat.SetFloat("_Intensity", 2.6f); edgeMat.SetFloat("_Lit", 0f);
            // The engine flare: an additive slab behind the tail that stretches with speed.
            var fl = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(fl.GetComponent<Collider>()); fl.transform.SetParent(body, false);
            fl.transform.localPosition = new Vector3(0, 0, -0.45f); fl.transform.localScale = new Vector3(0.14f, 0.07f, 0.3f);
            flare = fl.GetComponent<Renderer>(); flareMat = new Material(additive); flareMat.SetFloat("_Intensity", 1.4f); flare.sharedMaterial = flareMat; flare.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        Renderer Part(Mesh m, Material baseMat, out Material mat)
        {
            var go = new GameObject(m.name); go.transform.SetParent(body, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>(); mat = new Material(baseMat); r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return r;
        }

        public Vector3 EnginePos => body.TransformPoint(new Vector3(0, 0, -0.45f));

        public void Place(FlightSim sim, TunnelPath path, Color lineCol, Color haze, float dt, Sparks sparks)
        {
            bool show = Visible && (sim.Playing || sim.Shield > 0);
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
            if (!show) return;
            transform.position = path.World(sim.Rx, sim.Ry, sim.Pz);
            var fwd = path.Tangent(sim.Pz + 1.5f); var upv = path.Up(sim.Pz + 1.5f);
            float k = Mathf.Min(1, dt * 8);
            bank += (-sim.Vx * 0.22f - bank) * k; pitch += (sim.Vy * 0.09f - pitch) * k; yaw += (sim.Vx * 0.05f - yaw) * k;
            transform.rotation = Quaternion.LookRotation(fwd, upv) * Quaternion.Euler(-pitch * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, bank * Mathf.Rad2Deg);
            // the engine: longer and brighter under boost, flickering a little
            float len = 0.3f + 0.5f * sim.BoostEase + 0.05f * Mathf.Sin(sim.Now * 40);
            flare.transform.localScale = new Vector3(0.14f + 0.06f * sim.BoostEase, 0.07f + 0.03f * sim.BoostEase, len);
            flare.transform.localPosition = new Vector3(0, 0, -0.3f - len * 0.5f);
            flareMat.SetColor(TintId, Color.Lerp(lineCol, Color.white, 0.3f + 0.5f * sim.BoostEase));
            // damage: edges go from white toward warning red, sparks when nearly gone
            float dmg = 1 - Mathf.Clamp01(sim.Shield / 60f);
            edgeMat.SetColor(TintId, Color.Lerp(Palette.Ink, Palette.Warn, dmg * dmg));
            edgeMat.SetFloat("_Intensity", 2.6f + 1.2f * Mathf.Max(0, sim.Invuln) * (0.5f + 0.5f * Mathf.Sin(sim.Now * 20)));
            hullMat.SetColor("_Haze", haze); edgeMat.SetColor("_Haze", haze);
            sparkCd -= dt;
            if (sim.Playing && sim.Shield < 30 && sparkCd <= 0) { sparkCd = 0.12f + 0.3f * sim.Shield / 30f; sparks.Burst(EnginePos, Palette.Warn, 3, 1.5f); }
        }
    }
}
