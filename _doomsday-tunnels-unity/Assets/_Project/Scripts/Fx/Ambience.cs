using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Tunnel;
using UnityEngine;

namespace DoomsdayTunnels.Fx
{
    /// <summary>
    /// The air in the tunnel: the glow of the exit, visible from far down the tunnel and growing as it nears; dust
    /// motes drifting in the beam of the ship's lights, which give the speed something to be measured against; and
    /// streaks that rush past the lens under boost.
    /// </summary>
    public sealed class Ambience : MonoBehaviour
    {
        Transform glow; Material glowMat, unlitBase; ParticleSystem motes, streaks; Camera cam;
        readonly System.Collections.Generic.List<GameObject> gate = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<(Renderer r, MaterialPropertyBlock b, int i)> strips = new System.Collections.Generic.List<(Renderer, MaterialPropertyBlock, int)>();
        Material gateBright, gateHue, portalMat; readonly System.Collections.Generic.List<Transform> portals = new System.Collections.Generic.List<Transform>();
        static readonly int IntensityId = Shader.PropertyToID("_Intensity"), OpenId = Shader.PropertyToID("_Open"), SpinId = Shader.PropertyToID("_Spin"), ColorId = Shader.PropertyToID("_Color"), BrightId = Shader.PropertyToID("_Bright");
        public float Open { get; private set; }     // how far the exit's iris has opened, 0..1
        public const float OpenFrom = 70f, OpenAt = 18f;   // metres from the exit where it starts to open, and is fully open
        const float GateRun = 44f;                     // the lit strips lead in over the last stretch
        int moteBudget = 200; bool streaksOn = true; float moteCd;
        Color line; float length;
        static readonly int TintId = Shader.PropertyToID("_Tint");

