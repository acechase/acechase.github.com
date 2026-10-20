using System;
using System.Collections.Generic;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Tunnel;
using UnityEngine;

namespace DoomsdayTunnels.Flight
{
    /// <summary>
    /// The whole flight model in one deterministic, engine-free class: the ship's motion in tunnel coordinates, the
    /// guns, collisions with everything in the tunnel, shield, clock and score. It is a port of the web game's update
    /// loop, stepped at a fixed rate. Nothing here touches Unity objects; views read the state and drain the events.
    /// PhysX is never involved, which keeps sector times comparable between devices.
    /// </summary>
    public sealed class FlightSim
    {
        public Tune Tune;
        public Sector Sector;
        public TunnelPath Path;

        // Ship: depth along the tunnel, offset from the centre, velocity across the tunnel, forward speed, bank.
        public float Pz, Rx, Ry, Vx, Vy, Speed, Roll;
        public float Time, Shield = 100f, Score, SectorT, Invuln;
        public int Lives = G.StartSpares;
        public bool ShipEarned;
        public bool Playing;                       // false once the sector is cleared or the ship is lost
        public float Now;                          // seconds since the sim was created (drives animations)

        public bool Boosting, BoostHeld; public float BoostFuel = 1f, BoostEase;
        public float GunHeat; public bool GunHot; float fireCd; int gunSide = 1;
        public float AimSX, AimSY;                 // smoothed aim offset (tilt mode)
        public readonly List<Bullet> Bullets = new List<Bullet>();
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public int ScrapeX, ScrapeY;               // which wall is being scraped this step, if any
        float scrapeCd; int lastBeep;

        // Points for things shot or collected add up into one running "+N" beside the score.
        public int GainN; public float GainT = -9f;

        public void StartSector(Sector lv, Tune tune)
        {
            Sector = lv; Tune = tune; Path = new TunnelPath(lv, tune);
            Pz = 0; Rx = Ry = Vx = Vy = 0; Speed = lv.V0 * tune.Speed * 0.5f;
            Time = lv.Limit; SectorT = 0; GainN = 0; lastBeep = 0;
            Bullets.Clear(); AimSX = AimSY = 0; BoostFuel = 1; Boosting = false; ShipEarned = false; GunHeat = 0; GunHot = false;
            Playing = true;
        }

        /// <summary>A new ship at the same spot, briefly untouchable.</summary>
        public void Respawn()
        {
            Shield = 100; Rx = Ry = Vx = Vy = 0; Speed = Sector.V0 * Tune.Speed * 0.5f; Bullets.Clear(); BoostFuel = 1; AimSX = AimSY = 0;
            Invuln = 3; GunHeat = 0; GunHot = false; Playing = true;
            Events.Add(SimEvent.Say("NEW SHIP"));
        }

        void Scored(int n) { Score += n; GainN = (Now - GainT < 1.2f ? GainN : 0) + n; GainT = Now; }

