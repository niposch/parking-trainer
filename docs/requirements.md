# Parking Trainer: requirements and research

**Date:** 18 October 2026. **Status:** requirements baseline; first-prototype results and limits are recorded in [prototype validation](prototype-validation.md).

“Must” describes the intended Windows prototype. Numerical targets below are proposed engineering criteria, not measured properties of the learner's car or driving-test rules.

## 1. Agreed direction

- Focus exclusively on low-speed parking, positioning, observation, and corrections. No traffic simulation, open world, racing, or conventional authored levels.
- C# on Windows using the installed Godot 4.7.2 Mono. Steam Input for the Windows controller. An experimental C# desktop-browser prototype was subsequently requested and implemented; regular browser-compatible controllers are sufficient there. See [browser instructions](web-playing.md).
- Right trigger accelerates; left trigger brakes in both directions. Reverse is selected separately.
- English initially; prepare all text for later translation, including German.
- Optional visual tutor explaining the manoeuvre and showing what to look at and align.
- Blocky, flat-colored cars are acceptable, as in the user's second reference image. Reuse assets; avoid substantial custom vehicle modelling.
- Free assets only; no paid packs. All driving must be first person. Mount mirrors, the camera screen, and a minimal digital speed/gear/parking-brake display physically in the cockpit. Keep the forward view clear, with instruments behind a realistically sized wheel and the camera in the center console. The optional tutor can remain an overlay.
- No existing vehicle brands, logos, model names, branded scenery, or copied infotainment artwork in the game. Use generic controller symbols. Steam Input remains the requested external integration.
- Feedback update: six stepped proximity indicators; automatically switch to a third-person orbit view after the car is parked and secured, with driving disabled until returning to the cockpit. Remove D-pad camera shortcuts, including arrow-key camera movement that Steam could emulate.

**Owner-provided facts:** lessons are in German; the learning car is a SEAT a few years old, with a reversing camera, 360° camera, and front/rear distance beepers. The controller is the **Steam Controller (2026)**. Exact car model, transmission variant, and guide-line behavior are unknown. Real manufacturer names in this document identify research sources only.

**Provisional assumptions:** left-hand drive, right-hand traffic, dry flat ground, one generic player car. These are configurable assumptions, not confirmed details of the learning car.

## 2. Findings that affect the design

| Research finding | Requirement / decision |
|---|---|
| Automatic vehicles can transmit drive at idle; creep must be controlled with the brake. A SEAT manual describes this behavior. [Automatic transmission reference][automatic] | Model gradual forward creep in D and backward creep in R, with no accelerator input. Do not hard-code one universal creep speed. |
| Auto Hold can retain braking after the pedal is released. [Volkswagen explanation][hold] | Default Auto Hold off to match the requested creep behavior; represent it separately if later confirmed for the learning car. |
| Parking reference points change with the car, driver height, and seating position. [Instructor explanation][instructor] | Teach geometry and observation; treat exact lamp/pillar alignment cues as calibrated examples. |
| SEAT's 360° system combines cameras at the front, rear, and mirrors. [System description][topview] | A car's surround-camera display must be distinguished from an unrestricted overhead teaching camera. |
| Camera guides vary. One SEAT manual illustrates approximate 0.4 m, 1 m, and 2 m rear references; another manufacturer's system distinguishes fixed and steering-dependent lines. [SEAT manual, printed pp. 287–288][seatmanual], [Ford guide explanation][fordcamera] | Keep line distances, width convention, appearance, and steering behavior in the vehicle profile. Do not infer exact distances from the supplied image. |
| Godot warns that `VehicleBody3D` is not designed for realistic vehicle physics. [Class documentation][vehiclebody] | Prefer a calibrated low-speed vehicle model rather than assuming an off-the-shelf racing controller is realistic. |

The user's reversing-camera image establishes the visual direction: wide rear view, visible bumper edge, yellow side and cross lines, and a red near-distance line. It does **not** establish lens calibration, physical distances, mirroring, or whether lines move with steering. The image is a reference, not an asset to redistribute.

