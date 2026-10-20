using System;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Online;
using UnityEngine;

namespace DoomsdayTunnels.UI
{
    /// <summary>
    /// The scoreboard and the post-a-score card: a SCORES button on the title and game-over screens, POST SCORE on
    /// game over. The board is the top twenty names and the fastest times per sector, as on the web.
    /// </summary>
    public sealed partial class Hud
    {
        public bool BoardOpen, PostOpen;
        public bool Modal => PanelOpen || BoardOpen || PostOpen || DailyOpen;
        public BoardData Board; public string BoardError; public bool BoardLoading;
        public string PostName = "", PostStatus = ""; public bool Posting; public string PostedAs;
        public Action FetchBoard; public Action<string> PostScore;          // wired by the game
        GUIStyle field;

        static Rect ScoresRect(float w, float h, float u) => new Rect(64 * u, h - 46 * u, 96 * u, 30 * u);
        static Rect PostRect(float w, float h, float u) => new Rect(w / 2 - 70 * u, h / 2 + 40 * u, 140 * u, 30 * u);
        public bool ScoresHit(Vector2 screenPos) => Hit(screenPos, ScoresRect(SafeArea.W, SafeArea.H, UI.SafeArea.U), 10);
        public bool PostHit(Vector2 screenPos) => Hit(screenPos, PostRect(SafeArea.W, SafeArea.H, UI.SafeArea.U), 8);

        void DrawScoresButton() { var r = ScoresRect(W, H, u); Box(r.x, r.y, r.width, r.height, Palette.Void, 0.5f); Outline(r, Palette.Ink, u); Text("SCORES", r.center.x, r.center.y, 12 * u, Palette.Ink, TextAnchor.MiddleCenter); }
        void DrawPostButton() { var r = PostRect(W, H, u); Box(r.x, r.y, r.width, r.height, Palette.Gate, 0.9f); Text("POST SCORE", r.center.x, r.center.y, 12 * u, Palette.Void, TextAnchor.MiddleCenter, true); }

        public void OpenBoard() { BoardOpen = true; PostOpen = false; if (Board == null && !BoardLoading) { BoardLoading = true; BoardError = null; FetchBoard?.Invoke(); } }
        public void BoardArrived(BoardData b, string err) { BoardLoading = false; if (b != null) { Board = b; BoardError = null; } else BoardError = err ?? "Could not reach the scoreboard."; }
        public void OpenPost(string lastName) { PostOpen = true; PostIsDaily = false; BoardOpen = false; PostStatus = ""; if (string.IsNullOrEmpty(PostName)) PostName = lastName ?? ""; }
        public void PostDone(PostResult r, string err)
        {
            Posting = false;
            if (r == null) { PostStatus = err; return; }
            PostedAs = r.name; Board = r.AsBoard(); BoardError = null; PostOpen = false; BoardOpen = true;
            PostStatus = "POSTED AS " + r.name + ": RANK #" + r.rank;
        }

        void Card(out float cx0, out float cy0, out float cw, out float ch)
        {
            cw = Mathf.Min(W - 24 * u, 640 * u); ch = Mathf.Min(H - 12 * u, 340 * u); cx0 = (W - cw) / 2; cy0 = (H - ch) / 2;
            Full(Palette.Void, 0.6f);
            Box(cx0, cy0, cw, ch, Palette.Void, 0.94f); Outline(new Rect(cx0, cy0, cw, ch), Palette.Dim, u);
        }

