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
        public bool Ghost;                       // the replay of a best run: edges only, dim, no engine
        Transform body; Renderer hull, edges; readonly Renderer[] flares = new Renderer[2], cores = new Renderer[2]; Material hullMat, edgeMat, flareMat, coreMat; Vector3[] nozzles;
        float bank, pitch, yaw, sparkCd; Vector3 jolt, joltVel; float tumble, tumbleVel;
        static readonly int TintId = Shader.PropertyToID("_Tint");

        public void Init(Material unlit, Material additive)
        {
            body = new GameObject("Body").transform; body.SetParent(transform, false); body.localScale = Vector3.one * 0.55f;      // small enough that its wings stay inside the collision margins
            ShipMesh.Build(out var mesh, out var lines, out nozzles);
            hull = Part(mesh, unlit, out hullMat); hullMat.SetColor(TintId, Color.Lerp(Palette.Ink, Palette.Void, 0.78f)); hullMat.SetFloat("_Intensity", 1f); hullMat.SetFloat("_Lit", 0.5f);
            edges = Part(lines, unlit, out edgeMat); edgeMat.SetColor(TintId, Palette.Ink); edgeMat.SetFloat("_Intensity", 2.6f); edgeMat.SetFloat("_Lit", 0f);
            if (Ghost) { hull.enabled = false; edgeMat.SetColor(TintId, Palette.Dim); edgeMat.SetFloat("_Intensity", 1.3f); }
            // The engine flares: an additive cone behind each nozzle that stretches with speed, and a hot core inside it.
            flareMat = new Material(additive); flareMat.SetFloat("_Intensity", 1.4f);
            coreMat = new Material(additive); coreMat.SetFloat("_Intensity", 2.2f); coreMat.SetColor(TintId, Color.white);
            for (int i = 0; i < 2; i++)
            {
                var fl = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(fl.GetComponent<Collider>()); fl.transform.SetParent(body, false); if (Ghost) fl.SetActive(false);
                fl.transform.localPosition = nozzles[i] + new Vector3(0, 0, -0.15f); fl.transform.localScale = new Vector3(0.09f, 0.09f, 0.3f);
                flares[i] = fl.GetComponent<Renderer>(); flares[i].sharedMaterial = flareMat; flares[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var co = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(co.GetComponent<Collider>()); co.transform.SetParent(body, false); if (Ghost) co.SetActive(false);
                co.transform.localPosition = nozzles[i] + new Vector3(0, 0, -0.06f); co.transform.localScale = new Vector3(0.045f, 0.045f, 0.12f);
                cores[i] = co.GetComponent<Renderer>(); cores[i].sharedMaterial = coreMat; cores[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        Renderer Part(Mesh m, Material baseMat, out Material mat)
        {
            var go = new GameObject(m.name); go.transform.SetParent(body, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>(); mat = new Material(baseMat); r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return r;
        }

        public Vector3 EnginePos => body.TransformPoint(new Vector3(0, -0.045f, -0.55f));
        /// <summary>A knock: the hull is shoved back along the tunnel and tumbles, then settles on a spring.</summary>
        public void Jolt(float back, float spin) { joltVel += new Vector3(0, 0, -back); tumbleVel += spin; }

        public void Place(FlightSim sim, TunnelPath path, Color lineCol, Color haze, float dt, Sparks sparks)
        {
            bool show = Visible && (sim.Playing || sim.Shield > 0);
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
            if (!show) return;
            joltVel += (-jolt * 120f - joltVel * 10f) * dt; jolt += joltVel * dt; tumbleVel += (-tumble * 60f - tumbleVel * 6f) * dt; tumble += tumbleVel * dt; tumble = Mathf.Clamp(tumble, -0.5f, 0.5f);   // a nod, never a somersault
            transform.position = path.World(sim.Rx, sim.Ry, sim.Pz, sim.Path.Chosen);
            body.localPosition = jolt; body.localRotation = Quaternion.Euler(tumble * Mathf.Rad2Deg, 0, 0);
            var fwd = path.Tangent(sim.Pz + 1.5f); var upv = path.Up(sim.Pz + 1.5f);
            float k = Mathf.Min(1, dt * 8);
            bank += (-sim.Vx * 0.22f - bank) * k; pitch += (sim.Vy * 0.09f - pitch) * k; yaw += (sim.Vx * 0.05f - yaw) * k;
            transform.rotation = Quaternion.LookRotation(fwd, upv) * Quaternion.Euler(-pitch * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, bank * Mathf.Rad2Deg);
            if (Ghost) { edgeMat.SetColor("_Haze", haze); return; }                 // a ghost is just its outline: no engine, no damage
            // the engine: longer and brighter under boost, flickering a little
            float len = 0.28f + 0.55f * sim.BoostEase + 0.04f * Mathf.Sin(sim.Now * 40);
            for (int i = 0; i < 2; i++)
            {
                float fl = len * (1 + 0.06f * Mathf.Sin(sim.Now * 37 + i * 2));
                flares[i].transform.localScale = new Vector3(0.09f + 0.05f * sim.BoostEase, 0.09f + 0.05f * sim.BoostEase, fl);
                flares[i].transform.localPosition = nozzles[i] + new Vector3(0, 0, -fl * 0.5f);
                cores[i].transform.localScale = new Vector3(0.045f, 0.045f, 0.1f + 0.25f * sim.BoostEase);
                cores[i].transform.localPosition = nozzles[i] + new Vector3(0, 0, -(0.1f + 0.25f * sim.BoostEase) * 0.5f);
            }
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