## 3. Asset recommendation: reuse first

Initial listings were reviewed during research. The prototype now uses four free CC0 Kenney models; see [asset provenance](../assets/THIRD_PARTY.md). Other candidates remain unimported. Paid assets are excluded by the owner's instruction.

| Candidate | Verified listing information | Recommended use / limitation |
|---|---|---|
| [Cosmo: Low Poly Cars][cosmo] | 12 free models under CC0; OBJ, FBX, GLTF. Additional paid content exists. | First free pack to evaluate for the simple visual style and parked-car variations. Interior suitability is unverified. |
| [Kenney: Car Kit][kenneycars] | Free CC0, 45 files. | Alternative parked-car pack. Do not assume its windows or cockpit support a driver-eye camera. |
| [Quaternius: Cars Pack][quaternius] | Eight car models; CC0; FBX, OBJ, Blend. | Another free source of varied parked cars; cabin and scale need checking. |
| [Kenney: City Kit (Roads)][roads] | Free CC0, 90 files. | Optional curb/road props. Generate dimensioned bay lines and ground directly; no need for a city kit to dictate parking dimensions. |

**Selection rule:** use free assets with small edits: remove badges, set dimensions, separate existing wheel parts, fix glass/materials, add landmark anchors, or make a minor pillar/light adjustment. If extensive reshaping is required, switch to another free asset. The prototype reuses the Kenney exterior with a small primitive cabin and physical instrument/display mounts.

The player car must have usable window openings, A/B/C pillars, mirrors, recognizable rear lamps, bumpers, wheels, and a basic interior. Parked cars need credible dimensions and landmarks; they need no elaborate interiors. Avoid global scaling as a substitute for checking wheelbase, overhangs, and pillar positions individually.

Use generic fictional vehicle names. Inspect models, textures, badges, and plates for branding before inclusion. Keep source/licence records for reused assets. Restricted source assets must not be committed to a public repository; the `local-assets/` exclusion provides a local acquisition location. Prefer GLB/glTF for the game asset pipeline; Godot documents available import formats. [Import documentation][formats]

## 4. Driving and controls

**DRV-1:** Explicit P/R/N/D and parking-brake states. Start stationary with a brake applied. Require a stop and brake before changing D/R or selecting P; a held button must not repeatedly change gear. N supplies no drive; P and the parking brake hold the stopped vehicle.

**DRV-2:** Creep starts smoothly when the service brake is released in D/R. Partial brake pressure permits inching; sufficient pressure holds the car without jitter. Gas increases drive in the selected direction. Braking reduces speed toward zero and never selects reverse. Define behavior for simultaneous gas/brake; braking must reliably stop low-speed movement.

**DRV-3:** Use metres, actual wheelbase, track, body width, mirror width, front/rear overhang, and steering limits. Simulate rear-wheel offtracking and front-corner swing. Collision geometry must agree with the visible body and mirror extents; curb contact must be detected.

**DRV-4:** Recommended foundation: a low-speed kinematic bicycle/Ackermann model with longitudinal acceleration, braking, rolling resistance, and collision sweeps. A bicycle model is appropriate for low-speed parking as a starting approximation. [MathWorks explanation][lowvelocity] It is not a substitute for calibration or collision handling. No full racing drivetrain, tyre-temperature, crash-damage, or suspension simulation is required on flat ground.

**DRV-5:** Controller steering must expose small corrections and full lock with a finite steering rate. Show steering-wheel position and neutral. Releasing the stick must not unrealistically snap the road wheels straight at a standstill. Compare a steering-rate/hold mode with a smoothed angle mode during later usability testing; choose the one that supports deliberate parking corrections.

**INP-1:** Initial Steam Input route: launch the exported Windows executable as a non-Steam game and use Steam's gamepad emulation into Godot input actions. This avoids needing a native Steamworks API dependency for the first prototype. [Valve emulation guidance][steamemulation], [Adding a non-Steam shortcut][steamshortcut]

