// Numbers that define the game. Kept together, and named, so a tuning pass is a diff on one file.
namespace DoomsdayTunnels.Core
{
    public static class G
    {
        // Tunnel cross-section: half-width and half-height, in metres. Everything else is sized from these.
        public const float HW = 1.3f, HH = 0.85f;
        public const float Seg = 2f;            // length of one tunnel segment (the banding and ribs are counted in these)
        public const int Rings = 38;            // segments drawn ahead of the ship
        public const float Depth = Rings * Seg; // how far you can see before the haze swallows everything
        public const float Near = 0.35f;

        // Guns
        public const float GunRange = 30f;      // a shot travels this far ahead of the ship, well short of the draw distance
        public const int HeatShots = 18;        // shots of unbroken fire before the guns overheat
        public const float HeatCool = 0.45f;    // heat shed per second while the trigger is released
        public const float HeatReady = 0.3f;    // overheated guns come back once below this
        public const float FireInterval = 0.14f;
        public const float BulletSpeedBonus = 95f;

        // Boost
        public const float BoostSecs = 6f;      // a full reserve lasts this long with the button held
        public const float BoostMul = 1.5f;

        // The fork: the tunnel widens over ForkWiden metres, then splits into two branches that drift ForkSep metres
        // apart over ForkSpread metres. Clipping the dividing wall costs ForkWallHit shield.
        public const float ForkWiden = 24f, ForkSep = 5f, ForkSpread = 70f, ForkWallHit = 18f;

        // Ship and shield
        public const int StartSpares = 0, MaxSpares = 5;
        public const float LowShield = 15f;
        public const float WallHit = 20f, GateHit = 18f, MineHit = 16f, ScrapePerSec = 11f;
        // Red walls are solid: the ship is held BounceBack metres short of one, thrown back at BounceRecoil m/s at least,
        // and shoved sideways toward the nearest gap at BounceShove m/s.
        public const float BounceRecoil = 5f, BounceShove = 3.2f;
        // The ship as drawn: its nose reaches ShipNose metres ahead of its centre and its wings ShipSpan to each side. A red
        // slab (ShipNose includes the slab's half thickness) is met when the nose reaches it, not the centre, and counts
        // when a wing would clip it. The web game used a 5 cm margin with no ship to see.
        public const float ShipNose = 0.42f, ShipSpan = 0.24f, BounceBack = 0.02f;
        public const float WallMargin = ShipSpan;
        public const float MineShotRadius = 0.45f;

        // Aiming (tilt mode): how far the stick can swing the guns, and the depth the aim point is drawn at
        public const float AimMaxX = 0.24f, AimMaxY = 0.17f, AimDepth = 20f;
    }

    /// <summary>The player-facing feel sliders, with the web game's defaults.</summary>
    [System.Serializable]
    public class Tune
    {
        public float Steer = 2.6f;   // 1.4 .. 4.4  how fast the ship slides sideways
        public float Snap = 4.5f;    // 2 .. 12     lower is smoother, higher snappier
        public float Speed = 1f;     // 0.6 .. 1.4  forward speed multiplier
        public float Pull = 0.14f;   // 0 .. 0.4    how hard bends push you outward
        public float Twist = 0.7f;   // 0 .. 1.5    how sharply the tunnel bends
        public float Sway = 0.4f;    // 0 .. 1.5    banking and shake on impacts
        public float Tilt = 18f;     // 8 .. 35     degrees of tilt for full steering

        /// <summary>Times are only comparable at the standard flight speed.</summary>
        public bool FairSpeed => System.Math.Abs(Speed - 1f) < 0.001f;
    }
}
