using DoomsdayTunnels.Tunnel;

namespace DoomsdayTunnels.Flight
{
    /// <summary>
    /// Flies a recorded run again, one step per live step, in a sim of its own with its own copy of the sector, so
    /// nothing it shoots or picks up touches the player's world. Its events are thrown away. The ghost ship is drawn
    /// where this sim says it is.
    /// </summary>
    public sealed class GhostRunner
    {
        public readonly FlightSim Sim = new FlightSim();
        public readonly InputLog Log;
        public int Step { get; private set; }
        public bool Done => Step >= Log.Steps;
        public float Seconds => Log.Steps / 60f;

        public GhostRunner(InputLog log, int sector)
        {
            Log = log;
            var tune = log.TuneUsed();
            Sim.StartSector(SectorGenerator.Make(sector, tune), tune);
            Sim.Now = log.Now0; Sim.Shield = log.Shield0; Sim.Invuln = log.Invuln0;
        }

        public static GhostRunner From(string serialized, int sector)
        {
            var log = InputLog.Parse(serialized);
            return log == null ? null : new GhostRunner(log, sector);
        }

        public void Advance(float dt)
        {
            if (Done) return;
            var inp = Log.Read(Step++, out bool respawn);
            if (respawn) Sim.Respawn();
            Sim.Step(dt, inp);
            Sim.Events.Clear();
        }
    }
}
