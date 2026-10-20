using System;
using System.Globalization;
using DoomsdayTunnels.Core;

namespace DoomsdayTunnels.Tunnel
{
    /// <summary>
    /// The daily sector: one sector a day that everyone flies, built from the date alone. Its difficulty (sector 4 to
    /// 7, so some days are Part Two neon) and its seed come from the number of days since the start of 2026. The
    /// scoreboard server uses the same formulas to judge the times posted for it: keep them in step.
    /// </summary>
    public static class Daily
    {
        static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static string Today() => DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static int DayIndex(string day) => (int)Math.Round((DateTime.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal) - Epoch).TotalDays);
        public static int Difficulty(int dayIndex) => 4 + ((dayIndex % 4) + 4) % 4;
        public static int Seed(int dayIndex) => unchecked(dayIndex * 7919 + 101);

        /// <summary>That day's sector. Its number is its difficulty, so the hue, speed, length and clock follow from it.</summary>
        public static Sector Make(string day, Tune tune)
        {
            int idx = DayIndex(day), n = Difficulty(idx), seed = Seed(idx);
            var lv = SectorGenerator.Make(n, tune, seed, unchecked(seed * 31 + 5));
            lv.Daily = day;
            return lv;
        }
    }
}
