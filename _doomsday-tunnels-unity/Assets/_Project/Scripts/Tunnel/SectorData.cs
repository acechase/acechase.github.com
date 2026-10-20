using System.Collections.Generic;
using UnityEngine;

namespace DoomsdayTunnels.Tunnel
{
    public enum ObjKind { Wall, Gate, Mine, Time, Shield, Boost, Ship }

    /// <summary>A rectangle in tunnel cross-section coordinates (x across, y up), as min/max corners.</summary>
    public struct Rect4
    {
        public float X0, Y0, X1, Y1;
        public Rect4(float x0, float y0, float x1, float y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
        public bool Contains(float x, float y, float margin) => x > X0 - margin && x < X1 + margin && y > Y0 - margin && y < Y1 + margin;
        public float Cx => (X0 + X1) * 0.5f;
        public float Cy => (Y0 + Y1) * 0.5f;
    }

    public sealed class GateBlock { public Rect4 R; public int Hp; }

    /// <summary>One thing in the tunnel: a wall, a gate, a mine or a pickup. Pure data; the sim owns it, views mirror it.</summary>
    public sealed class SectorObject
    {
        public ObjKind Kind;
        public float Z, X, Y;              // x and y are measured from the centre of the trunk, or of the object's branch
        public int Branch = -1;            // which fork branch it belongs to; -1 before the fork
        public Rect4[] Rects;              // walls
        public GateBlock[] Blocks;         // gates
        public bool Hit;                   // wall: the ship ran into it
        public bool Alive = true;          // mine
        public float Phase, Sway;          // mine: side-to-side motion
        public bool Taken, Gone, Hinted;   // pickups
        public int Version;                // bumped whenever the visible state changes, so views can refresh cheaply

        public float MineX(float t) => X + Mathf.Sin(t * 1.6f + Phase) * Sway;
    }

    /// <summary>
    /// The fork: the tunnel widens to twice its width over [Z0, Z1], then splits into branch 0 (left) and branch 1 (right),
    /// each a full tunnel again. One of them is the dark, harder branch that holds the sector's ship token.
    /// </summary>
    public sealed class Fork
    {
        public float Z0, Z1;
        public int Dark;                                      // which branch is the dark one
        public bool Hinted;                                   // the "fork ahead" warning has been shown
        public Core.Palette[] Pal = new Core.Palette[2];      // the light branch is paler than the sector, the dark one deeper
        public float[][] Wig = new float[2][];                // each branch's own wander: amplitude and frequency
    }

    /// <summary>A sector: its tunnel shape, timing and contents. Built once by SectorGenerator; read by everything else.</summary>
    public sealed class Sector
    {
        public int N;
        public Color Hue;
        public Core.Palette Pal;
        public float V0;                   // cruising speed, metres per second
        public float Length;               // metres to the exit
        public float Limit;                // seconds on the clock
        public float[] Px = new float[6], Py = new float[6];   // the two-sine centreline recipe per axis
        public Fork Fork;
        public Course Course;              // the 3D course: climbs, dives, bends, loops, corkscrews (cosmetic; the flight model never sees it)
        public List<SectorObject> Objs = new List<SectorObject>();
        public SectorObject ShipToken;     // null once the ship has taken the other branch
    }
}
