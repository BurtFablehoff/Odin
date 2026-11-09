$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$destination = Join-Path $root 'test-exes\Odin-Sleep-Belly-Protection-QA.exe'
$report = Join-Path $root 'build\qa-sleep-belly-protection-v3.json'
New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $report) -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe /platform:x64 /optimize+ /define:ODIN_CONTACT_REVIEW ("/out:" + $destination) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'QA-SleepBellyProtection.cs') (Join-Path $root 'app\PoseVariantRenderer.cs')
if ($LASTEXITCODE -ne 0) { throw 'Sleep belly protection QA compilation failed.' }
& $destination $root $report
if ($LASTEXITCODE -ne 0) { throw 'Sleep belly protection QA failed.' }
