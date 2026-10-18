# Asset provenance

Only free assets are used. No purchases, premium packs, copied game assets, logos, or manufacturer models were added.

## Kenney Car Kit 3.1

- Source: https://kenney.nl/assets/car-kit
- Acquired: 18 October 2026 from the provider's free ZIP download.
- Licence: CC0; original notice retained in `kenney/LICENSE.txt`.
- Included models: sedan, SUV, van, sports hatchback; shared colormap texture.
- Runtime modifications adapt one continuous sedan body into a generic liftback: upper cabin widened, rear hatch extended, a rear side-window aperture added, glazing separated for exterior rendering, and dark inner surfaces/window reveals added. The original painted body, lights, wheel arches and roof remain connected. The original toy mirrors are replaced by functioning mirrors, their attachment openings are capped, and the recessed lower side rail is brought flush with the fender plane. Body/arch positions match the 4.45 m profile and 2.65 m axles; wheels remain circular on a 1.52 m track. Simple seats, interior door cards, dashboard and steering wheel use primitives. No second exterior shell or purchased vehicle model is used.
- Parked vehicles are dimensioned variants of the reused meshes. Their wheelbases and landmark positions are approximate; they are not vehicle replicas.

The original licence identifies the asset creator; it is kept as repository documentation and is not displayed as in-game branding.

## Godot Engine 4.7.2 Mono

The local runtime bundle copies the user's installed Godot executable and support files. Engine licence and third-party notices are retained under `licenses/`. This repository does not track those engine binaries. Official notices: https://github.com/godotengine/godot/tree/4.7.2-stable

Audio beeps and environment/parking markings are generated locally by the prototype. Reference images supplied in chat have not been copied into the game or redistributed.

## Experimental browser toolchain

- [ComplexRobot Godot .NET web export 4.7.2-stable](https://github.com/ComplexRobot/godot-dotnet-web-export/releases/tag/4.7.2-stable), upstream of the owner-supplied sethyanow fork. Windows toolchain archive SHA-256: `7e54140fd3e84dfbf676bce56f6a792f1bc7734db36c67d860376b002f1a21d8`.
- Builds use an isolated .NET SDK 9.0.319 and browser runtime 9.0.21. The runtime's licence and third-party notices are retained under `licenses/web-dotnet/`; Godot notices remain under `licenses/`. Build output includes these notices.
- The browser uses the same CC0 vehicle resources. Its four PCM sensor WAV files are baked from our existing generated sine tones by `tools/generate-sensor-audio.py`; no additional audio assets were acquired.
- The Windows SDL compatibility DLL is excluded from web compilation and distribution. Toolchain binaries and generated exports are not tracked in Git.

## SDL 3.4.16 controller compatibility library

- Official release: https://github.com/libsdl-org/SDL/releases/tag/release-3.4.16
- Unmodified Windows x64 `SDL3.dll` is included at `native/win-x64/SDL3.dll`.
- Free zlib license retained at `native/LICENSE-SDL.txt` and copied into the runtime bundle.
- Downloaded archive: `SDL3-3.4.16-win32-x64.zip`; SHA-256 verified against the official release asset digest: `4217944b4e51457af4a59c82d883f8443b3e65964b2acd8943484c492756c4b6`.
- Used only as a fallback input reader. It does not replace or patch the user's installed Godot engine.