        public void Step(float dt, in SimInput inp)
        {
            Now += dt;
            // Boost: a reserve that drains while held; the view eases its effects in and out.
            bool held = Playing && inp.Boost, was = Boosting;
            Boosting = held && BoostFuel > 0;
            if (Boosting) BoostFuel = Math.Max(0, BoostFuel - dt / G.BoostSecs);
            if (Boosting && !was) Events.Add(SimEvent.Of(SimEventKind.BoostOn));
            else if (was && !Boosting && Playing) { Events.Add(SimEvent.Of(SimEventKind.BoostOff)); if (held) Events.Add(SimEvent.Say("BOOST EMPTY")); }
            else if (held && !BoostHeld && !Boosting) Events.Add(SimEvent.Of(SimEventKind.BoostDry));
            BoostHeld = held;
            BoostEase += ((Boosting ? 1 : 0) - BoostEase) * Math.Min(1, dt * 4);

            if (!Playing) { Speed *= Math.Max(0, 1 - 3 * dt); Pz += Speed * dt; ForkCheck(); Roll += (0 - Roll) * Math.Min(1, dt * 4); return; }
            if (Invuln > 0) Invuln -= dt;

            // Steering: a first-order lag toward the input, so the ship eases into its turns.
            float ix = inp.SteerX, iy = inp.SteerY;
            { float k = Math.Min(1, dt * 14); AimSX += (inp.AimX * G.AimMaxX - AimSX) * k; AimSY += (inp.AimY * G.AimMaxY - AimSY) * k; }
            float il = Mathf.Sqrt(ix * ix + iy * iy); if (il > 1) { ix /= il; iy /= il; }

            float target = Sector.V0 * Tune.Speed * (Boosting ? G.BoostMul : 1);
            Speed += (target - Speed) * Math.Min(1, dt * (Speed < target ? (Boosting ? 2.4f : 1.0f) : 2.0f));
            float S = Tune.Steer, kk = Math.Min(1, dt * Tune.Snap);
            Vx += (ix * S - Vx) * kk; Vy += (iy * S - Vy) * kk;
            // Bends push the ship outward, in proportion to speed.
            float dfx = Mathf.Clamp((Path.Cx(Pz + 0.5f) - Path.Cx(Pz - 0.5f)) * Speed * Tune.Pull, -2, 2);
            float dfy = Mathf.Clamp((Path.Cy(Pz + 0.5f) - Path.Cy(Pz - 0.5f)) * Speed * Tune.Pull, -2, 2);
            float mx = Path.HalfWidth(Pz) - 0.16f, my = G.HH - 0.14f, cush = 0.32f;
            float mvx = Vx - dfx, mvy = Vy - dfy;   // ease off as the ship nears a wall instead of slamming into it
            if (mvx > 0 && Rx > mx - cush) mvx *= 0.3f + 0.7f * Math.Max(0, mx - Rx) / cush;
            else if (mvx < 0 && Rx < -mx + cush) mvx *= 0.3f + 0.7f * Math.Max(0, Rx + mx) / cush;
            if (mvy > 0 && Ry > my - cush) mvy *= 0.3f + 0.7f * Math.Max(0, my - Ry) / cush;
            else if (mvy < 0 && Ry < -my + cush) mvy *= 0.3f + 0.7f * Math.Max(0, Ry + my) / cush;
            Rx += mvx * dt; Ry += mvy * dt;

            // Scraping a wall: shield drains, speed drops, sparks fly.
            int sx = 0, sy = 0;
            if (Rx > mx) { Rx = mx; sx = 1; } else if (Rx < -mx) { Rx = -mx; sx = -1; }
            if (Ry > my) { Ry = my; sy = -1; } else if (Ry < -my) { Ry = -my; sy = 1; }
            ScrapeX = sx; ScrapeY = sy; scrapeCd -= dt;
            if (sx != 0 || sy != 0)
            {
                if (Invuln <= 0) Shield -= G.ScrapePerSec * dt;
                Speed *= 1 - Math.Min(1, 1.0f * dt);
                Events.Add(SimEvent.At(SimEventKind.ScreenBurst, sx, sy, 0, Palette.Warn, 2));
                if (scrapeCd <= 0) { scrapeCd = 0.09f; Events.Add(SimEvent.Of(SimEventKind.Scrape)); }
            }
            Roll += (-Vx * 0.045f * Tune.Sway - Roll) * Math.Min(1, dt * 4);

            // Forward.
            float pz0 = Pz; Pz = Math.Max(0, Pz + Speed * dt); Time -= dt; SectorT += dt; Score += Math.Max(0, Speed) * dt * 0.5f;
            foreach (var o in Sector.Objs) { if (o.Z <= pz0) continue; if (o.Z > Pz) break; if (o.Branch >= 0 && o.Branch != Path.Chosen) continue; Collide(o); }
            ForkCheck();

            // Guns: heat up while firing, cool when released, lock when overheated.
            fireCd -= dt;
            if (G.HeatShots > 0)
            {
                if (!inp.Fire || GunHot) GunHeat = Math.Max(0, GunHeat - G.HeatCool * dt);
                if (GunHot && GunHeat <= G.HeatReady) { GunHot = false; Events.Add(SimEvent.Of(SimEventKind.Beep)); }
            }
            if (inp.Fire && fireCd <= 0 && !GunHot)
            {
                fireCd = G.FireInterval; gunSide = -gunSide; Events.Add(SimEvent.Of(SimEventKind.Laser));
                Bullets.Add(new Bullet { Z = Pz + 0.6f, X = Rx + gunSide * 0.14f, Y = Ry - 0.09f, Sx = AimSX, Sy = AimSY, Branch = Path.Chosen });
                if (G.HeatShots > 0) { GunHeat += 1f / G.HeatShots; if (GunHeat >= 1) { GunHeat = 1; GunHot = true; Events.Add(SimEvent.Of(SimEventKind.Tink)); } }
            }
            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                var b = Bullets[i]; float z0 = b.Z, step = (Speed + G.BulletSpeedBonus) * dt; var fk = Sector.Fork; bool open = fk != null && Path.Chosen < 0;
                b.Z += step; b.X += b.Sx * step; b.Y += b.Sy * step;
                if (open && b.Branch < 0 && b.Z > fk.Z1) { b.Branch = b.X < 0 ? 0 : 1; b.X -= (b.Branch == 1 ? 1 : -1) * G.HW; }      // a shot flying on past the split picks a branch too
                foreach (var o in Sector.Objs) { if (o.Z <= z0) continue; if (o.Z > b.Z) break; if (o.Branch >= 0 && o.Branch != b.Branch) continue; if (BulletHit(o, b)) { b.Dead = true; break; } }
                float bw = b.Branch >= 0 ? G.HW : Path.HalfWidth(b.Z);
                if (!b.Dead && (Math.Abs(b.X) > bw || Math.Abs(b.Y) > G.HH))
                { Events.Add(SimEvent.At(SimEventKind.Burst, Mathf.Clamp(b.X, -bw, bw), Mathf.Clamp(b.Y, -G.HH, G.HH), b.Z, Palette.Ink, 3, b.Branch)); b.Dead = true; }
                if (!b.Dead && b.Z - Pz > G.GunRange) { Events.Add(SimEvent.At(SimEventKind.Burst, b.X, b.Y, b.Z, Palette.Ink, 2, b.Branch)); b.Dead = true; }
                if (b.Dead || b.Z > Sector.Length) Bullets.RemoveAt(i);
            }

