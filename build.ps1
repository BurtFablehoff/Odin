param([string]$AssetPack, [switch]$CompileOnly)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$arguments = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+', '/unsafe',
    ('/out:' + (Join-Path $output 'Odin.exe')),
    ('/win32manifest:' + (Join-Path $PSScriptRoot 'app\app.manifest')),
    '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll', '/reference:System.Web.Extensions.dll')
if ($AssetPack) {
    $resolvedAssets = (Resolve-Path -LiteralPath $AssetPack).Path
    $arguments += '/resource:' + $resolvedAssets + ',Odin.Assets'
} elseif (-not $CompileOnly) {
    throw 'Artwork is not included. Supply -AssetPack with your own compatible pack, or use -CompileOnly to check the source.'
}
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'app') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compiler @arguments @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output (Join-Path $output 'Odin.exe')