**INP-2:** Independent analog gas and brake, left-stick steering, right-stick look, and discrete actions for gear selection, parking brake, camera selection, tutor, reset, and pause. All menus must work with the controller. Keep prompts generic and remappable. Physical controller is the Steam Controller (2026); test independent trigger ranges and prevent duplicate physical/virtual-device input. D-pad navigates menus, without camera shortcuts.

**INP-3:** Provide calibration for drift, dead zones, trigger endpoints, sensitivity, and inversion. Pause on focus loss, Steam overlay/menu use, or disconnect; clear stale input and require deliberate resume. Steam remapping must retain analog triggers rather than converting them to keyboard presses. Godot supports analog input actions but device behavior still requires testing. [Controller documentation][controllers]

## 5. Visibility, cameras, and parking aids

**VIS-1:** Driver-eye cockpit is the primary driving view. Set eye height/position and monitor field of view explicitly. Looking behind rotates around a believable head position with limited optional lean; it must preserve pillars, seats, headrests, and window occlusion. Avoid camera bob and speed-dependent FOV.

**VIS-2:** Both door mirrors and the interior mirror must show the correct scene, side, scale, and occlusions. Mirror pose and curvature/FOV belong to the profile. Eye movement should affect the reflected view appropriately. A simplified mirror is acceptable only if it preserves the visibility needed for the taught cues.

**CAM-1:** Reverse camera is mounted on the vehicle at a defined position and angle, separate from driver head movement. Match the reference's wide-angle character, bumper visibility, aspect ratio, and ground perspective. Confirm lateral orientation with asymmetric markers. Lens distortion and crop are configurable; camera calibration involves intrinsics, extrinsics, and distortion rather than FOV alone. [Calibration reference][calibration]

**CAM-2:** Provide fixed and dynamic guide modes. Dynamic paths use the **actual road-wheel angle and the same vehicle geometry as driving**; they update while stationary. Straight steering gives the straight-path limit. State whether each line denotes body width, tyre path, or clearance margin. Match the eventual vehicle profile rather than presenting a decorative trapezoid as an accurate path.

**CAM-3:** Project guides consistently into the camera image, accounting for its crop, orientation, and lens treatment. Actual cars may use processed images and independently drawn overlays: validate the final appearance against the chosen reference. Separate the on-screen guide convention from the full swept collision envelope; guide lines do not imply every corner or mirror will clear an obstacle.

**CAM-4:** Include a selectable surround view with a generic car graphic and front/rear/side camera selections. For credible simulation, compose finite camera views with defined coverage and blind areas. Do not label an omniscient overhead render as a faithful 360° camera. A simple overhead render may be available as a clearly labelled tutor aid during early development. SEAT's manual describes a stitched view and limitations around protruding objects. [Printed pp. 283–286][seatmanual]

**AID-1:** Front/rear parking beepers and visual sectors use bumper sensor positions, finite coverage, and nearest relevant obstacle surfaces—not distance from the car's centre. Beeps become more frequent as clearance decreases; front/rear sounds are distinguishable. One SEAT reference reaches a continuous tone at about 0.30 m; actual ranges and timing remain profile parameters. [Parking System Plus example][sensors]

**AID-2:** Separate volume/mute controls and a visual alternative. Do not make sensors detect all obstacles perfectly or imply that silence proves clearance. Automatic emergency parking braking is out of scope unless separately requested.

## 6. Procedural practice

| Scenario | Initial scope | Useful variation |
|---|---|---|
| Reverse parallel | Required | One reference car, then two; short/long/tall neighbors; gap length; curb distance; initial alignment |
| Reverse 90° bay | Required | Neighbor size/offset; bay width/depth; aisle width; starting angle |
| Forward 90° bay | Required | Approach offset; available turning room; stopping depth |
| Angled bay | Next expansion | 45°/60° bays, left/right entry |
| Leaving a space / correction drill | Next expansion | Pull-forward correction, straight reverse, exiting between parked cars |

