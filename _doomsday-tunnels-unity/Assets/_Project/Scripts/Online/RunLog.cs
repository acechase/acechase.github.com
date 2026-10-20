using System;
using System.Collections.Generic;

namespace DoomsdayTunnels.Online
{
    /// <summary>
    /// What one run tells the server: the token it was started with, how far it got, how long it took and the time
    /// through each sector cleared (in tenths of a second, for the fastest-times board). Engine-free.
    /// </summary>
    public sealed class RunLog
    {
        public string Token;                       // from the start event; a score can only be posted with one
        public int Sector = 1;                     // the sector the run is in, or ended in
        public int Cleared;                        // sectors finished
        public float Seconds;                      // flying time
        public bool Posted, Ended;
        readonly List<int> times = new List<int>();

        public void Begin() { Token = null; Sector = 1; Cleared = 0; Seconds = 0; Posted = false; Ended = false; times.Clear(); }

        /// <summary>Sector `n` was cleared in `secs`; only fair-speed runs offer times to the board.</summary>
        public void SectorCleared(int n, float secs, bool fair)
        {
            Cleared = Math.Max(Cleared, n); Sector = n + 1;
            while (times.Count < n) times.Add(0);
            times[n - 1] = fair ? (int)Math.Round(secs * 10) : 0;
        }

        /// <summary>Times for sectors 1..Cleared, 0 where there is nothing to offer; what POST /v2/scores takes as `times`.</summary>
        public int[] Times() => times.ToArray();

        /// <summary>The day flags the start event carries: whether this is the device's first play today, and ever.</summary>
        public static void DayFlags(string lastDay, string today, out bool firstToday, out bool firstEver)
        {
            firstEver = string.IsNullOrEmpty(lastDay);
            firstToday = lastDay != today;
        }
        public static string Today() => DateTime.UtcNow.ToString("yyyy-MM-dd");
    }
}
