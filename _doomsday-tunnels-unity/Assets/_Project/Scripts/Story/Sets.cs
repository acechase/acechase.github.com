using UnityEngine;

namespace DoomsdayTunnels.Story
{
    /// <summary>A film-poster title the HUD letters over the picture in chrome, since the canvas has no text.</summary>
    public struct ChromeTitle { public string Text; public float Cx, Cy, Size; }

    /// <summary>
    /// The sets: one painter per background, each given the time in the shot, who is speaking and whether they are
    /// mid-sentence, the shot and the line. Ported from the web game's PAINT table.
    /// </summary>
    public static class Sets
    {
        public static readonly string[] Names = { "card", "lair", "hq", "cockpit", "buddy", "launch", "device", "pod", "rental", "fondue", "catboss" };
        const int VW = PixelCanvas.VW;
        const float PI = Mathf.PI;
        static float R(float v) => Mathf.Floor(v + 0.5f);
        static float Rnd(float i) => Cast.Rnd(i);
        static Color32 H(string hex) => K.H(hex);

        static readonly Color32[] SkyBands = { K.Night, K.Navy, K.Indigo, K.Purple, K.Magenta, K.Pink, K.Orange }, MoonBands = { K.Black, K.Black, K.Night, K.Night, K.Navy, K.Indigo, K.Purple };
        static readonly Color32[] Sun = { K.Yellow, H("#ffb347"), K.Orange, K.Pink, K.Magenta }, Moon = { H("#ffe98a"), K.Yellow, K.Yellow, H("#f0c93a"), H("#e0b52e") };
        static readonly Color32[] Lamps3 = { K.Red, K.Yellow, K.Green }, Lamps4 = { K.Red, K.Yellow, K.Green, K.Cyan }, Lamps3b = { K.Red, K.Yellow, K.Cyan };
        static readonly Color32 Cheese = H("#c9a227"), CheeseHole = H("#a67c14"), LairWall = H("#1c0714"), LairBrick = H("#260a1c"), Frame = H("#0d0309"), MoonGlow = H("#e9e2ff"),
            Screen = H("#06121f"), LampOff = H("#3a0d1e"), Throne = H("#3a0d4d"), Cushion = H("#2a0b1e"), HqWall = H("#06202b"), HqDots = H("#0f6b7a"), Radar = H("#052b16"),
            HqFloor = H("#0a1319"), Console = H("#2a2f47"), CockpitFloor = H("#1a1d30"), TunnelA = H("#1c7f9c"), TunnelB = H("#125266"), TunnelEnd = H("#07222c"),
            DeviceWall = H("#140611"), DevicePipe = H("#221020"), DeviceFloor = H("#2a0b1e"), Readout = H("#1a0508"), DimRed = H("#7a1020"), PodShell = H("#7b3fe0"),
            RentalWall = H("#16525c"), RentalFloor = H("#5a3d2b"), FondueWall = H("#3a2c08"), FondueHole = H("#2a1f05"), FondueFloor = H("#241a04"), Bread = H("#e8c07a"),
            FondueHi = H("#fff3b0"), FondueLo = H("#f0c93a"), ButtonLit = H("#ff7a7a"), CatWall = H("#1a0b2e");

        public static void Paint(PixelCanvas g, string bg, float t, string who, bool talk, Shot sh, Line ln, out ChromeTitle title)
        {
            title = default; g.Alpha = 1; g.ClipNone();
            switch (bg)
            {
                case "card": Card(g, t, sh, out title); break;
                case "lair": Lair(g, t, who, talk, sh, ln); break;
                case "hq": Hq(g, t, who, talk); break;
                case "cockpit": Cockpit(g, t, who, talk); break;
                case "buddy": Buddy(g, t, who, talk); break;
                case "launch": Launch(g, t); break;
                case "device": Device(g, t, sh); break;
                case "pod": Pod(g, t); break;
                case "rental": Rental(g, t, who, talk); break;
                case "fondue": Fondue(g, t, sh); break;
                case "catboss": Catboss(g, t, who, talk); break;
                default: g.Bx(0, 0, VW, g.H, K.Night); break;
            }
        }

