# Parking Practice — prototype 01

A first-person parking practice game for Windows and experimental desktop browsers, written in C# for Godot 4.7.2 Mono. Uses **free CC0 assets only**, with no vehicle brands.

Play in your browser: **[niposch.de/parking-trainer](https://niposch.de/parking-trainer/)**. Find browser tools and games at **[Live projects](https://niposch.de/live/)**.

## Play

Double-click **Launch.cmd**, or run `builds/ParkingPractice/ParkingPractice.exe` from its folder. The local runtime bundle uses the installed Godot version and the machine's .NET 8 runtime; it is not a standalone installer.

For Steam Input, add `builds/ParkingPractice/ParkingPractice.exe` as a non-Steam game. Set **Start In** to its containing folder and use an analog **Gamepad** layout. If needed, set launch options to `--path "C:\Users\Nickp\Development\parking-trainer\builds\ParkingPractice"`. Launch through Steam and press a controller button before choosing Drive.

See [controls and playing notes](docs/playing.md). Use **Esc → Choose scenario** for parking type, task, space difficulty, side, aisle, obstacles and coaching mode, or **Esc → Options** for display and control settings. Scenarios now start before the space, with a varied lateral offset and heading: drive up in D and choose your own setup position.

Scenarios start in P with the parking brake on. Hold L2/S, select D for the approach, then release the parking brake with Y/Space. Mouse look is captured while driving; Escape releases it. The pause menu includes a live pedal input test and rebinding. Steam keyboard/mouse layouts also support left-click gas/right-click brake as digital fallbacks; use a Gamepad layout for analog triggers.

## Included

- First-person cockpit, head look and shoulder view; no chase camera.
- Automatic creep, independent gas/brake, P/R/N/D, parking brake, finite steering rate, collision stopping.
- Seven procedural parking types: reverse parallel, reverse and forward 90° bays, forward 60° angled bays, reversing into a garage, reversing around a corner and turning in the road. Every type has a left/right side, three space difficulties and a varied forward approach.
- Three tasks: park, fix an off-centre park, and drive out. Optional obstacles (pillars, low posts below the sensors, crooked neighbours, tow bars, bike racks, trolleys, bins, signposts) and a narrow aisle with cars opposite. **Surprise me** picks a random combination.
- Graded parking report after securing the car: left/right and depth offsets in centimetres, angle, line/kerb/wall gaps, door clearance, contacts and habits, with a top-down replay of your path against the demonstration.
- Guided / Hints only / Test coaching: live tips on the approach and manoeuvre, and a computed correction plan drawn as a cyan line when you stop off-centre (F in Hints mode).
- Physically mounted mirrors and center-console camera/surround display; minimal digital speed, gear and parking-brake instruments behind the steering wheel.
- Fixed/steering rear guides, stable emulated bird’s-eye surround view, lower-pitched front/rear parking beepers with a volume control.
- Always-visible translucent desktop shortcuts and collapsible tutor instructions (H / T / controller D-pad up / Hint), landmark highlight, first-person demonstrations and a paused teaching map.
- In-world cues: checkpoint rings with instructions, predicted wheel tracks and clearance tags; a briefing card for each new kind of scenario; unmistakable contact feedback.
- Desktop onboarding: a **How to drive** page on first launch, a live key bar (pedals, wheel, gear, parking brake) and a next-step prompt. Both switch to controller labels after controller input.
- Six stepped bumper sensor indicators and four independent lateral bars on the console and automatic third-person inspection after securing the car.
- Bundled SDL 3.4.16 compatibility driver for the 2026 Steam Controller; the installed Godot remains unchanged.
- Diagram-based scenario selection; separate Options and controller setup pages; fullscreen, borderless fullscreen and windowed modes; field-of-view, eye-height and dead-zone settings.

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

See [validation and remaining limits](docs/prototype-validation.md), [requirements](docs/requirements.md), and [asset provenance](assets/THIRD_PARTY.md). The 2026 Steam Controller is detected by the compatibility driver and both physical analog triggers were checked with the user; a complete through-Steam play session remains to be checked. The generic vehicle, camera optics, and reference points are not calibrated to the lesson car.

This is a standalone Git repository at `C:\Users\Nickp\Development\parking-trainer`. Run Git commands from this directory. Source is published to [niposch/parking-trainer](https://github.com/niposch/parking-trainer); Pages deployment is documented in [deployment.md](docs/deployment.md).

For the browser prototype, double-click **Launch-Web.cmd**, or run `./serve-web.ps1 -Open`. The generated build is in `builds/web/`. Use an ordinary desktop browser for mouse capture and press a gamepad button to connect. See [browser controls, building and hosting](docs/web-playing.md) and the original [feasibility investigation](docs/web-feasibility.md). Physical standard-gamepad testing remains pending.

Touch controls: **Esc → Options → On-screen controls → Auto / On / Off**. Auto activates on touch devices or the first real touch; On works on desktop too. Drag empty space to look, hold the steering pad, and use the separate gas/brake pads. Tap higher on a pedal for more pressure. Hold the brake to select a gear, then release P BRAKE to move. The hint starts as a single `?` icon and remembers your preference. The game renders at the full window size in both native and web builds; menus reflow on narrow windows.
