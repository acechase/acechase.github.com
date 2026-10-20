using System;
using UnityEngine;

namespace DoomsdayTunnels.Core
{
    public enum SteerMode { Stick, Tilt }

    /// <summary>One slider on the settings panel: the web game's TUNE_DEFS, with the same ranges and defaults.</summary>
    public sealed class TuneDef
    {
        public string Key, Label, Hint; public float Min, Max, Step, Def;
        public Func<Tune, float> Get; public Action<Tune, float> Set;
        public string Format(float v) => v.ToString(Step >= 1 ? "0" : Step < 0.1f ? "0.00" : "0.0");
    }

    /// <summary>
    /// Everything the player can set, saved on the device: the feel sliders, how the ship is steered, sound, whether
    /// plays are counted, reduced motion, and the per-sector personal best times. The keys are listed in the README.
    /// The store is swappable so the tests run without PlayerPrefs.
    /// </summary>
    public sealed class Settings
    {
        public static readonly TuneDef[] Defs =
        {
            new TuneDef { Key = "steer", Label = "Steering speed", Hint = "How fast the ship slides sideways", Min = 1.4f, Max = 4.4f, Step = 0.1f, Def = 2.6f, Get = t => t.Steer, Set = (t, v) => t.Steer = v },
            new TuneDef { Key = "snap", Label = "Steering snap", Hint = "Lower is smoother, higher snappier", Min = 2, Max = 12, Step = 0.5f, Def = 4.5f, Get = t => t.Snap, Set = (t, v) => t.Snap = v },
            new TuneDef { Key = "speed", Label = "Flight speed", Hint = "Forward speed through the tunnel", Min = 0.6f, Max = 1.4f, Step = 0.05f, Def = 1f, Get = t => t.Speed, Set = (t, v) => t.Speed = v },
            new TuneDef { Key = "pull", Label = "Curve pull", Hint = "How hard bends push you outward", Min = 0, Max = 0.4f, Step = 0.01f, Def = 0.14f, Get = t => t.Pull, Set = (t, v) => t.Pull = v },
            new TuneDef { Key = "twist", Label = "Tunnel twist", Hint = "How sharply the tunnel bends", Min = 0, Max = 1.5f, Step = 0.05f, Def = 0.7f, Get = t => t.Twist, Set = (t, v) => t.Twist = v },
            new TuneDef { Key = "sway", Label = "Camera sway", Hint = "Banking and shake on impacts", Min = 0, Max = 1.5f, Step = 0.05f, Def = 0.4f, Get = t => t.Sway, Set = (t, v) => t.Sway = v },
            new TuneDef { Key = "tilt", Label = "Tilt range", Hint = "Degrees of tilt for full steering", Min = 8, Max = 35, Step = 1, Def = 18, Get = t => t.Tilt, Set = (t, v) => t.Tilt = v },
        };

        public interface IStore
        {
            float GetFloat(string key, float def); void SetFloat(string key, float v);
            int GetInt(string key, int def); void SetInt(string key, int v);
            string GetString(string key, string def); void SetString(string key, string v);
            bool Has(string key); void Save();
        }

        sealed class PrefsStore : IStore
        {
            public float GetFloat(string k, float d) => PlayerPrefs.GetFloat(k, d);
            public void SetFloat(string k, float v) => PlayerPrefs.SetFloat(k, v);
            public int GetInt(string k, int d) => PlayerPrefs.GetInt(k, d);
            public void SetInt(string k, int v) => PlayerPrefs.SetInt(k, v);
            public string GetString(string k, string d) => PlayerPrefs.GetString(k, d);
            public void SetString(string k, string v) => PlayerPrefs.SetString(k, v);
            public bool Has(string k) => PlayerPrefs.HasKey(k);
            public void Save() => PlayerPrefs.Save();
        }