        /// <summary>Neon sunset over an endless grid.</summary>
        static void Card(PixelCanvas g, float t, Shot sh, out ChromeTitle title)
        {
            title = default; int VH = g.H; float hz = R(VH * 0.62f), cx = VW / 2f; var bands = sh.Moon ? MoonBands : SkyBands;
            for (int i = 0; i < bands.Length; i++) g.Bx(0, i * hz / bands.Length, VW, hz / bands.Length + 1, bands[i]);
            for (int i = 0; i < 44; i++) if ((t * 1.7f + i * 0.61f) % 3 > 0.5f) g.Bx(Rnd(i) * VW, Rnd(i + 50) * hz * 0.55f, 1, 1, K.White);
            float sr = hz * 0.6f;
            g.ClipCircle(cx, hz, sr, true);
            var sun = sh.Moon ? Moon : Sun;
            for (int i = 0; i < 5; i++) g.Bx(cx - sr, hz - sr + i * sr / 5, sr * 2, sr / 5 + 1, sun[i]);
            if (sh.Moon) for (int i = 0; i < 9; i++) g.Ov(cx - sr * 0.8f + Rnd(i + 3) * sr * 1.6f, hz - Rnd(i + 12) * sr * 0.85f, 2 + Rnd(i + 21) * 6, 1.5f + Rnd(i + 30) * 4, H("#c9981c"));   // a cheese Moon rising
            else for (int k = 0; k < 6; k++) { float yy = hz - ((k + (t * 0.35f) % 1) / 6f) * sr * 0.8f; g.Bx(cx - sr, yy, sr * 2, 1 + (5 - k) * 0.5f, K.Indigo); }
            g.ClipNone();
            g.Pg(K.Night, 0, hz, 0, hz - 10, 22, hz - 24, 40, hz - 9, 58, hz - 19, 86, hz); g.Pg(K.Night, VW, hz, VW, hz - 14, VW - 26, hz - 27, VW - 48, hz - 8, VW - 66, hz - 17, VW - 92, hz);
            g.Bx(0, hz, VW, VH - hz, K.Night);
            for (int i = -9; i <= 9; i++) g.Li(cx + i * 7, hz, cx + i * 52, VH, K.Magenta, 1);
            for (int j = 0; j < 7; j++) { float f = (j + (t * 0.9f) % 1) / 7f, yy = hz + (VH - hz) * f * f; g.Li(0, yy, VW, yy, K.Pink, 1); }
            g.Bx(0, hz, VW, 1, K.Cyan);
            if (sh.Title != null)
            {
                float z = Mathf.Min(1, t / 0.5f);
                title = new ChromeTitle { Text = sh.Title, Cx = cx, Cy = VH * 0.36f, Size = R(VH * 0.26f * (0.4f + 0.6f * z)) };
                if (t > 0.5f) { float s = (t * 0.9f) % 2; if (s < 0.4f) Cast.Star4(g, cx - 70 + Rnd(Mathf.Floor(t * 0.45f)) * 140, VH * 0.3f, 7 * (1 - Mathf.Abs(s - 0.2f) / 0.2f), K.White); }
            }
        }

