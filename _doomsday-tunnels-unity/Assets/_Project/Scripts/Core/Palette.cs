using UnityEngine;

namespace DoomsdayTunnels.Core
{
    /// <summary>One hue per sector: floor, ceiling, walls, lines and haze are all shades of the sector colour.</summary>
    public sealed class Palette
    {
        public bool Neon;
        public Color Line, Bright, FloorA, FloorB, CeilA, CeilB, WallA, WallB, Light, LightWall, Haze, Glow;

        public static readonly Color Void = Hex("#07060f"), Ink = Hex("#e8f1ff"), Dim = Hex("#8a94b3"),
            Warn = Hex("#ff4a2e"), Gate = Hex("#ffe14a"), Pick = Color.white;
        public static readonly Color[] Sectors = { Hex("#35d9ff"), Hex("#b07cff"), Hex("#4dffa6"), Hex("#ff6ad5"), Hex("#5b8cff") };
        public static Color GateFill => Color.Lerp(Gate, Void, 0.58f);
        public static Color GateFill2 => Color.Lerp(Gate, Void, 0.36f);
        public static Color MineFill => Color.Lerp(Gate, Void, 0.82f);

        public static Color HueFor(int sector) => Sectors[(sector - 1) % Sectors.Length];
        public static bool IsNeon(int sector) => sector > 5;          // sectors 6 and up: Part Two
        public static Palette For(int sector, float shade = 0f) => IsNeon(sector) ? NeonFrom(HueFor(sector), shade) : From(HueFor(sector), shade);

        // `shade` slides every surface lighter (negative) or darker (positive) without leaving the colour, which is how
        // the two branches of a fork differ.
        public static Palette From(Color a, float shade)
        {
            float k = shade;
            Color D(float v) => Color.Lerp(a, Void, Mathf.Clamp(v + k, 0f, 0.96f));
            return new Palette
            {
                Line = Color.Lerp(a, Color.white, k > 0 ? 0.25f : 0.45f), Bright = Color.Lerp(a, Color.white, 0.8f),
                FloorA = D(0.24f), FloorB = D(0.56f), CeilA = D(0.4f), CeilB = D(0.66f), WallA = D(0.34f), WallB = D(0.62f),
                Light = Color.Lerp(a, Color.white, 0.4f), LightWall = Color.Lerp(a, Color.white, 0.22f),
                Haze = D(0.66f + (k > 0 ? -0.1f : 0.02f)), Glow = Color.Lerp(a, Color.white, 0.45f)
            };
        }

        // Part Two look: hollow neon. Surfaces stay close to black with a tint of the sector's colour; the lines carry it.
        public static Palette NeonFrom(Color a, float shade)
        {
            float k = shade;
            Color D(float v) => Color.Lerp(a, Void, Mathf.Clamp(v + k * 0.6f, 0f, 0.985f));
            return new Palette
            {
                Neon = true,
                Line = k > 0 ? Color.Lerp(a, Void, 0.42f) : Color.Lerp(a, Color.white, k < 0 ? 0.35f : 0.1f),
                Bright = k > 0 ? Color.Lerp(a, Void, 0.12f) : Color.Lerp(a, Color.white, k < 0 ? 0.75f : 0.5f),
                FloorA = D(0.95f), FloorB = D(0.88f), CeilA = D(0.95f), CeilB = D(0.89f), WallA = D(0.92f), WallB = D(0.85f),
                Light = D(0.72f), LightWall = D(0.78f), Haze = D(0.9f), Glow = Color.Lerp(a, Color.white, 0.4f)
            };
        }

        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out var c); c.a = 1f; return c;
        }
    }
}