**GEN-1:** Parameterized templates with repeatable seeds, “retry same setup,” and “new variation.” Save generator version and vehicle profile with the seed. Change parked neighbors before changing the player car, so early lessons retain consistent references.

**GEN-2:** Generate physically plausible vehicle sizes and positions. Validate spawn clearance, full-body/mirror swept clearance, available approach space, and at least one achievable parking solution including corrections. Final fit alone does not prove a manoeuvre is possible. Reject invalid seeds. Tutor exercises additionally require visible, valid cues; a bigger neighboring car may hide a previously useful landmark.

**GEN-3:** Basic flat ground, curb, parking lines, parked cars, and occasional walls/posts suffice. Difficulty comes from geometry, not time pressure or traffic.

## 7. Visual tutor and feedback

Teach adaptable phases rather than a universal sequence of exact steering turns. German ADAC guidance emphasizes observation, mirror setup, slow manoeuvring, and correction; an instructor demonstrates a 45° bay approach while explicitly identifying reference-point variation. [ADAC][adac], [Instructor][instructor]

| Manoeuvre | Tutor sequence |
|---|---|
| Reverse parallel | Assess space; position beside reference car; observe; reverse and steer toward the gap; monitor curb, front swing, and rear clearance; countersteer when geometry permits; straighten, correct, and secure. |
| Reverse bay | Select bay; position for the chosen 90° or angled approach; observe; reverse toward it; compare both bay lines in the mirrors; unwind as the body aligns; stop at safe depth. |
| Forward bay | Give the front enough turning room; observe; enter slowly; monitor the inside rear corner and outside front corner; straighten; judge stopping depth; correct if needed. |

**TUT-1:** Offer demonstration, guided practice, and independent practice. Allow pause, single-step explanation, retry, and repeat. Corrections are normal and must be teachable, rather than automatically failing the exercise.

**TUT-2:** Highlight a real landmark and the relevant mirror/window/camera, explain what its alignment means, and show an optional paused overhead comparison. “Left” always has an explicit reference frame, such as “the parked car's left rear lamp.” Parked-car lamps remain visible landmarks without being artificially illuminated as brake lights.

**TUT-3:** Advance from measured pose, steering, speed, clearance, and visible landmark alignment—not elapsed time. Never highlight a hidden lamp as though it were visible. Recompute or invalidate cues when the eye position, vehicle geometry, or neighboring car changes.

**TUT-4:** Gradually remove highlights and teaching views. Feedback covers final angle, curb/bay offset, minimum clearance, contact, speed control, and unnecessary steering/reversals. Show distance and angle errors without claiming driving-test certification. A camera-look event can record the selected view, but does not prove the learner actually observed a hazard.

## 8. Technical boundaries and acceptance

Local verification: `Godot_v4.7.2-stable_mono_win64_console.exe --headless --version` returned `4.7.2.stable.mono.official.ed1daf0bf`. `dotnet --list-sdks` reported SDK `10.0.204`; C# project build compatibility has not been tested. Pin a supported SDK/framework when implementation starts.

Use C# for the simulation, generator, and tutor; keep their logic separate from input and rendering so geometry can be tested without a running game. Choose Compatibility rendering initially to suit simple graphics. English strings use translation keys and placeholders; no sentence concatenation or baked-in tutorial text. [Godot translation workflow][i18n]