        /// <summary>Baron Von Kaboom's regrettably damp headquarters.</summary>
        static void Lair(PixelCanvas g, float t, string who, bool talk, Shot sh, Line ln)
        {
            int VH = g.H; bool moon = sh.Moon; float ft = t % 3.7f; bool fl = !moon && (ft < 0.1f || (ft > 0.18f && ft < 0.25f)); float cx = VW / 2f;
            g.Bx(0, 0, VW, VH, moon ? Cheese : LairWall);           // the Moon base is carved out of cheese
            if (moon) for (int i = 0; i < 28; i++) g.Ov(Rnd(i) * VW, Rnd(i + 40) * VH, 3 + Rnd(i + 80) * 8, 2 + Rnd(i + 90) * 5, CheeseHole);
            else for (int r = 0; r < 10; r++) for (int c = 0; c < 9; c++) if (Rnd(r * 9 + c) > 0.45f) g.Bx(c * 40 - (r % 2) * 20, r * 18, 38, 16, LairBrick);
            float wx = 30, wy = VH * 0.2f, ww = 42, wh = VH * 0.46f; Color32 sky = fl ? K.White : moon ? K.Black : K.Indigo;       // window, with weather
            g.Ov(wx + ww / 2, wy, ww / 2 + 3, 13, Frame); g.Bx(wx - 3, wy, ww + 6, wh + 3, Frame);
            g.Ov(wx + ww / 2, wy, ww / 2, 10, sky); g.Bx(wx, wy, ww, wh, sky);
            if (fl) g.Pg(K.Yellow, wx + 27, wy - 6, wx + 14, wy + wh * 0.45f, wx + 22, wy + wh * 0.45f, wx + 10, wy + wh, wx + 31, wy + wh * 0.38f, wx + 23, wy + wh * 0.38f, wx + 33, wy - 6);
            else if (moon) Cast.EarthIcon(g, wx + 27, wy + 17, 9, t, false);
            else g.Ov(wx + 30, wy + 8, 6, 6, MoonGlow);
            g.Bx(wx + ww / 2 - 1, wy - 10, 2, wh + 10, Frame); g.Bx(wx, wy + wh * 0.45f, ww, 2, Frame);
            float mx = VW - 90, my = VH * 0.14f, mw = 62, mh = VH * 0.42f;                                  // scheming monitor
            if (!sh.Mother)
            {
                g.Bx(mx - 3, my - 3, mw + 6, mh + 6, K.DGrey); g.Bx(mx, my, mw, mh, Screen); Cast.EarthIcon(g, mx + mw / 2, my + mh / 2, mh * 0.34f, t);
                for (int i = 0; i < 6; i++) g.Bx(mx + 2 + i * 7, my + mh + 6, 4, 2, (t * 3 + i * 0.4f) % 1 < 0.5f ? Lamps3[i % 3] : LampOff);
            }
            g.Pg(Throne, cx - 30, VH, cx - 26, VH - 92, cx - 18, VH - 100, cx - 10, VH - 92, cx, VH - 104, cx + 10, VH - 92, cx + 18, VH - 100, cx + 26, VH - 92, cx + 30, VH);
            if (!sh.NoBaron) Cast.Baron(g, cx, VH, t, who == "baron" && talk, who == "baron" && ln.Fx == "laugh");
            if (sh.Mother) Cast.Mother(g, cx + 90, VH, t, who == "mother" && talk);
            g.Bx(cx - 90, VH - 15, 46, 15, Cushion); g.Ov(cx - 67, VH - 16, 21, 4, K.Pink);
            Cast.Cat(g, cx - 65, VH - 18, t, who == "cat" && talk);
            if (fl) { g.Alpha = 0.22f; g.Bx(0, 0, VW, VH, K.White); g.Alpha = 1; }
        }

        /// <summary>Earth Defence Command, budget permitting.</summary>
        static void Hq(PixelCanvas g, float t, string who, bool talk)
        {
            int VH = g.H;
            g.Bx(0, 0, VW, VH, HqWall);
            for (int y = 6; y < VH - 26; y += 5) for (int x = 4; x < VW; x += 5)
                if (Mathf.Sin(x * 0.045f + 1) * Mathf.Cos(y * 0.09f) + Mathf.Sin(x * 0.11f) * 0.4f > 0.25f) g.Bx(x, y, 2, 2, HqDots);
            for (int i = 0; i < 10; i++) if ((t * 1.5f + i * 0.37f) % 1 < 0.5f) g.Bx(16 + Rnd(i) * (VW - 32), 8 + Rnd(i + 3) * (VH - 46), 2, 2, i % 3 != 0 ? K.Yellow : K.Red);
            float qx = 46, qy = VH * 0.42f, rr = Mathf.Min(26, VH * 0.25f);                                  // radar
            g.Ov(qx, qy, rr + 3, rr + 3, K.DGrey); g.Ov(qx, qy, rr, rr, Radar);
            g.Arc(qx, qy, rr * 0.55f, 0, PixelCanvas.TAU, K.Green, 1);
            g.Li(qx, qy, qx + Mathf.Cos(t * 2.5f) * rr, qy + Mathf.Sin(t * 2.5f) * rr, K.Green, 1);
            if ((t * 2.5f) % 6.283f < 2.4f) g.Bx(qx + rr * 0.45f, qy + rr * 0.25f, 2, 2, K.Green);
            float mx = VW - 86, my = VH * 0.13f, mw = 60, mh = VH * 0.44f;
            g.Bx(mx - 3, my - 3, mw + 6, mh + 6, K.DGrey); g.Bx(mx, my, mw, mh, Screen); Cast.EarthIcon(g, mx + mw / 2, my + mh / 2, mh * 0.34f, t);
            Cast.General(g, VW / 2f, VH, t, who == "general" && talk);
            g.Bx(0, VH - 9, VW, 9, HqFloor);
            g.Bx(VW / 2f + 46, VH - 15, 16, 7, K.Red); g.Bx(VW / 2f + 44, VH - 18, 20, 4, K.Red);                // the red phone
            if ((t * 3) % 1 < 0.5f) g.Bx(VW / 2f + 53, VH - 12, 3, 2, K.Yellow);
        }

