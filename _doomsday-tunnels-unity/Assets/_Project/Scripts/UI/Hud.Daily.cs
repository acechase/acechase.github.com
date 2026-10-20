using System;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Online;
using UnityEngine;

namespace DoomsdayTunnels.UI
{
    /// <summary>
    /// The daily sector's screens: a DAILY button on the title, the clear screen with the time and the two buttons
    /// (post it, see today's times), and the board of today's ten fastest.
    /// </summary>
    public sealed partial class Hud
    {
        public bool DailyOpen, DailyLoading; public DailyBoard DailyData; public string DailyError, DailyPostedAs;
        public Action FetchDaily; public Action<string> PostDaily;          // wired by the game
        public bool PostIsDaily;                                            // the post card is posting a daily time, not a score

        static Rect DailyRect(float w, float h, float u) => new Rect(168 * u, h - 46 * u, 96 * u, 30 * u);
        static Rect DailyPostRect(float w, float h, float u) => new Rect(w / 2 - 150 * u, h / 2 + 62 * u, 140 * u, 30 * u);
        static Rect DailyTimesRect(float w, float h, float u) => new Rect(w / 2 + 10 * u, h / 2 + 62 * u, 140 * u, 30 * u);
        static float U() => UI.SafeArea.U;
        public bool DailyHit(Vector2 p) => Hit(p, DailyRect(SafeArea.W, SafeArea.H, U()), 10);
        public bool DailyPostHit(Vector2 p) => Hit(p, DailyPostRect(SafeArea.W, SafeArea.H, U()), 8);
        public bool DailyTimesHit(Vector2 p) => Hit(p, DailyTimesRect(SafeArea.W, SafeArea.H, U()), 8);

        void DrawDailyButton() { var r = DailyRect(W, H, u); Box(r.x, r.y, r.width, r.height, Palette.Void, 0.5f); Outline(r, Palette.Gate, u); Text("DAILY", r.center.x, r.center.y, 12 * u, Palette.Gate, TextAnchor.MiddleCenter); }

        public void OpenDaily() { DailyOpen = true; PostOpen = false; BoardOpen = false; if (DailyData == null && !DailyLoading) { DailyLoading = true; DailyError = null; FetchDaily?.Invoke(); } }
        public void DailyArrived(DailyBoard b, string err) { DailyLoading = false; if (b != null) { DailyData = b; DailyError = null; } else DailyError = err ?? "Could not reach the scoreboard."; }
        public void OpenDailyPost(string lastName) { PostOpen = true; PostIsDaily = true; DailyOpen = false; PostStatus = ""; if (string.IsNullOrEmpty(PostName)) PostName = lastName ?? ""; }
        public void DailyPostDone(DailyBoard r, string err)
        {
            Posting = false;
            if (r == null) { PostStatus = err; return; }
            DailyPostedAs = r.name; DailyData = r; DailyError = null; PostOpen = false; DailyOpen = true;
            PostStatus = "POSTED AS " + r.name + ": RANK #" + r.rank + " TODAY";
        }

