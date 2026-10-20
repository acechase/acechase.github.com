using System.Collections.Generic;
using UnityEngine;

namespace DoomsdayTunnels.Fx
{
    /// <summary>
    /// The game's own point lights, fed to the Doomsday/Unlit shader as global arrays: a flash for every explosion and
    /// shot impact, a glow for each shot in flight, the ship's engine and the ship token. The shader takes at most eight,
    /// so the nearest ones to the camera win; the quality tier can lower that further.
    /// </summary>
    public sealed class DynamicLights
    {
        public const int Max = 8;
        public struct Light { public Vector3 Pos; public float Radius, Intensity, Life, Fade; public Color Col; }
        public readonly List<Light> Lights = new List<Light>();
        public int Budget = Max;

        static readonly int PosId = Shader.PropertyToID("_DLPos"), ColId = Shader.PropertyToID("_DLCol"), CountId = Shader.PropertyToID("_DLCount");
        readonly Vector4[] pos = new Vector4[Max], col = new Vector4[Max];
        readonly List<Light> persistent = new List<Light>();

        /// <summary>A flash that fades out over `life` seconds.</summary>
        public void Flash(Vector3 at, Color c, float radius, float intensity, float life)
        {
            Lights.Add(new Light { Pos = at, Col = c, Radius = radius, Intensity = intensity, Life = life, Fade = life });
        }

        /// <summary>Lights that are placed afresh every frame (shots, the engine): call before Apply.</summary>
        public void BeginFrame() => persistent.Clear();
        public void Hold(Vector3 at, Color c, float radius, float intensity) => persistent.Add(new Light { Pos = at, Col = c, Radius = radius, Intensity = intensity, Life = 1, Fade = 1 });

        public void Tick(float dt)
        {
            for (int i = Lights.Count - 1; i >= 0; i--) { var l = Lights[i]; l.Life -= dt; if (l.Life <= 0) Lights.RemoveAt(i); else Lights[i] = l; }
        }

        /// <summary>Picks the lights that matter most from the camera's point of view and hands them to the shaders.</summary>
        public void Apply(Vector3 camPos, Vector3 camForward)
        {
            int n = Select(camPos, camForward, pos, col);
            Shader.SetGlobalVectorArray(PosId, pos); Shader.SetGlobalVectorArray(ColId, col); Shader.SetGlobalInt(CountId, n);
        }

        /// <summary>Fills `p` and `c` with the chosen lights, nearest first; returns how many. Pure, so it can be tested.</summary>
        public int Select(Vector3 camPos, Vector3 camForward, Vector4[] p, Vector4[] c)
        {
            fwd = camForward;
            all.Clear(); all.AddRange(persistent); all.AddRange(Lights);
            int budget = Mathf.Min(Budget, Max);
            // weight: brightness over distance, so a big explosion far ahead still beats a dim spark by the lens
            all.Sort((a, b) => Score(b, camPos).CompareTo(Score(a, camPos)));
            int n = Mathf.Min(budget, all.Count);
            for (int i = 0; i < Max; i++)
            {
                if (i < n) { var l = all[i]; float k = l.Fade > 0 ? Mathf.Clamp01(l.Life / l.Fade) : 1; p[i] = new Vector4(l.Pos.x, l.Pos.y, l.Pos.z, Mathf.Max(0.01f, l.Radius)); c[i] = new Vector4(l.Col.r, l.Col.g, l.Col.b, l.Intensity * k); }
                else { p[i] = new Vector4(0, 0, 0, 0.01f); c[i] = Vector4.zero; }
            }
            return n;
        }
        readonly List<Light> all = new List<Light>(); Vector3 fwd = Vector3.forward;
        float Score(in Light l, Vector3 cam)
        {
            float k = l.Fade > 0 ? Mathf.Clamp01(l.Life / l.Fade) : 1;
            float dz = Vector3.Dot(l.Pos - cam, fwd); if (dz < -3) return -1;        // behind the camera: never worth a slot
            return l.Intensity * k * l.Radius / (1 + Mathf.Max(0, dz) * 0.08f);
        }
    }
}