        public void Init(Material additive, Material unlit, Material portal, Camera camera, Quality q)
        {
            cam = camera; moteBudget = q.Motes; streaksOn = q.Streaks; unlitBase = unlit; if (portal) portalMat = new Material(portal);
            // exit glow: a soft disc
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) { float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32)) / 32f; float a = Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f); tex.SetPixel(x, y, new Color(1, 1, 1, a)); }
            tex.Apply();
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(g.GetComponent<Collider>()); g.name = "ExitGlow"; g.transform.SetParent(transform, false);
            glowMat = new Material(additive) { mainTexture = tex }; glowMat.SetFloat("_Intensity", 1.6f);
            var gr = g.GetComponent<Renderer>(); gr.sharedMaterial = glowMat; gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glow = g.transform;
            // motes
            motes = new GameObject("Motes").AddComponent<ParticleSystem>(); motes.transform.SetParent(transform, false);
            var mm = motes.main; mm.playOnAwake = false; mm.loop = false; mm.simulationSpace = ParticleSystemSimulationSpace.World; mm.maxParticles = Mathf.Max(1, moteBudget); mm.startSpeed = 0; mm.startSize = 0.02f; mm.startLifetime = 6f;
            var me = motes.emission; me.enabled = false;
            var mc = motes.colorOverLifetime; mc.enabled = true; var mg = new Gradient();
            mg.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.7f, 0.2f), new GradientAlphaKey(0.7f, 0.8f), new GradientAlphaKey(0, 1) });
            mc.color = mg;
            var mr = motes.GetComponent<ParticleSystemRenderer>(); mr.renderMode = ParticleSystemRenderMode.Billboard; var mmat = new Material(additive) { mainTexture = tex }; mmat.SetFloat("_Intensity", 0.5f); mr.sharedMaterial = mmat;
            // boost streaks
            streaks = new GameObject("Streaks").AddComponent<ParticleSystem>(); streaks.transform.SetParent(transform, false);
            var sm = streaks.main; sm.playOnAwake = false; sm.loop = false; sm.simulationSpace = ParticleSystemSimulationSpace.World; sm.maxParticles = 300; sm.startSpeed = 0; sm.startSize = 0.01f; sm.startLifetime = 0.35f;
            var se = streaks.emission; se.enabled = false;
            var sc = streaks.colorOverLifetime; sc.enabled = true; var sg = new Gradient();
            sg.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.5f, 0.3f), new GradientAlphaKey(0, 1) });
            sc.color = sg;
            var sr = streaks.GetComponent<ParticleSystemRenderer>(); sr.renderMode = ParticleSystemRenderMode.Stretch; sr.lengthScale = 0; sr.velocityScale = 0.08f; var smat = new Material(additive); smat.SetFloat("_Intensity", 1.2f); sr.sharedMaterial = smat;
        }

        public void SetSector(Palette P, float len) { line = P.Line; length = len; glowMat.SetColor(TintId, P.Glow); motes.Clear(); }

        /// <summary>
        /// The exit: a bright gate across the end of each branch, a frame in white inside a frame in the sector's colour,
        /// with lit strips on the floor and ceiling chasing toward it over the last stretch. Something to fly through.
        /// </summary>
        public void BuildGate(Palette P, float len, TunnelPath path, bool forked)
        {
            foreach (var g in gate) if (g) Destroy(g); gate.Clear(); strips.Clear(); portals.Clear();
            if (portalMat) { portalMat.SetColor(ColorId, P.Line); portalMat.SetColor(BrightId, Palette.Ink); portalMat.SetFloat(OpenId, 0); }
            Open = 0;
            if (gateBright) Destroy(gateBright); if (gateHue) Destroy(gateHue);
            gateBright = new Material(unlitBase); gateBright.SetColor(TintId, Palette.Ink); gateBright.SetFloat(IntensityId, 2.2f); gateBright.SetFloat("_FogAmount", 0f); gateBright.SetFloat("_Lit", 0f);
            gateHue = new Material(unlitBase); gateHue.SetColor(TintId, P.Line); gateHue.SetFloat(IntensityId, 2.6f); gateHue.SetFloat("_FogAmount", 0f); gateHue.SetFloat("_Lit", 0f);
            int branches = forked ? 2 : 1;
            for (int b = 0; b < branches; b++)
            {
                int br = forked ? b : -1;
                var root = new GameObject("ExitGate" + b).transform; root.SetParent(transform, false);
                root.position = path.World(0, 0, len, br); root.rotation = path.Rotation(len);
                if (portalMat)
                {   // the iris itself, a quad filling the frame, drawn just inside the exit
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.transform.SetParent(root, false);
                    q.transform.localPosition = new Vector3(0, 0, -0.05f); q.transform.localScale = new Vector3((G.HW + 0.12f) * 2f, (G.HH + 0.12f) * 2f, 1);      // the quad is the opening: r = 1 at its edges
                    var qr = q.GetComponent<Renderer>(); qr.sharedMaterial = portalMat; qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    portals.Add(q.transform);
                }
                Frame(root, G.HW + 0.1f, G.HH + 0.1f, 0.14f, gateBright);
                Frame(root, G.HW + 0.42f, G.HH + 0.42f, 0.1f, gateHue);
                Frame(root, G.HW + 0.75f, G.HH + 0.75f, 0.07f, gateHue);
                gate.Add(root.gameObject);
                for (int i = 0; i < 9; i++)
                {   // strips on the floor and ceiling, every few metres, each lit in turn
                    float z = len - GateRun + i * (GateRun / 9);
                    foreach (float y in new[] { -G.HH + 0.03f, G.HH - 0.03f })
                    {
                        var s = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(s.GetComponent<Collider>()); s.transform.SetParent(transform, false);
                        s.transform.position = path.World(0, y, z, br); s.transform.rotation = path.Rotation(z); s.transform.localScale = new Vector3(0.7f, 0.04f, 0.22f);
                        var r = s.GetComponent<Renderer>(); r.sharedMaterial = gateHue; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        var pb = new MaterialPropertyBlock(); strips.Add((r, pb, i)); gate.Add(s);
                    }
                }
            }
        }
        void Frame(Transform root, float hw, float hh, float t, Material m)
        {
            foreach (var (pos, scale) in new[] { (new Vector3(0, -hh, 0), new Vector3(2 * hw + t, t, t)), (new Vector3(0, hh, 0), new Vector3(2 * hw + t, t, t)), (new Vector3(-hw, 0, 0), new Vector3(t, 2 * hh + t, t)), (new Vector3(hw, 0, 0), new Vector3(t, 2 * hh + t, t)) })
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(bar.GetComponent<Collider>()); bar.transform.SetParent(root, false);
                bar.transform.localPosition = pos; bar.transform.localScale = scale;
                var r = bar.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        public void SetPalette(Palette P) { line = P.Line; glowMat.SetColor(TintId, P.Glow); }

        public void Tick(FlightSim sim, TunnelPath path, float dt, bool reduceMotion)
        {
            // the exit: a disc at the far end that pulses slowly and swells as the ship closes in
            float left = length - sim.Pz;
            var at = path.World(0, 0, length);
            glow.position = at; glow.rotation = path.Rotation(length);
            float pulse = reduceMotion ? 1 : 0.9f + 0.1f * Mathf.Sin(sim.Now * 2.2f);
            glow.localScale = Vector3.one * Mathf.Lerp(3.4f, 6f, Mathf.Clamp01(1 - left / 120f)) * pulse;
            // the iris opens as the ship closes in, and spins faster as it does
            Open = Mathf.Clamp01((OpenFrom - left) / (OpenFrom - OpenAt));
            if (portalMat) { portalMat.SetFloat(OpenId, Open); portalMat.SetFloat(SpinId, sim.Now * (0.4f + 1.2f * Open)); }
            glowMat.SetFloat(IntensityId, 1.6f * (1 - 0.85f * Open));          // the far glow gives way to the iris as it opens
            // the gate's strips chase toward the exit; the gate itself breathes
            foreach (var (r, b, i) in strips) { float k = reduceMotion ? 0.5f : Mathf.Pow(0.5f + 0.5f * Mathf.Sin(sim.Now * 6f - i * 0.7f), 3); b.SetFloat(IntensityId, 0.8f + 2.2f * k); r.SetPropertyBlock(b); }
            if (gateHue) gateHue.SetFloat(IntensityId, reduceMotion ? 2.6f : 2.2f + 1.0f * (0.5f + 0.5f * Mathf.Sin(sim.Now * 3f)));
            // motes ahead of the ship, in the tunnel it is in, drifting down a little
            if (moteBudget > 0)
            {
                moteCd -= dt;
                if (moteCd <= 0)
                {
                    moteCd = 0.05f;
                    int n = Mathf.CeilToInt(moteBudget / 120f);
                    var p = new ParticleSystem.EmitParams { startColor = line };
                    for (int i = 0; i < n; i++)
                    {
                        float z = sim.Pz + 4 + Random.value * 55, hw = path.HalfWidth(z) - 0.1f;
                        p.position = path.World((Random.value - 0.5f) * 2 * hw, (Random.value - 0.5f) * 2 * (G.HH - 0.1f), z);
                        p.velocity = new Vector3((Random.value - 0.5f) * 0.1f, -0.03f - Random.value * 0.05f, 0);
                        motes.Emit(p, 1);
                    }
                }
            }
            // streaks under boost: lines rushing past the lens from the edges of the view
            if (streaksOn && sim.BoostEase > 0.15f && !reduceMotion)
            {
                var t = cam.transform; float d = 1.6f, h = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * d, w = h * cam.aspect;
                int n = Mathf.RoundToInt(sim.BoostEase * 3);
                var p = new ParticleSystem.EmitParams { startColor = new Color(line.r, line.g, line.b, 0.6f) };
                for (int i = 0; i < n; i++)
                {
                    float a = Random.value * Mathf.PI * 2, r = 0.55f + Random.value * 0.5f;
                    var off = t.right * Mathf.Cos(a) * w * r + t.up * Mathf.Sin(a) * h * r;
                    p.position = t.position + t.forward * (d + 6f) + off;
                    p.velocity = -t.forward * (26f + 20f * Random.value) + off.normalized * 0.4f;
                    streaks.Emit(p, 1);
                }
            }
        }
    }
}
