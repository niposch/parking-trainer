# Playing prototype 01

Run `ParkingPractice.exe` from the runtime bundle, or `Launch.cmd` from the source repository. All driving is **first person**.

Choose the exercise and select **Drive / Resume**. Every scenario starts in **P with the parking brake applied**. Hold L2/S, select R or D, then explicitly release the parking brake with Y/Space. Release the service brake gently to creep. Resuming does not change gear or release the parking brake. Park only after stopping: select P, then apply the parking brake.

The mouse is captured while driving and moves the driver's head. Escape pauses and releases it for menus; losing application focus also pauses. Retry/new variation returns to pause home. Start practice in the scenario selector captures the mouse immediately while the car remains secured.

| Action | Controller / Steam Input gamepad layout | Keyboard |
|---|---|---|
| Gas | Right trigger, analog | W |
| Service brake, in either direction | Left trigger, analog | S |
| Turn steering wheel | Left stick | A / D |
| Look around | Right stick | Move mouse |
| Look around with mouse | Steam mouse/trackpad output | Move mouse |
| Select D / R / P while stopped and braked | RB / LB / A | 4 / 2 / 1 (3 = N) |
| Parking brake | Y (top face button) | Space |
| Return steering toward centre | Hold left-stick click | Hold Z |
| Look forward | Right-stick click | Home |
| Glance at the camera screen (press again: forward) | B (right face button) | V |
| Look over the shoulder (press again: forward) | View / Back | C |
| Rear camera + sensors ↔ surround camera + sensors | X (left face button) | Tab |
| Expand/collapse parking instructions (`?`) | D-pad up | H or T |
| Correction plan (Hints mode) | D-pad down | F |
| Pause / resume / back | Start (B goes back in menus) | Escape |
| Report: back to cockpit · scrub replay · zoom | A or B · D-pad left/right · LB / RB | Enter · ← / → · mouse wheel |
| Retry / new variation | Pause menu | F5 / F6 |
| Watch demonstration | Pause menu | F2 |
| Fixed / steering rear guides | Pause menu | G |

The default steering mode **turns and holds**: hold the stick toward a side to turn the wheel; release it to hold the angle. This avoids snapping the wheels straight while stopped. The menu also offers stick-angle steering. Use the physical steering wheel as the angle cue; hold left-stick click/Z to return toward centre.

Speed, gear and the amber `(P)` parking-brake indicator are on the instrument panel behind the wheel. The camera and sensor readout are in the center console. Use B/V to glance down at it and press it again (or right-stick click/Home) to look forward. Mirrors are mounted on the car and can be inspected with head-look controls. **D-pad and arrow keys do not control the camera**; the D-pad navigates menus, and while driving D-pad up/down toggle the instructions and the correction plan. No floating mirror, speed, or pedal panels are shown while driving.

Six visual sensor zones (front left/centre/right and rear left/centre/right) always appear beside the active camera feed. Each has four discrete steps: below **1.6 m, 1.0 m, 0.60 m and 0.30 m**, progressing from green to red. Each zone uses its own measured bumper-sensor distance. There is no sensor-only mode. Low curbs/sidewalks are excluded from sensor readings in this practice profile; they still cause physical contact.

After moving, stop, select **P** and apply the **parking brake** to automatically enter third-person inspection. This also works for an imperfect parking position. Driving is frozen while you inspect. Mouse/right stick orbits, mouse wheel zooms, **B / Enter** returns to the cockpit with P and parking brake retained. Demonstrations also finish in inspection.

Esc / Start opens the pause home page. **Choose scenario** opens three schematic parking cards and **Spacious / Standard / Tight** choices. Both 90-degree types also offer **Bay side: Left / Right**, relative to your direction during the drive-up approach. Their diagrams reflect with the choice. Changes stay pending until **Start practice**; Back cancels them and keeps the current attempt. Retry preserves the layout seed and difficulty; New variation keeps the parking type/difficulty and generates another layout. Esc / B returns one menu level; from pause home it resumes driving.

**Options** contains Windowed / Borderless fullscreen / Fullscreen, view settings, steering and assistance. **Controller setup & controls** holds the live pedal test, rebinding and keyboard help. **Teaching map** has its own paused page, separate from the car's surround-camera display. D-pad or left stick navigates; A / Enter selects. Display mode and practice preferences persist. Windowed mode restores its previous size and position.

The yellow camera lines represent approximately body width plus 2 cm per side. Cross lines are at 0.4 m, 1 m and 2 m of reverse travel from the rear bumper reference; they are **not** a guarantee of mirror/front-corner clearance. Front and rear beepers have different pitches and become continuous near 0.30 m.

