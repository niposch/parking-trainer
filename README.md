# Parking Practice — prototype 01

A first-person parking practice game for Windows, written in C# for Godot 4.7.2 Mono. Uses **free CC0 assets only**, with no vehicle brands.

## Play

Double-click **Launch.cmd**, or run `builds/ParkingPractice/ParkingPractice.exe` from its folder. The local runtime bundle uses the installed Godot version and the machine's .NET 8 runtime; it is not a standalone installer.

For Steam Input, add `builds/ParkingPractice/ParkingPractice.exe` as a non-Steam game. Set **Start In** to its containing folder and use an analog **Gamepad** layout. If needed, set launch options to `--path "C:\Users\Nickp\Development\homelab\parking-trainer\builds\ParkingPractice"`. Launch through Steam and press a controller button before choosing Drive.

See [controls and playing notes](docs/playing.md).

Scenarios start in P with the parking brake on. Hold L2/S, select R/D, then release the parking brake with right-stick click/Space. Mouse look is captured while driving; Escape releases it. The pause menu includes a live pedal input test and rebinding. Steam keyboard/mouse layouts also support left-click gas/right-click brake as digital fallbacks; use a Gamepad layout for analog triggers.

## Included

- First-person cockpit, head look and shoulder view; no chase camera.
- Automatic creep, independent gas/brake, P/R/N/D, parking brake, finite steering rate, collision stopping.
- Procedural reverse parallel, reverse 90° bay and forward 90° bay exercises; repeatable variations.
- Physically mounted mirrors and center-console camera/surround display; minimal digital speed, gear and parking-brake instruments behind the steering wheel.
- Fixed/steering rear guides, four-camera surround composite, front/rear parking beepers.
- Optional tutor, landmark highlight, first-person demonstrations and a paused teaching map.
- Field-of-view, eye-height and dead-zone settings; controller menu navigation and keyboard fallback.

## Build and verify

Run PowerShell from this directory:

```powershell
./build.ps1 -Test -Import -Package
./build.ps1 -Run
```

`build.ps1` uses the installed editor at `C:\Program Files\Godot 4.7.2 Mono` and its bundled NuGet packages. It does not download dependencies. SDK/reference packs needed here are already installed. `.cache/` contains local build caches; `.godot/` and `builds/` are generated and excluded from Git.

Runtime input/UI checks:

```powershell
& 'C:\Program Files\Godot 4.7.2 Mono\Godot_v4.7.2-stable_mono_win64_console.exe' --headless --path . --fixed-fps 120 -- --smoke
```

See [validation and remaining limits](docs/prototype-validation.md), [requirements](docs/requirements.md), and [asset provenance](assets/THIRD_PARTY.md). No physical controller was connected during automated validation; Steam Input hardware behavior still needs the user's feedback. The generic vehicle, camera optics, and reference points are not calibrated to the lesson car.

This is a standalone Git repository inside the current workspace. Run Git commands from this directory. No remote or publishing destination is configured.
