param(
    [string]$Destination = 'C:\_repo\Odin',
    [switch]$SkipVerification
)
$ErrorActionPreference = 'Stop'
$packageRoot = $PSScriptRoot
$targetRoot = [System.IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $targetRoot) { throw "Destination already exists; nothing was overwritten: $targetRoot" }
if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'Install Git for Windows, then run this installer again.' }
$manifestPath = Join-Path $packageRoot 'TRANSFER-MANIFEST.json'
if (-not (Test-Path -LiteralPath $manifestPath)) { throw 'Extract the complete transfer ZIP, then run Install-OdinLocal.ps1 from its top folder.' }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
Write-Host 'Verifying transfer files...'
$packagePrefix = [System.IO.Path]::GetFullPath($packageRoot).TrimEnd('\') + '\'
foreach ($item in $manifest.files) {
    $sourcePath = [System.IO.Path]::GetFullPath((Join-Path $packageRoot $item.path))
    if (-not $sourcePath.StartsWith($packagePrefix,[StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe transfer entry: $($item.path)" }
    $sourceFile = Get-Item -LiteralPath $sourcePath
    if ($sourceFile.Length -ne $item.bytes -or (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne $item.sha256) {
        throw "Transfer verification failed: $($item.path)"
    }
}
$parentRoot = Split-Path -Parent $targetRoot
New-Item -ItemType Directory -Path $parentRoot -Force | Out-Null
$stagingRoot = Join-Path $parentRoot ('.odin-import-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stagingRoot | Out-Null
$historyBundle = Join-Path $packageRoot 'Odin-history.bundle'
& git -C $stagingRoot init --quiet
if ($LASTEXITCODE -ne 0) { throw "Git init failed; staging retained at $stagingRoot" }
& git -C $stagingRoot fetch --quiet $historyBundle HEAD
if ($LASTEXITCODE -ne 0) { throw "Git import failed; staging retained at $stagingRoot" }
& git -C $stagingRoot checkout --quiet -b codex/local-odin FETCH_HEAD
if ($LASTEXITCODE -ne 0) { throw "Git checkout failed; staging retained at $stagingRoot" }
$actualCommit = (& git -C $stagingRoot rev-parse HEAD).Trim()
if ($actualCommit -ne $manifest.commit) { throw 'Imported Git checkpoint differs from the transfer manifest.' }
& git -C $stagingRoot remote add origin 'https://github.com/BurtFablehoff/Odin.git'
if ($LASTEXITCODE -ne 0) { throw 'Could not configure the Git origin.' }
Write-Host 'Copying photos, videos, and review files into local-data...'
Copy-Item -LiteralPath (Join-Path $packageRoot 'payload') -Destination (Join-Path $stagingRoot 'local-data') -Recurse
# Both absolute paths are verified siblings in the requested parent; move only
# this newly created staging checkout. No existing checkout is removed.
$resolvedStage = [System.IO.Path]::GetFullPath($stagingRoot)
$parentPrefix = [System.IO.Path]::GetFullPath($parentRoot).TrimEnd('\') + '\'
if (-not $resolvedStage.StartsWith($parentPrefix,[StringComparison]::OrdinalIgnoreCase) -or
    -not $targetRoot.StartsWith($parentPrefix,[StringComparison]::OrdinalIgnoreCase) -or
    (Test-Path -LiteralPath $targetRoot)) { throw 'Destination changed during import; staging retained.' }
Move-Item -LiteralPath $resolvedStage -Destination $targetRoot
if (-not $SkipVerification) {
    & (Join-Path $targetRoot 'tools\local\Verify-OdinLocal.ps1') -ProjectRoot $targetRoot
}
Write-Host "Odin local checkout is ready: $targetRoot"
Write-Host 'Read tools\local\RESUME.txt before continuing the artwork work.'
