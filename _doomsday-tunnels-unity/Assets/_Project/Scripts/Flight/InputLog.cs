using System;
using System.Collections.Generic;
using System.Globalization;
using DoomsdayTunnels.Core;

namespace DoomsdayTunnels.Flight
{
    /// <summary>
    /// A run through one sector as the flight model saw it: what the sim started with, and the input for every fixed
    /// step. The flight model is deterministic, so this is enough to fly the run again exactly, which is what a ghost
    /// is. Steering and aim are kept to 1/127, and the live sim is fed the same quantised input, so the replay and
    /// the original never differ by a rounding error. Five bytes a step: a minute of flying is a few kilobytes.
    /// </summary>
    public sealed class InputLog
    {
        public const byte FireBit = 1, BoostBit = 2, RespawnBit = 4;
        public float Now0, Shield0, Invuln0;
        public float Steer, Snap, Speed, Pull, Twist;             // the tune the run flew with
        public readonly List<byte> Bytes = new List<byte>();
        public int Steps => Bytes.Count / 5;

        public InputLog() { }
        public InputLog(FlightSim sim, Tune tune) { Now0 = sim.Now; Shield0 = sim.Shield; Invuln0 = sim.Invuln; Steer = tune.Steer; Snap = tune.Snap; Speed = tune.Speed; Pull = tune.Pull; Twist = tune.Twist; }

        public Tune TuneUsed() => new Tune { Steer = Steer, Snap = Snap, Speed = Speed, Pull = Pull, Twist = Twist };

        static sbyte Q(float v) => (sbyte)Math.Round(Math.Max(-1f, Math.Min(1f, v)) * 127f);
        static float D(sbyte b) => b / 127f;

        /// <summary>The input as it will be recorded, for the live sim to use too.</summary>
        public static SimInput Quantise(in SimInput inp) => new SimInput { SteerX = D(Q(inp.SteerX)), SteerY = D(Q(inp.SteerY)), AimX = D(Q(inp.AimX)), AimY = D(Q(inp.AimY)), Fire = inp.Fire, Boost = inp.Boost };

        public void Record(in SimInput inp, bool respawnBefore)
        {
            Bytes.Add((byte)Q(inp.SteerX)); Bytes.Add((byte)Q(inp.SteerY)); Bytes.Add((byte)Q(inp.AimX)); Bytes.Add((byte)Q(inp.AimY));
            Bytes.Add((byte)((inp.Fire ? FireBit : 0) | (inp.Boost ? BoostBit : 0) | (respawnBefore ? RespawnBit : 0)));
        }

        /// <summary>Step i's input, and whether the sim should respawn just before it.</summary>
        public SimInput Read(int i, out bool respawnBefore)
        {
            int o = i * 5; byte f = Bytes[o + 4]; respawnBefore = (f & RespawnBit) != 0;
            return new SimInput { SteerX = D((sbyte)Bytes[o]), SteerY = D((sbyte)Bytes[o + 1]), AimX = D((sbyte)Bytes[o + 2]), AimY = D((sbyte)Bytes[o + 3]), Fire = (f & FireBit) != 0, Boost = (f & BoostBit) != 0 };
        }

        /// <summary>One line: the header fields, then the steps as base64.</summary>
        public string Serialize()
        {
            var ci = CultureInfo.InvariantCulture;
            return string.Join(",", "1", Now0.ToString("R", ci), Shield0.ToString("R", ci), Invuln0.ToString("R", ci), Steer.ToString("R", ci), Snap.ToString("R", ci), Speed.ToString("R", ci), Pull.ToString("R", ci), Twist.ToString("R", ci), Convert.ToBase64String(Bytes.ToArray()));
        }

        public static InputLog Parse(string s)
        {
            try
            {
                var p = s.Split(','); if (p.Length != 10 || p[0] != "1") return null;
                var ci = CultureInfo.InvariantCulture; float F(int i) => float.Parse(p[i], ci);
                var log = new InputLog { Now0 = F(1), Shield0 = F(2), Invuln0 = F(3), Steer = F(4), Snap = F(5), Speed = F(6), Pull = F(7), Twist = F(8) };
                log.Bytes.AddRange(Convert.FromBase64String(p[9]));
                return log.Bytes.Count % 5 == 0 && log.Steps > 0 ? log : null;
            }
            catch { return null; }
        }
    }
}
