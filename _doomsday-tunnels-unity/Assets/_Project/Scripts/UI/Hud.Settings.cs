using DoomsdayTunnels.Core;
using DoomsdayTunnels.Input;
using UnityEngine;

namespace DoomsdayTunnels.UI
{
    /// <summary>
    /// The CONTROLS AND FEEL panel: how the ship is steered, the seven feel sliders, sound, play counting and reduced
    /// motion. Opened from a TUNE button on the screens between runs and from the pause screen. The pause button for
    /// touch screens lives here too. The panel's own controls are immediate-mode GUI, which takes touch directly.
    /// </summary>
    public sealed partial class Hud
    {
        public bool PanelOpen;
        public bool PanelChanged;                       // something was set while the panel was up: save on close
        GUIStyle thumb, track;

        static Rect TuneRect(float w, float h, float u) => new Rect(16 * u, h - 46 * u, 84 * u, 30 * u);
        static Rect PauseRect(float w, float u) => new Rect(w - 48 * u, 10 * u, 38 * u, 38 * u);

        /// <summary>The pause button in Unity screen coordinates (y up), for the input reader to keep clear of.</summary>
        public Rect PauseScreenRect { get { float sw = Screen.width, sh = Screen.height, uu = Mathf.Clamp(sh / 400f, 0.75f, 1.7f); var r = PauseRect(sw, uu); return new Rect(r.x - 8 * uu, sh - r.yMax - 8 * uu, r.width + 16 * uu, r.height + 16 * uu); } }

        public bool TuneHit(Vector2 screenPos) => Hit(screenPos, TuneRect(Screen.width, Screen.height, Mathf.Clamp(Screen.height / 400f, 0.75f, 1.7f)), 10);
        public bool PauseHit(Vector2 screenPos) => Hit(screenPos, PauseRect(Screen.width, Mathf.Clamp(Screen.height / 400f, 0.75f, 1.7f)), 8);
        static bool Hit(Vector2 screenPos, Rect r, float pad)
        {
            if (screenPos.x < 0) return false;
            float uu = Mathf.Clamp(Screen.height / 400f, 0.75f, 1.7f), gx = screenPos.x, gy = Screen.height - screenPos.y;
            return gx > r.x - pad * uu && gx < r.xMax + pad * uu && gy > r.y - pad * uu && gy < r.yMax + pad * uu;
        }

        void Outline(Rect r, Color col, float t) { Box(r.x, r.y, r.width, t, col); Box(r.x, r.yMax - t, r.width, t, col); Box(r.x, r.y, t, r.height, col); Box(r.xMax - t, r.y, t, r.height, col); }

        void DrawTuneButton()
        {
            var r = TuneRect(W, H, u);
            Box(r.x, r.y, r.width, r.height, Palette.Void, 0.5f); Outline(r, Palette.Ink, u);
            Text("TUNE", r.center.x, r.center.y, 12 * u, Palette.Ink, TextAnchor.MiddleCenter);
        }

        void DrawPauseButton()
        {
            var r = PauseRect(W, u);
            Box(r.x, r.y, r.width, r.height, Palette.Void, 0.35f); Outline(r, Palette.Ink, u);
            Box(r.x + 12 * u, r.y + 10 * u, 5 * u, 18 * u, Palette.Ink); Box(r.x + 21 * u, r.y + 10 * u, 5 * u, 18 * u, Palette.Ink);
        }

        // A labelled box with a tick, that flips on a tap.
        bool Check(float x, float y, string label, bool on, bool enabled = true)
        {
            float s = 14 * u; var col = enabled ? Palette.Ink : Palette.Dim;
            Outline(new Rect(x, y, s, s), col, u);
            if (on) Box(x + 3 * u, y + 3 * u, s - 6 * u, s - 6 * u, col);
            Text(label, x + s + 7 * u, y + s / 2, 11 * u, col, TextAnchor.MiddleLeft);
            float w = s + 9 * u + mono.CalcSize(new GUIContent(label)).x;
            return enabled && GUI.Button(new Rect(x - 4 * u, y - 6 * u, w + 8 * u, s + 12 * u), GUIContent.none, GUIStyle.none);
        }

