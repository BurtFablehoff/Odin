$ErrorActionPreference='Stop'
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path (Join-Path $PSScriptRoot 'Render-RearBodyV9.cs') -ReferencedAssemblies System.Drawing,System.Web.Extensions
[RearBodyV9]::Render($projectRoot)
Write-Output 'Rendered local rear-body candidate; head, tail and contact unchanged; no EXE or window created.'
