param([switch]$Run, [switch]$Test, [switch]$Package, [switch]$Import)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$godotRoot = 'C:\Program Files\Godot 4.7.2 Mono'
$env:GODOT_NUGET = Join-Path $godotRoot 'GodotSharp\Tools\nupkgs'
$env:NUGET_PACKAGES = Join-Path $projectRoot '.cache\nuget'
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.cache\dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
Push-Location $projectRoot
try {
    if ($Test) {
        dotnet run --project tests/Tests.csproj
        if ($LASTEXITCODE) { throw 'Simulation checks failed.' }
    }
    dotnet build ParkingTrainer.csproj --nologo
    if ($LASTEXITCODE) { throw 'C# build failed.' }
    $engine = Join-Path $godotRoot 'Godot_v4.7.2-stable_mono_win64_console.exe'
    if ($Import -or $Run -or $Package) {
        & $engine --headless --editor --path $projectRoot --import
        if ($LASTEXITCODE) { throw 'Godot resource import failed.' }
    }
    if ($Package) {
        $bundle = Join-Path $projectRoot 'builds\ParkingPractice'
        New-Item -ItemType Directory -Path $bundle -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $projectRoot 'builds\.gdignore') -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $godotRoot 'Godot_v4.7.2-stable_mono_win64.exe') -Destination (Join-Path $bundle 'ParkingPractice.exe')
        foreach ($entry in @('GodotSharp')) { Copy-Item -LiteralPath (Join-Path $godotRoot $entry) -Destination $bundle -Recurse -Force }
        foreach ($entry in @('project.godot','Main.tscn','assets','src','native','.godot')) { Copy-Item -LiteralPath (Join-Path $projectRoot $entry) -Destination $bundle -Recurse -Force }
        Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\playing.md') -Destination (Join-Path $bundle 'README.md') -Force
        Copy-Item -LiteralPath (Join-Path $projectRoot 'licenses') -Destination $bundle -Recurse -Force
        Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\THIRD_PARTY.md') -Destination $bundle -Force
        Write-Host "Windows runtime bundle: $bundle\ParkingPractice.exe"
    }
    if ($Run) { & (Join-Path $godotRoot 'Godot_v4.7.2-stable_mono_win64.exe') --path $projectRoot }
} finally { Pop-Location }
