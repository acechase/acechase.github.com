using DoomsdayTunnels.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DoomsdayTunnels.Fx
{
    /// <summary>
    /// The post-processing the game drives at runtime: bloom sized to the quality tier, chromatic aberration that
    /// creeps in under boost and spikes on a hit, and a vignette that closes in when the shield is low.
    /// </summary>
    public sealed class PostFx
    {
        Bloom bloom; ChromaticAberration ca; Vignette vig; float hit;
        public bool ReduceMotion;

        public PostFx(Quality q)
        {
            var vol = Object.FindFirstObjectByType<Volume>();
            if (vol == null) return;
            var p = vol.profile;                                        // an instance: the asset on disk is untouched
            if (!p.TryGet(out bloom)) bloom = p.Add<Bloom>(true);
            bloom.maxIterations.Override(q.BloomIterations);
            if (!p.TryGet(out ca)) ca = p.Add<ChromaticAberration>(true);
            ca.intensity.Override(0f);
            if (!p.TryGet(out vig)) vig = p.Add<Vignette>(true);
            vig.intensity.Override(0f); vig.smoothness.Override(0.5f); vig.color.Override(Palette.Warn);
            var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (rp != null) rp.renderScale = q.RenderScale;
        }

        public void Hit(float amount = 1f) => hit = Mathf.Max(hit, amount);

        public void Tick(float boostEase, float shield, float dt)
        {
            hit = Mathf.Max(0, hit - dt * 3f);
            if (ca != null) ca.intensity.Override(ReduceMotion ? 0 : Mathf.Clamp01(0.35f * boostEase + 0.6f * hit));
            if (vig != null)
            {
                float low = shield > 0 && shield <= 30 ? (1 - shield / 30f) : 0;
                vig.intensity.Override(Mathf.Clamp01(0.32f * low + 0.25f * hit));
            }
        }
    }
}