The prototype permits pull-forward corrections. A green target outline marks the desired body footprint. The tutor shows goal offset, heading error, and contact events; no timer or traffic pressure is involved.

Steam must launch the executable for its per-game controller layout to apply. For **analog pedals**, select a **Gamepad** layout and keep L2/LT and R2/RT analog output enabled as their respective triggers. A keyboard/mouse layout can make other controls work while the game reports no gamepad. That layout now also works with **left mouse button = gas, right mouse button = brake**, but these pedals are on/off rather than analog.

Open **Esc → Options → Controller setup & controls** for the **Input test**: slowly press each trigger and check that its percentage varies smoothly. The menu shows detected gamepads and the last received button/axis/key. **Bind L2 brake / Bind R2 gas** allow an alternate axis, button, key or mouse button; press the desired input fully, or Escape to cancel. Bindings persist, and **Reset pedals** restores defaults. Analog trigger events are accepted from any gamepad, including Steam's virtual gamepad. No native Steamworks API or Steam store release is required.

The **2026 Steam Controller** has a compatibility driver bundled with this prototype: SDL 3.4.16, because the installed Godot's SDL 3.2.28 lacks this hardware's native support. Godot-detected gamepads take priority; the compatibility driver activates when Godot detects none. Its default pedals read the physical analog triggers directly so Steam-emulated mouse clicks cannot turn a partial squeeze into full gas/brake. Keyboard steering/pedals are fallbacks when this driver is inactive; mouse look remains available. The driver uses standard physical controls rather than Steam remaps when operating directly. Both physical triggers were verified reaching full range with intermediate levels on the user's connected controller.

This uses a generic 4.45 m × 1.80 m car with a 2.65 m wheelbase. Exact seat position, mirrors, wide-angle lens distortion, and lesson-car reference cues remain uncalibrated. This first version is for feedback on controls and parking practice, not a claim of exact real-car sightline matching.

Looking beyond about 100 degrees progressively moves the viewpoint inboard and rearward as the torso turns; looking fully back lets you see between the front seats. Ordinary side views keep the seated eye position. The generic liftback has distinct front seats, a rear bench, three rear head restraints and solid window pillars. Its proportions follow the supplied body-shape references, not an identified lesson-car model.

Every scenario now starts on a forward approach, roughly 16–19 m of travel before the reference setup pose, with a varied lateral offset and small heading difference. Start in P with the parking brake on, hold the service brake, select **D**, release the parking brake and drive up. You choose where to stop, how far beside the parked vehicles to position yourself, and when to reverse. The tutor's opening phase covers this approach. Demonstrations show it too, with a stopped pause at gear changes; tighter parallel spaces include a final correction.

| Space difficulty | Parallel gap length | 90-degree bay width |
| --- | --- | --- |
| Spacious | 8.00–8.30 m | 2.90–3.10 m |
| Standard | 7.00–7.30 m | 2.55–2.70 m |
| Tight | 6.45–6.65 m | 2.30–2.40 m |

The bay side is saved, retained by Retry/New variation and shown on pause home. Mirroring reflects the complete bay layout, approach and demonstration steering while keeping the same left-hand-drive cockpit. Parallel parking keeps its existing layout.

The selector shows these ranges; pause home shows the actual generated size. Difficulty changes physical space dimensions, independently of tutor/camera assistance. Layout generation validates a complete collision-free approach and parking witness without silently enlarging the selected space. Generator version 2 intentionally changes the layouts produced by earlier seeds.

Parking audio now uses lower-pitched tones while retaining the original loudness and repeat timing, as requested. The close-range warning remains continuous, at a softer 325 Hz front / 250 Hz rear pitch. Pulses stay at 650 / 500 Hz. One continuously running oscillator smooths amplitude and pitch changes sample by sample, including muting and volume changes. **Esc → Options → Beeper volume (%)** adjusts the level; 0% is silent, and the separate mute button is still available. The level is saved between sessions.

## Touch and hint controls

Options → On-screen controls offers Auto, On and Off on Windows and web. Auto uses touch-device detection and also reacts to the first real touch. On overrides detection and can be used with a mouse on desktop; Off overrides touch detection. The choice persists.

Hold/drag the left pad to steer. Gas and brake are independent: touch near the top for full pressure, near the bottom for light pressure; release to let go. Hold the brake while selecting P/R/N/D; P BRAKE is a separate toggle. Drag elsewhere to look. Multiple fingers can hold steering, pedals and look independently. Menu pauses and releases held controls; View changes the console camera. After parking, drag to orbit, use +/− to zoom and Return to cockpit to continue.

