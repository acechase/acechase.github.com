using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DoomsdayTunnels.Online
{
    /// <summary>
    /// Talks to the scoreboard Worker's /v2 routes. Everything is fire-and-forget with a callback; nothing here ever
    /// blocks the game, and a server that is down just means no board. The requests carry no identifier: the app key
    /// is the same in every copy of the app, the run token names a moment, not a player.
    /// </summary>
    public sealed class ApiClient : MonoBehaviour
    {
        public OnlineConfig Config;
        const int TimeoutSecs = 8;

        string Url(string path) => Config.api + path + (path.Contains("?") ? "&" : "?") + "game=" + OnlineConfig.Game;

        public void StartRun(bool count, bool firstToday, bool firstEver, bool touch, bool tilt, Action<string> done)
        {
            string body = "{\"t\":\"start\",\"count\":" + (count ? "true" : "false") + ",\"firstToday\":" + (firstToday ? "true" : "false") + ",\"firstEver\":" + (firstEver ? "true" : "false")
                + ",\"touch\":" + (touch ? "true" : "false") + ",\"tilt\":" + (tilt ? "true" : "false") + "}";
            StartCoroutine(Post("/v2/event", body, (ok, text) => done?.Invoke(ok ? TokenResult.Parse(text)?.token : null)));
        }

        public void EndRun(string token, int sector, int cleared, int secs, bool count)
        {
            if (string.IsNullOrEmpty(token)) return;
            string body = "{\"t\":\"end\",\"token\":\"" + token + "\",\"sector\":" + sector + ",\"cleared\":" + cleared + ",\"secs\":" + secs + ",\"count\":" + (count ? "true" : "false") + "}";
            StartCoroutine(Post("/v2/event", body, null));
        }

        public void FetchDaily(string day, Action<DailyBoard, string> done) => StartCoroutine(Get("/v2/daily?day=" + day, (ok, text) => { var b = ok ? DailyBoard.Parse(text) : null; done?.Invoke(b, b == null ? Friendly(text) : null); }));

        public void PostDaily(string name, string day, int tenths, string token, Action<DailyBoard, string> done)
        {
            string body = "{\"name\":" + Quote(name) + ",\"day\":" + Quote(day) + ",\"t\":" + tenths + ",\"token\":" + Quote(token) + "}";
            StartCoroutine(Post("/v2/daily", body, (ok, text) =>
            {
                var r = DailyBoard.Parse(text);
                if (r != null && r.ok) done?.Invoke(r, null);
                else done?.Invoke(null, r != null && !string.IsNullOrEmpty(r.message) ? r.message : r != null && r.error == "name" ? "That name was not accepted." : r != null && (r.error == "time" || r.error == "day") ? "That time was not accepted." : Friendly(text));
            }));
        }

        public void FetchBoard(Action<BoardData, string> done) => StartCoroutine(Get("/v2/board", (ok, text) => { var b = ok ? BoardData.Parse(text) : null; done?.Invoke(b, b == null ? Friendly(text) : null); }));

        public void PostScore(string name, int score, int sector, string token, int[] times, Action<PostResult, string> done)
        {
            var sb = new StringBuilder("{\"name\":").Append(Quote(name)).Append(",\"score\":").Append(score).Append(",\"sector\":").Append(sector).Append(",\"token\":").Append(Quote(token)).Append(",\"times\":[");
            for (int i = 0; i < times.Length; i++) { if (i > 0) sb.Append(','); sb.Append(times[i]); }
            sb.Append("]}");
            StartCoroutine(Post("/v2/scores", sb.ToString(), (ok, text) =>
            {
                var r = PostResult.Parse(text);
                if (r != null && r.ok) done?.Invoke(r, null);
                else done?.Invoke(null, r != null && !string.IsNullOrEmpty(r.message) ? r.message : r != null && r.error == "name" ? "That name was not accepted." : r != null && r.error == "score" ? "That score was not accepted." : Friendly(text));
            }));
        }

        static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in s ?? "") { if (c == '"' || c == '\\') sb.Append('\\').Append(c); else if (c < ' ') sb.Append(' '); else sb.Append(c); }
            return sb.Append('"').ToString();
        }
        static string Friendly(string text) => string.IsNullOrEmpty(text) ? "Could not reach the scoreboard." : text.Contains("slow down") ? "Too many tries. Wait a minute." : "The scoreboard is not answering.";

        IEnumerator Get(string path, Action<bool, string> done)
        {
            using (var req = UnityWebRequest.Get(Url(path)))
            {
                req.timeout = TimeoutSecs;
                yield return req.SendWebRequest();
                done?.Invoke(req.result == UnityWebRequest.Result.Success, req.downloadHandler != null ? req.downloadHandler.text : "");
            }
        }

        IEnumerator Post(string path, string body, Action<bool, string> done)
        {
            using (var req = new UnityWebRequest(Url(path), "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)); req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json"); req.SetRequestHeader("X-App-Key", Config.appKey);
                req.timeout = TimeoutSecs;
                yield return req.SendWebRequest();
                done?.Invoke(req.result == UnityWebRequest.Result.Success, req.downloadHandler != null ? req.downloadHandler.text : "");
            }
        }
    }
}