        readonly IStore store;
        public readonly Tune Tune = new Tune();
        public SteerMode Steer = SteerMode.Stick;
        public bool InvertTilt, Sound = true, CountPlays = true, ReduceMotion, ShowGhost = true, Haptics = true;
        public int Best;
        public string Name = "", LastDay = "";    // the name last posted with; the last day a run was started, for the anonymous player count

        public Settings() : this(new PrefsStore()) { }
        public Settings(IStore store) { this.store = store; Load(); }

        public void Load()
        {
            foreach (var d in Defs) d.Set(Tune, Mathf.Clamp(store.GetFloat("tune." + d.Key, d.Def), d.Min, d.Max));
            Steer = store.GetInt("ctl.mode", 0) == 1 ? SteerMode.Tilt : SteerMode.Stick;
            InvertTilt = store.GetInt("ctl.invertY", 0) == 1;
            Sound = store.GetInt("sound", 1) == 1;
            CountPlays = store.GetInt("countPlays", 1) == 1;
            ReduceMotion = store.GetInt("reduceMotion", 0) == 1; ShowGhost = store.GetInt("ghost", 1) == 1; Haptics = store.GetInt("haptics", 1) == 1;
            Best = store.GetInt("best", 0);
            Name = store.GetString("name", ""); LastDay = store.GetString("day", "");
        }

        public void Save()
        {
            foreach (var d in Defs) store.SetFloat("tune." + d.Key, d.Get(Tune));
            store.SetInt("ctl.mode", Steer == SteerMode.Tilt ? 1 : 0);
            store.SetInt("ctl.invertY", InvertTilt ? 1 : 0);
            store.SetInt("sound", Sound ? 1 : 0);
            store.SetInt("countPlays", CountPlays ? 1 : 0);
            store.SetInt("reduceMotion", ReduceMotion ? 1 : 0); store.SetInt("ghost", ShowGhost ? 1 : 0); store.SetInt("haptics", Haptics ? 1 : 0);
            store.SetInt("best", Best);
            store.SetString("name", Name ?? ""); store.SetString("day", LastDay ?? "");
            store.Save();
        }

        public void ResetTune() { foreach (var d in Defs) d.Set(Tune, d.Def); }

        /// <summary>A slider's value snapped to its step and kept in range.</summary>
        public static float Snap(TuneDef d, float v) => Mathf.Clamp(Mathf.Round((v - d.Min) / d.Step) * d.Step + d.Min, d.Min, d.Max);

        /// <summary>The best time through a day's daily sector on this device, or 0. Only the latest day is kept.</summary>
        public float DailyBest(string day) => store.GetString("daily.day", "") == day ? store.GetFloat("daily.t", 0f) : 0f;
        public bool RecordDaily(string day, float t)
        {
            float was = DailyBest(day); if (was > 0 && t >= was) return false;
            store.SetString("daily.day", day); store.SetFloat("daily.t", t); store.Save(); return was > 0;
        }

        /// <summary>The recorded best run through a sector (an InputLog, serialised), or null.</summary>
        public string Ghost(int sector) { var s = store.GetString("ghost." + sector, ""); return s.Length > 0 ? s : null; }
        public void SaveGhost(int sector, string log) { store.SetString("ghost." + sector, log); store.Save(); }

        /// <summary>Forgets a sector's time (used by the screenshot runs to stage a personal best).</summary>
        public void ForgetTime(int sector) { store.SetFloat("pb." + sector, 0f); store.Save(); }

        /// <summary>The fastest fair time flown through a sector on this device, or 0.</summary>
        public float BestTime(int sector) => store.GetFloat("pb." + sector, 0f);
        /// <summary>Records a sector time; true when it is a new personal best. Only fair-speed runs count.</summary>
        public bool RecordTime(int sector, float t, bool fair)
        {
            if (!fair || t <= 0) return false;
            float was = BestTime(sector);
            if (was > 0 && t >= was) return false;
            store.SetFloat("pb." + sector, t); store.Save();
            return was > 0;                    // a first time through is not a "best" yet, as in the web game
        }
    }
}