        static void Cockpit(PixelCanvas g, float t, string who, bool talk)
        {
            int VH = g.H; float cx = VW / 2f, cy = VH * 0.4f;
            g.Bx(0, 0, VW, VH, K.Night);
            for (int i = 0; i < 28; i++)                                                                      // tunnel lights streaking past
            {
                float a = Rnd(i) * 6.283f, f = (t * (0.5f + Rnd(i + 7)) + Rnd(i + 3)) % 1, d = 8 + f * f * 190, e = d + 4 + f * 24;
                g.Li(cx + Mathf.Cos(a) * d, cy + Mathf.Sin(a) * d * 0.7f, cx + Mathf.Cos(a) * e, cy + Mathf.Sin(a) * e * 0.7f, i % 4 != 0 ? K.Cyan : K.Pink, 1);
            }
            g.Pg(K.DGrey, 0, 0, 28, 0, 9, VH, 0, VH); g.Pg(K.DGrey, VW, 0, VW - 28, 0, VW - 9, VH, VW, VH);
            g.Pg(Console, cx - 54, VH, cx - 48, VH - 72, cx - 2, VH - 72, cx + 4, VH);
            Cast.Ace(g, cx - 25, VH, t, who == "ace" && talk);
            float kx = cx + 44, ky = VH - 54;                                                                 // console, with B.U.D.D.Y.
            g.Pg(Console, kx - 10, VH, kx, ky - 6, kx + 86, ky - 6, kx + 96, VH);
            g.Bx(kx + 4, ky - 2, 48, 34, K.Black); Cast.BuddyFace(g, kx + 7, ky + 1, 42, 28, t, who == "buddy" && talk);
            for (int i = 0; i < 8; i++) g.Bx(kx + 58 + (i % 4) * 7, ky + 2 + (i / 4) * 8, 4, 4, (t * 2 + i * 0.3f) % 1 < 0.5f ? Lamps4[i % 4] : K.DGrey);
            g.Bx(0, VH - 7, VW, 7, CockpitFloor);
        }

        /// <summary>Close on the ship's computer.</summary>
        static void Buddy(PixelCanvas g, float t, string who, bool talk)
        {
            int VH = g.H; float cx = VW / 2f, w = 150, h = VH * 0.62f, y = VH * 0.14f;
            g.Bx(0, 0, VW, VH, CockpitFloor);
            for (int i = 0; i < 12; i++) g.Bx(14 + (i % 2) * 12, 12 + (i / 2) * 12, 6, 6, (t * 2 + i * 0.27f) % 1 < 0.5f ? Lamps3b[i % 3] : K.DGrey);
            for (int i = 0; i < 5; i++) { g.Bx(VW - 40, 14 + i * 14, 26, 8, K.Black); g.Bx(VW - 39, 15 + i * 14, 24 * (0.5f + 0.5f * Mathf.Sin(t * (2 + i) + i)), 6, K.Green); }
            g.Bx(cx - w / 2 - 8, y - 8, w + 16, h + 16, K.Steel); g.Bx(cx - w / 2 - 4, y - 4, w + 8, h + 8, K.DGrey);
            Cast.BuddyFace(g, cx - w / 2, y, w, h, t, who == "buddy" && talk);
            g.Bx(cx - 20, y + h + 8, 40, VH, K.DGrey);
            if ((t * 2) % 1 < 0.5f) g.Bx(cx + w / 2 - 8, y + h + 2, 4, 3, K.Red);
        }

