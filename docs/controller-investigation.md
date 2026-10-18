# Steam Controller (2026) investigation — 18 October 2026

## Findings

- The installed engine is Godot 4.7.2 Mono. Its release sources list **SDL 3.2.28** ([version manifest](https://github.com/godotengine/godot/blob/4.7.2-stable/thirdparty/README.md#sdl)). This predates native support for the 2026 controller; a changed input mapping cannot supply a missing hardware driver.
- Godot's issue **119465** reports this controller not appearing in its input enumeration, including Windows observations. The issue was closed by the SDL 3.4.16 upgrade for the 4.8 development line ([issue](https://github.com/godotengine/godot/issues/119465), [merged fix](https://github.com/godotengine/godot/pull/120373)). This does not establish that the fix is present in 4.7.2.
- Windows recognizes the connected Valve puck (VID `28de`, PID `1304`); Steam's controller log also recognizes it. These checks did not expose credentials, copy account configuration, change firmware, or alter Steam's layout.
- In a local comparison, Godot's controller list was empty while the official SDL **3.4.16** library enumerated **Steam Controller**. The newer driver therefore resolves device discovery on this machine.
- The first live input check detected the device but received no trigger movement. After explicit gamepad polling was added and the old game closed, a second check with the user slowly pressing both triggers reported **L2 maximum 1.00, 10 sampled levels; R2 maximum 1.00, 10 sampled levels**. That confirms physical analog input, beyond enumeration or synthetic events. It does not isolate which of those two changes resolved the first check.

The earlier assumption that “no controller detected” meant only keyboard/mouse emulation was incomplete. This machine has a real driver compatibility gap. Valve also documents that Windows Steam Input emulation depends on input-API hooks ([emulation guidance](https://partner.steamgames.com/doc/features/steam_controller/steam_input_gamepad_emulation_bestpractices)). A desktop layout alone is not proof that a virtual gamepad reaches this process.

## Prototype remedy

Keep the installed engine unchanged. Bundle the official unmodified SDL 3.4.16 x64 library and use it as a fallback when Godot sees no controllers. The C# adapter polls standardized axes/buttons and emits Godot input events for menus and bindings, while reading the same state directly for steering and default pedals. Native trigger values span 0–32767 ([SDL API](https://wiki.libsdl.org/SDL3/SDL_GetGamepadAxis)). Polling is explicit because this library does not own the game window/event loop ([SDL polling API](https://wiki.libsdl.org/SDL3/SDL_UpdateGamepads)).

Godot-detected gamepads take priority to avoid reading both physical and emulated devices. Direct fallback input uses standard physical buttons rather than Steam's remaps; native Steamworks action integration remains deferred. Digital mouse pedal fallbacks cannot override partial analog squeezes while this driver is active. Mouse look remains supported.

Both actual controller D-pad shortcuts and arrow-key camera movement were removed. This also prevents a Steam layout that sends arrow keys from moving the camera. D-pad menu navigation remains available. Full menu/button, focus-loss, hotplug and through-Steam play testing still needs feedback; do not infer those from the trigger check.

The library's license, release link and verified archive checksum are in [asset provenance](../assets/THIRD_PARTY.md). Diagnostic mode: run the game with `-- --controller-check`, then slowly press both triggers within 35 seconds. It reports aggregate trigger ranges/levels and exits without driving.
