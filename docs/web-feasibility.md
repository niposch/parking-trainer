# Browser feasibility — 18 October 2026

**Status update: the C# browser prototype has now been exported and tested.** See [browser instructions and validation](web-playing.md). The investigation below records the initial toolchain audit; its proposed adaptations have subsequently been implemented. Physical controller and external-browser mouse/fullscreen validation remain pending.

The owner accepts regular browser-compatible controllers for this version. Steam Controller-specific support and native Steam Input integration are not web requirements. Preserve independent analog RT gas / LT brake and separate gear selection.

## Export route

Official Godot 4 still excludes C# web export. The project already uses the Compatibility renderer required for WebGL 2. [Godot documentation](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html)

The [user-supplied fork](https://github.com/sethyanow/godot-dotnet-web-export) currently defaults to `macos-editor-4.7.1`; its README links to the upstream Windows releases. Use the upstream [ComplexRobot 4.7.2-stable release](https://github.com/ComplexRobot/godot-dotnet-web-export/releases/tag/4.7.2-stable), matching our engine version, rather than a development release.

Verified release metadata: published 18 August 2026; Windows editor/export bundle `Godot_v4.7.2-stable_mono_web_export_win64.zip`, 166,896,143 bytes. Its instructions require `net9.0`, a top-level `Program.cs`, matching custom templates and NuGet packages, and `wasm-tools`. Its [SDK pin](https://github.com/ComplexRobot/godot-dotnet-web-export/blob/4.7.2-stable/global.json) selects .NET 9.0.100 with feature-band roll-forward. The fork enables invariant globalization and excludes GDExtensions. Its example project still names an older Godot SDK; use the matching release's packages rather than copying that example unchanged.

Locally verified: the game targets `net8.0` with `Godot.NET.Sdk/4.7.2`; only SDK 10.0.204 is installed and `wasm-tools` is absent. There is no export preset. Our current `build.ps1` makes a Windows runtime folder, not a web export.

Use a separate, pinned web toolchain, package cache and build output. The supplied installer changes shared NuGet/template locations; inspect it and configure isolated equivalents. Keep the working Windows build available. No toolchain was downloaded or installed during this investigation.

## Project changes identified

| Area | Required adaptation / verification |
|---|---|
| C# build | Add a web build configuration targeting .NET 9 and the fork's SDK; add its entry point and a reproducible export script. Test trimming and generated Godot bindings in a release export. |
| Controller | Exclude the native SDL implementation and `native/win-x64/SDL3.dll` from web compilation/export; provide a no-op fallback with the same interface. The existing non-Windows constructor guard is useful but does not prove linker compatibility. Keep normal Godot joypad input. |
| Culture | `_Ready()` currently requests `CultureInfo.GetCultureInfo("en")`. Replace that dependency for invariant builds to avoid a possible startup exception. Keep Godot translation resources and localized UI independent of .NET culture support. |
| Window / mouse | Replace desktop window modes and saved window coordinates with browser fullscreen. Add a click-to-start/resume flow for mouse capture and audio; pause on focus/capture loss. |
| Sensor sounds | Existing tones are generated into `AudioStreamWav` data at startup. Verify playback/looping; preferably export four small WAV assets at build time. Preserve 650/500 Hz pitch, loudness and cadence. |
| Rendering | Profile the three mirrors, rear camera, four surround sources and cockpit displays together. Reduce render-target resolution/update frequency if needed, preserving camera geometry, visibility and driving behavior. |
| Settings | Test `user://` settings across reloads and tolerate unavailable browser persistence. |

There are no additional managed dependencies or GDExtensions in the inspected project. Driving, scenario generation and tutor geometry are ordinary C# code; these can remain shared.

Browser fullscreen/mouse capture require an input gesture, gamepads may need a button press before discovery, and audio/persistence have browser restrictions. [Godot browser limitations](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html#limitations)

The [Gamepad API](https://developer.mozilla.org/en-US/docs/Web/API/Gamepad_API/Using_the_Gamepad_API) exposes analog button values from 0 to 1, sufficient for separate pedals. Test a standard Xbox-style or PlayStation-style controller in Chrome/Edge and Firefox; verify partial and simultaneous trigger presses, sticks, menus, reconnect and pause on disconnect. Device/browser mappings still need checking. Windows Steam Input emulation uses native API hooks, so it should not be promised for a normal browser tab. [Valve documentation](https://partner.steamgames.com/doc/features/steam_controller/steam_input_gamepad_emulation_bestpractices?l=english)

## Hosting and proof required

Serve a static export over HTTPS (localhost for development), with correct WASM MIME types and compression. Plan for cross-origin isolation headers for threaded templates:

```http
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Embedder-Policy: require-corp
```

Do not assume the fork supports the official engine's single-thread export path without testing. No game backend is needed. [Hosting reference](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html#serving-the-files)

First prove a small C# export boots and receives independent analog triggers. Then export the actual game and verify driving, all camera views, sensors/audio, tutor, inspection, pointer-lock recovery and saved options. Measure startup size/time and frame rate with the displays active; target 60 FPS at 1080p on a recorded desktop configuration. Mobile browsers are outside this initial target.

**Decision, subsequently implemented:** use this fork for the experimental desktop-browser build. Standard controller support removes the need to port our special Windows controller driver. The successful browser execution and remaining manual checks are recorded in [browser instructions](web-playing.md).
