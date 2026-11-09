param([string]$OutputDirectory,[ValidateSet('nearest-cardinal','blend-qa')][string]$DirectionMode='nearest-cardinal')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $root 'local-data\reference-review\registered-families-v1' }
$python = Join-Path $root 'local-data\toolchain\python\python.exe'
& $python (Join-Path $PSScriptRoot 'Build-FamilyRegistration.py') --check
if ($LASTEXITCODE -ne 0) { throw 'Family registration plan is stale or invalid.' }
$tests = Join-Path $root 'test-exes'
New-Item -ItemType Directory -Path $tests -Force | Out-Null
$executable = Join-Path $tests 'Odin-FamilyFrame-Exporter.exe'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe /platform:x64 /optimize+ /define:ODIN_CONTACT_REVIEW ("/out:" + $executable) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'FamilyFrameExporter.cs') (Join-Path $root 'app\BlinkEyeRenderer.cs') (Join-Path $root 'app\PoseVariantRenderer.cs')
if ($LASTEXITCODE -ne 0) { throw 'Headless family exporter compilation failed.' }
& $executable $root (Join-Path $root 'planning\family-registration-v1.json') $OutputDirectory $DirectionMode
if ($LASTEXITCODE -ne 0) { throw 'Registered family export failed.' }
