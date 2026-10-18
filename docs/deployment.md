# GitHub Pages deployment

Source repository: https://github.com/niposch/parking-trainer
Public game: https://niposch.de/parking-trainer/
Project directory: https://niposch.de/live/

The existing user site `niposch/niposch.github.io` owns the custom domain `niposch.de`. This project site inherits that domain at its repository-name path; do not add a project CNAME or change DNS. The homepage project directory is maintained separately in `homepage-workshop/src/data/live-projects.ts`.

## Publish an update

Push source to `main` or manually run **Build and deploy parking trainer** in Actions. `.github/workflows/pages.yml` builds on Windows using the checksum-pinned private Godot 4.7.2 C# exporter and .NET 9.0.319 SDK from `build-web.ps1 -Setup`. The toolchain is cached; no workstation binaries or secrets are uploaded. The build uploads `builds/web` as a Pages artifact, then deploys with Pages/OIDC permissions. Documentation-only pushes do not rebuild the game.

For the local `codex/requirements` working branch, publish an approved revision with `git push origin HEAD:main`; no force push is needed. Watch the workflow in the repository Actions page, then visit the public game in a fresh browser session. Append `?check=1` to run browser regressions. A successful Pages job alone does not verify that the WebAssembly game starts.

## Static-host browser support

The threaded experimental C# export requires cross-origin isolation. Pages does not provide configurable response headers, so the export enables Godot's built-in PWA service worker and `ensure_cross_origin_isolation_headers`. The custom shell installs it when isolation is missing and reloads once it is ready. All executable/data/worker URLs are relative to the project path; the service worker is scoped to `/parking-trainer/`. It does not control the homepage or sibling projects.

The first visit may briefly display “Preparing browser support”. A compatible HTTPS browser, WebGL 2, service workers and shared memory are required. The worker can cache game files; if a stale version persists, reload/close old game tabs, or clear this application's site storage in browser developer tools. The browser runtime remains experimental, and physical mobile/controller testing is still incomplete.

Licence notices are copied into the Pages artifact. Generated exports, caches and compiler packages remain ignored by Git. No backend or homelab ingress route is involved.

Sources: [GitHub project-domain inheritance](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site/about-custom-domains-and-github-pages), [Godot isolation support](https://docs.godotengine.org/en/stable/classes/class_editorexportplatformweb.html#class-editorexportplatformweb-property-progressive-web-app-ensure-cross-origin-isolation-headers).

Initial publication succeeded in [Pages run 39158559732](https://github.com/niposch/parking-trainer/actions/runs/39158559732). Pages initially chose `master`; its source was corrected to `main`, HTTPS enforcement enabled, and the `github-pages` environment restricted to `main` only. A rerun of the deployment job succeeded. The actual public game passed all 50 checks for that revision, including first-load isolation; later validation counts are recorded in `prototype-validation.md`.
