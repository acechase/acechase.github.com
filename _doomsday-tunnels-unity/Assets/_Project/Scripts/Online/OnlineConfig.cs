using UnityEngine;

namespace DoomsdayTunnels.Online
{
    /// <summary>
    /// Where the scoreboard server is and the app key it expects, read from Resources/OnlineConfig.json, which is
    /// not in git (copy OnlineConfig.example.json). Without it the game is simply offline: no counts, no board.
    /// </summary>
    [System.Serializable]
    public sealed class OnlineConfig
    {
        public const string Game = "app3d";             // the board this app plays on, as the server knows it
        public string api = "", appKey = "";
        public bool Enabled => !string.IsNullOrEmpty(api) && !string.IsNullOrEmpty(appKey);

        public static OnlineConfig Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return new OnlineConfig();
            try { var c = JsonUtility.FromJson<OnlineConfig>(json) ?? new OnlineConfig(); c.api = (c.api ?? "").TrimEnd('/'); c.appKey = c.appKey ?? ""; return c; }
            catch { return new OnlineConfig(); }
        }

        public static OnlineConfig Load()
        {
            var t = Resources.Load<TextAsset>("OnlineConfig");
            return Parse(t != null ? t.text : null);
        }
    }
}