        /// <summary>Into the tunnels.</summary>
        static void Launch(PixelCanvas g, float t)
        {
            int VH = g.H; float cx = VW / 2f, cy = VH * 0.46f, ph = t * 2.2f; int bse = Mathf.FloorToInt(ph);
            g.Bx(0, 0, VW, VH, K.Night);
            for (int i = 10; i >= 0; i--)
            {
                float f = (i + ph % 1) / 10f, s = f * f;
                g.Bx(cx - (14 + s * 340) / 2, cy - (9 + s * 220) / 2, 14 + s * 340, 9 + s * 220, ((i - bse) % 2 + 2) % 2 != 0 ? TunnelA : TunnelB);
            }
            g.Bx(cx - 7, cy - 4, 14, 9, TunnelEnd);
            for (int i = 0; i < 18; i++) { float a = Rnd(i) * 6.283f, f = (t * 1.7f + Rnd(i + 4)) % 1, d = 26 + f * 170; g.Li(cx + Mathf.Cos(a) * d, cy + Mathf.Sin(a) * d * 0.66f, cx + Mathf.Cos(a) * (d + 14), cy + Mathf.Sin(a) * (d + 14) * 0.66f, K.White, 1); }
            float p = Mathf.Min(1, t / 2.8f), e = 1 - (1 - p) * (1 - p);
            Cast.Ship(g, cx + Mathf.Sin(t * 5) * 4 * (1 - e), VH * 0.86f - e * (VH * 0.86f - cy), 2.6f - e * 2.1f, t);
        }

        /// <summary>The Doomsday Device, in all its tasteless glory.</summary>
        static void Device(PixelCanvas g, float t, Shot sh)
        {
            int VH = g.H; float cx = VW / 2f, by = VH - 6; bool cut = sh.Cut; float wy = by - 40;
            g.Bx(0, 0, VW, VH, DeviceWall);
            for (int i = 0; i < 8; i++) g.Bx(i * 46 - 8, 0, 6, VH, DevicePipe);
            g.Bx(0, VH - 6, VW, 6, DeviceFloor);
            for (int s = -1; s <= 1; s += 2)
            {
                float tx = cx + s * 118; g.Bx(tx - 3, by - 60, 6, 60, K.DGrey); g.Ov(tx, by - 64, 8, 6, K.Steel);
                if (!cut && (t * 9 + (s > 0 ? 0.5f : 0)) % 1 < 0.5f) { float j = by - 76 + Rnd(Mathf.Floor(t * 9) + s) * 20; g.Li(tx, by - 64, tx - s * 26, j, K.Cyan, 1); g.Li(tx - s * 26, j, tx - s * 52, by - 62, K.Cyan, 1); g.Li(tx - s * 52, by - 62, cx + s * 34, by - 56, K.White, 1); }
            }
            for (int s = -1; s <= 1; s += 2)
            {
                Color32 c = s < 0 ? K.Red : K.Sea;
                if (cut) { g.Li(cx + s * 30, wy, cx + s * 52, wy + 14, c, 3); g.Li(cx + s * 72, wy + 18, cx + s * 98, wy + 2, c, 3); if ((t * 12) % 1 < 0.5f) Cast.Star4(g, cx + s * 54, wy + 15, 4, K.Yellow); }
                else g.QuadCurve(cx + s * 30, wy, cx + s * 62, wy + 28, cx + s * 98, wy + 2, c, 3);
                g.Bx(cx + s * 98 - 3, wy - 5, 6, 13, K.DGrey);
            }
            g.Bx(cx - 22, by - 9, 44, 9, K.DGrey);
            g.Ov(cx, by - 44, 36, 36, K.Black); g.Ov(cx - 14, by - 62, 9, 5, K.DGrey);
            g.Bx(cx - 6, by - 86, 12, 8, K.DGrey);
            if (!cut) { g.Li(cx, by - 86, cx + 9, by - 95, K.Brown, 2); Cast.Star4(g, cx + 10, by - 96, 3 + ((t * 14) % 1) * 3, (t * 14) % 1 < 0.5f ? K.Yellow : K.Orange); }
            g.Bx(cx - 25, by - 55, 50, 20, Readout);
            Color32 col = cut ? K.Green : (t * 2) % 1 < 0.5f ? K.Red : DimRed; int n = cut ? -1 : Mathf.Max(1, 7 - Mathf.FloorToInt(t * 0.5f));
            Cast.Digit7(g, cx - 21, by - 52, cut ? -1 : 0, col); Cast.Digit7(g, cx - 12, by - 52, cut ? -1 : 0, col);
            g.Bx(cx - 1, by - 49, 2, 2, col); g.Bx(cx - 1, by - 43, 2, 2, col);
            Cast.Digit7(g, cx + 5, by - 52, cut ? -1 : 0, col); Cast.Digit7(g, cx + 14, by - 52, n, col);
            if (cut && t < 0.5f) { g.Alpha = 1 - t / 0.5f; g.Bx(0, 0, VW, VH, K.White); g.Alpha = 1; }
        }

