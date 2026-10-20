# Doomsday Tunnels 3D

The Unity remake of [Doomsday Tunnels](https://acechase.github.io/doomsday-tunnels/): the same tunnel flyer,
rebuilt as a native iPhone and Android app with real 3D geometry, lighting and physics. The web game is
untouched and stays where it is; this is its sibling, not its replacement.

This folder starts with an underscore so GitHub Pages does not publish it with the site. Builds are never
committed. The plan, milestones and design notes live in the project changelog once there is something to
show; until then see the commit history.

## Opening the project

- Unity **6.3 LTS** (6000.3.x) via Unity Hub, with the iOS and Android build support modules.
- Xcode for iPhone builds and App Store submission. Its major version must be at least the iOS major
  version on the phone you run on: Xcode ships the developer disk image that iOS needs, and an older Xcode
  reports "The developer disk image could not be mounted on this device". Add your Apple ID under
  Xcode > Settings > Accounts once; the project uses automatic signing.
- In Unity Hub choose Add > Add project from disk and pick this folder.

Everything we author is under `Assets/_Project/`. Anything Unity or a package adds at the top of `Assets/`
is either moved in there by the bootstrap or deleted.

## Project settings as code

`Assets/_Project/Editor/ProjectBootstrap.cs` holds every player setting that matters (name, bundle id,
landscape only, iOS 15+, Android 7.1+ 64-bit, Input System, graphics APIs). Run it from the menu
**Doomsday > Apply Project Settings** after pulling a change to it, or headless:

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" -logFile - \
  -executeMethod DoomsdayTunnels.EditorTools.ProjectBootstrap.Apply
```

## Building

From the menu, **Doomsday > Build**, or headless (`Assets/_Project/Editor/Builds.cs`):

```sh
# iPhone: writes an Xcode project to Builds/iOS
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" -logFile - -buildTarget iOS \
  -executeMethod DoomsdayTunnels.EditorTools.Builds.IOS
open Builds/iOS/Unity-iPhone.xcodeproj      # choose your phone, press Run (first time: see below)

# Android: an APK to sideload (adb install -r Builds/DoomsdayTunnels3D.apk), or an AAB for Google Play
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" -logFile - -buildTarget Android \
  -executeMethod DoomsdayTunnels.EditorTools.Builds.AndroidApk
```

### First run on an iPhone

1. Xcode > Settings > Accounts: sign in with the Apple ID that holds the Developer Program membership.
2. Plug the phone in, unlock it, and tap Trust. On iOS 16+ also turn on Settings > Privacy & Security >
   Developer Mode on the phone (it reboots).
3. Open `Builds/iOS/Unity-iPhone.xcodeproj`, select the `Unity-iPhone` target, Signing & Capabilities, and
   pick your Team. Xcode creates the development certificate and profile on the spot.
4. Choose the phone in the run destination bar and press Run.

Unity rewrites the Xcode project on every build, so to keep the team selection put the ten-character Team
ID (shown next to the team name in that Signing panel) in a file called `.apple-team-id` at the project
root. It is ignored by git; `ProjectBootstrap.Apply` reads it so every generated project is pre-signed.

Switching build target re-imports textures for that platform, so the first iOS or Android build takes a
while; later ones are quick.

## Checking it works

- Edit-mode tests (sector generator determinism, the RNG and the fork layout against the web game, the flight model,
  the fork and the ship token, the story script, the scene player and every cut-scene set, the settings store, the
  tilt maths, every synthesised sound and the Part Two palette). With
  `DT3D_SCENE_SHOTS` set to a folder, the cut-scene test also writes one PNG per set there, to be looked at:

```sh
"$UNITY" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/dt3d-tests.xml
DT3D_SCENE_SHOTS=/tmp/dt3d-scenes "$UNITY" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/dt3d-tests.xml
```

- Screenshots without a phone: build **Doomsday > Build > macOS (debug)** and run it with a folder to write to.
  The first macOS build after an iOS export has twice come out rendering black with "Render Graph Execution
  error" in its log (the target switch rewrites the pipeline settings asset); building a second time fixes it.
  It plays a few seconds of sector 1 on its own and saves frames there. Add `-shotz <metres>` to start that
  run part-way down the sector: sector 1's fork is at 420 m, so `-shotz 370` shows the widening and the split.
  `-shotsector 6` flies that sector instead (6 is the first Part Two sector, in hollow neon). `-shotscene` instead
  captures the opening cut scene, advancing a line or two between frames, `-shotpanel` captures the settings panel and
  `-shotboard` opens the scoreboard (needs an `OnlineConfig.json`; `wrangler dev` in the API folder serves a local one), and
  `-shottouch` draws the thumb controls as a phone would; `-shotattract` waits for the title's demo; `-shotcheer` stages
  a personal best on the clear screen; `-shotghost` records a few seconds as sector 1's ghost and flies beside it; `-shotdaily` skips to the end of the daily
  sector for its clear screen and today's board.

```sh
Builds/DoomsdayTunnels3D.app/Contents/MacOS/"Doomsday Tunnels 3D" -shot /tmp/dt3d-shots -shotz 370 -screen-fullscreen 0 -screen-width 1600 -screen-height 740
```

## What differs from the web game

- The tunnel is a real 3D course (`Tunnel/Course.cs`, `Tunnel/TunnelPath.cs`): climbs, dives, hard bends, loops
  and corkscrews, drawn from the sector's own second random stream so the obstacle layout still matches the web
  game draw for draw. Sector 1 only climbs and dives; loops arrive in sector 3 and corkscrews in 4. The flight model
  never sees any of it: it flies in tunnel coordinates, so sector times stay comparable. The course is logged at the
  start of each sector ("Sector 3 course: Loop 104-230 ...") for finding a feature with `-shotz`.
- Everything on screen is sized in the web game's unit, the screen height in points over 400 (`UI/SafeArea.U`), scaled
  back to device pixels through `Screen.dpi`, so text on a 3x phone is the size the web game draws it, not a third.
- The ship (`Fx/ShipMesh.cs`) is built from code: a lofted fuselage with a canopy, swept wings with tip fins, a tail
  fin and twin engine nacelles with nozzle rings, each with a flare and a hot core that stretch under boost; its edges
  are a separate line mesh, the neon outline that is all a ghost shows. `-shotship` frames it from several angles.
- The exit is a portal (`Shaders/Portal.shader`): an iris of eight blades across the end of the tunnel that kaleidoscopes
  open over the last 70 m, revealing a vortex of grid rings and spokes rushing inward in the sector's colour, with a
  rising sound as it starts to open. A wall hit freezes the world for a beat, knocks the hull back, flashes the screen
  and the slab, and throws sparks and chunks, on every bounce. A TITLE button on the pause, clear and over screens goes
  back to the menu (and the daily sector).
- Discs, rings, arcs and capped strokes in the HUD go through IMGUI's anti-aliased rounded-rectangle path
  (`Disc`, `Circle`, `RoundBox` in `UI/Hud.cs`), not pixel boxes, so the thumb gauges have clean edges.
- A wall hit is unmistakable: the ship's nose is held touching the slab, the hull is knocked back and tumbles on a
  spring, and the slab flashes white and settles to its dented shade, on every bounce.
- The HUD lays itself out inside the phone's safe area (`UI/SafeArea.cs`): nothing sits under the Dynamic Island or the
  home bar. `-shotsafe` fakes those insets on a Mac, and `-shotdpr 2.9` a phone's pixel density. The exit of every sector is a lit gate, a white frame inside two in
  the sector's colour with strips chasing toward it, and clearing it sets off a ring and a blaze of light. A red slab
  is met by the ship's nose (42 cm ahead of its centre) and counts when a wing (24 cm to each side) would clip it, so
  the drawn ship never passes into a wall before the hit lands. The aiming reticle shows in every
  mode now, since the chase camera sits off the gun line. Cut-scene letters blip at the speaker's pitch, as on the web.
- What the web game's changelog added, checked item by item: the title plays itself after six idle seconds (a bot
  flies a random sector behind the menu, `Flight/Bot.cs`; nothing counts, no sound, any input ends it, and after
  ninety seconds it rests); the fire and boost buttons are their own gauges (`UI/Hud.Controls.cs`: a bright disc
  with an icon that drains, dark and hollow when spent, a red blink just before and when pressed anyway, a ring
  when it comes back), so touch screens drop the BOOST and GUNS bars; celebrations (`UI/Hud.Celebrate.cs`:
  confetti, a headline in the five sector colours, a line from the cast) for a personal best, a place on the record
  (when the board has been seen this session) and a best score; the shield bar breaks into pieces when it gives
  out; the gear and speaker buttons; the web game's two typefaces (Syncopate, Share Tech Mono, both open licences,
  in `Resources/Fonts`). Not carried over: the manual, the changelog page, offline caching and home-screen
  install, which a native app has no need of.
- Haptics (`Input/Haptics.cs`): iPhones get the Taptic Engine through `Assets/Plugins/iOS/DoomsdayHaptics.mm` (impact
  and notification feedback), Android the view's haptic feedback (no permission needed). A heavy knock on a hit or a wall bounce, light taps on scrapes and
  pickups, medium for boost, success for the token and a cleared sector, warning for overheated guns and a dry boost.
- The daily sector (`Tunnel/Daily.cs`): DAILY on the title flies one sector a day that everyone gets, built from the
  date alone (difficulty 4 to 7 and the seed both come from the days since 2026-01-01; the Worker uses the same
  formulas to judge posted times). No spares, one go per attempt; the clear screen shows the time and the device's best
  today and offers POST TIME and TODAY'S TIMES, the ten fastest, when online. The run token and the usual plausibility
  checks apply; a new sector arrives at midnight UTC.
- Ghosts. Every run through a sector is recorded as its inputs (`Flight/InputLog.cs`, five bytes a step); the best
  fair-speed run is kept on the device (`ghost.<sector>`) and flies alongside next time as a dim outline
  (`Flight/GhostRunner.cs` replays it in a sim of its own). The flight model is deterministic, so the replay is
  exact; the live sim is fed the same quantised input the log keeps. Off in settings with GHOST OF YOUR BEST.
- Red walls are solid. The web game lets the ship punch through for a shield hit; here it bounces off, is shoved
  toward the nearest gap, and has to pick its speed back up (boost helps). Only the first hit on a wall costs shield.

## The look

Milestone 3 is the high-end pass, all of it cosmetic so sector times stay comparable:

- `Shaders/Unlit.shader` takes up to eight of the game's own point lights as global arrays (`Fx/DynamicLights.cs`):
  explosions and impacts flash, shots glow as they fly, the ship's engine and the ship token shine. A light brightens
  a surface in its own colour, so the flat neon look survives.
- `Fx/ShipView.cs` is the ship the web game never showed, a dart with glowing edges that bank into turns, an engine
  flare that stretches under boost, edges that redden as the shield drains and sparks when it is nearly gone.
  `Fx/CameraRig.cs` is a chase camera on a soft arm behind it, with spring-mounted kicks away from scraped walls.
- `Fx/DebrisSim.cs` shatters gates, mines and the ship into chunks that tumble, fall and bounce off the real tunnel
  box, in tunnel coordinates, with no colliders. `Fx/Shockwaves.cs` draws the rings. Mines nudged by a near miss
  swing on a spring; the token wobbles.
- `Fx/Ambience.cs` is the exit glow at the far end, the dust motes that give speed something to be measured
  against, and the streaks that rush past the lens under boost. `Fx/PostFx.cs` drives bloom, the chromatic
  aberration that creeps in under boost and spikes on a hit, and the vignette that closes in on a low shield.
- `Core/Quality.cs` picks a tier from the device (iPhone generation from the model string, memory elsewhere) and
  sizes bloom, the light count, the debris cap, the motes and the render scale to it. The tier is logged at start.

## Settings, sound and what is saved

The TUNE button on the title, pause, sector-clear and game-over screens opens CONTROLS AND FEEL: thumb stick or
tilt steering (tilt re-centres to how the phone is held whenever a sector starts or the game resumes; the stick then
aims the guns), the seven feel sliders from the web game, sound, play counting and reduced motion. Everything is kept
in `PlayerPrefs` under these keys:

| Key | What |
| --- | --- |
| `tune.steer` `tune.snap` `tune.speed` `tune.pull` `tune.twist` `tune.sway` `tune.tilt` | the sliders (floats) |
| `ctl.mode` | 0 thumb stick, 1 tilt |
| `ctl.invertY` | 1 to invert tilt up/down |
| `sound` | 1 on (default), 0 muted |
| `countPlays` | 1 on (default): anonymous daily play counts, once the app is online |
| `reduceMotion` | 1 to turn off shake, banking, flashes and the tape effects |
| `haptics` | 1 on (default): a buzz on hits, bounces, boost, the token, a cleared sector, overheated guns |
| `ghost` | 1 on (default): the ghost of your best run flies alongside |
| `best` | best score on this device |
| `pb.<sector>` | fastest time through that sector at the standard flight speed |

There are no sound files: `Scripts/Audio/Synth.cs` renders the web game's WebAudio recipes (an oscillator sliding
between two pitches with a fading gain, and filtered noise) into clips on first use, and the engine hum is a one-second
loop whose pitch and volume follow the ship. The cut scenes play their static, jingles and stings through the same
table.

## Git

- `.gitignore` keeps Unity's generated folders (`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`)
  out of the repository.
- `.gitattributes` sends binary assets (images, audio, fonts, models) through Git LFS. Install it once per
  machine with `brew install git-lfs && git lfs install`.
- Scenes, prefabs and other Unity YAML files merge with Unity's own tool. Register it once per clone:

```sh
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver \
  "'/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/Resources/UnityYAMLMerge' merge -p %O %A %B %A"
git config merge.unityyamlmerge.recursive binary
```

## Online: the scoreboard and play counts

The scoreboard server is `_doomsday-tunnels-api/`; the app talks to its `/v2/*` routes (`Scripts/Online/`). To
switch it on, copy `Assets/_Project/Resources/OnlineConfig.example.json` to `OnlineConfig.json` next to it and put
the Worker's `APP_KEY` secret in it. That file is not in git. Without it the game is simply offline: no SCORES
button, no counts. With it:

- starting a run tells the server "a run began" (plus "first play today / ever" for the anonymous player count,
  and nothing at all when COUNT MY PLAYS is off in settings) and gets a signed token back;
- the game-over screen offers POST SCORE, which sends the name typed, the score, the sector and the run's sector
  times (at the standard flight speed only), and shows the rank that comes back;
- SCORES on the title and game-over screens shows the app's own board: the top twenty and the fastest times per
  sector. The web game's board is separate.

## Privacy

The app follows the same rule as the web game: no accounts, no ads, no analytics service, nothing that
identifies a player. Play counts are anonymous daily totals and can be switched off in settings; a score is
sent only when the player chooses to post it.
