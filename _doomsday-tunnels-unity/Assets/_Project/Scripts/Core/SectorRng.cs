// Deterministic random numbers (mulberry32), the same generator the web game uses, so a sector seed produces the
// same tunnel on every device. That is what makes shared fastest times fair.
namespace DoomsdayTunnels.Core
{
    public struct SectorRng
    {
        uint a;
        public SectorRng(int seed) { a = unchecked((uint)seed); }

        /// <summary>Uniform in [0, 1).</summary>
        public double Next()
        {
            unchecked
            {
                a += 0x6D2B79F5u;
                uint t = (a ^ (a >> 15)) * (1u | a);
                t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
        public float NextF() => (float)Next();
        /// <summary>Uniform in [-1, 1).</summary>
        public float Signed() => (float)(Next() * 2 - 1);
    }
}
