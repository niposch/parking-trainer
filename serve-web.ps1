param([int]$Port = 8060, [switch]$Open)
$ErrorActionPreference = 'Stop'
$url = "http://127.0.0.1:$Port"
$existing = $null
try { $existing = Invoke-WebRequest $url -Method Head -UseBasicParsing -TimeoutSec 1 } catch {}
if ($existing) {
    if ($existing.Headers['X-Parking-Practice'] -ne 'web-prototype') { throw "Port $Port is already used by another application. Choose -Port with a different port." }
    if ($Open) { Start-Process $url -WindowStyle Hidden }
    Write-Host "Parking Practice is already running at $url"
    return
}
$python = Get-Command python -ErrorAction SilentlyContinue
$bundled = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
if (Test-Path -LiteralPath $bundled) { $pythonPath = $bundled }
elseif ($python) { $pythonPath = $python.Source }
else { throw 'Python 3 is required for the local test server. Static HTTPS hosting also works.' }
if ($Open) { Start-Process $url -WindowStyle Hidden }
& $pythonPath (Join-Path $PSScriptRoot 'tools\serve-web.py') --port $Port