            // Warnings: the fork coming up, and a ship token flown past.
            var f = Sector.Fork;
            if (f != null && !f.Hinted && Path.Chosen < 0 && f.Z0 - Pz < 70) { f.Hinted = true; Events.Add(SimEvent.Of(SimEventKind.Beep)); Events.Add(SimEvent.Say("FORK AHEAD: DARK SIDE HAS THE SHIP TOKEN")); }
            var tk = Sector.ShipToken;
            if (tk != null && !tk.Taken && !tk.Gone && Pz > tk.Z + 1) { tk.Gone = true; Events.Add(SimEvent.Say("SHIP TOKEN MISSED")); }

            if (Time < 10 && Time > 0) { int c = (int)Math.Ceiling(Time); if (c != lastBeep) { lastBeep = c; Events.Add(SimEvent.Of(SimEventKind.Beep)); } }
            if (Pz >= Sector.Length) { Playing = false; Events.Add(SimEvent.Of(SimEventKind.SectorClear)); }
            else if (Shield <= 0) { Shield = 0; Playing = false; Events.Add(SimEvent.Of(SimEventKind.ShipLost)); }
            else if (Time <= 0) { Time = 0; Playing = false; Events.Add(SimEvent.Of(SimEventKind.TimeOut)); }
        }

        /// <summary>The ship has reached the split: whichever side of the dividing wall it is on becomes the tunnel.</summary>
        void ForkCheck()
        {
            var f = Sector.Fork;
            if (f == null || Path.Chosen >= 0 || Pz < f.Z1) return;
            int side = Rx < 0 ? 0 : 1; float sgn = side == 1 ? 1 : -1;
            if (Math.Abs(Rx) < 0.22f) { if (Playing) Crash(G.ForkWallHit); Rx = sgn * 0.22f; }      // clipped the dividing wall
            Path.Chosen = side; Rx -= sgn * G.HW;                                                   // same spot, now measured from the branch's centre
            foreach (var b in Bullets) if (b.Branch < 0) { b.Branch = b.X < 0 ? 0 : 1; b.X -= (b.Branch == 1 ? 1 : -1) * G.HW; }
            Bullets.RemoveAll(b => b.Branch != side);
            if (Sector.ShipToken != null && Sector.ShipToken.Branch != side) Sector.ShipToken = null;
            Events.Add(SimEvent.At(SimEventKind.Fork, 0, 0, 0, Color.white, side));
            Events.Add(SimEvent.Say(side == f.Dark ? "DARK TUNNEL" : "LIGHT TUNNEL: NO SHIP TOKEN THIS WAY")); Events.Add(SimEvent.Of(SimEventKind.Tink));
        }

