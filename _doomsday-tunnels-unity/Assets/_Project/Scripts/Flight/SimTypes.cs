using UnityEngine;

namespace DoomsdayTunnels.Flight
{
    /// <summary>What the player is doing this step. Steering is -1..1 on each axis; aim is -1..1 (tilt mode only).</summary>
    public struct SimInput
    {
        public float SteerX, SteerY, AimX, AimY;
        public bool Fire, Boost;
    }

    public enum SimEventKind
    {
        Laser, Tink, Boom, Hit, Pick, Refuel, Clear, Beep, Scrape,
        BoostOn, BoostOff, BoostDry, Popup,
        Burst,        // sparks at a point in the tunnel (X, Y, Z, Color, Count)
        ScreenBurst,  // sparks on the screen plane (X, Y as screen fractions -1..1, Color, Count)
        Shock,        // shockwave ring at a point
        Flash,        // full-screen flash in Color
        Shake,
        Fork,         // the ship has taken a branch (Count)
        SectorClear, ShipLost, TimeOut
    }

    /// <summary>Something the view or the audio should react to. Drained by the game after each step.</summary>
    public struct SimEvent
    {
        public SimEventKind Kind;
        public float X, Y, Z;
        public int Count;
        public int Branch;      // which branch X is measured in, for a point in the tunnel; -1 is the trunk
        public Color Color;
        public string Text;
        public static SimEvent At(SimEventKind k, float x, float y, float z, Color c, int n, int br = -1) => new SimEvent { Kind = k, X = x, Y = y, Z = z, Color = c, Count = n, Branch = br };
        public static SimEvent Of(SimEventKind k) => new SimEvent { Kind = k, Branch = -1 };
        public static SimEvent Say(string s) => new SimEvent { Kind = SimEventKind.Popup, Text = s, Branch = -1 };
    }

    public sealed class Bullet
    {
        public float Z, X, Y, Sx, Sy;
        public int Branch = -1;
        public bool Dead;
    }

    public struct ClearBonus { public int Sector, TimeBonus, ShieldBonus; public bool Ship, Token; public float SectorTime; }
}