        /// <summary>One escape pod, slightly singed.</summary>
        static void Pod(PixelCanvas g, float t)
        {
            int VH = g.H;
            g.Bx(0, 0, VW, VH, K.Night);
            for (int i = 0; i < 60; i++) g.Bx(((Rnd(i) * VW - t * (6 + Rnd(i + 2) * 30)) % VW + VW) % VW, Rnd(i + 20) * VH, 1, 1, i % 5 != 0 ? K.White : K.Cyan);
            Cast.EarthIcon(g, 48, VH * 0.72f, VH * 0.3f, t, false);
            float px = VW * 0.6f + Mathf.Sin(t * 1.3f) * 10, py = VH * 0.44f + Mathf.Sin(t * 2.1f) * 6, f = (t * 20) % 1;
            g.Pg(K.Orange, px - 30, py - 6, px - 46 - f * 12, py, px - 30, py + 6); g.Pg(K.Yellow, px - 30, py - 3, px - 38 - f * 5, py, px - 30, py + 3);
            g.Ov(px, py, 32, 18, K.Purple); g.Ov(px, py - 5, 28, 11, PodShell); g.Bx(px - 31, py + 4, 62, 3, K.Yellow);
            g.Ov(px + 9, py - 3, 10, 9, K.Glass); g.Ov(px - 14, py - 2, 6, 6, K.Glass);
            g.Ov(px + 9, py - 1, 5, 6, K.Skin); g.Bx(px + 4, py - 8, 10, 3, K.Black); g.Bx(px + 5, py + 1, 8, 2, K.Black); g.Bx(px + 10, py - 3, 2, 2, K.Yellow);
            g.Ov(px - 14, py - 1, 4, 3.5f, K.Cat); g.Pg(K.Cat, px - 18, py - 3, px - 17, py - 7, px - 14, py - 4); g.Pg(K.Cat, px - 13, py - 4, px - 11, py - 7, px - 10, py - 3);
            float fy = py - 24 + Mathf.Sin(t * 16) * 3; g.Li(px + 19, fy + 6, px + 15, py - 13, K.Navy, 3); g.Bx(px + 16, fy, 6, 6, K.Purple);
        }

        /// <summary>Prestige Rocket Rental, customer care.</summary>
        static void Rental(PixelCanvas g, float t, string who, bool talk)
        {
            int VH = g.H; float cx = VW / 2f;
            g.Bx(0, 0, VW, VH, RentalWall);
            for (int i = 0; i < 30; i++)                     // tasteful wall confetti
            {
                float x = Rnd(i) * VW, y = Rnd(i + 30) * VH * 0.85f; int k = i % 4;
                if (k == 0) g.Pg(K.Pink, x, y, x + 6, y + 3, x, y + 6); else if (k == 1) g.Bx(x, y, 5, 2, K.Yellow); else if (k == 2) g.Ov(x, y, 2, 2, K.Cyan); else g.Li(x, y, x + 5, y + 4, K.White, 1);
            }
            float px = 30, py = VH * 0.12f, ph = VH * 0.52f;   // poster of the fleet
            g.Bx(px - 2, py - 2, 48, ph + 4, K.White); g.Bx(px, py, 44, ph, K.Navy); Cast.Ship(g, px + 22, py + ph * 0.5f, 0.9f, t);
            for (int i = 0; i < 5; i++) if ((t + i * 0.4f) % 2 > 0.3f) g.Bx(px + 4 + Rnd(i + 60) * 36, py + 3 + Rnd(i + 70) * (ph - 8), 1, 1, K.White);
            float sx = VW - 54, sy = VH * 0.32f; var pts = new float[40];    // today's offer
            for (int i = 0; i < 20; i++) { float a = i * PI / 10 + t * 0.4f, r = i % 2 != 0 ? 15 : 23; pts[2 * i] = sx + Mathf.Cos(a) * r; pts[2 * i + 1] = sy + Mathf.Sin(a) * r; }
            g.Pg(K.Yellow, pts); Cast.Digit7(g, sx - 4, sy - 7, 9, K.Red); g.Bx(sx - 10, sy - 5, 4, 1, K.Red); g.Bx(sx - 9, sy - 7, 2, 12, K.Red); g.Bx(sx - 11, sy - 2, 5, 1, K.Red); g.Bx(sx - 10, sy + 2, 4, 1, K.Red);
            Cast.Debbie(g, cx, VH, t, who == "debbie" && talk);
            g.Bx(0, VH - 9, VW, 9, RentalFloor);
            float kx = cx + 54;                              // terminal
            g.Bx(kx, VH - 36, 36, 28, K.Grey); g.Bx(kx + 3, VH - 33, 30, 19, Radar);
            for (int i = 0; i < 4; i++) g.Bx(kx + 5, VH - 31 + i * 4, 8 + ((i * 7 + Mathf.FloorToInt(t * 3)) % 16), 1, K.Green);
            g.Bx(cx - 82, VH - 21, 12, 12, K.Orange);          // office fern
            for (int i = 0; i < 6; i++) g.Li(cx - 76, VH - 21, cx - 76 + (i - 2.5f) * 6, VH - 36 - (i % 2) * 6 + Mathf.Sin(t + i) * 1, K.Land, 2);
            if ((t * 2) % 1 < 0.5f) g.Bx(cx - 50, VH - 13, 3, 2, K.Red);
        }

