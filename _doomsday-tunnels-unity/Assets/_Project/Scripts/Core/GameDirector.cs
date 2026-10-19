using System.Collections.Generic;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Fx;
using DoomsdayTunnels.Hazards;
using DoomsdayTunnels.Input;
using DoomsdayTunnels.Tunnel;
using DoomsdayTunnels.UI;
using UnityEngine;

namespace DoomsdayTunnels
{
    public enum GameState { Title, Play, Clear, Respawn, Over }

    /// <summary>
    /// The game: one of these in the scene. Owns the simulation, builds the world for each sector, feeds input in,
    /// turns sim events into sparks, flashes and popups, and runs the screens between runs.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour
    {
        [Tooltip("Base material with the Doomsday/Unlit shader; every surface is an instance of it.")] public Material Unlit;
        [Tooltip("Base material with the Doomsday/UnlitAdd shader, for sparks and shot glow.")] public Material Additive;

        public GameState State = GameState.Title;
        public bool Paused;
        public float StateT;
        public int Best;
        public ClearBonus Bonus;
        public string DeadWhy = "";
        public SimEventKind RespawnWhy;

        readonly FlightSim sim = new FlightSim();
        readonly Tune tune = new Tune();
        readonly List<HazardView> hazards = new List<HazardView>();
        readonly Hud hud = new Hud();
        readonly InputReader input = new InputReader();
        TunnelView tunnel; BulletPool bullets; Sparks sparks; CameraRig rig; Transform hazardRoot;
        SimInput latest; int level = 1;

        void Awake()
        {
            Debug.Log("[Doomsday] Awake");
            Application.targetFrameRate = 60; Time.fixedDeltaTime = 1f / 60f;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Best = PlayerPrefs.GetInt("best", 0);
            var cam = Camera.main;
            rig = cam.gameObject.AddComponent<CameraRig>(); rig.Cam = cam;
            tunnel = new GameObject("Tunnel").AddComponent<TunnelView>();
            hazardRoot = new GameObject("Hazards").transform;
            bullets = new GameObject("Shots").AddComponent<BulletPool>(); bullets.Init(Unlit, Additive, Color.white);
            sparks = new GameObject("Sparks").AddComponent<Sparks>(); sparks.Init(Additive, cam);
            Attract();
            // Debug: "-shot <folder>" on the command line plays a few seconds unattended and saves screenshots there, so a
            // headless build can be checked by eye. Used by the verification notes in the README.
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shot") StartCoroutine(ShotRun(args[i + 1]));
            Debug.Log("[Doomsday] Ready: sector " + sim.Sector.N + ", " + hazards.Count + " objects");
        }

        System.Collections.IEnumerator ShotRun(string folder)
        {
            System.IO.Directory.CreateDirectory(folder);
            yield return new WaitForSeconds(1.5f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "01-title.png"));
            yield return new WaitForSeconds(0.5f);
            NewGame();
            for (int i = 0; i < 4; i++)
            {
                latestOverride = new SimInput { SteerX = (i % 2 == 0 ? 0.6f : -0.6f), SteerY = i == 1 ? 0.5f : 0f, Fire = true, Boost = i == 3 };
                yield return new WaitForSeconds(2.5f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "0" + (i + 2) + "-play.png"));
            }
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
        SimInput? latestOverride;

        void LoadSector(int n)
        {
            var lv = SectorGenerator.Make(n, tune);
            sim.StartSector(lv, tune);
            tunnel.Build(lv, sim.Path, Unlit);
            foreach (var h in hazards) if (h) Destroy(h.gameObject);
            hazards.Clear();
            foreach (var o in lv.Objs) hazards.Add(HazardView.Create(o, sim.Path, Unlit, lv.Pal, hazardRoot));
            bullets.SetLineColor(lv.Pal.Line);
            rig.Cam.backgroundColor = lv.Pal.Haze;
        }

        void Attract() { level = 1; LoadSector(1); sim.Playing = false; State = GameState.Title; StateT = 0; }
        void StartLevel(int n) { level = n; LoadSector(n); State = GameState.Play; StateT = 0; Paused = false; hud.Popups.Clear(); }
        void NewGame() { sim.Score = 0; sim.Shield = 100; sim.Lives = G.StartSpares; sim.Invuln = 0; StartLevel(1); }