        public void DrawBoard()
        {
            Card(out float cx0, out float cy0, out float cw, out float ch);
            float pad = 14 * u, x = cx0 + pad, y = cy0 + 12 * u, right = cx0 + cw - pad;
            Text("SCOREBOARD", x, y + 13 * u, 14 * u, Palette.Ink, TextAnchor.MiddleLeft, true);
            Text(Board != null && Board.title != null ? Board.title.ToUpperInvariant() : "DOOMSDAY TUNNELS 3D", x + 120 * u, y + 13 * u, 10 * u, Palette.Dim, TextAnchor.MiddleLeft);
            if (Button(new Rect(right - 70 * u, y, 70 * u, 26 * u), "BACK", true)) BoardOpen = false;
            if (!string.IsNullOrEmpty(PostStatus) && PostedAs != null) Text(PostStatus, x, y + 36 * u, 11 * u, Palette.Gate);
            y = cy0 + 54 * u;
            if (BoardLoading) { Text("LOADING...", W / 2, H / 2, 13 * u, Palette.Dim, TextAnchor.MiddleCenter); return; }
            if (Board == null) { Text(BoardError ?? "", W / 2, H / 2 - 8 * u, 12 * u, Palette.Warn, TextAnchor.MiddleCenter); if (Button(new Rect(W / 2 - 50 * u, H / 2 + 10 * u, 100 * u, 26 * u), "TRY AGAIN")) { Board = null; BoardLoading = true; FetchBoard?.Invoke(); } return; }
            // left: the top twenty, in two columns of ten
            float colW = (cw - 3 * pad) * 0.3f, rowH = 13 * u;
            Text("TOP 20", x, y, 10 * u, Palette.Dim);
            for (int i = 0; i < 20; i++)
            {
                float px = x + (i / 10) * (colW + 8 * u), py = y + 16 * u + (i % 10) * rowH;
                if (i >= Board.top.Length) { Text((i + 1).ToString("00") + "  ---", px, py, 10 * u, Palette.Dim); continue; }
                var s = Board.top[i]; bool me = PostedAs != null && s.name == PostedAs; var col = me ? Palette.Gate : Palette.Ink;
                Text((i + 1).ToString("00") + "  " + s.name, px, py, 10 * u, col);
                Text(s.score.ToString("000000"), px + colW, py, 10 * u, col, TextAnchor.UpperRight);
            }
            if (Board.top.Length == 0) Text("No scores yet. Be the first.", x, y + 16 * u, 10 * u, Palette.Dim);
            // right: the fastest times, the three best names per sector, as many sectors as fit
            float tx = x + 2 * (colW + 8 * u) + 8 * u, tw = right - tx;
            Text("FASTEST TIMES", tx, y, 10 * u, Palette.Dim);
            int rows = Mathf.FloorToInt((ch - 80 * u) / rowH);
            for (int i = 0; i < Board.times.Length && i < rows; i++)
            {
                var st = Board.times[i]; float py = y + 16 * u + i * rowH;
                Text("S" + st.sector.ToString("00"), tx, py, 10 * u, Palette.Dim);
                string line = "";
                for (int k = 0; k < st.top.Length && k < 3; k++) line += (k > 0 ? "   " : "") + BoardFormat.Tenths(st.top[k].t) + " " + st.top[k].name;
                bool me = PostedAs != null && st.top.Length > 0 && Array.Exists(st.top, r => r.name == PostedAs);
                Text(line, tx + 30 * u, py, 9.5f * u, me ? Palette.Gate : Palette.Ink);
            }
            if (Board.times.Length == 0) Text("No sector times yet.", tx, y + 16 * u, 10 * u, Palette.Dim);
            Text("Times count at the standard flight speed only.", x, cy0 + ch - 22 * u, 9 * u, Palette.Dim);
        }

        public void DrawPost(int score, int sector, string dailyDay = null, float dailyTime = 0)
        {
            if (field == null) { field = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleLeft }; field.normal.background = Texture2D.whiteTexture; field.focused.background = Texture2D.whiteTexture; field.normal.textColor = Palette.Void; field.focused.textColor = Palette.Void; }
            Card(out float cx0, out float cy0, out float cw, out float ch);
            float x = cx0 + 14 * u, y = cy0 + 12 * u, right = cx0 + cw - 14 * u;
            Text(PostIsDaily ? "POST YOUR TIME" : "POST YOUR SCORE", x, y + 13 * u, 14 * u, Palette.Ink, TextAnchor.MiddleLeft, true);
            if (Button(new Rect(right - 70 * u, y, 70 * u, 26 * u), "CANCEL")) { PostOpen = false; }
            Text(PostIsDaily ? "TIME " + dailyTime.ToString("0.0") + "   DAILY SECTOR " + dailyDay : "SCORE " + score.ToString("000000") + "   SECTOR " + sector.ToString("00"), x, y + 44 * u, 13 * u, Palette.Ink);
            Text("YOUR NAME (UP TO 12 LETTERS). Only the name you type is sent: nothing identifies you.", x, y + 66 * u, 9.5f * u, Palette.Dim);
            field.fontSize = Mathf.RoundToInt(14 * u);
            GUI.SetNextControlName("postname");
            GUI.color = Palette.Ink;
            string typed = GUI.TextField(new Rect(x, y + 84 * u, 220 * u, 30 * u), PostName, 12, field);
            GUI.color = Color.white;
            if (typed != PostName) { PostName = typed; PostStatus = ""; }
            bool enter = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
            bool can = !Posting && PostName.Trim().Length > 0;
            if ((Button(new Rect(x + 232 * u, y + 84 * u, 90 * u, 30 * u), Posting ? "POSTING..." : "POST", can) || (enter && can)) && can)
            { Posting = true; PostStatus = ""; if (PostIsDaily) PostDaily?.Invoke(PostName.Trim()); else PostScore?.Invoke(PostName.Trim()); }
            if (!string.IsNullOrEmpty(PostStatus)) Text(PostStatus, x, y + 126 * u, 11 * u, Palette.Warn);
            Text("Posting is public and cannot be undone from the app. One best score per name.", x, cy0 + ch - 22 * u, 9 * u, Palette.Dim);
        }
    }
}