        /// <summary>The spare Doomsday Device: regrettably, a fondue set.</summary>
        static void Fondue(PixelCanvas g, float t, Shot sh)
        {
            int VH = g.H; float cx = VW / 2f, by = VH - 6; bool calm = sh.Dot;
            g.Bx(0, 0, VW, VH, FondueWall);
            for (int i = 0; i < 30; i++) g.Ov(Rnd(i) * VW, Rnd(i + 40) * VH, 3 + Rnd(i + 80) * 9, 2 + Rnd(i + 90) * 6, FondueHole);
            g.Bx(0, VH - 6, VW, 6, FondueFloor);
            for (int s = -1; s <= 1; s += 2)
            {
                float tx = cx + s * 126; g.Bx(tx - 3, by - 60, 6, 60, K.DGrey); g.Ov(tx, by - 64, 8, 6, K.Steel);
                if (!calm && (t * 9 + (s > 0 ? 0.5f : 0)) % 1 < 0.5f) { float j = by - 78 + Rnd(Mathf.Floor(t * 9) + s) * 22; g.Li(tx, by - 64, tx - s * 32, j, K.Cyan, 1); g.Li(tx - s * 32, j, cx + s * 46, by - 52, K.White, 1); }
            }
            for (int i = -2; i <= 2; i++) { float f = (t * 9 + i * 0.37f + 9) % 1; g.Pg(i % 2 != 0 ? K.Orange : K.Yellow, cx + i * 11 - 4, by - 8, cx + i * 11 + 4, by - 8, cx + i * 11, by - 14 - f * 7); }
            g.Bx(cx - 34, by - 8, 68, 8, K.DGrey);
            g.Pg(K.Black, cx - 44, by - 54, cx + 44, by - 54, cx + 36, by - 14, cx - 36, by - 14);                        // the pot
            g.Bx(cx - 56, by - 50, 12, 4, K.DGrey); g.Bx(cx + 44, by - 50, 12, 4, K.DGrey);
            g.Ov(cx, by - 56, 46, 7, K.DGrey); g.Ov(cx, by - 57, 43, 6, K.Yellow);
            for (int i = 0; i < 6; i++) { float f = (t * (0.7f + Rnd(i) * 0.6f) + Rnd(i + 5)) % 1; g.Ov(cx - 34 + Rnd(i + 9) * 68, by - 57 - f * 3, 1 + f * 4, 1 + f * 2.5f, f > 0.8f ? FondueHi : FondueLo); }
            g.Li(cx + 22, by - 60, cx + 48, by - 99, K.Steel, 2); g.Bx(cx + 17, by - 64, 8, 7, Bread);                    // one optimistic cube of bread
            g.Bx(cx - 25, by - 44, 50, 20, Readout);
            Color32 col = calm ? K.Green : (t * 2) % 1 < 0.5f ? K.Red : DimRed; int n = calm ? -1 : Mathf.Max(1, 7 - Mathf.FloorToInt(t * 0.5f)), z = calm ? -1 : 0;
            Cast.Digit7(g, cx - 21, by - 41, z, col); Cast.Digit7(g, cx - 12, by - 41, z, col); g.Bx(cx - 1, by - 38, 2, 2, col); g.Bx(cx - 1, by - 32, 2, 2, col); Cast.Digit7(g, cx + 5, by - 41, z, col); Cast.Digit7(g, cx + 14, by - 41, n, col);
            float px = cx - 94;                              // The Button, and what is left of its label
            g.Bx(px - 9, by - 30, 18, 30, K.DGrey); g.Bx(px - 12, by - 34, 24, 5, K.Steel); g.Ov(px, by - 37, 8, 5, !calm && (t * 3) % 1 < 0.5f ? ButtonLit : K.Red);
            g.Pg(K.White, px - 15, by - 64, px + 10, by - 64, px + 15, by - 57, px + 5, by - 52, px - 15, by - 50); g.Bx(px - 12, by - 61, 15, 2, K.Black); g.Bx(px - 12, by - 56, 10, 2, K.Black);
            if (calm)                                        // one small red dot, one large distraction
            {
                float dx = cx + Mathf.Sin(t * 2.3f) * 120, dy = by - 4 - Mathf.Abs(Mathf.Sin(t * 3.1f)) * 26, kx = cx + Mathf.Sin(t * 2.3f - 0.45f) * 120;
                g.Alpha = 0.4f; g.Ov(dx, dy, 4, 4, K.Red); g.Alpha = 1; g.Ov(dx, dy, 2, 2, K.Red);
                g.Save(); g.Translate(kx, by + 4 - Mathf.Abs(Mathf.Sin(t * 9)) * 3); if (dx > kx) g.Scale(-1, 1); Cast.Cat(g, 0, 0, t * 2, false); g.Restore();
            }
        }

