using System;
using UnityEngine;

namespace DoomsdayTunnels.Online
{
    // What the server sends back, shaped for JsonUtility (public fields, serializable classes, arrays).
    [Serializable] public sealed class ScoreRow { public string name; public int score, sector; public string day; }
    [Serializable] public sealed class TimeRow { public string name; public int t; public string day; }
    [Serializable] public sealed class SectorTimes { public int sector; public TimeRow[] top; }

    /// <summary>GET /v2/board: the top names and the fastest times, one board per game.</summary>
    [Serializable]
    public sealed class BoardData
    {
        public string game, title;
        public ScoreRow[] top;
        public SectorTimes[] times;
        public static BoardData Parse(string json) { try { var b = JsonUtility.FromJson<BoardData>(json); if (b != null) { b.top = b.top ?? new ScoreRow[0]; b.times = b.times ?? new SectorTimes[0]; } return b; } catch { return null; } }
    }

    /// <summary>POST /v2/scores: the name as the server kept it, that name's best, its rank, and the fresh board. On a refusal, `error` and maybe `message`.</summary>
    [Serializable]
    public sealed class PostResult
    {
        public bool ok;
        public string name, error, message;
        public int best, rank;
        public ScoreRow[] top;
        public SectorTimes[] times;
        public static PostResult Parse(string json) { try { var r = JsonUtility.FromJson<PostResult>(json); if (r != null) { r.top = r.top ?? new ScoreRow[0]; r.times = r.times ?? new SectorTimes[0]; } return r; } catch { return null; } }
        public BoardData AsBoard() => new BoardData { game = OnlineConfig.Game, top = top, times = times };
    }

    [Serializable] public sealed class TokenResult { public string token; public static TokenResult Parse(string json) { try { return JsonUtility.FromJson<TokenResult>(json); } catch { return null; } } }

    /// <summary>A sector time the way the board shows it: tenths of a second as "12.3".</summary>
    public static class BoardFormat
    {
        public static string Tenths(int t) => (t / 10) + "." + (t % 10);
    }
}