        /// <summary>
        /// A red wall is solid: the ship is thrown back off it and shoved toward the nearest gap, and has to pick its
        /// speed up again (boost helps). The first hit on a wall costs shield; bouncing off the same wall again does not.
        /// </summary>
        void Bounce(SectorObject o, Rect4 r)
        {
            bool first = !o.Hit;
            if (first) { o.Hit = true; o.Version++; if (Invuln > 0) { Events.Add(SimEvent.At(SimEventKind.ScreenBurst, 0, 0, 0, Palette.Ink, 10)); } else { Shield -= G.WallHit; Events.Add(SimEvent.Of(SimEventKind.Hit)); Events.Add(SimEvent.At(SimEventKind.Flash, 0, 0, 0, Palette.Warn, 0)); Events.Add(SimEvent.At(SimEventKind.ScreenBurst, 0, 0, 0, Palette.Warn, 30)); } }
            else Events.Add(SimEvent.Of(SimEventKind.Tink));
            Events.Add(SimEvent.Of(SimEventKind.Shake)); Events.Add(SimEvent.At(SimEventKind.Shock, Rx, Ry, o.Z - 0.1f, Palette.Warn, 0, Path.Chosen));
            Events.Add(SimEvent.At(SimEventKind.Burst, Rx, Ry, o.Z - 0.1f, Palette.Warn, first ? 14 : 6, Path.Chosen));
            Pz = o.Z - G.BounceBack;
            Speed = -Math.Max(G.BounceRecoil, Speed * 0.3f);
            // shove toward the nearest open edge of the slab (never toward the tunnel wall behind it)
            float best = float.MaxValue; float px = 0, py = 0;
            void Try(float dist, float vx, float vy) { if (dist < best) { best = dist; px = vx; py = vy; } }
            if (r.X0 > -G.HW + 0.02f) Try(Rx - r.X0, -1, 0);
            if (r.X1 < G.HW - 0.02f) Try(r.X1 - Rx, 1, 0);
            if (r.Y0 > -G.HH + 0.02f) Try(Ry - r.Y0, 0, -1);
            if (r.Y1 < G.HH - 0.02f) Try(r.Y1 - Ry, 0, 1);
            if (best < float.MaxValue) { Vx = px * G.BounceShove; Vy = py * G.BounceShove; }
        }

        void Crash(float d)
        {
            if (Invuln > 0) { Events.Add(SimEvent.At(SimEventKind.ScreenBurst, 0, 0, 0, Palette.Ink, 10)); Events.Add(SimEvent.Of(SimEventKind.Tink)); return; }   // a fresh ship shrugs it off
            Shield -= d; Speed *= 0.6f;
            Events.Add(SimEvent.Of(SimEventKind.Hit)); Events.Add(SimEvent.At(SimEventKind.Flash, 0, 0, 0, Palette.Warn, 0)); Events.Add(SimEvent.Of(SimEventKind.Shake));
            Events.Add(SimEvent.At(SimEventKind.ScreenBurst, 0, 0, 0, Palette.Warn, 30));
            Events.Add(SimEvent.At(SimEventKind.Shock, Rx, Ry, Pz + 0.5f, Palette.Warn, 0, Path.Chosen));
        }

