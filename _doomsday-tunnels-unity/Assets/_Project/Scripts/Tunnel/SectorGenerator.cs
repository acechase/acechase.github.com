using System;
using System.Collections.Generic;
using System.Linq;
using DoomsdayTunnels.Core;

namespace DoomsdayTunnels.Tunnel
{
    /// <summary>
    /// Builds sector N from its seed. Same N, same tune, same layout on every device, and the same layout as the web
    /// game: the random numbers are drawn in the same order it draws them.
    /// </summary>
    public static class SectorGenerator
    {
        const float TAU = (float)(Math.PI * 2);

        public static Sector Make(int n, Tune tune)
        {
            var R = new SectorRng(n * 7919 + 13);
            var lv = new Sector { N = n, Hue = Palette.HueFor(n), V0 = Math.Min(20 + 2 * n, 38), Length = 900 + 150 * Math.Min(n, 8) };
            lv.Px = new[] { Math.Min(1.2f + 0.35f * n, 3f), TAU / (135 + R.NextF() * 35), R.NextF() * TAU, Math.Min(0.4f + 0.1f * n, 0.9f), TAU / (64 + R.NextF() * 18), R.NextF() * TAU };
            lv.Py = new[] { Math.Min(0.6f + 0.25f * n, 2f), TAU / (150 + R.NextF() * 40), R.NextF() * TAU, Math.Min(0.25f + 0.08f * n, 0.7f), TAU / (72 + R.NextF() * 16), R.NextF() * TAU };
            lv.Pal = Palette.For(n);
            lv.Limit = (float)Math.Ceiling(lv.Length / (lv.V0 * tune.Speed) * 1.2 + 5);
            // The fork: the tunnel widens, then splits into a light branch and a dark branch of the sector's colour.
            // The dark branch is the harder one and holds the sector's ship token; the light branch is the easy way round.
            float z0 = (float)Math.Round(lv.Length * 0.4 / 4) * 4, z1 = z0 + G.ForkWiden;
            int dark = R.Next() < 0.5 ? 0 : 1;
            var fork = new Fork { Z0 = z0, Z1 = z1, Dark = dark };
            for (int b = 0; b < 2; b++) fork.Pal[b] = Palette.For(n, b == dark ? 0.3f : -0.22f);
            for (int b = 0; b < 2; b++) fork.Wig[b] = new[] { 0.6f + R.NextF() * 0.9f, TAU / (70 + R.NextF() * 40) };
            lv.Fork = fork;
            GenObjs(lv.Objs, ref R, n, 90, z0 - 14, -1, Mode.Trunk);
            for (int b = 0; b < 2; b++) GenObjs(lv.Objs, ref R, n, z1 + 34, lv.Length - 50, b, b == dark ? Mode.Hard : Mode.Easy);
            lv.ShipToken = new SectorObject { Z = z1 + 22, Kind = ObjKind.Ship, X = 0, Y = 0, Branch = dark };   // in sight from the split
            lv.Objs.Add(lv.ShipToken);
            lv.Objs = lv.Objs.OrderBy(o => o.Z).ToList();     // a stable sort, as the web game's is: ties keep their order
            lv.Course = Course.For(lv);                        // its own random stream: the layout above is untouched
            return lv;
        }

        public enum Mode { Trunk, Easy, Hard }

        // Fill a stretch of tunnel with obstacles and pickups. `br` says which branch they belong to (-1 before the fork).
        public static void GenObjs(List<SectorObject> o, ref SectorRng R, int n, float zA, float zB, int br, Mode mode)
        {
            float HW = G.HW, HH = G.HH;
            float gap = Math.Max(18, 33 - 1.5f * n) * (mode == Mode.Hard ? 0.8f : mode == Mode.Easy ? 1.25f : 1f);
            float[] odds = mode == Mode.Hard ? new[] { 0.36f, 0.58f, 0.88f } : mode == Mode.Easy ? new[] { 0.4f, 0.52f, 0.64f } : new[] { 0.38f, 0.56f, 0.8f };
            float z = zA;
            while (z < zB)
            {
                float k = R.NextF(); int first = o.Count;
                if (k < odds[0])
                {
                    int t = (int)Math.Floor(R.NextF() * (n >= 2 ? 7 : 5)); Rect4[] rects;
                    if (t == 0) rects = new[] { new Rect4(-HW, -HH, 0.15f, HH) };
                    else if (t == 1) rects = new[] { new Rect4(-0.15f, -HH, HW, HH) };
                    else if (t == 2) rects = new[] { new Rect4(-HW, -0.1f, HW, HH) };
                    else if (t == 3) rects = new[] { new Rect4(-HW, -HH, HW, 0.1f) };
                    else if (t == 4) rects = new[] { new Rect4(-0.38f, -HH, 0.38f, HH) };
                    else if (t == 5) rects = new[] { new Rect4(-HW, -0.24f, HW, 0.24f) };
                    else
                    {
                        float hx = R.Signed() * (HW - 0.6f), hy = R.Signed() * (HH - 0.42f);   // a wall with one hole in it
                        rects = new[] { new Rect4(-HW, -HH, hx - 0.5f, HH), new Rect4(hx + 0.5f, -HH, HW, HH),
                            new Rect4(hx - 0.5f, -HH, hx + 0.5f, hy - 0.36f), new Rect4(hx - 0.5f, hy + 0.36f, hx + 0.5f, HH) };
                    }
                    o.Add(new SectorObject { Z = z, Kind = ObjKind.Wall, Rects = rects });
                }
                else if (k < odds[1])
                {
                    var blocks = new List<GateBlock>();
                    for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++)
                        blocks.Add(new GateBlock { R = new Rect4(-HW + i * HW * 2 / 3, -HH + j * HH, -HW + (i + 1) * HW * 2 / 3, -HH + (j + 1) * HH),
                            Hp = (n >= 3 && R.NextF() < 0.3f) ? 2 : 1 });
                    o.Add(new SectorObject { Z = z, Kind = ObjKind.Gate, Blocks = blocks.ToArray() });
                }
                else if (k < odds[2])
                {
                    int m = 1 + (int)Math.Floor(R.NextF() * Math.Min(3, 1 + n * 0.5f));
                    for (int i = 0; i < m; i++)
                        o.Add(new SectorObject { Z = z + i * 3, Kind = ObjKind.Mine, X = R.Signed() * (HW - 0.45f), Y = R.Signed() * (HH - 0.32f),
                            Phase = R.NextF() * TAU, Sway = n >= 2 ? R.NextF() * 0.4f : 0 });
                }
                else
                {
                    float q = R.NextF();
                    o.Add(new SectorObject { Z = z, Kind = q < 0.4f ? ObjKind.Time : q < 0.67f ? ObjKind.Shield : ObjKind.Boost,
                        X = R.Signed() * (HW - 0.45f), Y = R.Signed() * (HH - 0.32f) });
                }
                for (int i = first; i < o.Count; i++) o[i].Branch = br;
                z += gap * (0.8f + R.NextF() * 0.6f);
            }
        }
    }
}
