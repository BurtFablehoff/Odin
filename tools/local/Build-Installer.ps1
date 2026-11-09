param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$compiler = Join-Path $projectRoot 'local-data\toolchain\inno\ISCC.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Local Inno Setup compiler is missing. See LOCAL-DEVELOPMENT.md.' }
# Never package the development build: build.ps1 without -Dev restores the main identity.
& (Join-Path $projectRoot 'build.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Production build failed.' }
& $compiler (Join-Path $projectRoot 'installer\Odin.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
Write-Host 'Local package built. Nothing installed, uploaded, or released.'
