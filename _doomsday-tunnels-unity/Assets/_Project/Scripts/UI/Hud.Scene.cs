using System.Collections.Generic;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Input;
using DoomsdayTunnels.Story;
using UnityEngine;

namespace DoomsdayTunnels.UI
{
    /// <summary>
    /// The cut-scene presentation: the pixel canvas blown up without smoothing in the top part of the screen, worn
    /// like an old video tape (scan lines, a drifting tracking band, static on every cut), a PLAY label and tape
    /// counter, a SKIP button, and the dialogue typed out underneath in the speaker's colour.
    /// </summary>
    public sealed partial class Hud
    {
        PixelCanvas canvas;
        string wrapKey; List<string> wrap = new List<string>(); float wrapFs;
        static readonly (float, float, Color32)[] ChromeBands = { (0, 0.23f, K.White), (0.23f, 0.46f, K.Cyan), (0.46f, 0.56f, K.Navy), (0.56f, 0.78f, K.Pink), (0.78f, 1f, K.Yellow) };

        static Rect SkipRect(float w, float u) => new Rect(w - 100 * u, 12 * u, 86 * u, 34 * u);

        /// <summary>Whether a tap at this screen position (Unity coordinates, y up) lands on the SKIP button.</summary>
        public bool SkipHit(Vector2 screenPos)
        {
            if (screenPos.x < 0) return false;
            float uu = SafeArea.U;
            var b = SkipRect(SafeArea.W, uu); var g = SafeArea.ToGui(screenPos); float gx = g.x, gy = g.y;
            return gx > b.x - 10 * uu && gx < b.xMax + 10 * uu && gy < b.yMax + 12 * uu;
        }

        public void DrawScene(ScenePlayer sc, InputReader input, float now)
        {
            var sh = sc.Shot; var ln = sc.Cur; var who = Script.Who[ln.Who];
            float dh = Mathf.Round(Mathf.Max(104 * u, H * 0.33f)), ph = H - dh; bool typing = sc.Typing;
            int vh = Mathf.Clamp(Mathf.RoundToInt(PixelCanvas.VW * ph / W), 96, 180);
            if (canvas == null) canvas = new PixelCanvas(vh); else canvas.Resize(vh);
            Sets.Paint(canvas, sh.Bg, sc.T, ln.Who, typing, sh, ln, out var title);
            canvas.Upload();
            float pw = Mathf.Min(W, ph * PixelCanvas.VW / vh), pxh = pw * vh / PixelCanvas.VW, px0 = (W - pw) / 2, py0 = (ph - pxh) / 2, scale = pw / PixelCanvas.VW;
            Full(Palette.Void);
            GUI.DrawTexture(new Rect(px0, py0, pw, pxh), canvas.Texture);
            if (title.Text != null) Chrome(title.Text, px0 + title.Cx * scale, py0 + title.Cy * scale, title.Size * scale, pw - 28 * scale, scale);
            // the picture's edges fall off a little, like a tube
            for (int i = 0; i < 5; i++) { float a = 0.12f; Box(px0, py0 + i * 3 * u, pw, 3 * u, Palette.Void, a); Box(px0, py0 + pxh - (i + 1) * 3 * u, pw, 3 * u, Palette.Void, a); Box(px0 + i * 4 * u, py0, 4 * u, pxh, Palette.Void, a); Box(px0 + pw - (i + 1) * 4 * u, py0, 4 * u, pxh, Palette.Void, a); }
            // worn video tape: scan lines, a drifting tracking band, a burst of static on every cut
            for (float y = py0; y < py0 + pxh; y += 3) Box(px0, y, pw, 1, Palette.Void, 0.14f);
            if (!ReduceMotion) Box(px0, py0 + ((sc.T * 0.23f) % 1) * (pxh + 40) - 30, pw, 10 * u, Palette.Ink, 0.07f);
            if (sc.T < 0.22f && !ReduceMotion) Box(px0, py0, pw, pxh, Palette.Ink, (1 - sc.T / 0.22f) * 0.85f);
            int tt = Mathf.FloorToInt(sc.Elapsed); float lx = px0 + 14 * u;
            Text("PLAY >", lx, py0 + 12 * u, 14 * u, Palette.Ink);
            Text("SP  0:" + (tt / 60).ToString("00") + ":" + (tt % 60).ToString("00"), lx, py0 + pxh - 24 * u, 12 * u, Palette.Ink);
            var b = SkipRect(W, u);
            Box(b.x, b.y, b.width, b.height, Palette.Void, 0.5f);
            Box(b.x, b.y, b.width, u, Palette.Ink); Box(b.x, b.yMax - u, b.width, u, Palette.Ink); Box(b.x, b.y, u, b.height, Palette.Ink); Box(b.xMax - u, b.y, u, b.height, Palette.Ink);
            Text("SKIP >>", b.center.x, b.center.y, 12 * u, Palette.Ink, TextAnchor.MiddleCenter);
            // dialogue: a panel under the picture, edged in the speaker's colour, with their name on a tab
            float x0 = 26 * u, x1 = W - 26 * u;
            Box(0, ph, W, dh, Palette.Void); Box(0, ph, W, 2.5f * u, who.Col);
            Box(10 * u, ph + 10 * u, W - 20 * u, dh - 20 * u, Color.Lerp(who.Col, Palette.Void, 0.9f)); Outline(new Rect(10 * u, ph + 10 * u, W - 20 * u, dh - 20 * u), Color.Lerp(who.Col, Palette.Void, 0.45f), u);
            float nameW = 0; { display.fontSize = Mathf.RoundToInt(12 * u); nameW = display.CalcSize(new GUIContent(who.Name)).x + 20 * u; }
            Box(x0 - 8 * u, ph + 2.5f * u, nameW, 20 * u, who.Col); Text(who.Name, x0 - 8 * u + nameW / 2, ph + 12.5f * u, 12 * u, Palette.Void, TextAnchor.MiddleCenter, true);
            string key = sc.I + ":" + sc.LineNo + ":" + Mathf.RoundToInt(W) + ":" + Mathf.RoundToInt(H);
            if (wrapKey != key)
            {
                float fs = 21 * u;
                do { wrap = Wrap(ln.Text, x1 - x0, fs); if (wrap.Count * fs * 1.3f <= dh - 52 * u) break; fs *= 0.92f; } while (fs > 10);
                wrapKey = key; wrapFs = fs;
            }
            int left = sc.Shown;
            for (int i = 0; i < wrap.Count; i++)
            {
                var s = wrap[i];
                if (left > 0) Text(s.Substring(0, Mathf.Min(s.Length, left)), x0, ph + 34 * u + i * wrapFs * 1.3f, wrapFs, Palette.Ink);
                left -= s.Length + 1;
            }
            if (!typing && (now * 2) % 1 < 0.6f)
            {   // a small arrow, pulsing, and the hint beside it
                Text(input.TouchUI ? "TAP TO CONTINUE" : "SPACE TO CONTINUE", x1 - 16 * u, H - 26 * u, 11 * u, Palette.Dim, TextAnchor.UpperRight);
                for (int k = 0; k < 4; k++) Box(x1 - 10 * u + k * 2 * u, H - 24 * u + k * 1.5f * u, 2 * u, 8 * u - k * 3 * u, who.Col);
            }
        }

