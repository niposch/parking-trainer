param([switch]$Setup, [switch]$DebugExport)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$cache = Join-Path $root '.cache\web-toolchain'
$sdk = Join-Path $cache 'dotnet'
$engineRoot = Join-Path $cache 'godot\Godot_v4.7.2-stable_mono_web_export_win64'
$engine = Join-Path $engineRoot 'Godot_v4.7.2-stable_mono_web_export_win64_console.exe'
$stage = Join-Path $root '.cache\web-project'
$output = Join-Path $root 'builds\web'
$revision = $env:GITHUB_SHA
if (!$revision) {
    $revision = git -C $root rev-parse --short=12 HEAD
    if ($LASTEXITCODE) { throw 'Cannot identify source revision.' }
}
$revision = $revision.Trim().Substring(0,[Math]::Min(12,$revision.Trim().Length))
$buildStamp = if ($env:GITHUB_RUN_ID) { "$env:GITHUB_RUN_ID-$env:GITHUB_RUN_ATTEMPT" } else { [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff') }
$buildId = "$revision-$buildStamp"
if ($buildId -notmatch '^[a-zA-Z0-9-]{1,100}$') { throw 'Invalid build identifier.' }
$executable = "game-$buildId"
$names = @('PATH','DOTNET_ROOT','DOTNET_CLI_HOME','NUGET_PACKAGES','ParkingWeb','DOTNET_SKIP_FIRST_TIME_EXPERIENCE','DOTNET_CLI_TELEMETRY_OPTOUT','DOTNET_GENERATE_ASPNET_CERTIFICATE')
$saved = @{}; foreach ($name in $names) { $saved[$name] = [Environment]::GetEnvironmentVariable($name,'Process') }
try {
    New-Item -ItemType Directory -Force -Path $cache | Out-Null
    if ($Setup) {
        $downloads = @(
            @{Name='godot-web.zip'; Destination=(Join-Path $cache 'godot'); Algorithm='SHA256'; Hash='7e54140fd3e84dfbf676bce56f6a792f1bc7734db36c67d860376b002f1a21d8'; Url='https://github.com/ComplexRobot/godot-dotnet-web-export/releases/download/4.7.2-stable/Godot_v4.7.2-stable_mono_web_export_win64.zip'},
            @{Name='dotnet-sdk.zip'; Destination=$sdk; Algorithm='SHA512'; Hash='663d866ad213602d8e267ea5eb575e79bf3f4b920b29f246017021551e93e895e1b2a1f0abee08ec26030eb95b818792a2a7a5835fbada90798ad66eb5c7b168'; Url='https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.319/dotnet-sdk-9.0.319-win-x64.zip'}
        )
        foreach ($download in $downloads) {
            $archive = Join-Path $cache $download.Name
            if (!(Test-Path -LiteralPath $archive)) { Invoke-WebRequest $download.Url -OutFile $archive }
            if ((Get-FileHash -LiteralPath $archive -Algorithm $download.Algorithm).Hash -ne $download.Hash) { throw "Checksum mismatch: $archive" }
            if (!(Test-Path -LiteralPath $download.Destination)) { Expand-Archive -LiteralPath $archive -DestinationPath $download.Destination }
        }
    }
    if (!(Test-Path -LiteralPath $engine) -or !(Test-Path -LiteralPath "$sdk\dotnet.exe")) { throw 'Run ./build-web.ps1 -Setup once to download the isolated web toolchain.' }
    $env:PATH = "$sdk;$env:PATH"; $env:DOTNET_ROOT = $sdk
    $env:DOTNET_CLI_HOME = Join-Path $cache 'cli'; $env:NUGET_PACKAGES = Join-Path $cache 'packages'
    $env:ParkingWeb = 'true'; $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    if ($Setup) {
        & "$sdk\dotnet.exe" workload install wasm-tools --skip-manifest-update --source https://api.nuget.org/v3/index.json
        if ($LASTEXITCODE) { throw 'WASM workload setup failed.' }
    }
    # Only clear the known generated output, after verifying its absolute path.
    $expectedOutput = [System.IO.Path]::GetFullPath((Join-Path $root 'builds\web'))
    if ([System.IO.Path]::GetFullPath($output) -ne $expectedOutput) { throw 'Invalid web output path.' }
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $stage,$output | Out-Null
    # Stage a clean source copy. Neither toolchain packages nor native Windows binaries enter the export.
    $stagePrefix = [System.IO.Path]::GetFullPath($stage) + [System.IO.Path]::DirectorySeparatorChar
    foreach ($folder in @('src','assets','web')) {
        $stagedFolder = [System.IO.Path]::GetFullPath((Join-Path $stage $folder))
        if (!$stagedFolder.StartsWith($stagePrefix,[System.StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid staging path.' }
        if (Test-Path -LiteralPath $stagedFolder) { Remove-Item -LiteralPath $stagedFolder -Recurse -Force }
    }
    foreach ($entry in @('src','assets','web','Main.tscn','project.godot','ParkingTrainer.csproj','Program.cs')) {
        Copy-Item -LiteralPath (Join-Path $root $entry) -Destination $stage -Recurse -Force
    }
    '{"sdk":{"version":"9.0.319","rollForward":"disable"}}' | Set-Content (Join-Path $stage 'global.json') -Encoding UTF8
    $packageSource = [System.Security.SecurityElement]::Escape((Join-Path $engineRoot 'nuget'))
    "<configuration><packageSources><clear/><add key=`"Godot web fork`" value=`"$packageSource`"/><add key=`"nuget.org`" value=`"https://api.nuget.org/v3/index.json`"/></packageSources></configuration>" | Set-Content (Join-Path $stage 'NuGet.Config') -Encoding UTF8
    New-Item -ItemType File -Force -Path (Join-Path $engineRoot '_sc_') | Out-Null
    $templates = Join-Path $engineRoot 'editor_data\export_templates\4.7.2.stable.mono'
    New-Item -ItemType Directory -Force -Path $templates | Out-Null
    foreach ($template in @('web_release.zip','web_debug.zip')) { Copy-Item -LiteralPath (Join-Path $engineRoot $template) -Destination $templates -Force }
    $releaseTemplate = (Join-Path $templates 'web_release.zip').Replace('\','/')
    $debugTemplate = (Join-Path $templates 'web_debug.zip').Replace('\','/')
    @"
[preset.0]
name="Web"
platform="Web"
runnable=true
export_filter="all_resources"
include_filter=""
exclude_filter=""
export_path=""
script_export_mode=2

[preset.0.options]
custom_template/debug="$debugTemplate"
custom_template/release="$releaseTemplate"
variant/extensions_support=false
variant/thread_support=true
vram_texture_compression/for_desktop=true
vram_texture_compression/for_mobile=false
html/export_icon=false
html/custom_html_shell="res://web/shell.html"
html/canvas_resize_policy=2
html/focus_canvas_on_start=true
progressive_web_app/enabled=true
progressive_web_app/display=0
progressive_web_app/ensure_cross_origin_isolation_headers=true
"@ | Set-Content (Join-Path $stage 'export_presets.cfg') -Encoding UTF8
    Push-Location $stage
    try {
        if (!(Test-Path -LiteralPath 'ParkingTrainer.sln')) {
            & "$sdk\dotnet.exe" new sln --name ParkingTrainer
            & "$sdk\dotnet.exe" sln ParkingTrainer.sln add ParkingTrainer.csproj
            if ($LASTEXITCODE) { throw 'Web solution setup failed.' }
        }
        & "$sdk\dotnet.exe" build ParkingTrainer.csproj --nologo
        if ($LASTEXITCODE) { throw 'Web C# build failed.' }
        & $engine --headless --editor --path $stage --import
        if ($LASTEXITCODE) { throw 'Web resource import failed.' }
        $exportFlag = if ($DebugExport) { '--export-debug' } else { '--export-release' }
        & $engine --headless --path $stage $exportFlag Web (Join-Path $output "$executable.html") 2>&1 | Tee-Object -Variable exportLines
        if ($LASTEXITCODE -or ($exportLines -match '^ERROR:')) { throw 'Web export failed; inspect the export log.' }
        $packIndex = [System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes((Join-Path $output "$executable.pck")))
        foreach ($required in @('.godot/mono/publish/wasm32/ParkingTrainer.dll','GodotSharp.dll','.dotnet-publish-manifest')) {
            if (!$packIndex.Contains($required)) { throw "Exporter omitted managed content: $required" }
        }
        if ($packIndex.Contains('SDL3.dll')) { throw 'Windows native controller library entered the browser package.' }
    } finally { Pop-Location }
    $htmlPath = Join-Path $output "$executable.html"
    $html = [System.IO.File]::ReadAllText($htmlPath)
    $client = [System.IO.File]::ReadAllText((Join-Path $root 'web\update-client.js'))
    $html = $html.Replace('__PARKING_UPDATE_CLIENT__',$client).Replace('__PARKING_BUILD__',$buildId)
    $html = $html.Replace("$executable.service.worker.js",'index.service.worker.js').Replace("$executable.manifest.json",'index.manifest.json')
    # Keep installed-app identity and launch URLs stable after renaming the HTML.
    $manifestPath = Join-Path $output "$executable.manifest.json"
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $manifest.start_url = './index.html'
    $manifest | Add-Member -NotePropertyName id -NotePropertyValue './index.html' -Force
    $manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'index.manifest.json') -Encoding utf8
    Remove-Item -LiteralPath $manifestPath -Force
    [System.IO.File]::WriteAllText((Join-Path $output 'index.html'),$html)
    Remove-Item -LiteralPath $htmlPath,(Join-Path $output "$executable.service.worker.js") -Force
    Copy-Item -LiteralPath (Join-Path $root 'web\service-worker.js') -Destination (Join-Path $output 'index.service.worker.js') -Force
    @{build=$buildId;revision=$revision} | ConvertTo-Json -Compress | Set-Content (Join-Path $output 'version.json') -Encoding utf8
    New-Item -ItemType File -Force -Path (Join-Path $output '.nojekyll') | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'licenses') -Destination $output -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $root 'assets\THIRD_PARTY.md') -Destination $output -Force
    foreach ($file in Get-ChildItem -LiteralPath $output -File | Where-Object { $_.Extension -in @('.wasm','.pck','.js') }) {
        $source = [System.IO.File]::OpenRead($file.FullName)
        $compressed = [System.IO.File]::Create($file.FullName+'.gz')
        $gzip = [System.IO.Compression.GZipStream]::new($compressed,[System.IO.Compression.CompressionLevel]::Optimal)
        try { $source.CopyTo($gzip) } finally { $gzip.Dispose();$compressed.Dispose();$source.Dispose() }
    }
    Write-Host "Browser build: $output\index.html"
    Write-Host 'Serve with ./serve-web.ps1 (do not open index.html directly).'
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$saved[$name],'Process') }
}
