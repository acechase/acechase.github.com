using System.Collections.Generic;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Input;
using UnityEngine;

namespace DoomsdayTunnels.UI
{
    /// <summary>
    /// The heads-up display and the screens between runs, drawn with immediate-mode GUI for milestone 1. Everything is
    /// measured in the web game's unit u (screen height / 400), so the layout matches the original on any phone.
    /// Coordinates here are GUI coordinates: origin top-left, y down, the same as the web game's canvas.
    /// </summary>
    public sealed class Hud
    {
        public struct Popup { public string Text; public float T; }
        public readonly List<Popup> Popups = new List<Popup>();
        public float Flash; public Color FlashColor = Palette.Warn;
        public bool ReduceMotion;

        GUIStyle mono, display, small; Texture2D circle;
        float W, H, u;

        void Prepare()
        {
            if (mono == null)
            {
                mono = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, richText = false };
                mono.normal.textColor = Palette.Ink;
                display = new GUIStyle(mono) { fontStyle = FontStyle.Bold };
                small = new GUIStyle(mono);
                circle = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32)) / 32f;
                    circle.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - d) * 16)));
                }
                circle.Apply();
            }
            W = Screen.width; H = Screen.height; u = Mathf.Clamp(H / 400f, 0.75f, 1.7f);
        }

        void Text(string s, float x, float y, float size, Color col, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false)
        {
            var st = bold ? display : mono; st.fontSize = Mathf.RoundToInt(size); st.alignment = anchor;
            float w = W, h = size * 1.6f; float bx = x, by = y - h / 2;
            if (anchor == TextAnchor.UpperCenter || anchor == TextAnchor.MiddleCenter) bx = x - w / 2;
            else if (anchor == TextAnchor.UpperRight || anchor == TextAnchor.MiddleRight) bx = x - w;
            if (anchor == TextAnchor.UpperLeft || anchor == TextAnchor.UpperCenter || anchor == TextAnchor.UpperRight) by = y;
            st.normal.textColor = Palette.Void; GUI.Label(new Rect(bx + 1, by + 1, w, h), s, st);     // a dark shadow keeps text readable on any tunnel
            st.normal.textColor = col; GUI.Label(new Rect(bx, by, w, h), s, st);
        }
        void Box(float x, float y, float w, float h, Color col, float alpha = 1f) { GUI.color = new Color(col.r, col.g, col.b, alpha); GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); GUI.color = Color.white; }
        void Ring(Vector2 c, float r, Color col, float alpha) { GUI.color = new Color(col.r, col.g, col.b, alpha); GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2, r * 2), circle); GUI.color = Color.white; }
        Vector2 Gui(Vector2 screen) => new Vector2(screen.x, H - screen.y);     // Unity screen (y up) to GUI (y down)

        public void Bar(float x, float y, float w, float h, float frac, Color fill, Color edge, string label, Color labelCol)
        {
            Text(label, x, y - 13 * u, 11 * u, labelCol);
            Box(x, y, w, h, Palette.Void, 0.55f);
            Box(x, y, w, 1, edge, 0.9f); Box(x, y + h - 1, w, 1, edge, 0.9f); Box(x, y, 1, h, edge, 0.9f); Box(x + w - 1, y, 1, h, edge, 0.9f);
            Box(x, y, w * Mathf.Clamp01(frac), h, fill);
        }

        public void Draw(GameDirector g, FlightSim sim, InputReader input)
        {
            Prepare();
            float m = 14 * u, x0 = m;
            var state = g.State;
            if (Flash > 0 && !ReduceMotion) Box(0, 0, W, H, FlashColor, Flash * 0.35f);
            if (sim.Invuln > 0 && state == GameState.Play)
            {
                float a = 0.35f + 0.35f * Mathf.Sin(sim.Now * 14); Box(5 * u, 5 * u, W - 10 * u, 4 * u, Palette.Ink, a); Box(5 * u, H - 9 * u, W - 10 * u, 4 * u, Palette.Ink, a);
                Box(5 * u, 5 * u, 4 * u, H - 10 * u, Palette.Ink, a); Box(W - 9 * u, 5 * u, 4 * u, H - 10 * u, Palette.Ink, a);
            }
            if (state == GameState.Play && sim.Shield > 0 && sim.Shield <= G.LowShield)
            {
                float beat = ReduceMotion ? 0.6f : Mathf.Pow(0.5f + 0.5f * Mathf.Sin(sim.Now * 9), 1.4f);
                float a = (0.34f + 0.26f * (1 - sim.Shield / G.LowShield)) * (0.3f + 0.7f * beat) * 0.5f, d = Mathf.Min(W, H) * 0.12f;
                Box(0, 0, W, d, Palette.Warn, a); Box(0, H - d, W, d, Palette.Warn, a); Box(0, 0, d, H, Palette.Warn, a); Box(W - d, 0, d, H, Palette.Warn, a);
            }

            if (state != GameState.Title && state != GameState.Clear)
            {
                Text("SECTOR " + sim.Sector.N.ToString("00"), x0, m, 12 * u, Palette.Dim);
                float ga = sim.Now - sim.GainT; bool gaining = sim.GainN > 0 && ga < 1.7f;
                if (gaining) { var c = Palette.Ink; c.a = ga < 1.2f ? 1 : 1 - (ga - 1.2f) / 0.5f; Text("+" + sim.GainN, x0 + 78 * u, m, (sim.GainN >= 100 ? 15 : 12.5f) * u, c); }
                Text(((int)sim.Score).ToString("000000"), x0, m + 15 * u, 21 * u, gaining && ga < 0.22f && !ReduceMotion ? sim.Sector.Hue : Palette.Ink);
                for (int i = 0; i < sim.Lives + (sim.ShipEarned && sim.Lives < G.MaxSpares ? 1 : 0); i++)
                {
                    if (i == sim.Lives && (sim.Now * 3) % 1 < 0.4f) continue;
                    Box(x0 + 92 * u + i * 15 * u, m + 19 * u, 10 * u, 12 * u, Palette.Ink);
                }
                bool low = sim.Shield < 30, crit = sim.Shield > 0 && sim.Shield <= G.LowShield;
                Bar(x0, m + 58 * u, 112 * u, 7 * u, sim.Shield / 100f, low ? Palette.Warn : sim.Sector.Hue, low ? Palette.Warn : Palette.Ink, crit ? "SHIELD LOW" : "SHIELD", crit ? Palette.Warn : Palette.Dim);
                bool dry = sim.BoostFuel <= 0;
                Bar(x0, m + 87 * u, 112 * u, 7 * u, sim.BoostFuel, Palette.Ink, dry ? Palette.Dim : Palette.Ink, dry ? "BOOST EMPTY" : sim.Boosting ? "BOOST ON" : "BOOST", sim.Boosting ? Palette.Ink : Palette.Dim);
                bool warm = sim.GunHeat > 0.7f;
                Bar(x0, m + 116 * u, 112 * u, 7 * u, sim.GunHeat, sim.GunHot || warm ? Palette.Warn : Palette.Ink, sim.GunHot || warm ? Palette.Warn : Palette.Ink, sim.GunHot ? "GUNS OVERHEATED" : "GUNS", sim.GunHot ? Palette.Warn : Palette.Dim);

                bool urgent = sim.Time < 10, blink = urgent && (sim.Now * 4) % 1 < 0.5f;
                Text("DETONATION IN", W / 2, m, 11 * u, urgent ? Palette.Warn : Palette.Dim, TextAnchor.UpperCenter);
                Text(Mathf.Max(0, sim.Time).ToString("0.0"), W / 2, m + 14 * u, 26 * u, blink ? Palette.Warn : Palette.Ink, TextAnchor.UpperCenter, true);
                float pw = 170 * u, py = m + 48 * u, prog = Mathf.Clamp01(sim.Pz / sim.Sector.Length);
                Box(W / 2 - pw / 2, py, pw, 2 * u, Palette.Ink, 0.45f); Box(W / 2 - pw / 2, py - u, pw * prog, 4 * u, sim.Sector.Hue); Box(W / 2 - pw / 2 + pw * prog - u, py - 4 * u, 2 * u, 10 * u, Palette.Ink);
                Text("VEL " + Mathf.RoundToInt(sim.Speed * 12).ToString("000") + (sim.Boosting ? "  BOOST" : ""), W / 2, H - 22 * u, (sim.Boosting ? 14 : 12) * u, sim.Boosting ? Palette.Ink : Palette.Dim, TextAnchor.UpperCenter);
                for (int i = 0; i < Popups.Count; i++) { var p = Popups[i]; var c = Palette.Pick; c.a = Mathf.Clamp01(1 - p.T / 1.2f); Text(p.Text, W / 2, H * 0.3f - p.T * 34 * u, 18 * u, c, TextAnchor.MiddleCenter); }
            }
            DrawControls(sim, input, state);
            DrawOverlay(g, sim, input);
        }

        void DrawControls(FlightSim sim, InputReader input, GameState state)
        {
            if (!input.TouchUI || state != GameState.Play) return;
            float R = input.StickRadius; bool on = input.StickDown;
            var a = on ? Gui(input.StickAnchor) : new Vector2(112 * u, H - 104 * u);
            Ring(a, R, Palette.Void, 0.28f); Ring(a, R, Palette.Ink, on ? 0.35f : 0.18f);
            Ring(a + new Vector2(input.Stick.x, -input.Stick.y) * R, 22 * u, Palette.Ink, on ? 0.55f : 0.22f);
            if (!on) Text("STEER", a.x, a.y, 11 * u, Palette.Ink, TextAnchor.MiddleCenter);
            var f = Gui(input.FireCentre); Ring(f, input.FireRadius, input.FireDown && !sim.GunHot ? Palette.Ink : Palette.Void, 0.4f); Ring(f, input.FireRadius, Palette.Ink, 0.3f);
            Text(sim.GunHot ? "HOT" : "FIRE", f.x, f.y, 15 * u, Palette.Ink, TextAnchor.MiddleCenter);
            var b = Gui(input.BoostCentre); bool dry = sim.BoostFuel <= 0;
            Ring(b, input.BoostRadius, sim.Boosting ? Palette.Ink : Palette.Void, dry ? 0.2f : 0.4f); Ring(b, input.BoostRadius, Palette.Ink, 0.3f);
            Text(dry ? "EMPTY" : "BOOST", b.x, b.y, 12 * u, Palette.Ink, TextAnchor.MiddleCenter);
        }

        void DrawOverlay(GameDirector g, FlightSim sim, InputReader input)
        {
            var state = g.State; if (state == GameState.Play && !g.Paused) return;
            float cx = W / 2, cy = H / 2, big = Mathf.Min(44 * u, W * 0.06f); bool blink = (sim.Now * 1.6f) % 1 < 0.65f;
            string go = input.TouchUI ? "TAP" : "PRESS SPACE";
            Box(0, 0, W, H, Palette.Void, state == GameState.Title ? 0.4f : 0.55f);
            if (state == GameState.Play) { Text("PAUSED", cx, cy - 10 * u, big * 0.8f, Palette.Ink, TextAnchor.MiddleCenter, true); Text(go + " TO RESUME", cx, cy + 34 * u, 15 * u, Palette.Dim, TextAnchor.MiddleCenter); return; }
            if (state == GameState.Respawn)
            {
                Text(g.RespawnWhy == SimEventKind.ShipLost ? "SHIP LOST" : "OUT OF TIME", cx, cy - 44 * u, big * 0.75f, Palette.Warn, TextAnchor.MiddleCenter, true);
                Text(sim.Lives == 0 ? "LAST SHIP" : sim.Lives + (sim.Lives == 1 ? " SPARE SHIP LEFT" : " SPARE SHIPS LEFT"), cx, cy, 18 * u, Palette.Ink, TextAnchor.MiddleCenter);
                return;
            }
            if (state == GameState.Title)
            {
                Text("DOOMSDAY", cx, cy - 92 * u, big, Palette.Ink, TextAnchor.MiddleCenter, true);
                Text("TUNNELS 3D", cx, cy - 92 * u + big * 1.15f, big, sim.Sector.Hue, TextAnchor.MiddleCenter, true);
                Text("Reach the end of each sector before the countdown hits zero.", cx, cy + 6 * u, Mathf.Min(15 * u, W * 0.026f), Palette.Ink, TextAnchor.MiddleCenter);
                Text("RED WALLS: DODGE   YELLOW: SHOOT   WHITE CELLS: COLLECT", cx, cy + 36 * u, Mathf.Min(13 * u, W * 0.021f), Palette.Dim, TextAnchor.MiddleCenter);
                Text(input.TouchUI ? "LEFT THUMB STEERS  /  RIGHT THUMB FIRES  /  BOOST FOR SPEED" : "ARROWS OR WASD STEER  /  SPACE FIRES  /  SHIFT BOOSTS", cx, cy + 62 * u, Mathf.Min(12 * u, W * 0.02f), Palette.Dim, TextAnchor.MiddleCenter);
                if (blink) Text(go + " TO LAUNCH", cx, cy + 104 * u, 15 * u, Palette.Ink, TextAnchor.MiddleCenter, true);
                if (g.Best > 0) Text("BEST " + g.Best.ToString("000000"), cx, cy + 136 * u, 13 * u, Palette.Dim, TextAnchor.MiddleCenter);
                Text("MILESTONE 2 BUILD", W - 16 * u, H - 20 * u, 10 * u, Palette.Dim, TextAnchor.MiddleRight);
            }
            else if (state == GameState.Clear)
            {
                var b = g.Bonus;
                Text("SECTOR " + b.Sector.ToString("00") + " CLEAR", cx, cy - 84 * u, big * 0.75f, Palette.Ink, TextAnchor.MiddleCenter, true);
                Text("SECTOR TIME " + b.SectorTime.ToString("0.0"), cx, cy - 48 * u, 16 * u, Palette.Ink, TextAnchor.MiddleCenter);
                Text("TIME BONUS    +" + b.TimeBonus, cx, cy - 4 * u, 16 * u, Palette.Ink, TextAnchor.MiddleCenter);
                Text("SHIELD BONUS  +" + b.ShieldBonus, cx, cy + 18 * u, 16 * u, Palette.Ink, TextAnchor.MiddleCenter);
                Text(b.Ship ? "SHIP TOKEN: EXTRA SHIP  +1" : b.Token ? "SHIP TOKEN: SHIP BAY FULL" : "NO SHIP TOKEN, NO EXTRA SHIP", cx, cy + 40 * u, 16 * u, b.Ship ? Palette.Gate : Palette.Dim, TextAnchor.MiddleCenter);
                Text("SCORE " + ((int)sim.Score).ToString("000000"), cx, cy + 64 * u, 16 * u, sim.Sector.Hue, TextAnchor.MiddleCenter);
                if (blink) Text(go + " FOR SECTOR " + sim.Sector.N.ToString("00"), cx, cy + 102 * u, 14 * u, Palette.Ink, TextAnchor.MiddleCenter, true);
            }
            else
            {
                Text(g.DeadWhy, cx, cy - 70 * u, big * 0.8f, Palette.Warn, TextAnchor.MiddleCenter, true);
                Text("SECTOR " + sim.Sector.N.ToString("00") + "   SCORE " + ((int)sim.Score).ToString("000000"), cx, cy - 8 * u, 17 * u, Palette.Ink, TextAnchor.MiddleCenter);
                Text("BEST " + g.Best.ToString("000000"), cx, cy + 18 * u, 14 * u, Palette.Dim, TextAnchor.MiddleCenter);
                if (blink && g.StateT > 0.6f) Text(go + " TO RETRY", cx, cy + 74 * u, 15 * u, Palette.Ink, TextAnchor.MiddleCenter, true);
            }
        }

        public void Tick(float dt)
        {
            for (int i = Popups.Count - 1; i >= 0; i--) { var p = Popups[i]; p.T += dt; if (p.T > 1.2f) Popups.RemoveAt(i); else Popups[i] = p; }
            Flash = Mathf.Max(0, Flash - dt * 2.5f);
        }
    }
}