        List<string> Wrap(string text, float maxW, float fs)
        {
            mono.fontSize = Mathf.RoundToInt(fs); var lines = new List<string>(); string cur = "";
            foreach (var w in text.Split(' '))
            {
                string next = cur.Length > 0 ? cur + " " + w : w;
                if (cur.Length > 0 && mono.CalcSize(new GUIContent(next)).x > maxW) { lines.Add(cur); cur = w; } else cur = next;
            }
            if (cur.Length > 0) lines.Add(cur);
            return lines;
        }

        // Big chrome lettering, as seen on every 1987 film poster: a drop shadow, a dark outline, and a fill that
        // runs white to cyan, a dark seam, then pink to yellow. `scale` is screen pixels per canvas pixel.
        void Chrome(string text, float cx, float cy, float size, float maxW, float scale)
        {
            var st = display; int fs = Mathf.Max(8, Mathf.RoundToInt(size));
            for (; fs > 8; fs--) { st.fontSize = fs; if (st.CalcSize(new GUIContent(text)).x <= maxW) break; }
            st.fontSize = fs; st.alignment = TextAnchor.MiddleCenter;
            float h = fs * 1.6f; var r = new Rect(cx - W / 2, cy - h / 2, W, h);
            void Lbl(float dx, float dy, Color col) { st.normal.textColor = col; GUI.Label(new Rect(r.x + dx, r.y + dy, r.width, h), text, st); }
            Lbl(2 * scale, 2 * scale, K.Black);
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; Lbl(Mathf.Cos(a) * 1.5f * scale, Mathf.Sin(a) * 1.5f * scale, K.Navy); }
            foreach (var (f0, f1, col) in ChromeBands)
            {
                float by0 = cy - fs * 0.5f + f0 * fs, by1 = cy - fs * 0.5f + f1 * fs;
                GUI.BeginGroup(new Rect(r.x, by0, r.width, by1 - by0));
                st.normal.textColor = col; GUI.Label(new Rect(0, r.y - by0, r.width, h), text, st);
                GUI.EndGroup();
            }
        }
    }
}