        bool Button(Rect r, string label, bool lit = false)
        {
            Box(r.x, r.y, r.width, r.height, lit ? Palette.Ink : Palette.Void, lit ? 0.9f : 0.5f); Outline(r, Palette.Ink, u);
            Text(label, r.center.x, r.center.y, 11 * u, lit ? Palette.Void : Palette.Ink, TextAnchor.MiddleCenter);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        public void DrawPanel(Settings s, InputReader input)
        {
            if (thumb == null)
            {
                thumb = new GUIStyle { fixedWidth = 18 * u, fixedHeight = 18 * u }; thumb.normal.background = Texture2D.whiteTexture;
                track = new GUIStyle { fixedHeight = 18 * u };
            }
            thumb.fixedWidth = thumb.fixedHeight = track.fixedHeight = 18 * u;
            float cw = Mathf.Min(W - 24 * u, 600 * u), ch = Mathf.Min(H - 12 * u, 330 * u), cx0 = (W - cw) / 2, cy0 = (H - ch) / 2, pad = 14 * u;
            Box(0, 0, W, H, Palette.Void, 0.6f);
            Box(cx0, cy0, cw, ch, Palette.Void, 0.92f); Outline(new Rect(cx0, cy0, cw, ch), Palette.Dim, u);
            float x = cx0 + pad, y = cy0 + 12 * u, right = cx0 + cw - pad;
            Text("CONTROLS AND FEEL", x, y + 13 * u, 14 * u, Palette.Ink, TextAnchor.MiddleLeft, true);
            if (Button(new Rect(right - 70 * u, y, 70 * u, 26 * u), "DONE", true)) { PanelOpen = false; }
            if (Button(new Rect(right - 190 * u, y, 110 * u, 26 * u), "RESET SLIDERS")) { s.ResetTune(); PanelChanged = true; }
            // steering
            y += 36 * u;
            Text("STEERING", x, y + 12 * u, 11 * u, Palette.Dim, TextAnchor.MiddleLeft);
            float sx = x + 80 * u;
            bool tilt = s.Steer == SteerMode.Tilt;
            if (Button(new Rect(sx, y, 96 * u, 24 * u), "THUMB STICK", !tilt)) { s.Steer = SteerMode.Stick; PanelChanged = true; }
            if (Button(new Rect(sx + 96 * u, y, 96 * u, 24 * u), "TILT PHONE", tilt)) { s.Steer = SteerMode.Tilt; input.Tilt.Enable(); input.Tilt.NeedCal = true; PanelChanged = true; }
            if (Check(sx + 212 * u, y + 5 * u, "INVERT TILT UP/DOWN", s.InvertTilt, tilt)) { s.InvertTilt = !s.InvertTilt; PanelChanged = true; }
            y += 30 * u;
            string note = "Settings are saved on this device. Play counts are anonymous, and only scores you choose to post are public.";
            if (tilt)
            {
                var st = input.Tilt.Status;
                note = st == TiltStatus.On ? "Tilt steers and the thumb stick aims. It re-centres to how you are holding the phone each time you launch or resume."
                    : st == TiltStatus.None ? "No motion sensor is available here, so the thumb stick still steers." : "Waiting for the motion sensor...";
            }
            foreach (var line in Wrap(note, cw - 2 * pad, 10 * u)) { Text(line, x, y, 10 * u, Palette.Dim); y += 12 * u; }
            // the sliders, in two columns
            y = cy0 + 108 * u;
            float colW = (cw - 3 * pad) / 2, rowH = 44 * u;
            for (int i = 0; i < Settings.Defs.Length; i++)
            {
                var d = Settings.Defs[i];
                float px = x + (i % 2) * (colW + pad), py = y + (i / 2) * rowH;
                float v = d.Get(s.Tune);
                Text(d.Label, px, py, 11 * u, Palette.Ink);
                Text(d.Format(v), px + colW, py, 11 * u, Palette.Ink, TextAnchor.UpperRight);
                Text(d.Hint, px, py + 12 * u, 9 * u, Palette.Dim);
                var sr = new Rect(px, py + 22 * u, colW, 18 * u);
                Box(sr.x, sr.y + 7 * u, sr.width, 4 * u, Palette.Dim, 0.6f);
                Box(sr.x, sr.y + 7 * u, sr.width * Mathf.InverseLerp(d.Min, d.Max, v), 4 * u, Palette.Ink);
                GUI.color = Palette.Ink;
                float nv = Settings.Snap(d, GUI.HorizontalSlider(sr, v, d.Min, d.Max, track, thumb));
                GUI.color = Color.white;
                if (Mathf.Abs(nv - v) > 1e-4f) { d.Set(s.Tune, nv); PanelChanged = true; }
            }
            // the switches
            y = cy0 + ch - 36 * u;
            if (Check(x, y, "SOUND", s.Sound)) { s.Sound = !s.Sound; PanelChanged = true; }
            if (Check(x + 110 * u, y, "COUNT MY PLAYS", s.CountPlays)) { s.CountPlays = !s.CountPlays; PanelChanged = true; }
            if (Check(x + 270 * u, y, "REDUCE MOTION", s.ReduceMotion)) { s.ReduceMotion = !s.ReduceMotion; PanelChanged = true; }
        }

        /// <summary>In tilt mode the guns can be aimed off-centre: a small cross marks where they point.</summary>
        void DrawReticle(Camera cam, Vector3 world, bool hot)
        {
            var p = cam.WorldToScreenPoint(world); if (p.z <= 0) return;
            float gx = p.x, gy = H - p.y, r = 9 * u; var col = hot ? Palette.Warn : Palette.Ink;
            Box(gx - r, gy - u * 0.5f, r * 0.6f, u, col); Box(gx + r * 0.4f, gy - u * 0.5f, r * 0.6f, u, col);
            Box(gx - u * 0.5f, gy - r, u, r * 0.6f, col); Box(gx - u * 0.5f, gy + r * 0.4f, u, r * 0.6f, col);
        }
    }
}