        /// <summary>The real brains of the operation.</summary>
        static void Catboss(PixelCanvas g, float t, string who, bool talk)
        {
            int VH = g.H; float cx = VW / 2f;
            g.Bx(0, 0, VW, VH, CatWall);
            for (int i = 0; i < 4; i++)                      // every screen is showing fish
            {
                float mx = 14 + i * 76 + (i > 1 ? 18 : 0), my = VH * 0.1f + (i % 2) * 7; g.Bx(mx - 2, my - 2, 50, 31, K.DGrey); g.Bx(mx, my, 46, 27, Screen);
                float fx = mx + 12 + ((t * 14 + i * 13) % 26), fy = my + 13 + Mathf.Sin(t * 3 + i) * 3;
                g.Ov(fx, fy, 6, 3, K.Cyan); g.Pg(K.Cyan, fx - 5, fy, fx - 11, fy - 4, fx - 11, fy + 4); g.Bx(fx + 3, fy - 1, 1, 1, K.Black);
            }
            g.Pg(Throne, cx - 46, VH, cx - 42, VH - 96, cx - 30, VH - 104, cx - 16, VH - 96, cx, VH - 106, cx + 16, VH - 96, cx + 30, VH - 104, cx + 42, VH - 96, cx + 46, VH);
            g.Pg(K.DkRed, cx - 34, VH - 12, cx - 46, VH - 72, cx - 6, VH - 40); g.Pg(K.DkRed, cx + 38, VH - 12, cx + 52, VH - 68, cx + 16, VH - 40);   // small cape, large collar
            g.Bx(cx - 62, VH - 12, 124, 12, Cushion); g.Ov(cx + 4, VH - 13, 48, 6, K.Pink);
            g.Save(); g.Translate(cx + 16, VH - 14); g.Scale(3, 3); Cast.Cat(g, 0, 0, t, who == "cat" && talk);
            g.Arc(-5.5f, -13.5f, 2.7f, 0, PixelCanvas.TAU, K.Yellow, 0.6f); g.Li(-3.4f, -11.6f, -1, -5, K.Yellow, 0.5f);   // monocle, naturally
            g.Restore();
            g.Bx(cx - 92, VH - 14, 22, 5, K.Steel); g.Ov(cx - 81, VH - 17, 9, 5, (t * 3) % 1 < 0.5f ? ButtonLit : K.Red);    // a paw-sized button
        }
    }
}
