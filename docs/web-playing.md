# Browser prototype

An experimental C# browser build is available in `builds/web/`. The browser downloads its runtime; players do not need .NET installed. It retains the same driving model, first-person cabin, mirrors, camera/sensors, procedural scenarios, tutor and parked inspection.

## Play locally

Double-click **Launch-Web.cmd**. It starts a localhost-only server and opens the system browser at <http://127.0.0.1:8060/>. Keep the server window open while playing; close it or press Ctrl+C to stop. If our test server is already running, the launcher opens that existing instance.

Use a current desktop Chrome/Edge or Firefox. Click **Continue driving** to request mouse capture and unlock audio. Esc releases the mouse and pauses. Use Options → Display for browser fullscreen; the browser can also exit fullscreen with Esc. Fullscreen is not restored automatically after loading.

Press a controller button while the page is focused so the browser can discover it. Standard gamepads are the target: RT gas, LT brake, left stick steering, right stick look. RB selects D, LB selects R, A selects P; hold the brake when changing gear. Y toggles the parking brake, X the camera screen, B glances at it, View looks over the shoulder, right-stick click looks forward. The D-pad navigates menus; while driving, D-pad up/down toggle the instructions and the correction plan. See the controller map in [playing.md](playing.md). Controller setup includes pedal readouts and rebinding. The special Windows Steam Controller driver is excluded.

Keyboard fallback: W/S gas/brake, A/D steering, 1/2/3/4 P/R/N/D, Space parking brake, mouse look, Tab camera mode, V look at console, C shoulder view, Home forward. Every scenario starts in P with the parking brake applied.

The Codex embedded browser rejects pointer lock in the tested container and did not enter fullscreen. Use the launcher to play in an ordinary desktop browser. Real mouse capture/fullscreen and physical standard-gamepad triggers still need checking there; the successful automated tests do not establish hardware compatibility.

## Rebuild

From the repository in PowerShell:

```powershell
./build-web.ps1 -Setup  # First run: download/check the private SDK and exporter; install wasm-tools
./build-web.ps1         # Subsequent release exports
./serve-web.ps1 -Open   # Serve the generated build locally
```

The pinned experimental Godot 4.7.2 exporter, .NET SDK 9.0.319, workload and NuGet cache live under `.cache/web-toolchain/`. Source is staged under `.cache/web-project/`; the Windows project still targets .NET 8. The script does not run the fork's shared installer or modify the installed Godot. Initial setup downloads substantial compiler/runtime packages and requires network access. Build output, caches and binaries are excluded from Git.

The script checks for export errors, managed runtime content and accidental SDL inclusion. It creates gzip versions of WASM/PCK/JS assets. The included development server negotiates them automatically. Do not open `index.html` directly from disk.

## Hosting and validation

Play at **https://niposch.de/parking-trainer/**. The Pages build uses Godot’s service worker to supply isolation within the game’s subdirectory; see [deployment and maintenance](deployment.md).

Alternatively, publish the contents of `builds/web/` on a static HTTPS host with `application/wasm` for WASM files, plus:

```http
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Embedder-Policy: require-corp
```

If serving `.gz` files, retain the original request URLs and send `Content-Encoding: gzip` with the original MIME type. Include the generated licence/notice files. No game backend is needed; Pages hosting is configured through [the deployment workflow](deployment.md).

Append `?check=1` to run the browser checks. They verify trigger-event mappings and synthetic analog action values, automatic creep/braking, camera/sensor displays, all 15 type/difficulty/side demonstrations and inspection. They do not emulate physical Gamepad API discovery. Native-style synthetic joypad injection was unreliable in the experimental release runtime, so mapping and action handling are tested separately. Results and short frame-rate samples appear in the browser console; completion also appears on the page.

Before the touch/viewport update on 18 October 2026, all **31 browser runtime checks** passed in the app's Chromium WebGL 2 browser, plus **102 Windows headless runtime checks** and **1,169,077 simulation assertions**. The final browser test had no runtime errors. With a 1280×720 canvas and 1440×900 logical design space, three-second process-frame samples averaged **236.2 FPS rear** and **185.3 FPS surround**, including sensors and three mirrors. The PC has an RTX 4080 SUPER and AMD integrated graphics; the browser exposed a generic WebKit renderer name, so the active adapter was not identified. These short stationary samples are not a general hardware benchmark or mobile guarantee.

Scenario type and mirrored side survived a browser reload. Main assets total about **100 MB uncompressed / 31 MB gzip**. The export retains upstream GodotSharp/CoreLib trimming warnings; the exercised paths passed, but the fork remains experimental. Physical gamepad input, audio listening, pointer lock/fullscreen in an external browser and Firefox are remaining manual checks. Browser storage can be unavailable or evicted.

## Touch devices and window sizing

Options → On-screen controls → Auto / On / Off is available on desktop and mobile. Auto activates on a touchscreen/mobile device or the first actual touch; use On if detection fails. Independent gas/brake pads, steering, drag-to-look, gear selection, parking brake and inspection orbit are supported. See [touch controls](playing.md#touch-and-hint-controls). T / X / Hint collapses or expands the tutor without disabling it; the collapsed hint is a single `?` icon. Input mode and hint state are saved in browser storage.

Canvas and 3D viewport follow the actual window size. UI uses CSS-sized units accounting for screen density, menus reflow/scroll in portrait or small landscape windows, and the 3D camera retains a perspective projection without letterboxing. High-resolution mobile rendering may be demanding; the experimental threaded .NET/WebGL 2 build still requires a compatible browser and isolation headers. A physical phone/tablet has not been validated.

The touch/viewport update passed **49 browser runtime checks**, **119 Windows headless / 125 windowed checks** and **1,169,077 simulation assertions**. Browser checks include independent touch pointers, brake/gear interlocks, collapse/expand and full-window coverage in a small landscape viewport; phone hardware remains untested. See [current validation](prototype-validation.md#relocation-touch-controls-responsive-viewport-and-hint--18-october-2026).


The touch toolbar now includes **Full screen / Exit full**, usable while driving or inspecting. It must be tapped because browsers require a user gesture. The installed PWA requests fullscreen display, providing an alternative to browser-tab chrome where supported. Menus and enlarged controls scale with device pixel ratio rather than shrinking on dense screens. Test on your phone after reloading/closing old game tabs so its cached worker updates. See the latest section of [validation](prototype-validation.md) for automated coverage and remaining physical-device checks.