The hint starts as a single `?` icon. Tap/click the icon, tap the touch toolbar’s `?`, or press H or T / controller D-pad up to expand or collapse it. Collapse preserves tutor cues; Options → Tutor turns them off. Collapse state persists. Menus scroll/reflow on narrow screens. Rendering fills the window at any aspect ratio without stretching the world or adding black bars.


Use **Full screen** on the touch toolbar to hide browser chrome where supported; tap **Exit full** to return. The display option is also available in Options. Browser controls use screen-density-aware sizing, with larger pedals and steering regions. Installing/adding the PWA to the home screen provides another way to launch without the address bar; browser support varies.

The console now shows lateral warning bars as well as bumper sectors. Bumper corners, the four lateral bars (front and rear half of each side) and the mirrors are separate zones: each shows only obstacles in its own area, together covering the whole car without gaps. The beeper uses the same distances. This is generic geometric parking assistance, not a calibrated copy of the lesson car's sensor hardware.

Menus support finger swipes over their contents, including buttons; release without dragging to select an entry. In landscape touch mode, the gear row sits above the left steering pad so your right hand can keep pressing the brake during a shift. The surround display is now a tighter view of the area immediately around the car.


The desktop shortcut list stays visible during driving and menus, on a mostly transparent background. It is not the parking-instructions panel: **H toggles the same instructions as `?`**, leaving the shortcut list visible. Menus reserve space beside the list (or above it in narrow desktop windows). Touch mode omits the keyboard list.

The surround display now emulates a car-centred bird’s-eye view with uniform distance scale. It shows nearby obstacle footprints, your car and reverse guides without the previous stitched-camera seams and stretched cars. It is a virtual parking aid, not a measured reproduction of an OEM camera system or its blind spots. The separate physical reverse camera and distance indicators remain available together.

## Scenarios, coaching and the parking report

**Esc → Choose scenario** offers seven parking types:

| Type | Space difficulty controls | Notes |
| --- | --- | --- |
| Reverse parallel | Gap length 8.00 / 7.00 / 6.45 m | Kerb on the right, or the left for a one-way street |
| Reverse 90° bay | Bay width 2.90 / 2.55 / 2.30 m | Optional row of cars opposite: aisle 7.0 / 6.0 / 5.4 m |
| Forward 90° bay | Bay width as above | Optional opposite row |
| Forward 60° angled bay | Bay width as above | Entered forward in the direction of travel; optional opposite row |
| Reverse into a garage | Garage width 3.00 / 2.70 / 2.50 m | Walls on three sides; mirrors span 2.22 m |
| Reverse around a corner | Side-road width 7.0 / 6.0 / 5.2 m | Chamfered kerb corner; finish parallel to the side-road kerb |
| Turn in the road | Road width 9.0 / 7.8 / 7.0 m | Multi-point turn; finish in the opposite lane |

Each range starts at the value shown. **Task** chooses **Park**, **Fix an off-centre park** (start off-centre and crooked, then correct it) or **Drive out** (leave the space and stop in the green lane zone). The fix-it task is available for parallel, both 90° bays and the garage; drive-out for parallel, all bays and the garage. **Obstacles: Some / Busy** adds hazards such as a concrete pillar at the bay entrance, a low post or tow bar below the sensors, a neighbour over the line, a bike rack, a trolley, bins, signposts and parked cars along a road. A hazard never shrinks the advertised space, and every layout is still generated with a collision-checked solution. **Surprise me** on the pause home or scenario page starts a random combination.

**Coaching** sets the help level:

- **Guided**: tutor panel, landmark highlight, live coaching and automatic correction plans.
- **Hints only**: the tutor stays available, but coaching appears only after you stop. Press **F** to see a correction plan.
- **Test**: no tutor, coaching, target outline, landmark, demonstration or next-step prompt. The report says *test passed* only if the final position is accepted and there was no contact.

Live coaching (cyan-bordered panel) compares your drive-up stopping point and your line with the demonstration, warns about speed above creeping pace, and gives a one-time tip about steering at a standstill. Close to the target it shows the offset as *cm left/right of centre*, *cm too far forward/back* and nose angle. If you stop off-centre, Guided mode computes a **correction plan**: a straight pull-out, two opposite steering arcs and a final straight, listed step by step with the gear and wheel position and drawn on the ground as a cyan line. This is the same construction as the fix-it task. When no simple two-arc correction fits, the coach says to pull out further and re-park.

After you secure the car, the inspection view shows a **parking report**. It contains a grade (good / acceptable / re-park), left/right and depth offsets, angle, line or wall gaps on each side, the kerb or end-of-bay gap, and driver/passenger door clearance (about 0.6 m to open). It also lists contacts, direction changes, top speed, time and steering at a standstill. A top-down replay shows your path (blue forward, orange reverse) against the dashed demonstration. Its view is oriented with the target's forward direction up, so left and right match the text.

