param([string]$AssetPack, [switch]$CompileOnly, [switch]$Dev, [string]$DevExecutableName='Odin-Dev-Local.exe')
$ErrorActionPreference = 'Stop'
$output = Join-Path $PSScriptRoot $(if ($Dev) { 'test-exes' } else { 'build' })
New-Item -ItemType Directory -Path $output -Force | Out-Null
if ([IO.Path]::GetFileName($DevExecutableName) -ne $DevExecutableName -or -not $DevExecutableName.EndsWith('.exe')) { throw 'DevExecutableName must be an EXE filename without a directory.' }
$executableName = if ($Dev) { $DevExecutableName } else { 'Odin.exe' }
$arguments = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+', '/unsafe',
    ('/out:' + (Join-Path $output $executableName)),
    ('/win32manifest:' + (Join-Path $PSScriptRoot 'app\app.manifest')),
    '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll', '/reference:System.Web.Extensions.dll')
# Part 5b (rule A9): both builds embed the promoted artwork pack Odin.LatestArt. Production packs the tracked art/latest with
# PowerShell alone; the DEV build embeds the freshly authored Python pack under the same name (promotion copies it into art/latest).
$arguments += '/reference:System.IO.Compression.dll'
if (-not $Dev -and -not $CompileOnly) {
    $latestPack = Join-Path $PSScriptRoot 'build\Odin.latest.zip'
    & (Join-Path $PSScriptRoot 'tools\local\Pack-LatestArt.ps1') -OutputPath $latestPack
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne $null) { throw 'Latest artwork pack failed.' }
    $arguments += '/resource:' + $latestPack + ',Odin.LatestArt'
}
if ($Dev) {
    $arguments += '/define:ODIN_DEV'
    if (-not $CompileOnly) {
        $python = Join-Path $PSScriptRoot 'local-data\toolchain\python\python.exe'
        & $python (Join-Path $PSScriptRoot 'tools\local\Build-ContactReviewPack.py')
        if ($LASTEXITCODE -ne 0) { throw 'Development review artwork pack failed.' }
        & $python (Join-Path $PSScriptRoot 'tools\local\Build-DevArtworkPack.py')
        if ($LASTEXITCODE -ne 0) { throw 'Development native artwork pack failed.' }
        $arguments += '/resource:' + (Join-Path $PSScriptRoot 'local-data\reference-review\dev-latest-art\dev-artwork.zip') + ',Odin.LatestArt'
    }
}
if (-not $AssetPack -and -not $CompileOnly) {
    $AssetPack = Join-Path $PSScriptRoot 'build\Odin.assets'
    & (Join-Path $PSScriptRoot 'build-assets.ps1') -OutputPath $AssetPack
}
if ($AssetPack) {
    $arguments += '/resource:' + (Resolve-Path -LiteralPath $AssetPack).Path + ',Odin.Assets'
} elseif (-not $CompileOnly) {
    throw 'Supply -AssetPack with a compatible Odin.assets pack, or -CompileOnly for a source compilation check.'
}
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'app\Odin.ico')) { $arguments += '/win32icon:' + (Join-Path $PSScriptRoot 'app\Odin.ico') }
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'app') -Filter '*.cs' | ForEach-Object { $_.FullName })
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler @arguments @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Host ('Executable: ' + (Join-Path $output $executableName))
