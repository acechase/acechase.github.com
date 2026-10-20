using System.Collections.Generic;
using DoomsdayTunnels.Audio;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Fx;
using DoomsdayTunnels.Hazards;
using DoomsdayTunnels.Input;
using DoomsdayTunnels.Story;
using DoomsdayTunnels.Tunnel;
using DoomsdayTunnels.UI;
using UnityEngine;

namespace DoomsdayTunnels
{
    public enum GameState { Title, Play, Clear, Respawn, Over, Scene }

    /// <summary>
    /// The game: one of these in the scene. Owns the simulation, builds the world for each sector, feeds input in,
    /// turns sim events into sparks, flashes, sounds and popups, and runs the screens between runs.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour
    {
        [Tooltip("Base material with the Doomsday/Unlit shader; every surface is an instance of it.")] public Material Unlit;
        [Tooltip("Base material with the Doomsday/UnlitAdd shader, for sparks and shot glow.")] public Material Additive;

        public GameState State = GameState.Title;
        public bool Paused;
        public float StateT;
        public ClearBonus Bonus;
        public bool ClearPB;                        // the sector just cleared was a personal best time
        public string DeadWhy = "", DeadQuip = "";
        public SimEventKind RespawnWhy;
        public bool OverBest;                       // this run beat the best score on the device
        int bestWas; bool seenIntro;
        readonly ScenePlayer scene = new ScenePlayer();
        public ScenePlayer Scene => scene;
        public Settings S { get; private set; }
        public int Best => S.Best;
        public Camera Cam => rig.Cam;

        readonly FlightSim sim = new FlightSim();
        readonly List<HazardView> hazards = new List<HazardView>();
        readonly Hud hud = new Hud();
        readonly InputReader input = new InputReader();
        TunnelView tunnel; BulletPool bullets; Sparks sparks; CameraRig rig; Transform hazardRoot; GameAudio audio;
        SimInput latest; int level = 1;

        void Awake()
        {
            Debug.Log("[Doomsday] Awake");
            Application.targetFrameRate = 60; Time.fixedDeltaTime = 1f / 60f;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            S = new Settings(); input.S = S;
            var cam = Camera.main;
            rig = cam.gameObject.AddComponent<CameraRig>(); rig.Cam = cam;
            tunnel = new GameObject("Tunnel").AddComponent<TunnelView>();
            hazardRoot = new GameObject("Hazards").transform;
            bullets = new GameObject("Shots").AddComponent<BulletPool>(); bullets.Init(Unlit, Additive, Color.white);
            sparks = new GameObject("Sparks").AddComponent<Sparks>(); sparks.Init(Additive, cam);
            audio = new GameObject("Audio").AddComponent<GameAudio>(); audio.Muted = !S.Sound;
            scene.Sound = n => audio.Play(n);
            ApplySettings();
            Attract();
            // Debug: "-shot <folder>" on the command line plays a few seconds unattended and saves screenshots there, so a
            // headless build can be checked by eye; "-shotz <metres>" starts that run part-way down the sector (say, just
            // before the fork); "-shotsector <n>" flies that sector instead of sector 1; "-shotscene" captures the opening
            // cut scene instead, a line or two per frame; "-shotpanel" captures the settings panel. Used by the README.
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shotz") float.TryParse(args[i + 1], out shotZ);
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shotsector") int.TryParse(args[i + 1], out shotSector);
            foreach (var a in args) { if (a == "-shotscene") shotScene = true; if (a == "-shotpanel") shotPanel = true; }
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shot") StartCoroutine(ShotRun(args[i + 1]));
            Debug.Log("[Doomsday] Ready: sector " + sim.Sector.N + ", " + hazards.Count + " objects");
        }

        System.Collections.IEnumerator ShotRun(string folder)
        {
            System.IO.Directory.CreateDirectory(folder);
            yield return new WaitForSeconds(1.5f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "01-title.png"));
            yield return new WaitForSeconds(0.5f);
            if (shotPanel)
            {
                hud.PanelOpen = true; yield return new WaitForSeconds(0.5f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "02-panel.png"));
                yield return new WaitForSeconds(0.5f); Application.Quit(); yield break;
            }
            if (shotScene)
            {
                NewGame();                                                   // the intro plays first
                for (int i = 0; i < 6 && State == GameState.Scene; i++)
                {
                    yield return new WaitForSeconds(1.6f);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "0" + (i + 2) + "-scene.png"));
                    yield return null; scene.Advance(); scene.Advance();     // finish the sentence, then the next line
                }
                yield return new WaitForSeconds(0.5f);
                Application.Quit(); yield break;
            }
            seenIntro = true; NewGame();
            if (shotSector > 1) StartLevel(shotSector);
            if (shotZ > 0) sim.Pz = shotZ;
            Debug.Log("[Doomsday] Shot run: sector " + sim.Sector.N + " from " + sim.Pz + " m (args: " + string.Join(" ", System.Environment.GetCommandLineArgs()) + ")");
            for (int i = 0; i < 4; i++)
            {
                latestOverride = new SimInput { SteerX = (i % 2 == 0 ? 0.6f : -0.6f), SteerY = i == 1 ? 0.5f : 0f, Fire = true, Boost = i == 3 };
                yield return new WaitForSeconds(2.5f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "0" + (i + 2) + "-play.png"));
            }
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
        SimInput? latestOverride; float shotZ; int shotSector; bool shotScene, shotPanel;

        /// <summary>Settings that views hold a copy of: pushed whenever the panel closes.</summary>
        void ApplySettings()
        {
            hud.ReduceMotion = S.ReduceMotion; if (rig) rig.ReduceMotion = S.ReduceMotion;
            if (audio) audio.Muted = !S.Sound;
            if (S.Steer == SteerMode.Tilt) { input.Tilt.Enable(); input.Tilt.NeedCal = true; }
        }

        void LoadSector(int n)
        {
            var lv = SectorGenerator.Make(n, S.Tune);
            sim.StartSector(lv, S.Tune);
            tunnel.Build(lv, sim.Path, Unlit);
            foreach (var h in hazards) if (h) Destroy(h.gameObject);
            hazards.Clear();
            foreach (var o in lv.Objs) hazards.Add(HazardView.Create(o, sim.Path, Unlit, o.Branch >= 0 ? lv.Fork.Pal[o.Branch] : lv.Pal, hazardRoot));
            bullets.SetLineColor(lv.Pal.Line);
            rig.Cam.backgroundColor = lv.Pal.Haze;
        }

        void Attract() { level = 1; LoadSector(1); sim.Playing = false; State = GameState.Title; StateT = 0; }
        void StartLevel(int n) { Debug.Log("[Doomsday] Sector " + n); level = n; LoadSector(n); State = GameState.Play; StateT = 0; Paused = false; hud.Popups.Clear(); input.Tilt.NeedCal = true; }
        void NewGame()
        {
            bestWas = S.Best; OverBest = false; audio.CancelQueued();
            sim.Score = 0; sim.Shield = 100; sim.Lives = G.StartSpares; sim.Invuln = 0;
            if (!seenIntro) { seenIntro = true; PlayScene(Script.Intro(), () => StartLevel(1)); } else StartLevel(1);
        }
        void PlayScene(Shot[] shots, System.Action done) { scene.Play(shots, done); State = GameState.Scene; StateT = 0; Paused = false; hud.Popups.Clear(); }

        void Advance()
        {
            if (StateT < 0.6f) return;
            if (State == GameState.Title || State == GameState.Over) NewGame();
            else if (State == GameState.Clear) { int n = level; var sc = Script.For(n); if (sc != null) PlayScene(sc, () => StartLevel(n)); else StartLevel(n); }
        }

        void Update()
        {
            float dt = Time.deltaTime; StateT += dt; hud.Tick(dt);
            input.Blocked = hud.PanelOpen;
            input.Reserved = input.TouchUI && State == GameState.Play && !Paused ? hud.PauseScreenRect : Rect.zero;
            latest = input.Read();
            if (latestOverride.HasValue) latest = latestOverride.Value;
            if (hud.PanelOpen)
            {
                if (input.PauseKey) hud.PanelOpen = false;
            }
            else if (panelWas) { if (hud.PanelChanged) { S.Save(); hud.PanelChanged = false; } ApplySettings(); }     // just closed
            else if (State == GameState.Scene)
            {
                scene.Update(dt);
                if (input.PauseKey) scene.End();                                                  // escape skips the scene
                else if (input.Advance) { if (hud.SkipHit(input.TapPos)) scene.End(); else scene.Advance(); }
            }
            else
            {
                bool tuneable = State == GameState.Title || State == GameState.Clear || State == GameState.Over || (State == GameState.Play && Paused);
                if (input.PauseKey && State == GameState.Play) SetPaused(!Paused);
                if (input.Advance)
                {
                    if (tuneable && hud.TuneHit(input.TapPos)) { hud.PanelOpen = true; }
                    else if (State == GameState.Play) { if (Paused) SetPaused(false); else if (input.TouchUI && hud.PauseHit(input.TapPos)) SetPaused(true); }
                    else Advance();
                }
            }
            panelWas = hud.PanelOpen;
            if (State == GameState.Title) { sim.Now += dt; sim.Pz = Mathf.Min(sim.Pz + 14 * dt, (sim.Sector.Fork?.Z0 ?? sim.Sector.Length) - 90); }   // the idle cruise stops short of the fork
            if (State == GameState.Respawn && StateT > 2) FinishRespawn();
            foreach (var h in hazards) h.Tick(sim.Now, sim.Pz);
            bullets.Sync(sim, sim.Path);
            rig.Place(sim, sim.Path, S.Tune.Sway, dt);
            audio.Engine(State == GameState.Play && !Paused, sim.Speed, sim.BoostEase, dt);
        }
        bool panelWas;

        void SetPaused(bool p) { Paused = p; if (!p) input.Tilt.NeedCal = true; }     // resuming re-centres the tilt to how the phone is held now

        void FixedUpdate()
        {
            if (State == GameState.Play && !Paused) { sim.Step(Time.fixedDeltaTime, latest); Drain(); }
            else if (State == GameState.Respawn || State == GameState.Over) { sim.Step(Time.fixedDeltaTime, default); Drain(); }   // coasting: it may still pass the fork
        }

        void OnApplicationPause(bool paused) { if (paused && State == GameState.Play) Paused = true; }
        void OnApplicationFocus(bool focus) { if (!focus && State == GameState.Play) Paused = true; }

        static readonly Dictionary<SimEventKind, string> Sounds = new Dictionary<SimEventKind, string>
        {
            { SimEventKind.Laser, "laser" }, { SimEventKind.Tink, "tink" }, { SimEventKind.Boom, "boom" }, { SimEventKind.Hit, "hit" },
            { SimEventKind.Pick, "pick" }, { SimEventKind.Refuel, "refuel" }, { SimEventKind.Clear, "clear" }, { SimEventKind.Beep, "beep" },
            { SimEventKind.Scrape, "scrape" }, { SimEventKind.BoostOn, "boostOn" }, { SimEventKind.BoostOff, "boostOff" }, { SimEventKind.BoostDry, "boostDry" },
        };

        void Drain()
        {
            foreach (var e in sim.Events)
            {
                switch (e.Kind)
                {
                    case SimEventKind.Popup: hud.Popups.Add(new Hud.Popup { Text = e.Text }); break;
                    case SimEventKind.Burst: { var at = sim.Path.World(e.X, e.Y, e.Z, e.Branch); sparks.Burst(at, e.Color, e.Count, 3f); if (e.Count >= 12) sparks.Burst(at, Palette.Ink, e.Count / 2, 4f); break; }
                    case SimEventKind.Fork: OnFork(e.Count); break;
                    case SimEventKind.ScreenBurst: sparks.ScreenBurst(e.X, e.Y, e.Color, e.Count); break;
                    case SimEventKind.Flash: hud.Flash = 1; hud.FlashColor = e.Color; break;
                    case SimEventKind.Shake: rig.Shake = 1; break;
                    case SimEventKind.SectorClear: OnSectorClear(); break;
                    case SimEventKind.ShipLost: case SimEventKind.TimeOut: LoseShip(e.Kind); break;
                    default: if (Sounds.TryGetValue(e.Kind, out var snd)) audio.Play(snd); break;
                }
            }
            sim.Events.Clear();
        }

        // The ship has taken a branch: the world takes on that branch's palette, and the other branch is dropped.
        void OnFork(int side)
        {
            var P = sim.Sector.Fork.Pal[side];
            rig.Cam.backgroundColor = P.Haze; bullets.SetLineColor(P.Line);
            foreach (var h in hazards) if (h.Obj.Branch >= 0 && h.Obj.Branch != side) h.Dropped = true;
        }

        void OnSectorClear()
        {
            Bonus = sim.SectorClear(); SaveBest();
            ClearPB = S.RecordTime(level, Bonus.SectorTime, S.Tune.FairSpeed);
            audio.Play("clear"); if (ClearPB) audio.Play("cheerSmall", 0.5f);
            hud.Flash = 1; hud.FlashColor = Palette.Ink;
            level++; State = GameState.Clear; StateT = 0;
        }

        // Losing a ship: with a spare in hand the run carries on, otherwise it is game over.
        void LoseShip(SimEventKind why)
        {
            hud.Flash = 1; hud.FlashColor = Palette.Warn; rig.Shake = 1; audio.Play("boom");
            sparks.ScreenBurst(0, 0, Palette.Warn, 40); sparks.ScreenBurst(0, 0, Palette.Ink, 20);
            if (sim.Lives <= 0) { GameOver(why); return; }
            sim.Lives--; RespawnWhy = why; State = GameState.Respawn; StateT = 0;
            DeadQuip = Quips.Pick(why == SimEventKind.ShipLost ? Quips.SpareShip : Quips.SpareTime);
        }
        void FinishRespawn()
        {
            if (RespawnWhy == SimEventKind.ShipLost) { sim.Respawn(); State = GameState.Play; StateT = 0; input.Tilt.NeedCal = true; hud.Popups.Add(new Hud.Popup { Text = "NEW SHIP" }); }
            else { StartLevel(level); sim.Invuln = 1.5f; hud.Popups.Add(new Hud.Popup { Text = "CLOCK RESET" }); }   // out of time: the sector starts over with a fresh clock
        }
        void GameOver(SimEventKind why)
        {
            OverBest = bestWas > 0 && (int)sim.Score > bestWas;                                 // a first ever run has nothing to beat
            DeadQuip = Quips.Pick(OverBest ? Quips.BestScore : why == SimEventKind.ShipLost ? Quips.Ship : Quips.Time);
            DeadWhy = why == SimEventKind.ShipLost ? "SHIP LOST" : "CORE DETONATED"; State = GameState.Over; StateT = 0; SaveBest();
            if (OverBest) audio.Play("cheerBig", 0.9f);
        }
        void SaveBest() { if (sim.Score > S.Best) { S.Best = (int)sim.Score; S.Save(); } }

        void OnGUI() { hud.Draw(this, sim, input); }
    }
}
