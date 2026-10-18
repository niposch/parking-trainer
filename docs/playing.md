# Playing prototype 01

Run `ParkingPractice.exe` from the runtime bundle, or `Launch.cmd` from the source repository. All driving is **first person**.

Choose the exercise and select **Drive / Resume**. Every scenario starts in **P with the parking brake applied**. Hold L2/S, select R or D, then explicitly release the parking brake with right-stick click/Space. Release the service brake gently to creep. Resuming does not change gear or release the parking brake. Park only after stopping: select P, then apply the parking brake.

The mouse is captured while driving and moves the driver's head. Escape pauses and releases it for menus; losing application focus also pauses. Retry/new variation returns to pause home. Start practice in the scenario selector captures the mouse immediately while the car remains secured.

| Action | Controller / Steam Input gamepad layout | Keyboard |
|---|---|---|
| Gas | Right trigger, analog | W |
| Service brake, in either direction | Left trigger, analog | S |
| Turn steering wheel | Left stick | A / D |
| Look around | Right stick | Move mouse |
| Look around with mouse | Steam mouse/trackpad output | Move mouse |
| Select R / D while stopped and braked | Left / right shoulder | 2 / 4 |
| Cycle P → R → N → D while stopped and braked | Bottom face button (A mapping) | 1 P / 2 R / 3 N / 4 D |
| Parking brake | Right-stick click | Space |
| Return steering toward centre | Hold left-stick click | Hold Z |
| Shoulder view / forward view | Mouse / right stick | C / Home |
| Look down at center console | Right face button (B mapping) | V |
| Rear camera + sensors ↔ surround camera + sensors | Top face button (Y mapping) | Tab |
| Expand/collapse hint | Left face button (X mapping) | T |
| Pause / resume | Start | Escape |
| Open menu | Back | F1 |
| Retry / new variation | Pause menu | F5 / F6 |
| Watch demonstration | Pause menu | F2 |
| Fixed / steering rear guides | Pause menu | G |

The default steering mode **turns and holds**: hold the stick toward a side to turn the wheel; release it to hold the angle. This avoids snapping the wheels straight while stopped. The menu also offers stick-angle steering. Use the physical steering wheel as the angle cue; hold left-stick click/Z to return toward centre.

Speed, gear and the amber `(P)` parking-brake indicator are on the instrument panel behind the wheel. The camera and sensor readout are in the center console. Use B/V to glance down at it, then mouse/right stick/Home to look forward. Mirrors are mounted on the car and can be inspected with head-look controls. **D-pad and arrow keys do not control the camera**; D-pad still navigates menus. No floating mirror, speed, or pedal panels are shown while driving.

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

Parking audio now uses lower-pitched tones while retaining the original loudness and repeat timing, as requested. The close-range warning remains continuous and uses a smooth loop. **Esc → Options → Beeper volume (%)** adjusts the level; 0% is silent, and the separate mute button is still available. The level is saved between sessions.

## Touch and hint controls

Options → On-screen controls offers Auto, On and Off on Windows and web. Auto uses touch-device detection and also reacts to the first real touch. On overrides detection and can be used with a mouse on desktop; Off overrides touch detection. The choice persists.

Hold/drag the left pad to steer. Gas and brake are independent: touch near the top for full pressure, near the bottom for light pressure; release to let go. Hold the brake while selecting P/R/N/D; P BRAKE is a separate toggle. Drag elsewhere to look. Multiple fingers can hold steering, pedals and look independently. Menu pauses and releases held controls; View changes the console camera. After parking, drag to orbit, use +/− to zoom and Return to cockpit to continue.

The hint starts as a single `?` icon. Tap/click the icon, tap the touch toolbar’s `?`, or press T / controller X to expand or collapse it. Collapse preserves tutor cues; Options → Tutor turns them off. Collapse state persists. Menus scroll/reflow on narrow screens. Rendering fills the window at any aspect ratio without stretching the world or adding black bars.


Use **Full screen** on the touch toolbar to hide browser chrome where supported; tap **Exit full** to return. The display option is also available in Options. Browser controls use screen-density-aware sizing, with larger pedals and steering regions. Installing/adding the PWA to the home screen provides another way to launch without the address bar; browser support varies.

The console now shows lateral warning bars as well as bumper sectors. Outer zones cover nearby body sides, corners and mirrors continuously; their colour and beeper reflect the same distances. This is generic geometric parking assistance, not a calibrated copy of the lesson car's sensor hardware.