        void Collide(SectorObject o)
        {
            switch (o.Kind)
            {
                case ObjKind.Wall:
                    foreach (var r in o.Rects) if (r.Contains(Rx, Ry, 0.05f)) { Bounce(o, r); break; }
                    break;
                case ObjKind.Gate:
                    foreach (var b in o.Blocks) if (b.Hp > 0 && b.R.Contains(Rx, Ry, 0.04f)) { b.Hp = 0; o.Version++; Crash(G.GateHit); break; }
                    break;
                case ObjKind.Mine:
                    if (o.Alive && Dist(o.MineX(Now) - Rx, o.Y - Ry) < 0.34f) { o.Alive = false; o.Version++; Crash(G.MineHit); }
                    break;
                default:
                    if (!o.Taken && Dist(o.X - Rx, o.Y - Ry) < (o.Kind == ObjKind.Ship ? 0.57f : 0.45f))
                    {
                        o.Taken = true; o.Version++; Scored(50);
                        if (o.Kind == ObjKind.Time) { Events.Add(SimEvent.Of(SimEventKind.Pick)); Time += 4; Events.Add(SimEvent.Say("+4 SEC")); }
                        else if (o.Kind == ObjKind.Shield) { Events.Add(SimEvent.Of(SimEventKind.Pick)); Shield = Math.Min(100, Shield + 25); Events.Add(SimEvent.Say("SHIELD +25")); }
                        else if (o.Kind == ObjKind.Ship) { ShipEarned = true; Scored(150); Events.Add(SimEvent.Of(SimEventKind.Clear)); Events.Add(SimEvent.Say(Lives < G.MaxSpares ? "SHIP TOKEN! SPARE SHIP AT SECTOR END" : "SHIP TOKEN (SHIP BAY FULL)")); }
                        else { Events.Add(SimEvent.Of(SimEventKind.Refuel)); BoostFuel = 1; Events.Add(SimEvent.Say("BOOST FULL")); }
                    }
                    break;
            }
        }

        bool BulletHit(SectorObject o, Bullet b)
        {
            switch (o.Kind)
            {
                case ObjKind.Wall:
                    foreach (var r in o.Rects) if (r.Contains(b.X, b.Y, 0)) { Events.Add(SimEvent.At(SimEventKind.Burst, b.X, b.Y, o.Z, Palette.Warn, 4, o.Branch)); Events.Add(SimEvent.Of(SimEventKind.Tink)); return true; }
                    return false;
                case ObjKind.Gate:
                    foreach (var k in o.Blocks)
                        if (k.Hp > 0 && k.R.Contains(b.X, b.Y, 0))
                        {
                            k.Hp--; o.Version++;
                            if (k.Hp <= 0) { Scored(25); Events.Add(SimEvent.Of(SimEventKind.Boom)); Events.Add(SimEvent.At(SimEventKind.Burst, k.R.Cx, k.R.Cy, o.Z, Palette.Gate, 22, o.Branch)); Events.Add(SimEvent.At(SimEventKind.Shock, k.R.Cx, k.R.Cy, o.Z, Palette.Gate, 0, o.Branch)); }
                            else { Events.Add(SimEvent.Of(SimEventKind.Tink)); Events.Add(SimEvent.At(SimEventKind.Burst, b.X, b.Y, o.Z, Palette.Gate, 4, o.Branch)); }
                            return true;
                        }
                    return false;
                case ObjKind.Mine:
                    { float mx = o.MineX(Now);
                      if (o.Alive && Dist(mx - b.X, o.Y - b.Y) < 0.38f) { o.Alive = false; o.Version++; Scored(100); Events.Add(SimEvent.Of(SimEventKind.Boom)); Events.Add(SimEvent.At(SimEventKind.Burst, mx, o.Y, o.Z, Palette.Gate, 26, o.Branch)); Events.Add(SimEvent.At(SimEventKind.Shock, mx, o.Y, o.Z, Palette.Gate, 0, o.Branch)); return true; }
                      return false; }
                default: return false;
            }
        }

        /// <summary>Sector cleared: bonuses for time and shield left, and a spare ship if the token was collected.</summary>
        public ClearBonus SectorClear()
        {
            int tb = (int)Math.Ceiling(Time) * 10, sb = (int)Math.Round(Shield) * 2;
            bool token = ShipEarned, gotShip = token && Lives < G.MaxSpares; if (gotShip) Lives++;
            Score += tb + sb; Shield = Math.Min(100, Shield + 20);
            return new ClearBonus { Sector = Sector.N, TimeBonus = tb, ShieldBonus = sb, Ship = gotShip, Token = token, SectorTime = SectorT };
        }

        static float Dist(float dx, float dy) => Mathf.Sqrt(dx * dx + dy * dy);
    }
}