        void Advance()
        {
            if (StateT < 0.6f) return;
            if (State == GameState.Title || State == GameState.Over) NewGame();
            else if (State == GameState.Clear) StartLevel(level);
        }

        void Update()
        {
            float dt = Time.deltaTime; StateT += dt; hud.Tick(dt);
            latest = input.Read();
            if (latestOverride.HasValue) latest = latestOverride.Value;
            if (input.PauseKey && State == GameState.Play) Paused = !Paused;
            if (input.Advance) { if (State == GameState.Play) { if (Paused) Paused = false; } else Advance(); }
            if (State == GameState.Title) { sim.Now += dt; sim.Pz += 14 * dt; if (sim.Pz > sim.Sector.Length - 90) sim.Pz = 0; }   // the idle cruise
            if (State == GameState.Respawn && StateT > 2) FinishRespawn();
            foreach (var h in hazards) h.Tick(sim.Now, sim.Pz);
            bullets.Sync(sim, sim.Path);
            rig.Place(sim, sim.Path, tune.Sway, dt);
        }

        void FixedUpdate()
        {
            if (State == GameState.Play && !Paused) { sim.Step(Time.fixedDeltaTime, latest); Drain(); }
            else if (State == GameState.Respawn || State == GameState.Over) sim.Step(Time.fixedDeltaTime, default);
        }

        void OnApplicationPause(bool paused) { if (paused && State == GameState.Play) Paused = true; }
        void OnApplicationFocus(bool focus) { if (!focus && State == GameState.Play) Paused = true; }

        void Drain()
        {
            foreach (var e in sim.Events)
            {
                switch (e.Kind)
                {
                    case SimEventKind.Popup: hud.Popups.Add(new Hud.Popup { Text = e.Text }); break;
                    case SimEventKind.Burst: sparks.Burst(sim.Path.World(e.X, e.Y, e.Z), e.Color, e.Count, 3f); if (e.Count >= 12) sparks.Burst(sim.Path.World(e.X, e.Y, e.Z), Palette.Ink, e.Count / 2, 4f); break;
                    case SimEventKind.ScreenBurst: sparks.ScreenBurst(e.X, e.Y, e.Color, e.Count); break;
                    case SimEventKind.Flash: hud.Flash = 1; hud.FlashColor = e.Color; break;
                    case SimEventKind.Shake: rig.Shake = 1; break;
                    case SimEventKind.SectorClear: OnSectorClear(); break;
                    case SimEventKind.ShipLost: case SimEventKind.TimeOut: LoseShip(e.Kind); break;
                    default: break;   // sounds arrive in milestone 2
                }
            }
            sim.Events.Clear();
        }

        void OnSectorClear()
        {
            Bonus = sim.SectorClear(); SaveBest();
            hud.Flash = 1; hud.FlashColor = Palette.Ink;
            level++; State = GameState.Clear; StateT = 0;
        }

        // Losing a ship: with a spare in hand the run carries on, otherwise it is game over.
        void LoseShip(SimEventKind why)
        {
            hud.Flash = 1; hud.FlashColor = Palette.Warn; rig.Shake = 1;
            sparks.ScreenBurst(0, 0, Palette.Warn, 40); sparks.ScreenBurst(0, 0, Palette.Ink, 20);
            if (sim.Lives <= 0) { GameOver(why); return; }
            sim.Lives--; RespawnWhy = why; State = GameState.Respawn; StateT = 0;
        }
        void FinishRespawn()
        {
            if (RespawnWhy == SimEventKind.ShipLost) { sim.Respawn(); State = GameState.Play; StateT = 0; hud.Popups.Add(new Hud.Popup { Text = "NEW SHIP" }); }
            else { StartLevel(level); sim.Invuln = 1.5f; hud.Popups.Add(new Hud.Popup { Text = "CLOCK RESET" }); }   // out of time: the sector starts over with a fresh clock
        }
        void GameOver(SimEventKind why) { DeadWhy = why == SimEventKind.ShipLost ? "SHIP LOST" : "CORE DETONATED"; State = GameState.Over; StateT = 0; SaveBest(); }
        void SaveBest() { if (sim.Score > Best) { Best = (int)sim.Score; PlayerPrefs.SetInt("best", Best); PlayerPrefs.Save(); } }

        void OnGUI() { hud.Draw(this, sim, input); }
    }
}
