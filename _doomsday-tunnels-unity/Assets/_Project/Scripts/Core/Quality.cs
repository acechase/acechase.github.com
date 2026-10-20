using UnityEngine;

namespace DoomsdayTunnels.Core
{
    public enum QualityTier { Low, Mid, High }

    /// <summary>
    /// The effects budget, chosen once from the device: bloom iterations, how many of the game's lights the shader takes,
    /// how much debris flies and how many motes drift. Every effect has an off switch here so an old phone keeps 60 fps.
    /// </summary>
    public sealed class Quality
    {
        public QualityTier Tier;
        public int BloomIterations, Lights, DebrisCap, Motes;
        public float RenderScale;
        public bool Streaks, Shockwaves;

        public static Quality For(QualityTier t)
        {
            switch (t)
            {
                case QualityTier.Low: return new Quality { Tier = t, BloomIterations = 2, Lights = 3, DebrisCap = 90, Motes = 60, RenderScale = 0.8f, Streaks = false, Shockwaves = true };
                case QualityTier.Mid: return new Quality { Tier = t, BloomIterations = 3, Lights = 5, DebrisCap = 180, Motes = 180, RenderScale = 0.9f, Streaks = true, Shockwaves = true };
                default: return new Quality { Tier = t, BloomIterations = 4, Lights = 8, DebrisCap = 320, Motes = 360, RenderScale = 1f, Streaks = true, Shockwaves = true };
            }
        }

        public static Quality Detect() => For(TierFor(SystemInfo.deviceModel, SystemInfo.systemMemorySize, Application.isMobilePlatform));

        /// <summary>
        /// iPhones are read from the model string: "iPhone12,1" and later (the 11s onward) are High, iPhone10 and 11
        /// (the 8, X, XS, XR) Mid, older Low; iPads likewise from generation 13. Other phones go by memory. Desktops
        /// are High. Pure, so it can be tested.
        /// </summary>
        public static QualityTier TierFor(string model, int systemMB, bool mobile)
        {
            if (!mobile) return QualityTier.High;
            if (model != null && (model.StartsWith("iPhone") || model.StartsWith("iPad")))
            {
                bool pad = model.StartsWith("iPad");
                string rest = model.Substring(pad ? 4 : 6); int comma = rest.IndexOf(',');
                if (comma > 0 && int.TryParse(rest.Substring(0, comma), out int gen))
                {
                    if (pad) return gen >= 13 ? QualityTier.High : gen >= 8 ? QualityTier.Mid : QualityTier.Low;
                    return gen >= 12 ? QualityTier.High : gen >= 10 ? QualityTier.Mid : QualityTier.Low;
                }
                return QualityTier.Mid;
            }
            if (systemMB >= 6000) return QualityTier.High;
            if (systemMB >= 3000) return QualityTier.Mid;
            return QualityTier.Low;
        }
    }
}
