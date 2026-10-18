# Playing prototype 01

Run `ParkingPractice.exe` from the runtime bundle, or `Launch.cmd` from the source repository. All driving is **first person**.

Choose the exercise and select **Drive / Resume**. Every scenario starts in **P with the parking brake applied**. Hold L2/S, select R or D, then explicitly release the parking brake with right-stick click/Space. Release the service brake gently to creep. Resuming does not change gear or release the parking brake. Park only after stopping: select P, then apply the parking brake.

The mouse is captured while driving and moves the driver's head. Escape pauses and releases it for menus; losing application focus also pauses. Starting a new scenario releases the mouse until you choose Drive again.

| Action | Controller / Steam Input gamepad layout | Keyboard |
|---|---|---|
| Gas | Right trigger, analog | W |
| Service brake, in either direction | Left trigger, analog | S |
| Turn steering wheel | Left stick | A / D |
| Look around | Right stick | Arrow keys |
| Look around with mouse | Steam mouse/trackpad output | Move mouse |
| Select R / D while stopped and braked | Left / right shoulder | 2 / 4 |
| Cycle P → R → N → D while stopped and braked | Bottom face button (A mapping) | 1 P / 2 R / 3 N / 4 D |
| Parking brake | Right-stick click | Space |
| Return steering toward centre | Hold left-stick click | Hold Z |
| Look left / right | D-pad left / right | Arrow keys |
| Shoulder view / forward view | D-pad down / up | C / Home |
| Look down at center console | Right face button (B mapping) | V |
| Rear display → surround display → sensor-only | Top face button (Y mapping) | Tab |
| Toggle tutor | Left face button (X mapping) | T |
| Pause / resume | Start | Escape |
| Open menu | Back | F1 |
| Retry / new variation | Pause menu | F5 / F6 |
| Watch demonstration | Pause menu | F2 |
| Fixed / steering rear guides | Pause menu | G |

The default steering mode **turns and holds**: hold the stick toward a side to turn the wheel; release it to hold the angle. This avoids snapping the wheels straight while stopped. The menu also offers stick-angle steering. Use the physical steering wheel as the angle cue; hold left-stick click/Z to return toward centre.

Speed, gear and the amber `(P)` parking-brake indicator are on the instrument panel behind the wheel. The camera and sensor readout are in the center console. Use B/V to glance down at it, then D-pad Up/Home to look forward. Mirrors are mounted on the car and can be inspected with head-look controls. No floating mirror, speed, or pedal panels are shown while driving.

Menus support controller focus navigation. Scroll down for view settings, beeper mute, teaching map, and keyboard help. The map only appears while paused; it is separate from the car's surround-camera display.

The yellow camera lines represent approximately body width plus 2 cm per side. Cross lines are at 0.4 m, 1 m and 2 m of reverse travel from the rear bumper reference; they are **not** a guarantee of mirror/front-corner clearance. Front and rear beepers have different pitches and become continuous near 0.30 m.

The prototype permits pull-forward corrections. A green target outline marks the desired body footprint. The tutor shows goal offset, heading error, and contact events; no timer or traffic pressure is involved.

Steam must launch the executable for its per-game controller layout to apply. For **analog pedals**, select a **Gamepad** layout and keep L2/LT and R2/RT analog output enabled as their respective triggers. A keyboard/mouse layout can make other controls work while the game reports no gamepad. That layout now also works with **left mouse button = gas, right mouse button = brake**, but these pedals are on/off rather than analog.

Scroll down in the pause menu for the **Input test**: slowly press each trigger and check that its percentage varies smoothly. The menu shows detected gamepads and the last received button/axis/key. **Bind L2 brake / Bind R2 gas** allow an alternate axis, button, key or mouse button; press the desired input fully, or Escape to cancel. Bindings persist, and **Reset pedals** restores defaults. Analog trigger events are accepted from any gamepad, including Steam's virtual gamepad. No native Steamworks API or Steam store release is required. The physical Steam Controller/layout still needs a repeat test on the user's setup.

This uses a generic 4.45 m × 1.80 m car with a 2.65 m wheelbase. Exact seat position, mirrors, wide-angle lens distortion, and lesson-car reference cues remain uncalibrated. This first version is for feedback on controls and parking practice, not a claim of exact real-car sightline matching.
