# Parking Practice

A first-person parking trainer: drive up, line up and park a generic car using its mirrors, reversing camera and parking sensors, with a tutor that shows what went wrong and how to fix it. Written in C# for Godot 4.7.2 Mono. Free CC0 assets only, no vehicle brands.

**Play in your browser: [niposch.de/parking-trainer](https://niposch.de/parking-trainer/)** · more projects at [niposch.de/live](https://niposch.de/live/)

## What you can practise

| Parking type | Notes |
| --- | --- |
| Reverse parallel | Kerb on the right, or the left for a one-way street |
| Reverse 90° bay | Optional row of cars opposite, with a narrow aisle |
| Forward 90° bay | Optional opposite row |
| Forward 60° angled bay | Entered in the direction of travel |
| Reverse into a garage | Walls on three sides |
| Reverse around a corner | Finish parallel to the side road's kerb |
| Turn in the road | Multi-point turn into the opposite lane |

Each type has a left/right side and three space sizes (spacious, standard, tight). Every scenario starts on a drive-up approach, so you choose where to stop and set up. You can also pick a task:

- **Park**: drive up and park.
- **Fix an off-centre park**: start badly parked and correct it.
- **Drive out**: leave the space without touching anything.

Optional obstacles include pillars, low posts below the sensors, crooked neighbours, tow bars, bike racks, trolleys and bins. **Surprise me** picks a random combination. Every layout is generated together with a collision-checked solution, which the demonstration drives.

## How it teaches

- **Coaching levels**: *Guided* (everything on), *Hints only* (help once you stop) and *Test* (no aids, graded at the end).
- **Briefing card** for each new kind of scenario: the goal, which way to drive first, and how to finish.
- **In-world cues**: checkpoint rings with instructions where to stop or change gear, predicted wheel tracks for your current steering, and a clearance tag at the nearest obstacle.
- **Live coaching**: your stopping point and line compared with the demonstration; off-centre stops get a computed **correction plan**, listed step by step and drawn on the ground.
- **Parking report** after securing the car: centimetres left/right and forward/back, angle, line, kerb and door gaps, contacts and habits. It includes a replay that follows your car against the demonstration.
- **Clear contact feedback**: a banner naming what hit what, red screen edges, a sound and rumble. Rolling gently onto a kerb is fine.
- **Realistic cockpit**: working mirrors, a reversing camera with guides, a 360° view, front/rear/side parking sensors with beepers, and first-person demonstrations.

## Getting started

**Windows**: run `./build.ps1 -Run` from PowerShell. Or build the runtime bundle once with `./build.ps1 -Test -Import -Package`, then double-click **Launch.cmd**. On first launch the menu opens on **How to drive**.

**Browser (local)**: double-click **Launch-Web.cmd**, or run `./serve-web.ps1 -Open` after `./build-web.ps1`. Use an ordinary desktop browser; press a gamepad button to connect a controller. Details: [web-playing.md](docs/web-playing.md).

**Phones and tablets**: on-screen pedals, steering pad and gear buttons appear automatically on touch devices (Esc → Options → On-screen controls). The steering pad works relative to where your thumb lands, with a resting dead zone.

**Steam / Steam Controller**: add `builds/ParkingPractice/ParkingPractice.exe` as a non-Steam game with **Start In** set to its folder and an analog **Gamepad** layout. The 2026 Steam Controller also works without Steam Input through a bundled SDL 3.4.16 driver, which only takes pads Godot cannot read itself.

Every scenario starts in **P with the parking brake on**: hold the brake, select a gear, release the parking brake, then ease off the brake to creep.

## Controls

| Action | Keyboard / mouse | Controller |
| --- | --- | --- |
| Gas / brake | W / S | RT / LT |
| Steer · centre the wheel | A / D · hold Z | Left stick · hold left-stick click |
| Look around · look forward | Mouse · Home | Right stick · right-stick click |
| Select D / R / P (hold brake) | 4 / 2 / 1 (3 = N) | RB / LB / A |
| Parking brake | Space | Y |
| Rear ↔ 360° camera | Tab | X |
| Glance at the camera screen · over the shoulder | V · C | B · View |
| Parking instructions · correction plan | H · F | D-pad up · D-pad down |
| Pause menu | Esc | Start |
| Retry · new variation · demonstration | F5 · F6 · F2 | Pause menu |

Prompts and the on-screen key bar follow the device you use, including PlayStation and Nintendo button names. Pedals and steering can be rebound in **Esc → Options → Controller setup & controls**. Full notes: [playing.md](docs/playing.md).

## Build and test

`build.ps1` uses the installed editor at `C:\Program Files\Godot 4.7.2 Mono` and its bundled NuGet packages; it does not download anything. `build-web.ps1 -Setup` downloads the checksum-pinned web toolchain once.

```powershell
./build.ps1 -Test            # simulation and generator checks, then build
./build.ps1 -Import -Package # runtime bundle in builds/ParkingPractice
./build-web.ps1              # browser build in builds/web
```

In-game runtime checks (input, UI, demonstrations, teaching tools):

```powershell
& 'C:\Program Files\Godot 4.7.2 Mono\Godot_v4.7.2-stable_mono_win64_console.exe' --headless --path . --fixed-fps 120 -- --smoke
```

Append `?check=1` to the browser URL to run the browser checks. `.cache/`, `.godot/` and `builds/` are generated and not tracked.

## Publishing

Pushing to `main` builds and deploys the browser version through GitHub Pages ([deployment.md](docs/deployment.md)). Source: [niposch/parking-trainer](https://github.com/niposch/parking-trainer).

## Documentation and limits

- [Controls and playing notes](docs/playing.md)
- [Requirements](docs/requirements.md) and [validation results](docs/prototype-validation.md)
- [Parking sensors](docs/parking-sensors.md), [controller investigation](docs/controller-investigation.md), [browser feasibility](docs/web-feasibility.md)
- [Asset provenance](assets/THIRD_PARTY.md)

The car, mirrors, camera optics and reference points are generic, not calibrated to a particular lesson car. The physics is a low-speed kinematic model on flat ground. The browser runtime is experimental. Real-world transfer of the teaching aids has not been measured.