## Desktop onboarding

On first launch on a desktop, the pause menu opens on **How to drive**, which is also on pause home. While driving, a key bar at the bottom shows your pedals, the steering-wheel position, the selected gear and the parking brake live. A prompt above it gives the next step: hold the brake, select a gear, release the parking brake, ease off to creep, and finally P and the parking brake. After controller input, both show controller labels (LT/RT, LB/RB, R3). **Options → Key bar & next-step hints** turns them off; Test mode hides the prompt.

## World cues, briefings and contacts

**Scenario briefing.** When you start a new kind of scenario (type, task, side, coaching level or aisle), a card explains the goal, which gear to start in (for example "First drive forward (D) along the aisle… Then select R and reverse in"), how to finish, and any hazards. Dismiss it with Enter, the bottom face button, or a tap; it also closes once you start moving. Retries and new variations of the same kind skip it.

**3D cues** (Options → 3D cues, on by default; hidden in Test mode):

- **Checkpoints**: a cyan ring with a post at the next point where the reference solution changes gear or steering. In Guided mode it also shows a ghost outline of the car and a label such as "Stop · select R · wheel at full lock left". The drive-up end is a large ring ("Stop around here"), because the stopping point is your choice. If you stop off-centre, the checkpoints follow the correction plan instead. If you drift more than about 1.2 m from the reference line, they hide rather than steer you back to a line that no longer fits.
- **Predicted wheel tracks** (Guided): where each wheel goes (yellow rear, white front) and where the front corners swing (red) over the next 3 m with the current steering, in D or R.
- **Clearance tag** (Guided): the distance from the body or a mirror to the nearest obstacle point within 1 m, including kerbs and low objects that the sensors ignore.

**Hints panel.** The parking instructions now start expanded. On desktop the panel header shows its toggle key (`[H: hide]`, or the controller's button), and the collapsed `?` icon has a small key badge.

**Contacts.** A contact now shows a red banner naming what touched what (for example "CONTACT · rear bumper hit the kerb"), red screen edges that pulse while you stay pressed against it, a short camera jolt and a thud. It also rumbles the controller or vibrates a phone, and puts a red marker at the touching point. Pressing against the same obstacle counts as **one** contact; backing away and touching again counts another. **Rolling onto a kerb at creeping speed (up to about 5 km/h) is normal parking**: it shows a calm note, is listed in the report as a gentle kerb touch, and does not count as a contact or fail a test. Hitting a kerb faster is a contact. A body within 3 cm of a bay line is graded as re-park.

**Report replay.** The report's top-down replay now follows the car, pauses briefly at each change of direction and closes in on the target as the car arrives. A progress bar marks the direction changes; ← / → (or the D-pad) scrub through it.

**Touch steering.** The steering pad is now relative to where your thumb lands, with a dead zone of about 15% of its travel. You can rest a thumb on it without turning the wheel. While you hold it, the resting zone is shaded.

## Controllers

- **Desktop:** any gamepad Godot recognises works directly. The bundled SDL 3.4.16 driver now takes **any gamepad Godot cannot read** (such as the 2026 Steam Controller without Steam Input), even if other pads are connected. It identifies devices by vendor/product ID before opening them, so a pad is never read twice. With Steam running, Steam Input presents the Steam Controller as a virtual Xbox 360 pad, which Godot reads directly.
- **Browser:** uses the browser's Gamepad API. Press a button to connect. Controller setup lists each browser gamepad and whether the browser recognised its layout ("standard"). With a non-standard layout, use **Bind L2 brake**, **Bind R2 gas** and **Bind steering** (push the stick fully left).
- Prompts and the key bar use the button names of your controller family: Xbox style (RT/LT/LB/RB/A), PlayStation (R2/L2/L1/R1/Cross) or Nintendo (ZR/ZL/L/R/B). The names are matched from the controller's reported name.

## Controller map (24 October 2026)

The controller layout was reworked so that each driving function has exactly one button, with no button doing different things depending on hidden state. Gears are direct (A = P, LB = R, RB = D; neutral is keyboard-only), the parking brake moved from the right-stick click to Y so looking around cannot release it, and the camera glances toggle back to the forward view. Start is the only menu button; View is now the over-the-shoulder glance. In menus, B goes back; in the briefing, A or B closes it; in the report, A or B returns to the cockpit, the D-pad scrubs the replay and LB/RB zoom. Steam Input layouts that send a key alongside a button (for example B and Esc) no longer trigger an action twice: the copy from the other device type is ignored for 0.4 s.