| Acceptance check | Prototype target |
|---|---|
| Automatic behavior | D/R creep from rest; brake holds for 30 seconds; brake never reverses; N has no drive; gear interlocks work. |
| Geometry | Dimensions and sightline landmarks recorded per profile; constant-steer forward/reverse paths agree with an independent geometry calculation. |
| Path/guide agreement | Proposed maximum 5 cm ground-space error over a 5 m flat-ground manoeuvre at fixed steering; test centre and offset tracks in both turn directions. This checks internal agreement, not real-car accuracy. |
| Camera guides | Known ground markers at configured distances project onto their references; asymmetric left/right scene verifies orientation. Check straight, partial, and full steering. |
| Sightlines | Pillars and headrests occlude correctly; mirrors and shoulder views show expected landmarks; seat/FOV changes invalidate dependent cues. |
| Surround view / sensors | Check all sides, camera seams/blind areas, and obstacle heights; sector/beep transitions match configured thresholds without chattering. |
| Procedural scenarios | Same version/profile/seed reproduces setup; sample at least 100 accepted seeds per required template with no initial overlap and a verified feasible path. |
| Steam Input | Actual controller tested through Steam: independent trigger values, menus, remapping, overlay, focus loss, disconnect/reconnect, and no duplicate input. |
| Performance | Proposed 60 FPS at 1080p on the user's PC with cockpit mirrors and selected parking display; record hardware and adjust render resolution before simplifying geometry. |
| Learning usefulness | User/instructor checks control feel and landmark cues against lessons; independent practice on unseen seeds after tutor aids are removed. Real-world transfer remains unverified until compared in lessons. |
| Presentation | No visible brands; English UI and tutor; translation expansion check; simple high-contrast lines and optional audio. |

## 9. Remaining decisions

