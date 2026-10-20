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
  It plays a few seconds of sector 1 on its own and saves frames there. Add `-shotz <metres>` to start that
  run part-way down the sector: sector 1's fork is at 420 m, so `-shotz 370` shows the widening and the split.
  `-shotsector 6` flies that sector instead (6 is the first Part Two sector, in hollow neon). `-shotscene` instead
  captures the opening cut scene, advancing a line or two between frames, and `-shotpanel` captures the settings panel.

```sh
Builds/DoomsdayTunnels3D.app/Contents/MacOS/"Doomsday Tunnels 3D" -shot /tmp/dt3d-shots -shotz 370 -screen-fullscreen 0 -screen-width 1600 -screen-height 740
```

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

## Privacy

The app follows the same rule as the web game: no accounts, no ads, no analytics service, nothing that
identifies a player. Play counts are anonymous daily totals and can be switched off in settings; a score is
sent only when the player chooses to post it. The scoreboard server is `_doomsday-tunnels-api/`.