        /// <summary>The sector-clear screen for a daily run: the time, the device's best today, and what to do with it.</summary>
        void DrawDailyClear(GameDirector g, float cx, float cy, float big, bool blink, string go)
        {
            Text("DAILY SECTOR CLEAR", cx, cy - 84 * u, big * 0.75f, Palette.Gate, TextAnchor.MiddleCenter, true);
            Text(g.DailyDay + "   DIFFICULTY: SECTOR " + g.Bonus.Sector.ToString("00"), cx, cy - 48 * u, 13 * u, Palette.Dim, TextAnchor.MiddleCenter);
            Text("TIME " + g.Bonus.SectorTime.ToString("0.0") + (g.DailyPB ? "   PERSONAL BEST" : ""), cx, cy - 14 * u, 22 * u, g.DailyPB ? Palette.Gate : Palette.Ink, TextAnchor.MiddleCenter, true);
            float best = g.DailyBestT; if (best > 0 && Mathf.Abs(best - g.Bonus.SectorTime) > 0.05f) Text("YOUR BEST TODAY " + best.ToString("0.0"), cx, cy + 16 * u, 13 * u, Palette.Dim, TextAnchor.MiddleCenter);
            if (!g.FairRun) Text("Times post only at the standard flight speed.", cx, cy + 36 * u, 10 * u, Palette.Dim, TextAnchor.MiddleCenter);
            if (g.CanPostDaily) { var r = DailyPostRect(W, H, u); Box(r.x, r.y, r.width, r.height, Palette.Gate, 0.9f); Text("POST TIME", r.center.x, r.center.y, 12 * u, Palette.Void, TextAnchor.MiddleCenter, true); }
            if (g.Online) { var r = DailyTimesRect(W, H, u); Box(r.x, r.y, r.width, r.height, Palette.Void, 0.5f); Outline(r, Palette.Ink, u); Text("TODAY'S TIMES", r.center.x, r.center.y, 12 * u, Palette.Ink, TextAnchor.MiddleCenter); }
            if (blink) Text(go + " FOR THE TITLE", cx, cy + 112 * u, 14 * u, Palette.Ink, TextAnchor.MiddleCenter, true);
        }

        public void DrawDailyBoard(string day)
        {
            Card(out float cx0, out float cy0, out float cw, out float ch);
            float pad = 14 * u, x = cx0 + pad, y = cy0 + 12 * u, right = cx0 + cw - pad;
            Text("DAILY SECTOR", x, y + 13 * u, 14 * u, Palette.Gate, TextAnchor.MiddleLeft, true);
            Text(day + (DailyData != null ? "   DIFFICULTY: SECTOR " + DailyData.difficulty.ToString("00") : ""), x + 130 * u, y + 13 * u, 10 * u, Palette.Dim, TextAnchor.MiddleLeft);
            if (Button(new Rect(right - 70 * u, y, 70 * u, 26 * u), "BACK", true)) DailyOpen = false;
            if (!string.IsNullOrEmpty(PostStatus) && DailyPostedAs != null) Text(PostStatus, x, y + 36 * u, 11 * u, Palette.Gate);
            y = cy0 + 54 * u;
            if (DailyLoading) { Text("LOADING...", W / 2, H / 2, 13 * u, Palette.Dim, TextAnchor.MiddleCenter); return; }
            if (DailyData == null) { Text(DailyError ?? "", W / 2, H / 2 - 8 * u, 12 * u, Palette.Warn, TextAnchor.MiddleCenter); if (Button(new Rect(W / 2 - 50 * u, H / 2 + 10 * u, 100 * u, 26 * u), "TRY AGAIN")) { DailyLoading = true; FetchDaily?.Invoke(); } return; }
            Text("TODAY'S FASTEST", x, y, 10 * u, Palette.Dim);
            float colW = (cw - 3 * pad) * 0.45f, rowH = 14 * u;
            for (int i = 0; i < 10; i++)
            {
                float px = x + (i / 5) * (colW + pad), py = y + 16 * u + (i % 5) * rowH;
                if (i >= DailyData.top.Length) { Text((i + 1).ToString("00") + "  ---", px, py, 11 * u, Palette.Dim); continue; }
                var t = DailyData.top[i]; bool me = DailyPostedAs != null && t.name == DailyPostedAs; var col = me ? Palette.Gate : Palette.Ink;
                Text((i + 1).ToString("00") + "  " + t.name, px, py, 11 * u, col); Text(BoardFormat.Tenths(t.t), px + colW, py, 11 * u, col, TextAnchor.UpperRight);
            }
            if (DailyData.top.Length == 0) Text("Nobody has flown it yet today. Be the first.", x, y + 16 * u, 10 * u, Palette.Dim);
            Text("Everyone flies the same sector today. A new one arrives at midnight UTC.", x, cy0 + ch - 22 * u, 9 * u, Palette.Dim);
        }
    }
}
