# GitHub Pages deployment

Source repository: https://github.com/niposch/parking-trainer
Public game: https://niposch.de/parking-trainer/
Project directory: https://niposch.de/live/

The existing user site `niposch/niposch.github.io` owns the custom domain `niposch.de`. This project site inherits that domain at its repository-name path; do not add a project CNAME or change DNS. The homepage project directory is maintained separately in `homepage-workshop/src/data/live-projects.ts`.

## Publish an update

Push source to `main` or manually run **Build and deploy parking trainer** in Actions. `.github/workflows/pages.yml` builds on Windows using the checksum-pinned private Godot 4.7.2 C# exporter and .NET 9.0.319 SDK from `build-web.ps1 -Setup`. The toolchain is cached; no workstation binaries or secrets are uploaded. The build uploads `builds/web` as a Pages artifact, then deploys with Pages/OIDC permissions. Documentation-only pushes do not rebuild the game.

For the local `codex/requirements` working branch, publish an approved revision with `git push origin HEAD:main`; no force push is needed. Watch the workflow in the repository Actions page, then visit the public game in a fresh browser session. Append `?check=1` to run browser regressions. A successful Pages job alone does not verify that the WebAssembly game starts.

## Static-host browser support

The threaded experimental C# export requires cross-origin isolation. Pages does not provide configurable response headers, so `web/service-worker.js` supplies COOP/COEP within `/parking-trainer/`. It replaces the generated Godot worker at the stable URL `index.service.worker.js`. The homepage and sibling projects are outside its scope. A compatible HTTPS browser, WebGL 2, service workers and shared memory are required.

### Automatic browser updates — 24 October 2026

Pushing to `main` publishes this update behavior through the Pages workflow.

- Every export has a unique build ID: source revision plus Actions run/attempt, or a UTC timestamp locally. Godot exports matching `game-<build>.js/.wasm/.pck` and worklet filenames, preserving its required naming relationships. The root entry point remains `index.html`; `version.json` identifies the published build. The manifest stays at `index.manifest.json` with stable application identity and the original `./index.html` identity/launch URL, so installed apps never point to a removed export filename. The script clears only the verified generated output directory before export.
- The inlined `web/update-client.js` checks `version.json` before downloading the engine or game data. Discovery and navigation use network requests with unique query strings and `cache: no-store`, bypassing both the worker's old HTML and Pages' ten-minute HTTP cache. A stale shell reloads to the published release, with a two-retry limit for partial publication. A failed freshness check shows an error instead of silently starting an old game. New online tabs load the latest release visible to the server at that check; offline startup is not supported by this policy.
- Open sessions check automatically every 60 seconds, on focus/visibility return, and when connectivity returns. Browsers may throttle hidden-tab timers. A discovered update appears in the pause menu as **Reload to update**, with a warning that reloading ends the current practice session. There is no check-for-updates button and background checks never force a running current-generation game to reload. Settings are saved before a chosen reload; query options such as `?check=1` are retained.
- Only versioned executable assets use cache-first loading. Old versions remain cached while their marked tabs are open, and unused versions are pruned after loading a new pack. Cache-storage failures fall back to the network. HTML and release discovery never use cached fallbacks. IndexedDB/settings and other applications' caches are not cleared.
- Migration from the original Godot cache activates the replacement worker without waiting for all tabs to close. Legacy unmarked tabs refresh once to acquire the new startup gate; an old practice session may end during this one-time migration. Later updates do not force existing sessions to reload. Do not change the stable worker URL or remove this migration behavior without testing old cached clients.

The Pages workflow now gates deployment on `tools/test-web-updates.cjs`, using real headless Chromium. It covers warm caches, two releases with old tabs open, automatic notification, chosen reload, explicit cached `index.html` migration, network failure, and bounded retries during partial publication. Run locally:

```powershell
npm install --no-save --prefix .cache/web-tests playwright@1.64.1
node .cache/web-tests/node_modules/playwright/cli.js install chromium
node --test tools/test-web-updates.cjs
```

After publishing, compare the live `version.json` build with the deployed Actions revision. Verify the actual game with `?check=1`, then keep that tab open across another deployment and confirm a new tab loads the new build while the old tab offers the update in its menu. Existing October 18 observations below describe the previous worker behavior. The browser runtime remains experimental, and physical mobile/controller testing is still incomplete.

Licence notices are copied into the Pages artifact. Generated exports, caches and compiler packages remain ignored by Git. No backend or homelab ingress route is involved.

Sources: [GitHub project-domain inheritance](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site/about-custom-domains-and-github-pages), [Godot isolation support](https://docs.godotengine.org/en/stable/classes/class_editorexportplatformweb.html#class-editorexportplatformweb-property-progressive-web-app-ensure-cross-origin-isolation-headers).

Initial publication succeeded in [Pages run 39158559732](https://github.com/niposch/parking-trainer/actions/runs/39158559732). Pages initially chose `master`; its source was corrected to `main`, HTTPS enforcement enabled, and the `github-pages` environment restricted to `main` only. A rerun of the deployment job succeeded. The actual public game passed all 50 checks for that revision, including first-load isolation; later validation counts are recorded in `prototype-validation.md`.

Latest verified runtime update (18 October 2026): `ff49484`, [run 39162154796](https://github.com/niposch/parking-trainer/actions/runs/39162154796), including mobile sizing/fullscreen, continuous proximity and mirror/door repairs. Build/deploy succeeded; the public game passed 110 checks. Close old game tabs and reopen when a cached previous release persists. Installed-app manifest display mode is fullscreen.

Phone follow-up runtime `9ee9dc9` deployed successfully in [run 39164029366](https://github.com/niposch/parking-trainer/actions/runs/39164029366), with 128 public-browser checks passed. Includes menu swipes, streamed red-zone alarms, left-side landscape gears and surround-camera refinements. A reload may initially use the old controlled cache; close/reopen after the new worker installs.
