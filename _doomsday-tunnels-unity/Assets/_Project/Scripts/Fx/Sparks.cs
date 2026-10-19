using DoomsdayTunnels.Core;
using UnityEngine;

namespace DoomsdayTunnels.Fx
{
    /// <summary>One additive particle system for every burst of sparks, in the tunnel or on the screen plane.</summary>
    public sealed class Sparks : MonoBehaviour
    {
        ParticleSystem ps; Camera cam;

        public void Init(Material additive, Camera camera)
        {
            cam = camera;
            ps = gameObject.AddComponent<ParticleSystem>();
            var main = ps.main; main.playOnAwake = false; main.loop = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.6f; main.startSize = 0.06f; main.maxParticles = 600; main.startSpeed = 0;
            var em = ps.emission; em.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.5f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 3f; r.velocityScale = 0.03f;
            var m = new Material(additive); m.SetFloat("_Intensity", 1.6f); r.sharedMaterial = m;
            ps.Stop();
        }

        /// <summary>Sparks at a point in the world, flying every which way.</summary>
        public void Burst(Vector3 at, Color col, int n, float speed)
        {
            var p = new ParticleSystem.EmitParams { position = at, startColor = col };
            for (int i = 0; i < n; i++)
            {
                p.velocity = Random.insideUnitSphere.normalized * speed * (0.35f + Random.value);
                p.startLifetime = 0.45f + Random.value * 0.4f;
                ps.Emit(p, 1);
            }
        }

        /// <summary>Sparks on the screen plane, like the web game's impact and scrape sparks: sx, sy are -1..1 (0 = centre).</summary>
        public void ScreenBurst(float sx, float sy, Color col, int n)
        {
            float d = 1.2f;                                            // just in front of the lens
            var t = cam.transform;
            float h = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * d, w = h * cam.aspect;
            float x = sx != 0 ? sx * w * 0.84f : (Random.value - 0.5f) * w, y = sy != 0 ? sy * h * 0.84f : (Random.value - 0.5f) * h;
            var at = t.position + t.forward * d + t.right * x + t.up * y;
            var p = new ParticleSystem.EmitParams { position = at, startColor = col, startSize = 0.012f };
            for (int i = 0; i < n; i++)
            {
                var dir = (t.right * (Random.value - 0.5f) + t.up * (Random.value - 0.5f)).normalized;
                p.velocity = dir * 1.4f * (0.35f + Random.value) + t.forward * 0.2f;
                p.startLifetime = 0.35f + Random.value * 0.3f;
                ps.Emit(p, 1);
            }
        }
    }
}