- Exact learning-car model/year, seating/mirror setup, steering/turning circle, camera mode, and Auto Hold use: useful for later calibration; not required to write this specification or begin a generic prototype.
- Actual controller and preferred stick-steering mode: establish during the first input trial.
- Further free-pack refinement may follow user feedback; paid assets are excluded.
- Prototype cues can teach spatial principles immediately; do not describe an uncalibrated generic car as an exact replica of the lesson car.
- The experimental browser prototype uses the user-supplied [C# web-export fork](https://github.com/sethyanow/godot-dotnet-web-export)'s upstream Windows release. Regular gamepads are sufficient, with independent analog pedals; Steam Controller-specific support is not required there. Native Steam Input API integration remains deferred. See [browser validation and manual checks](web-playing.md).

## Visibility and sensor feedback - 18 October 2026

- Exterior reference is only a similar liftback body shape, not confirmation of the lesson-car make/model. Keep the unbranded 4.45 by 1.80 m generic physics profile until actual calibration is available.
- Adapt one coherent free vehicle body; no mismatched upper/lower shells, interior panels protruding outside, unexplained body gaps or duplicate mirrors. Keep A/B/C pillars substantial and opaque.
- Include separate front seats/headrests, a rear bench with three head restraints, usable passenger-side and rear-quarter windows, and a rear hatch window. Rotate and smoothly translate the eye inboard/backward for a full rear look.
- Interior and exterior must use the same window/body geometry. Interior trim and glazing must not obstruct the wheel, digital instruments or low console screen.
- For the requested prototype sensor profile, exclude low sidewalks/curbs from beepers and indicators while preserving their collision geometry.

## Approach, difficulty and menus - 18 October 2026

- Each scenario must begin before the parking space on a drive-up approach, with a varied lateral position and small heading difference. The player chooses the stopping point and alignment; do not spawn at the reference maneuver's setup pose. Preserve P plus parking brake at spawn.
- Provide a scenario submenu with schematic parking-type cards and Spacious / Standard / Tight space choices. Show real dimension ranges. Parallel difficulty controls gap length; bay difficulty controls width. Keep the chosen range fixed during generation and validate approach plus maneuver feasibility.
- Scenario edits are drafts until Start practice; backing out preserves the active run. Retry keeps the seed and difficulty; New variation changes the seed. Persist the active type/difficulty.
- Pause home separates Continue, scenario selection, Options, retry/variation and demonstration. Options holds display/view/driving/assistance, with controls/calibration and teaching-map subpages. Support controller focus and accept/back throughout; Escape backs out one level before resuming.
- Provide Windowed, Borderless fullscreen and Fullscreen modes; restore decorated window size/position when returning to Windowed and persist the choice.
- Tutor and demonstrations include the approach and stopped gear changes, preserving realistic separate throttle/brake operation.

## Mirrored bays, door repair and sensor audio - 18 October 2026

- Reverse and forward 90-degree parking must support bays on either side of the approach. Reflect the complete obstacle/goal/setup/approach layout and reference steering; keep the driver's cockpit left-hand-drive. Expose Left/Right beside difficulty, reflect the schematics, preserve draft/cancel behavior and persist the choice.
- Close the former toy-mirror attachment opening in each door using joined exterior and interior surfaces. Remove the recessed waist beneath the widened cabin so the door skin is flush with the fender plane. Preserve the physical 1.80 m body width and real window openings.
- Reduce the sharp pitch of distance warnings while keeping their original loudness and repetition timing, per the user's clarification. Keep a stable continuous warning near 0.30 m; retain mute and the optional saved volume adjustment.

## Sources

Sources accessed 18 October 2026. Manufacturer manuals describe example equipment, not confirmed specifications of the learner's car. Asset listing claims are not import or dimensional validation.

[automatic]: https://www.seat.com/datamanual-manual/ateca/my18_w45/en-uk/ATECA_11_17_EN.pdf
[hold]: https://www.volkswagen.co.uk/en/technology/driver-assist/braking.html
[instructor]: https://www.driving-school-beckenham.co.uk/bay-parking-45-degree-method.html
[topview]: https://www.seat.com/car-terms/t/top-view-camera
[seatmanual]: https://www.seat.com/datamanual-manual/ateca/my22_w48/en-uk/SEAT_ATECA_11_21_EN.pdf
[fordcamera]: https://www.fordservicecontent.com/Ford_Content/Catalog/owner_information/2019-Ford-Fiesta-Owners-Manual-version-1_om_EN-US_08_2018.pdf
[vehiclebody]: https://docs.godotengine.org/en/stable/classes/class_vehiclebody3d.html
[cosmo]: https://cosmo-art.itch.io/low-poly-cars
[kenneycars]: https://kenney.nl/assets/car-kit
[quaternius]: https://quaternius.com/packs/cars.html
[roads]: https://kenney.nl/assets/city-kit-roads
[formats]: https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html
[lowvelocity]: https://www.mathworks.com/help/driving/ref/lateralcontrollerstanley.html
[steamemulation]: https://partner.steamgames.com/doc/features/steam_controller/steam_input_gamepad_emulation_bestpractices
[steamshortcut]: https://help.steampowered.com/en/faqs/view/4B8B-9697-2338-40EC
[controllers]: https://docs.godotengine.org/en/stable/tutorials/inputs/controllers_gamepads_joysticks.html
[calibration]: https://docs.opencv.org/5.0/tutorials/calib3d/camera_calibration/camera_calibration.html
[sensors]: https://www.seat.com/datamanual-manual/tarraco/my21_w30/en-uk/TARRACO_07_20_EN.pdf
[adac]: https://www.adac.de/verkehr/verkehrssicherheit/unterwegs/richtig-einparken/
[i18n]: https://docs.godotengine.org/en/stable/tutorials/i18n/internationalizing_games.html

## Touch, window fill and hint update — 18 October 2026

- Keep the standalone Git project in `C:\Users\Nickp\Development\parking-trainer`.
- Support optional on-screen controls on Windows and web, with saved Auto / On / Off overrides. Auto detects touch-capable/mobile devices and actual touch events. Independent pointers permit simultaneous analog gas, brake, steering and camera look; preserve realistic separate gear selection and parking brake.
- Support touch inspection orbit/zoom/return, and release held inputs on pause, resize and focus loss.
- Fill desktop/browser windows at any aspect ratio without black bars or world stretching. Reflow/scroll menus for small portrait and landscape windows.
- Make the tutor hint collapsible to its phase header, preserving tutoring while freeing the view. Provide a touchable header, Hint button, T/controller X and a persisted state; keep the separate Tutor on/off option.

## Compact hint and publication — 18 October 2026

Collapsed tutoring must occupy only one hint icon, without a phase-title banner. Publish source to `niposch/parking-trainer` and the browser game through GitHub Pages at `https://niposch.de/parking-trainer/`; inherit the existing user-site domain without DNS changes. Make the game discoverable from the homepage’s centralized Live projects directory.
