param([string]$AssetPack, [switch]$CompileOnly, [switch]$Dev)
$ErrorActionPreference = 'Stop'
$output = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$arguments = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+', '/unsafe',
    ('/out:' + (Join-Path $output 'Odin.exe')),
    ('/win32manifest:' + (Join-Path $PSScriptRoot 'app\app.manifest')),
    '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll', '/reference:System.Web.Extensions.dll')
if ($Dev) { $arguments += '/define:ODIN_DEV' }
if (-not $AssetPack -and -not $CompileOnly) {
    $AssetPack = Join-Path $output 'Odin.assets'
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
