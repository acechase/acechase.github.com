using System.Collections.Generic;
using DoomsdayTunnels.Audio;
using DoomsdayTunnels.Core;
using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Fx;
using DoomsdayTunnels.Hazards;
using DoomsdayTunnels.Input;
using DoomsdayTunnels.Online;
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
        [Tooltip("Base material with the Doomsday/Portal shader, for the exit's iris.")] public Material Portal;

        public GameState State = GameState.Title;
        public bool Paused;
        public float StateT;
        public ClearBonus Bonus;
        public bool ClearPB;                        // the sector just cleared was a personal best time
        public int ClearTier; public string ClearQuip = "", ClearPlace = ""; public float ClearBestWas;   // the celebration earned: 1 a personal best, 2 a place on record, 3 the top
        // The title's demo: after a few idle seconds a random sector is flown by a bot behind the menu. Nothing counts.
        public bool Attract; bool rested; float idle, attractT; Bot bot;
        const float AttractAfter = 6f, AttractMax = 90f; const int AttractSectors = 11;
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
        ShipView ship; DebrisView debris; Shockwaves shocks; Ambience ambience; PostFx post;
        readonly DynamicLights lights = new DynamicLights();
        ApiClient api; readonly RunLog run = new RunLog();
        InputLog log; GhostRunner ghost; ShipView ghostShip; bool respawned;
        public float GhostTime => ghost != null ? ghost.Seconds : 0;
        public bool Online => api != null;
        public bool CanPost => api != null && run.Token != null && !run.Posted && sim.Score > 0 && !DailyMode;
        // The daily sector: one sector a day from the date, with its own board of times.
        public bool DailyMode, DailyPB; public string DailyDay; public float DailyBestT; bool dailyPosted;
        public bool FairRun => S.Tune.FairSpeed;
        public bool CanPostDaily => api != null && DailyMode && State == GameState.Clear && run.Token != null && !dailyPosted && FairRun;
        public Quality Quality { get; private set; }
        Palette pal;                                // the palette of the tunnel the ship is in (a branch's after the fork)
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
            Quality = Quality.Detect(); lights.Budget = Quality.Lights; post = new PostFx(Quality);
            Debug.Log("[Doomsday] Quality " + Quality.Tier + " on " + SystemInfo.deviceModel + ", " + Screen.width + "x" + Screen.height + " at " + Screen.dpi + " dpi: unit " + UI.SafeArea.U.ToString("0.00") + " px");
            ship = new GameObject("Ship").AddComponent<ShipView>(); ship.Init(Unlit, Additive);
            ghostShip = new GameObject("Ghost").AddComponent<ShipView>(); ghostShip.Ghost = true; ghostShip.Init(Unlit, Additive); ghostShip.gameObject.SetActive(false);
            debris = new GameObject("Debris").AddComponent<DebrisView>(); debris.Init(Unlit); debris.Sim.Cap = Quality.DebrisCap;
            shocks = new GameObject("Shockwaves").AddComponent<Shockwaves>(); shocks.Init(Additive); shocks.Enabled = Quality.Shockwaves;
            ambience = new GameObject("Ambience").AddComponent<Ambience>(); ambience.Init(Additive, Unlit, Portal, cam, Quality);
            scene.Sound = n => audio.Play(n);
            var cfg = OnlineConfig.Load();
            if (cfg.Enabled) { api = new GameObject("Online").AddComponent<ApiClient>(); api.Config = cfg; }
            Debug.Log("[Doomsday] Online: " + (api != null ? cfg.api : "off (no Resources/OnlineConfig.json)"));
            hud.FetchBoard = () => api.FetchBoard(hud.BoardArrived);
            hud.PostScore = name => api.PostScore(name, (int)sim.Score, run.Sector, run.Token, run.Times(), (r, err) => { if (r != null) { run.Posted = true; S.Name = r.name; S.Save(); } hud.PostDone(r, err); });
            hud.FetchDaily = () => api.FetchDaily(DailyDay ?? Daily.Today(), hud.DailyArrived);
            hud.PostDaily = name => api.PostDaily(name, DailyDay, Mathf.RoundToInt(Bonus.SectorTime * 10), run.Token, (r, err) => { if (r != null) { dailyPosted = true; S.Name = r.name; S.Save(); } hud.DailyPostDone(r, err); });
            ApplySettings();
            TitleScreen();
            // Debug: "-shot <folder>" on the command line plays a few seconds unattended and saves screenshots there, so a
            // headless build can be checked by eye; "-shotz <metres>" starts that run part-way down the sector (say, just
            // before the fork); "-shotsector <n>" flies that sector instead of sector 1; "-shotscene" captures the opening
            // cut scene instead, a line or two per frame; "-shotpanel" captures the settings panel. Used by the README.
            var args = System.Environment.GetCommandLineArgs();
            foreach (var a in args) if (a == "-shot") shotting = true;
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shotz") float.TryParse(args[i + 1], out shotZ);
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-gate") { int.TryParse(args[i + 1], out int g); Ambience.Style = g; }   // try a look for the exit
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shotdpr") { float.TryParse(args[i + 1], out float d); UI.SafeArea.FakeDpr = d; }   // preview a phone's pixel density on a Mac
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shotsector") int.TryParse(args[i + 1], out shotSector);
            foreach (var a in args) { if (a == "-shotscene") shotScene = true; if (a == "-shotpanel") shotPanel = true; if (a == "-shotboard") shotBoard = true; if (a == "-shotghost") shotGhost = true; if (a == "-shotdaily") shotDaily = true; if (a == "-shottouch") shotTouch = true; if (a == "-shotattract") shotAttract = true; if (a == "-shotcheer") shotCheer = true; if (a == "-shotsafe") shotSafe = true; if (a == "-shotend") shotEnd = true; if (a == "-shotship") shotShip = true; }
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shot") StartCoroutine(ShotRun(args[i + 1]));
            Debug.Log("[Doomsday] Ready: sector " + sim.Sector.N + ", " + hazards.Count + " objects");
        }

        System.Collections.IEnumerator ShotRun(string folder)
        {
            System.IO.Directory.CreateDirectory(folder);
            if (shotTouch) input.TouchUI = true;
            if (shotSafe) UI.SafeArea.Fake = new Rect(59, 21, Screen.width - 118, Screen.height - 21);      // an iPhone held sideways: the island on the left, the home bar below
            yield return new WaitForSeconds(1.5f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "01-title.png"));
            if (shotAttract)
            {   // the demo: let the title idle past its threshold, then watch the bot fly
                shotting = false; yield return new WaitForSeconds(AttractAfter + 2f);
                for (int i = 0; i < 3; i++) { ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "0" + (i + 2) + "-attract.png")); yield return new WaitForSeconds(3f); }
                Application.Quit(); yield break;
            }
            if (shotCheer)
            {   // a personal best on the clear screen: plant a slow best, then skip to the end of the sector
                S.ForgetTime(1); S.RecordTime(1, 999f, true); seenIntro = true; NewGame(); sim.Pz = sim.Sector.Length - 30; latestOverride = new SimInput();
                yield return new WaitForSeconds(2.8f); ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "02-cheer.png"));
                yield return new WaitForSeconds(0.8f); ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "03-cheer.png"));
                Application.Quit(); yield break;
            }
            yield return new WaitForSeconds(0.5f);
            if (shotBoard)
            {
                hud.OpenBoard(); yield return new WaitForSeconds(2f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "02-board.png"));
                yield return new WaitForSeconds(0.5f); Application.Quit(); yield break;
            }
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
            if (shotDaily)
            {   // the daily sector, skipped to its last stretch so the clear screen and today's board can be seen
                StartDaily(); sim.Pz = sim.Sector.Length - 40; latestOverride = new SimInput { SteerX = 0.2f };
                yield return new WaitForSeconds(3.5f); ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "02-dailyclear.png"));
                yield return new WaitForSeconds(0.5f); hud.OpenDaily(); yield return new WaitForSeconds(2f); ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "03-dailyboard.png"));
                yield return new WaitForSeconds(0.5f); Application.Quit(); yield break;
            }
            seenIntro = true; NewGame();
            if (shotGhost)
            {   // fly a few seconds, keep that as the sector's ghost, then fly again on another line beside it
                latestOverride = new SimInput { SteerX = -0.5f, Fire = true }; yield return new WaitForSeconds(4f);
                S.SaveGhost(1, log.Serialize()); StartLevel(1); latestOverride = new SimInput { SteerX = 0.6f, SteerY = -0.3f };
                yield return new WaitForSeconds(2.5f); ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "02-ghost.png"));
                yield return new WaitForSeconds(1f); ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "03-ghost.png"));
                yield return new WaitForSeconds(0.5f); Application.Quit(); yield break;
            }
            if (shotSector > 1) StartLevel(shotSector);
            if (shotZ > 0) sim.Pz = shotZ;
            if (shotShip)
            {   // the ship from close behind, from the side, and under boost
                latestOverride = new SimInput(); sim.Pz = 60; yield return new WaitForSeconds(0.5f);
                var cam = rig.Cam.transform; rig.Free = true;
                foreach (var (name, off, look) in new[] { ("back", new Vector3(0.9f, 0.5f, -1.8f), Vector3.zero), ("side", new Vector3(2.0f, 0.4f, -0.3f), Vector3.zero), ("front", new Vector3(-1.2f, 0.6f, 1.9f), Vector3.zero) })
                {
                    var at = ship.transform.position; cam.position = at + off; cam.LookAt(at + look); yield return new WaitForSeconds(0.4f);
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "ship-" + name + ".png")); yield return new WaitForSeconds(0.3f);
                }
                latestOverride = new SimInput { Boost = true }; yield return new WaitForSeconds(1.2f);
                { var at = ship.transform.position; cam.position = at + new Vector3(1.1f, 0.5f, -2.2f); cam.LookAt(at); yield return new WaitForSeconds(0.3f); ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "ship-boost.png")); yield return new WaitForSeconds(0.3f); }
                Application.Quit(); yield break;
            }
            if (shotEnd)
            {   // the exit from three distances: the iris shut, half open, and wide
                latestOverride = new SimInput();
                foreach (var back in new[] { 60f, 30f, 12f }) { sim.Pz = sim.Sector.Length - back; sim.Speed = 2f; yield return new WaitForSeconds(0.6f); ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "exit-" + (int)back + "m.png")); yield return new WaitForSeconds(0.3f); }
                Application.Quit(); yield break;
            }
            Debug.Log("[Doomsday] Shot run: sector " + sim.Sector.N + " from " + sim.Pz + " m (args: " + string.Join(" ", System.Environment.GetCommandLineArgs()) + ")");
            for (int i = 0; i < 4; i++)
            {
                latestOverride = new SimInput { SteerX = (i % 2 == 0 ? 0.6f : -0.6f), SteerY = i == 1 ? 0.5f : 0f, Fire = true, Boost = i == 3 };
                if (shotTouch) { input.FireDown = true; input.BoostDown = i == 3; }
                yield return new WaitForSeconds(2.5f);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "0" + (i + 2) + "-play.png"));
            }
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
        SimInput? latestOverride; float shotZ; int shotSector; bool shotScene, shotPanel, shotBoard, shotGhost, shotDaily, shotting, shotTouch, shotAttract, shotCheer, shotSafe, shotEnd, shotShip;

        /// <summary>Settings that views hold a copy of: pushed whenever the panel closes.</summary>
        void ApplySettings()
        {
            hud.ReduceMotion = S.ReduceMotion; if (rig) rig.ReduceMotion = S.ReduceMotion; if (post != null) post.ReduceMotion = S.ReduceMotion;
            if (audio) audio.Muted = !S.Sound; Haptics.Enabled = S.Haptics;
            if (S.Steer == SteerMode.Tilt) { input.Tilt.Enable(); input.Tilt.NeedCal = true; }
        }

        void LoadSector(int n) => LoadSector(SectorGenerator.Make(n, S.Tune));
        void LoadSector(Sector lv)
        {
            int n = lv.N;
            { var sb = new System.Text.StringBuilder("[Doomsday] Sector " + n + " course:"); foreach (var f in lv.Course.Features) sb.Append(' ').Append(f.Kind).Append(' ').Append((int)f.S0).Append('-').Append((int)f.S1); Debug.Log(sb.ToString()); }
            sim.StartSector(lv, S.Tune);
            tunnel.Build(lv, sim.Path, Unlit);
            foreach (var h in hazards) if (h) Destroy(h.gameObject);
            hazards.Clear();
            foreach (var o in lv.Objs) hazards.Add(HazardView.Create(o, sim.Path, Unlit, o.Branch >= 0 ? lv.Fork.Pal[o.Branch] : lv.Pal, hazardRoot));
            bullets.SetLineColor(lv.Pal.Line);
            rig.Cam.backgroundColor = lv.Pal.Haze; pal = lv.Pal;
            ambience.SetSector(lv.Pal, lv.Length); ambience.BuildGate(lv.Pal, lv.Length, sim.Path, lv.Fork != null); debris.Sim.Clear(); debris.SetHaze(lv.Pal.Haze); lights.Lights.Clear();
        }

        void TitleScreen() { level = 1; DailyMode = false; LoadSector(1); sim.Playing = false; State = GameState.Title; StateT = 0; rested = false; idle = 0; }
        void StartAttract()
        {
            Attract = true; attractT = 0; idle = 0; audio.Silent = true; Haptics.Enabled = false; hud.ClearParty();
            AttractLevel();
        }
        void AttractLevel()
        {   // a fresh random sector, flown as a player would get it, clock and all
            sim.Score = 0; sim.Shield = 100; sim.Lives = 0; sim.Invuln = 0; DailyMode = false;
            level = 1 + Random.Range(0, AttractSectors); LoadSector(level); bot = new Bot(Random.Range(1, 1 << 30));
            State = GameState.Play; Paused = false; hud.Popups.Clear(); log = null; ghost = null;
        }
        void EndAttract(bool rest)
        {
            Attract = false; audio.Silent = false; Haptics.Enabled = S.Haptics; hud.Flash = 0; rig.Shake = 0; hud.Popups.Clear(); audio.CancelQueued();
            TitleScreen(); rested = rest; StateT = 1;                      // the plain title, ready: the tap that ended the demo can launch the game
        }
        void StartLevel(int n)
        {
            hud.ClearParty();
            Debug.Log("[Doomsday] Sector " + n); level = n; LoadSector(n); State = GameState.Play; StateT = 0; Paused = false; hud.Popups.Clear(); input.Tilt.NeedCal = true;
            // every run is recorded; the best one through this sector flies alongside as a ghost
            log = new InputLog(sim, S.Tune); respawned = false;
            var saved = S.ShowGhost && !DailyMode ? S.Ghost(n) : null; ghost = saved != null ? GhostRunner.From(saved, n) : null;
        }
        void StartDaily()
        {
            DailyMode = true; DailyDay = Daily.Today(); DailyPB = false; dailyPosted = false; DailyBestT = S.DailyBest(DailyDay);
            bestWas = S.Best; OverBest = false; audio.CancelQueued(); run.Begin(); hud.PostedAs = null; hud.DailyPostedAs = null; hud.PostStatus = ""; hud.DailyData = null;
            sim.Score = 0; sim.Shield = 100; sim.Lives = 0; sim.Invuln = 0;
            if (api != null) { string today = RunLog.Today(); RunLog.DayFlags(S.LastDay, today, out bool firstToday, out bool firstEver); S.LastDay = today; S.Save(); api.StartRun(S.CountPlays, firstToday, firstEver, input.TouchUI, input.TiltActive, tok => run.Token = tok); }
            var lv = Daily.Make(DailyDay, S.Tune); level = lv.N; Debug.Log("[Doomsday] Daily sector " + DailyDay + ": difficulty " + lv.N);
            LoadSector(lv); State = GameState.Play; StateT = 0; Paused = false; hud.Popups.Clear(); input.Tilt.NeedCal = true; log = null; ghost = null;
        }
        void NewGame()
        {
            bestWas = S.Best; OverBest = false; audio.CancelQueued();
            run.Begin(); hud.PostedAs = null; hud.PostStatus = "";
            if (api != null)
            {   // the anonymous play count: "first play today / ever" is all the server learns, and nothing when counting is off
                string today = RunLog.Today(); RunLog.DayFlags(S.LastDay, today, out bool firstToday, out bool firstEver);
                S.LastDay = today; S.Save();
                api.StartRun(S.CountPlays, firstToday, firstEver, input.TouchUI, input.TiltActive, tok => run.Token = tok);
            }
            sim.Score = 0; sim.Shield = 100; sim.Lives = G.StartSpares; sim.Invuln = 0;
            if (!seenIntro) { seenIntro = true; PlayScene(Script.Intro(), () => StartLevel(1)); } else StartLevel(1);
        }
        void PlayScene(Shot[] shots, System.Action done) { scene.Play(shots, done); State = GameState.Scene; StateT = 0; Paused = false; hud.Popups.Clear(); }

        void Advance()
        {
            if (StateT < 0.6f) return;
            if (State == GameState.Over && DailyMode) StartDaily();
            else if (State == GameState.Title || State == GameState.Over) NewGame();
            else if (State == GameState.Clear && DailyMode) TitleScreen();
            else if (State == GameState.Clear) { int n = level; var sc = Script.For(n); if (sc != null) PlayScene(sc, () => StartLevel(n)); else StartLevel(n); }
        }

        void Update()
        {
            float dt = Time.deltaTime; StateT += dt; hud.Tick(dt);
            input.Blocked = hud.Modal;
            input.Reserved = input.TouchUI && State == GameState.Play && !Paused ? hud.PauseScreenRect : Rect.zero;
            latest = input.Read();
            if (latestOverride.HasValue) latest = latestOverride.Value;
            // the demo: any input ends it and then does what it would have done anyway
            if (Attract && (input.Advance || input.PauseKey || latest.Fire || latest.Boost || Mathf.Abs(latest.SteerX) + Mathf.Abs(latest.SteerY) > 0.3f)) EndAttract(false);
            if (Attract) { attractT += dt; bot.Think(sim, dt); latest = bot.Input; if (attractT > AttractMax) EndAttract(true); }
            else if (State == GameState.Title && !hud.Modal && !rested && !shotting) { idle += dt; if (idle > AttractAfter) StartAttract(); }
            else idle = 0;
            if (hud.Modal)
            {
                if (input.PauseKey) { hud.PanelOpen = false; hud.BoardOpen = false; hud.PostOpen = false; }
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
                    else if (tuneable && hud.MuteHit(input.TapPos)) { S.Sound = !S.Sound; S.Save(); ApplySettings(); }
                    else if (tuneable && State != GameState.Title && hud.MenuHit(input.TapPos)) { if (api != null && State == GameState.Play && !run.Ended) { run.Ended = true; api.EndRun(run.Token, level, run.Cleared, Mathf.RoundToInt(run.Seconds), S.CountPlays); } TitleScreen(); }
                    else if (api != null && (State == GameState.Title || State == GameState.Over) && hud.ScoresHit(input.TapPos)) hud.OpenBoard();
                    else if (State == GameState.Title && hud.DailyHit(input.TapPos)) StartDaily();
                    else if (CanPostDaily && hud.DailyPostHit(input.TapPos)) hud.OpenDailyPost(S.Name);
                    else if (api != null && DailyMode && State == GameState.Clear && hud.DailyTimesHit(input.TapPos)) hud.OpenDaily();
                    else if (State == GameState.Over && CanPost && hud.PostHit(input.TapPos)) hud.OpenPost(S.Name);
                    else if (State == GameState.Play) { if (Paused) SetPaused(false); else if (input.TouchUI && hud.PauseHit(input.TapPos)) SetPaused(true); }
                    else Advance();
                }
            }
            panelWas = hud.PanelOpen;
            if (State == GameState.Title) { sim.Now += dt; sim.Pz = Mathf.Min(sim.Pz + 14 * dt, (sim.Sector.Fork?.Z0 ?? sim.Sector.Length) - 90); }   // the idle cruise stops short of the fork
            if (State == GameState.Play && !Paused) run.Seconds += dt;
            if (sim.GunHot != gunWasHot) { gunWasHot = sim.GunHot; if (sim.GunHot) Haptics.Play(Haptic.Warning); }
            if (State == GameState.Respawn && StateT > 2) FinishRespawn();
            foreach (var h in hazards) h.Tick(sim.Now, sim.Pz, sim.Rx, sim.Ry);
            bullets.Sync(sim, sim.Path);
            rig.Place(sim, sim.Path, S.Tune.Sway, dt);
            ship.Place(sim, sim.Path, pal.Line, pal.Haze, dt, sparks);
            bool showGhost = ghost != null && !ghost.Done && (State == GameState.Play || State == GameState.Respawn);
            if (ghostShip.gameObject.activeSelf != showGhost) ghostShip.gameObject.SetActive(showGhost);
            if (showGhost) ghostShip.Place(ghost.Sim, sim.Path, pal.Line, pal.Haze, dt, sparks);
            audio.Engine(State == GameState.Play && !Paused, sim.Speed, sim.BoostEase, dt);
            // the game's lights: the engine, the shots in flight, the ship token, and whatever is flashing
            lights.BeginFrame();
            if (ship.gameObject.activeSelf) lights.Hold(ship.EnginePos, pal.Line, 2.6f, 0.25f + 0.5f * sim.BoostEase);
            for (int i = 0; i < sim.Bullets.Count && i < 4; i++) { var b = sim.Bullets[i]; lights.Hold(sim.Path.World(b.X, b.Y, b.Z + 1.3f, b.Branch), pal.Line, 1.8f, 0.22f); }
            foreach (var h in hazards) if (h.Obj.Kind == ObjKind.Ship && !h.Obj.Taken && !h.Dropped && h.gameObject.activeSelf) lights.Hold(h.transform.position, Palette.Pick, 4f, 0.7f + 0.25f * Mathf.Sin(sim.Now * 5));
            lights.Tick(dt); lights.Apply(rig.transform.position, rig.transform.forward);
            debris.Sim.Tick(dt, (z, br) => br >= 0 ? G.HW : sim.Path.BuiltHalfWidth(z)); debris.Sync(sim.Path);
            shocks.Tick(dt);
            ambience.Tick(sim, sim.Path, dt, S.ReduceMotion);
            if (ambience.Open > 0 && !portalSaid && State == GameState.Play) { portalSaid = true; audio.Play("portal"); } else if (ambience.Open <= 0) portalSaid = false;
            post.Tick(sim.BoostEase, State == GameState.Play ? sim.Shield : 100, dt);
        }
        bool panelWas, gunWasHot; float hitStop; bool portalSaid;

        void Record(in SimInput q) { log?.Record(q, respawned); respawned = false; ghost?.Advance(Time.fixedDeltaTime); }

        void SetPaused(bool p) { Paused = p; if (!p) input.Tilt.NeedCal = true; }     // resuming re-centres the tilt to how the phone is held now

        void FixedUpdate()
        {
            if (hitStop > 0) { hitStop -= Time.fixedDeltaTime; return; }                    // a hit freezes the world for a few frames
            if (State == GameState.Play && !Paused) { var q = InputLog.Quantise(latest); sim.Step(Time.fixedDeltaTime, q); Record(q); Drain(); }
            else if (State == GameState.Respawn || State == GameState.Over) { sim.Step(Time.fixedDeltaTime, default); if (State == GameState.Respawn) Record(default); Drain(); }   // coasting: it may still pass the fork
        }

        void OnApplicationPause(bool paused) { if (paused && State == GameState.Play) Paused = true; }
        void OnApplicationFocus(bool focus) { if (!focus && State == GameState.Play) Paused = true; }

        static readonly Dictionary<SimEventKind, string> Sounds = new Dictionary<SimEventKind, string>
        {
            { SimEventKind.Laser, "laser" }, { SimEventKind.Tink, "tink" }, { SimEventKind.Boom, "boom" },
            { SimEventKind.Pick, "pick" }, { SimEventKind.Refuel, "refuel" }, { SimEventKind.Clear, "clear" }, { SimEventKind.Beep, "beep" },
            { SimEventKind.BoostOn, "boostOn" }, { SimEventKind.BoostOff, "boostOff" }, { SimEventKind.BoostDry, "boostDry" },
        };

        void Drain()
        {
            foreach (var e in sim.Events)
            {
                Haptics.Play(Haptics.For(e.Kind));
                switch (e.Kind)
                {
                    case SimEventKind.Popup: hud.Popups.Add(new Hud.Popup { Text = e.Text }); break;
                    case SimEventKind.Burst:
                        {
                            var at = sim.Path.World(e.X, e.Y, e.Z, e.Branch); sparks.Burst(at, e.Color, e.Count, 3f);
                            if (e.Count >= 12) { sparks.Burst(at, Palette.Ink, e.Count / 2, 4f); debris.Sim.Burst(e.X, e.Y, e.Z, e.Branch, e.Count / 2, e.Color, 0.16f, 7f); }
                            else if (e.Count >= 4) lights.Flash(at, e.Color, 2.5f, 0.8f, 0.15f);
                            break;
                        }
                    case SimEventKind.Shock: { var at = sim.Path.World(e.X, e.Y, e.Z, e.Branch); shocks.Add(at, sim.Path.Rotation(e.Z), e.Color); lights.Flash(at, e.Color, 7f, 2f, 0.4f); break; }
                    case SimEventKind.Hit: audio.Play("hit"); post.Hit(1f); rig.Impulse(new Vector2(Random.value - 0.5f, Random.value - 0.5f).normalized, 0.9f); break;
                    case SimEventKind.Scrape: audio.Play("scrape"); rig.Impulse(new Vector2(-sim.ScrapeX, sim.ScrapeY), 0.35f); post.Hit(0.3f); break;
                    case SimEventKind.Fork: OnFork(e.Count); break;
                    case SimEventKind.ScreenBurst: sparks.ScreenBurst(e.X, e.Y, e.Color, e.Count); break;
                    case SimEventKind.Flash: hud.Flash = 1; hud.FlashColor = e.Color; break;
                    case SimEventKind.Shake: rig.Shake = 1; ship.Jolt(2.5f, -3f); if (!Attract) hitStop = 0.09f; break;      // a crash or a wall bounce: the hull is knocked back and tumbles, and the world freezes for a beat
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
            rig.Cam.backgroundColor = P.Haze; bullets.SetLineColor(P.Line); pal = P;
            ambience.SetPalette(P); debris.Sim.DropBranch(side); debris.SetHaze(P.Haze);
            foreach (var h in hazards) if (h.Obj.Branch >= 0 && h.Obj.Branch != side) h.Dropped = true;
        }

        void OnSectorClear()
        {
            {   // through the gate: a ring and a blaze of light at the exit
                var at = sim.Path.World(0, 0, sim.Sector.Length); shocks.Add(at, sim.Path.Rotation(sim.Sector.Length), Palette.Ink, 9f, 0.9f); lights.Flash(at, Palette.Ink, 16f, 4f, 0.9f); rig.Impulse(new Vector2(0, 0.6f), 0.5f);
            }
            if (Attract) { AttractLevel(); return; }
            Bonus = sim.SectorClear();
            if (DailyMode)
            {   // the daily sector: a time, a best for today, and the screen that offers to post it
                DailyPB = S.RecordDaily(DailyDay, Bonus.SectorTime); DailyBestT = S.DailyBest(DailyDay);
                audio.Play("clear"); if (DailyPB) audio.Play("cheerSmall", 0.5f);
                hud.Flash = 1; hud.FlashColor = Palette.Ink; State = GameState.Clear; StateT = 0; ghost = null;
                if (api != null) api.EndRun(run.Token, level, 1, Mathf.RoundToInt(run.Seconds), S.CountPlays);
                return;
            }
            SaveBest();
            bool fair = S.Tune.FairSpeed, best = fair && (S.BestTime(level) <= 0 || Bonus.SectorTime < S.BestTime(level));
            if (best && log != null) S.SaveGhost(level, log.Serialize());        // the ghost is the best run so far, from its very first time through
            ClearBestWas = S.BestTime(level);
            ClearPB = S.RecordTime(level, Bonus.SectorTime, fair); run.SectorCleared(level, Bonus.SectorTime, fair);
            ghost = null;
            // A time for the record book gets the full fanfare; beating your own best for the sector gets a smaller one.
            // The record is the shared top three for this sector, when the board has been seen this session.
            int place = 0;
            if (fair && run.Token != null && hud.Board != null) foreach (var st in hud.Board.times) if (st.sector == level)
            {
                int t = Mathf.RoundToInt(Bonus.SectorTime * 10); bool mineAlready = false; int faster = 0;
                foreach (var r in st.top) { if (r.name == S.Name) { if (r.t <= t) mineAlready = true; } else if (r.t <= t) faster++; }
                if (!mineAlready) place = faster + 1; if (place > 3) place = 0;
            }
            ClearTier = place == 1 ? 3 : place > 0 ? 2 : ClearPB ? 1 : 0;
            ClearPlace = place == 1 ? "1ST" : place == 2 ? "2ND" : place == 3 ? "3RD" : "";
            ClearQuip = ClearTier > 1 ? Quips.Pick(Quips.Record) : ClearTier == 1 ? Quips.Pick(Quips.PbTime) : "";
            if (ClearTier > 1) Celebrate(ClearTier, 0); else { audio.Play("clear"); if (ClearTier == 1) Celebrate(1, 0.5f); }
            hud.Flash = 1; hud.FlashColor = Palette.Ink;
            level++; State = GameState.Clear; StateT = 0;
        }

        // Losing a ship: with a spare in hand the run carries on, otherwise it is game over.
        void LoseShip(SimEventKind why)
        {
            hud.Flash = 1; hud.FlashColor = Palette.Warn; rig.Shake = 1; audio.Play("boom"); post.Hit(1.5f);
            if (Attract) { sparks.ScreenBurst(0, 0, Palette.Warn, 40); AttractLevel(); return; }      // the bot lost its ship or the clock ran out: just another sector
            sparks.ScreenBurst(0, 0, Palette.Warn, 40); sparks.ScreenBurst(0, 0, Palette.Ink, 20);
            if (why == SimEventKind.ShipLost)
            {   // the ship comes apart: hull chunks, a shockwave and a flash where it was
                var at = sim.Path.World(sim.Rx, sim.Ry, sim.Pz); sparks.Burst(at, Palette.Warn, 30, 5f); sparks.Burst(at, Palette.Ink, 20, 6f);
                debris.Sim.Burst(sim.Rx, sim.Ry, sim.Pz, sim.Path.Chosen, 18, Palette.Ink, 0.14f, 3f); debris.Sim.Burst(sim.Rx, sim.Ry, sim.Pz, sim.Path.Chosen, 8, Palette.Warn, 0.1f, 3f);
                shocks.Add(at, sim.Path.Rotation(sim.Pz), Palette.Warn, 4f, 0.6f); lights.Flash(at, Palette.Warn, 10f, 4f, 0.6f);
            }
            if (sim.Lives <= 0) { GameOver(why); return; }
            sim.Lives--; RespawnWhy = why; State = GameState.Respawn; StateT = 0;
            DeadQuip = Quips.Pick(why == SimEventKind.ShipLost ? Quips.SpareShip : Quips.SpareTime);
        }
        void FinishRespawn()
        {
            if (RespawnWhy == SimEventKind.ShipLost) { sim.Respawn(); respawned = true; State = GameState.Play; StateT = 0; input.Tilt.NeedCal = true; hud.Popups.Add(new Hud.Popup { Text = "NEW SHIP" }); }
            else { StartLevel(level); sim.Invuln = 1.5f; hud.Popups.Add(new Hud.Popup { Text = "CLOCK RESET" }); }   // out of time: the sector starts over with a fresh clock
        }
        void GameOver(SimEventKind why)
        {
            OverBest = bestWas > 0 && (int)sim.Score > bestWas;                                 // a first ever run has nothing to beat
            DeadQuip = Quips.Pick(OverBest ? Quips.BestScore : why == SimEventKind.ShipLost ? Quips.Ship : Quips.Time);
            DeadWhy = why == SimEventKind.ShipLost ? "SHIP LOST" : "CORE DETONATED"; State = GameState.Over; StateT = 0; SaveBest();
            if (DailyMode) OverBest = false; else if (OverBest) Celebrate(2, 0.9f);
            run.Sector = level; run.Ended = true;
            if (api != null) api.EndRun(run.Token, level, run.Cleared, Mathf.RoundToInt(run.Seconds), S.CountPlays);
        }
        // Tier 1: a personal best. Tier 2: a place in the record book. Tier 3: the very top of it.
        void Celebrate(int tier, float delay)
        {
            audio.Play(tier > 2 ? "cheerGrand" : tier > 1 ? "cheerBig" : "cheerSmall", delay);
            hud.Party(new[] { 0, 36, 70, 110 }[tier], delay + 0.1f, new[] { 0, 0, 16, 44 }[tier], Time.time);
            if (tier > 1) Haptics.Play(Haptic.Success);
        }
        void SaveBest() { if (sim.Score > S.Best) { S.Best = (int)sim.Score; S.Save(); } }

        void OnGUI() { hud.Draw(this, sim, input); }
    }
}
