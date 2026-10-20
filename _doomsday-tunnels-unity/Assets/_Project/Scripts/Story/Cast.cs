using UnityEngine;

namespace DoomsdayTunnels.Story
{
    /// <summary>The cut scenes' colours, by name, as the web game has them.</summary>
    public static class K
    {
        public static Color32 H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); c.a = 1; return c; }
        public static readonly Color32 Night = H("#0b0820"), Navy = H("#1b1147"), Indigo = H("#3a1578"), Purple = H("#5a1fb0"), Magenta = H("#c42bd0"), Pink = H("#ff3ea5"), Orange = H("#ff8a3c"),
            Yellow = H("#ffe14a"), Cyan = H("#35d9ff"), White = H("#f4f7ff"), Black = H("#05040a"), Red = H("#e8263c"), DkRed = H("#5a0f24"), Skin = H("#f2b38a"), SkinS = H("#c98562"),
            Grey = H("#8b93a8"), DGrey = H("#3b4058"), Steel = H("#a9b6d3"), Olive = H("#5f7a3a"), DOlive = H("#3a4d26"), Green = H("#4dffa6"), Brown = H("#6b3b1e"),
            Cat = H("#f29a2e"), CatS = H("#b9651a"), Sea = H("#2b6cff"), Land = H("#3fbf5a"), Glass = H("#bfe9ff");
    }

    /// <summary>The characters and props, painted from primitives. Each one is drawn standing on a baseline `by`.</summary>
    public static class Cast
    {
        const float PI = Mathf.PI, TAU = PixelCanvas.TAU;
        static float R(float v) => Mathf.Floor(v + 0.5f);
        /// <summary>A fixed pseudo-random number for index i, the same on every frame, so scattered details hold still.</summary>
        public static float Rnd(float i) { double x = System.Math.Sin(i * 127.1 + 311.7) * 43758.5453; return (float)(x - System.Math.Floor(x)); }
        /// <summary>Mouth movement while talking.</summary>
        public static float Flap(float t, float n) => R(Mathf.Abs(Mathf.Sin(t * 13)) * n);

        static readonly Color32 BuddyScreen = K.H("#052b16"), EarthRim = K.H("#16306e");
        static readonly Color32[] Medals = { K.Red, K.Yellow, K.Cyan, K.Pink, K.White, K.Orange };
        static readonly int[,] Perm = { { -14, 2, 9 }, { 14, 2, 9 }, { -10, -8, 10 }, { 10, -8, 10 }, { 0, -13, 11 }, { -17, 12, 7 }, { 17, 12, 7 }, { -15, 22, 6 }, { 15, 22, 6 } };
        static readonly string[] Seg7 = { "abcdef", "bc", "abged", "abgcd", "fgbc", "afgcd", "afgedc", "abc", "abcdefg", "abcdfg" };

        public static void Star4(PixelCanvas g, float x, float y, float r, Color32 c)
            => g.Pg(c, x, y - r, x + r * 0.25f, y - r * 0.25f, x + r, y, x + r * 0.25f, y + r * 0.25f, x, y + r, x - r * 0.25f, y + r * 0.25f, x - r, y, x - r * 0.25f, y - r * 0.25f);

        public static void Digit7(PixelCanvas g, float x, float y, int d, Color32 c)
        {
            string s = d < 0 ? "g" : Seg7[d]; bool On(char k) => s.IndexOf(k) >= 0;
            if (On('a')) g.Bx(x + 1, y, 5, 2, c); if (On('g')) g.Bx(x + 1, y + 6, 5, 2, c); if (On('d')) g.Bx(x + 1, y + 12, 5, 2, c);
            if (On('f')) g.Bx(x, y + 1, 2, 6, c); if (On('b')) g.Bx(x + 5, y + 1, 2, 6, c); if (On('e')) g.Bx(x, y + 7, 2, 6, c); if (On('c')) g.Bx(x + 5, y + 7, 2, 6, c);
        }

        public static void EarthIcon(PixelCanvas g, float x, float y, float r, float t, bool armed = true)
        {
            g.Ov(x, y, r + 2, r + 2, EarthRim); g.Ov(x, y, r, r, K.Sea);
            g.ClipCircle(x, y, r);
            float o = (t * 4) % (r * 4);
            for (int i = 0; i < 6; i++) g.Ov(x - r * 2 + ((i * r * 0.9f + o) % (r * 4)), y + (Rnd(i) - 0.5f) * r * 1.4f, r * (0.25f + Rnd(i + 9) * 0.3f), r * (0.18f + Rnd(i + 5) * 0.25f), K.Land);
            g.Alpha = 0.35f; g.Ov(x + r * 0.5f, y + r * 0.35f, r, r, K.Night); g.Alpha = 1;
            g.ClipNone();
            if (!armed) return;
            float p = (t * 1.5f) % 1; g.Alpha = 1 - p; g.Arc(x, y, 2 + p * r * 0.8f, 0, TAU, K.Red, 1); g.Alpha = 1;
            if ((t * 4) % 1 < 0.6f) g.Bx(x - 1, y - 1, 3, 3, K.Red);
        }

        public static void Baron(PixelCanvas g, float x, float by, float t, bool talk, bool laugh)
        {
            float y = by - 78 + (laugh ? R(Mathf.Sin(t * 18) * 1.5f) : 0);
            g.Pg(K.Navy, x - 34, by, x - 26, y + 34, x - 14, y + 26, x + 14, y + 26, x + 26, y + 34, x + 34, by);                 // cape
            g.Pg(K.DkRed, x - 20, y + 30, x - 27, y + 3, x - 8, y + 24); g.Pg(K.DkRed, x + 20, y + 30, x + 27, y + 3, x + 8, y + 24);  // collar
            g.Pg(K.White, x - 10, y + 26, x, y + 40, x + 10, y + 26); g.Bx(x - 2, y + 30, 4, 4, K.Red);
            g.Ov(x, y + 12, 13, 15, K.Skin);
            g.Pg(K.Black, x - 13, y + 8, x - 12, y - 2, x - 4, y - 5, x, y + 2, x + 4, y - 5, x + 12, y - 2, x + 13, y + 8, x + 8, y + 1, x, y + 5, x - 8, y + 1);  // widow's peak
            g.Li(x - 10, y + 6, x - 3, y + 10, K.Black, 2); g.Li(x + 10, y + 6, x + 3, y + 10, K.Black, 2);                          // scheming eyebrows
            g.Bx(x - 7, y + 11, 3, 2, K.White); g.Bx(x + 4, y + 11, 3, 2, K.White); g.Bx(x - 6, y + 11, 1, 2, K.Black); g.Bx(x + 5, y + 11, 1, 2, K.Black);
            g.Arc(x + 5.5f, y + 12, 4, 0, TAU, K.Yellow, 1); g.Li(x + 9, y + 14, x + 12, y + 27, K.Yellow, 1);                     // monocle
            if ((t * 1.1f) % 3 < 0.18f) Star4(g, x + 4, y + 10, 3, K.White);
            g.Bx(x - 1, y + 13, 2, 4, K.SkinS);
            g.Pg(K.Black, x, y + 18, x - 9, y + 17, x - 13, y + 12, x - 10, y + 20, x, y + 21, x + 10, y + 20, x + 13, y + 12, x + 9, y + 17);  // moustache
            g.Bx(x - 3, y + 22, 6, talk || laugh ? 2 + Flap(t, 3) : 1, K.DkRed);
            g.Pg(K.Black, x - 2, y + 26, x + 2, y + 26, x, y + 31);
            float fy = y + 20 + R(Mathf.Sin(t * (laugh ? 20 : 3)) * 2);                                                            // shaking fist
            g.Pg(K.Navy, x + 24, by, x + 30, fy + 8, x + 38, fy + 8, x + 36, by); g.Bx(x + 30, fy, 8, 8, K.Purple);
        }

        public static void Cat(PixelCanvas g, float x, float by, float t, bool talk)
        {
            float tail = Mathf.Sin(t * 3) * 4;
            g.Pg(K.CatS, x + 10, by - 2, x + 20, by - 4, x + 24 + tail, by - 15, x + 21 + tail, by - 16, x + 17, by - 7, x + 10, by - 5);
            g.Ov(x, by - 6, 13, 7, K.Cat); g.Bx(x - 2, by - 12, 2, 4, K.CatS); g.Bx(x + 3, by - 12, 2, 4, K.CatS);
            g.Ov(x - 9, by - 13, 7, 6, K.Cat);
            g.Pg(K.Cat, x - 16, by - 15, x - 15, by - 23, x - 10, by - 18); g.Pg(K.Cat, x - 8, by - 18, x - 4, by - 23, x - 2, by - 15);
            if (t % 4 < 0.15f) { g.Bx(x - 13, by - 13, 3, 1, K.Black); g.Bx(x - 7, by - 13, 3, 1, K.Black); }
            else { g.Bx(x - 13, by - 15, 3, 3, K.Green); g.Bx(x - 7, by - 15, 3, 3, K.Green); g.Bx(x - 12, by - 15, 1, 3, K.Black); g.Bx(x - 6, by - 15, 1, 3, K.Black); }
            g.Bx(x - 10, by - 11, 1, 1, K.Pink);
            if (talk) g.Bx(x - 11, by - 10, 3, 1 + Flap(t, 2), K.DkRed);
            g.Li(x - 13, by - 10, x - 20, by - 11, K.White, 1); g.Li(x - 6, by - 10, x + 1, by - 11, K.White, 1);
        }

        public static void General(PixelCanvas g, float x, float by, float t, bool talk)
        {
            float y = by - 76;
            g.Pg(K.Olive, x - 38, by, x - 34, y + 38, x - 12, y + 32, x + 12, y + 32, x + 34, y + 38, x + 38, by);
            g.Bx(x - 37, y + 36, 11, 4, K.Yellow); g.Bx(x + 26, y + 36, 11, 4, K.Yellow);
            g.Pg(K.White, x - 9, y + 32, x, y + 45, x + 9, y + 32); g.Bx(x - 1, y + 36, 3, 12, K.Black);
            for (int i = 0; i < 6; i++) g.Bx(x + 13 + (i % 3) * 5, y + 44 + (i / 3) * 4, 4, 3, Medals[i]);
            g.Bx(x - 7, y + 26, 14, 8, K.SkinS);
            g.Pg(K.Skin, x - 13, y, x + 13, y, x + 14, y + 18, x + 9, y + 28, x - 9, y + 28, x - 14, y + 18);                    // a jaw you could park a jeep on
            g.Bx(x - 14, y - 5, 28, 7, K.Grey); g.Bx(x - 14, y + 2, 3, 7, K.Grey); g.Bx(x + 11, y + 2, 3, 7, K.Grey);
            g.Bx(x - 12, y + 5, 10, 2, K.DGrey); g.Bx(x + 2, y + 5, 10, 2, K.DGrey);
            g.Bx(x - 12, y + 8, 10, 6, K.Black); g.Bx(x + 2, y + 8, 10, 6, K.Black); g.Bx(x - 2, y + 9, 4, 1, K.Black);           // aviators
            float gl = (t * 16) % 44; if (gl < 7) { g.Bx(x - 11 + gl, y + 9, 2, 1, K.Cyan); g.Bx(x + 3 + gl, y + 9, 2, 1, K.Cyan); }
            g.Bx(x - 1, y + 13, 3, 5, K.SkinS); g.Bx(x - 8, y + 19, 16, 3, K.Grey);
            g.Bx(x - 4, y + 23, 8, talk ? 1 + Flap(t, 3) : 1, talk ? K.DkRed : K.Brown);
            g.Bx(x, y + 26, 1, 2, K.SkinS);
        }

        public static void Ace(PixelCanvas g, float x, float by, float t, bool talk)
        {
            float y = by - 76;
            g.Pg(K.Orange, x - 36, by, x - 32, y + 38, x - 10, y + 32, x + 10, y + 32, x + 32, y + 38, x + 36, by);
            g.Pg(K.White, x - 10, y + 32, x, y + 43, x + 10, y + 32); g.Bx(x - 24, y + 46, 9, 6, K.Cyan); g.Bx(x - 22, y + 48, 5, 2, K.White);
            g.Bx(x - 6, y + 26, 12, 8, K.SkinS);
            g.Pg(K.Skin, x - 12, y + 4, x + 12, y + 4, x + 13, y + 18, x + 7, y + 28, x - 7, y + 28, x - 13, y + 18);
            g.FillArc(x, y + 6, 16, PI, TAU, K.White); g.Bx(x - 16, y + 6, 4, 12, K.White); g.Bx(x + 12, y + 6, 4, 12, K.White);  // helmet
            g.Bx(x - 2, y - 10, 4, 13, K.Red); g.Bx(x - 14, y + 2, 28, 4, K.Cyan);
            g.Bx(x - 9, y + 9, 6, 1, K.Brown); g.Bx(x + 3, y + 9, 6, 1, K.Brown);
            if (t % 3.2f < 0.12f) { g.Bx(x - 8, y + 12, 4, 1, K.Brown); g.Bx(x + 4, y + 12, 4, 1, K.Brown); }
            else { g.Bx(x - 8, y + 11, 4, 3, K.White); g.Bx(x + 4, y + 11, 4, 3, K.White); g.Bx(x - 6, y + 12, 2, 2, K.Black); g.Bx(x + 6, y + 12, 2, 2, K.Black); }
            g.Bx(x - 1, y + 14, 2, 4, K.SkinS);
            if (talk) { g.Bx(x - 5, y + 21, 10, 2 + Flap(t, 2), K.DkRed); g.Bx(x - 5, y + 21, 10, 1, K.White); }
            else { g.Bx(x - 6, y + 21, 12, 3, K.White); g.Bx(x - 7, y + 20, 1, 1, K.Brown); g.Bx(x + 6, y + 20, 1, 1, K.Brown); }
            float sp = t % 2.4f; if (sp < 0.36f && !talk) Star4(g, x + 4, y + 22, 6 * (1 - Mathf.Abs(sp - 0.18f) / 0.18f), K.White);   // the tooth sparkle
            g.Bx(x + 30, y + 41, 9, 9, K.Skin); g.Bx(x + 32, y + 34, 4, 8, K.Skin);                                                // thumbs up
        }

        public static void BuddyFace(PixelCanvas g, float x, float y, float w, float h, float t, bool talk)
        {
            g.Bx(x, y, w, h, BuddyScreen);
            float es = Mathf.Max(5, R(w * 0.13f)), ex = w * 0.27f;
            if (t % 2.9f < 0.1f) { g.Bx(x + ex - es / 2, y + h * 0.36f, es, 1, K.Green); g.Bx(x + w - ex - es / 2, y + h * 0.36f, es, 1, K.Green); }
            else { g.Bx(x + ex - es / 2, y + h * 0.22f, es, es, K.Green); g.Bx(x + w - ex - es / 2, y + h * 0.22f, es, es, K.Green); }
            float lw = Mathf.Max(1, w / 60f), px0 = 0, py0 = 0;
            for (int i = 0; i <= 12; i++)
            {
                float px = x + w * 0.2f + i * (w * 0.6f / 12), amp = talk ? Mathf.Sin(t * 30 + i * 1.7f) * h * 0.14f : -Mathf.Cos(i / 12f * TAU) * h * 0.05f, py = y + h * 0.72f + amp;
                if (i > 0) g.Li(px0, py0, px, py, K.Green, lw);
                px0 = px; py0 = py;
            }
            g.Alpha = 0.25f; for (float r = 0; r < h; r += 3) g.Bx(x, y + r, w, 1, K.Black); g.Alpha = 1;
        }

        /// <summary>The rental, seen from behind.</summary>
        public static void Ship(PixelCanvas g, float x, float y, float s, float t)
        {
            float f = (t * 23) % 1;
            g.Pg(K.Orange, x - 3 * s, y + 4 * s, x + 3 * s, y + 4 * s, x, y + (9 + f * 7) * s); g.Pg(K.Yellow, x - 2 * s, y + 4 * s, x + 2 * s, y + 4 * s, x, y + (6 + f * 3) * s);
            g.Pg(K.Red, x - 6 * s, y, x - 21 * s, y + 5 * s, x - 6 * s, y + 4 * s); g.Pg(K.Red, x + 6 * s, y, x + 21 * s, y + 5 * s, x + 6 * s, y + 4 * s);
            g.Pg(K.Steel, x, y - 10 * s, x + 7 * s, y + 3 * s, x - 7 * s, y + 3 * s); g.Pg(K.White, x, y - 10 * s, x + 2 * s, y + 3 * s, x - 2 * s, y + 3 * s);
            g.Bx(x - s, y - 15 * s, 2 * s, 7 * s, K.Red);
            g.Ov(x, y + 3 * s, 4 * s, 2.4f * s, K.Yellow); g.Ov(x, y + 3 * s, 2 * s, 1.2f * s, K.White);
        }

        /// <summary>Mother Von Kaboom: the hair alone has its own gravity.</summary>
        public static void Mother(PixelCanvas g, float x, float by, float t, bool talk)
        {
            float y = by - 78;
            g.Pg(K.Magenta, x - 40, by, x - 43, y + 37, x - 14, y + 30, x + 14, y + 30, x + 43, y + 37, x + 40, by);
            g.Pg(K.Purple, x - 43, y + 37, x - 30, y + 28, x - 14, y + 30, x - 22, y + 42); g.Pg(K.Purple, x + 43, y + 37, x + 30, y + 28, x + 14, y + 30, x + 22, y + 42);   // shoulder pads
            for (int i = -3; i <= 3; i++) g.Bx(x + i * 3 - 1, y + 35 - Mathf.Abs(i), 2, 2, K.White);                            // pearls
            g.Bx(x - 5, y + 25, 10, 7, K.SkinS);
            g.Ov(x, y - 8, 15, 16, K.Steel); g.Ov(x, y - 21, 11, 10, K.Steel); g.Ov(x - 12, y + 7, 5, 10, K.Steel); g.Ov(x + 12, y + 7, 5, 10, K.Steel);   // beehive
            g.Ov(x, y + 13, 12, 14, K.Skin);
            g.Pg(K.White, x - 3, y - 29, x + 2, y - 31, x + 7, y - 3, x + 2, y - 1);                                                // the streak
            g.Li(x - 10, y + 6, x - 3, y + 6, K.DGrey, 1); g.Li(x + 3, y + 5, x + 10, y + 2, K.DGrey, 1);                           // one eyebrow permanently raised
            g.Pg(K.Red, x - 13, y + 7, x - 2, y + 9, x - 3, y + 15, x - 10, y + 15); g.Pg(K.Red, x + 13, y + 7, x + 2, y + 9, x + 3, y + 15, x + 10, y + 15);   // cat-eye glasses
            g.Bx(x - 9, y + 10, 6, 4, K.Glass); g.Bx(x + 3, y + 10, 6, 4, K.Glass); g.Bx(x - 2, y + 10, 4, 1, K.Red);
            if (t % 3.6f < 0.12f) { g.Bx(x - 8, y + 12, 4, 1, K.Black); g.Bx(x + 4, y + 12, 4, 1, K.Black); } else { g.Bx(x - 7, y + 11, 2, 2, K.Black); g.Bx(x + 5, y + 11, 2, 2, K.Black); }
            g.Bx(x - 1, y + 15, 2, 4, K.SkinS);
            g.Bx(x - 3, y + 22, 6, talk ? 1 + Flap(t, 3) : 2, K.Red);
            g.Bx(x - 13, y + 17, 2, 5, K.Yellow); g.Bx(x + 11, y + 17, 2, 5, K.Yellow);
            g.Arc(x + 33, y + 47, 5, PI, TAU, K.Brown, 2);                                                                          // handbag, held like a weapon
            g.Bx(x + 26, y + 47, 14, 11, K.Brown); g.Bx(x + 31, y + 51, 4, 2, K.Yellow);
        }

        /// <summary>Debbie, Prestige Rocket Rental: relentlessly helpful.</summary>
        public static void Debbie(PixelCanvas g, float x, float by, float t, bool talk)
        {
            float y = by - 76;
            g.Pg(K.Pink, x - 40, by, x - 41, y + 36, x - 12, y + 30, x + 12, y + 30, x + 41, y + 36, x + 40, by);
            g.Pg(K.White, x - 9, y + 30, x, y + 45, x + 9, y + 30);
            g.Bx(x + 15, y + 45, 13, 8, K.White); g.Bx(x + 17, y + 47, 9, 1, K.Navy); g.Bx(x + 17, y + 50, 6, 1, K.Navy);          // name badge
            g.Bx(x - 5, y + 25, 10, 7, K.SkinS);
            for (int i = 0; i < 9; i++) g.Ov(x + Perm[i, 0], y + Perm[i, 1], Perm[i, 2], Perm[i, 2], K.Yellow);                   // the perm
            g.Ov(x, y + 13, 11, 13, K.Skin);
            g.Ov(x - 4, y + 1, 7, 4, K.Yellow); g.Ov(x + 5, y + 2, 6, 4, K.Yellow);
            g.Bx(x - 8, y + 9, 5, 2, K.Cyan); g.Bx(x + 3, y + 9, 5, 2, K.Cyan);                                                     // eyeshadow
            if (t % 2.7f < 0.12f) { g.Bx(x - 8, y + 12, 5, 1, K.Brown); g.Bx(x + 3, y + 12, 5, 1, K.Brown); }
            else { g.Bx(x - 7, y + 11, 3, 3, K.White); g.Bx(x + 4, y + 11, 3, 3, K.White); g.Bx(x - 6, y + 12, 2, 2, K.Black); g.Bx(x + 5, y + 12, 2, 2, K.Black); }
            g.Bx(x - 1, y + 15, 2, 3, K.SkinS); g.Bx(x - 10, y + 17, 3, 2, K.Pink); g.Bx(x + 7, y + 17, 3, 2, K.Pink);
            if (talk) { g.Bx(x - 5, y + 20, 10, 2 + Flap(t, 3), K.DkRed); g.Bx(x - 5, y + 20, 10, 1, K.White); } else { g.Bx(x - 6, y + 20, 12, 3, K.White); g.Bx(x - 6, y + 23, 12, 1, K.Red); }
            g.Arc(x, y + 4, 20, PI * 1.08f, PI * 1.92f, K.Black, 2);                                                                 // headset
            g.Bx(x - 22, y + 7, 5, 9, K.Black); g.Li(x - 19, y + 15, x - 8, y + 23, K.Black, 1); g.Bx(x - 9, y + 22, 3, 3, K.Black);
        }
    }
}
