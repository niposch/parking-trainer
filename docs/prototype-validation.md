# Prototype 01 validation — 18 October 2026

## Verified

- C# build on the installed .NET SDK 10.0.204, targeting .NET 8 with Godot.NET.Sdk 4.7.2: zero warnings and errors.
- Pure simulation tests: **74,791 assertions**, including forward/reverse creep settling, 30-second brake hold, neutral, shift interlocks, an independent Euler comparison against exact constant-curvature motion, bumper-based sensing, swept collision stopping, rotated rectangle overlap, and 100 deterministic/feasible seeds for each of three exercises.
- Generator constructs a path from the goal, validates body/mirror clearance, and reverses it to provide a feasible start-to-goal witness. It does not rely solely on final space dimensions.
- Godot runtime regression checks cover keyboard input processing, menu button activation, physical mirror/display mounting, forward/reverse creep, steering hold, braking, parking, display/tutor toggles, pause, and all three first-person demonstrations reaching their goals. The controller/mouse update also checks parking-brake retention on Drive/Resume, explicit release, new-scenario reset, synthetic analog trigger events on a nonzero virtual device, trigger release, mouse pedal fallbacks, rebinding/persistence, mouse look and menu suppression. Windowed checks additionally verify actual Windows cursor capture/release.
- Visual rendering inspected on the local NVIDIA RTX 4080 SUPER with Compatibility/OpenGL: physically mounted mirrors, digital instruments behind the larger 38 cm wheel, lowered center-console camera display, clear forward sightline, menu layout, and four-camera surround view. Screenshots are in `screenshots/`. A warm stationary capture reported about 240 FPS at 1440×900; this is a spot observation, not a driving benchmark.
- All driving remains in first person; a third-person diagnostic camera is available only to the automated asset screenshot flag, not in gameplay. The overhead map is explicitly paused teaching assistance.

## Practical limits for this first feedback build

- No physical controller was connected. Analog controller code and menu bindings are present, but a real Steam Input session, disconnection/reconnection, duplicate-device behavior, and overlay focus must still be checked with the user's controller. Focus loss pauses the game; an overlay that retains application focus may require Start/Escape.
- Physics is a flat-ground low-speed kinematic model with force-like longitudinal control and geometric collision substeps, not a suspension/tyre/drivetrain simulator. Speed is limited to 3.5 m/s within the practice lot. No slopes or traffic are simulated.
- Car dimensions are a generic profile, not a measured lesson car. Player wheelbase/track and body dimensions are explicit; parked car proportions remain stylized.
- Rear view is a wide perspective camera, without calibrated fisheye distortion. Its guides use the same geometry as driving; real-car optics and overlay conventions have not been matched. Side mirrors use a convex field-of-view approximation with reflected-eye/off-axis projection.
- Surround view reconstructs the ground plane from four finite camera feeds. Raised objects stretch and seams/near-body artifacts remain. It is not an omniscient top-down render, and has not been matched to a production camera system.
- Tutor phases are driven by progress along a collision-free reference and highlight a parked-car corner. They provide general cues rather than a validated exact lamp/pillar recipe. Complex recovery coaching and gaze verification are not implemented.
- UI uses Godot translation calls and placeholders, with English selected in one place. Full translation resources and a language selector are future work.
- The Windows folder is a **local runtime bundle**, using the existing Godot binary and .NET installation. Official export templates/standalone distribution are not installed or claimed.

No homelab infrastructure, network, DNS, or remote service was changed.

## Controller feedback update — 18 October 2026

The user reported that other Steam Controller inputs worked but L2/R2 did not. Their screenshot displayed **No controller detected**, consistent with Steam emulating keyboard/mouse; the exact Steam layout was not inspected. Added digital mouse pedal fallbacks and a live input test/rebinding, and replaced first-device-only trigger polling with any-device input actions. Gamepad emulation remains required for analog pedal output. Steam's trigger modes can produce analog or digital input ([Valve documentation](https://store.steampowered.com/app/353370/Steam_Controller/)); Godot documents device mapping limitations ([Godot controller documentation](https://docs.godotengine.org/en/stable/tutorials/inputs/controllers_gamepads_joysticks.html)). **32 headless and 34 windowed runtime checks passed**, including actual Windows mouse capture/release. Synthetic input checks do not establish that the physical Steam Controller is fixed.
