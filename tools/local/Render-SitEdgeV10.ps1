$ErrorActionPreference='Stop'
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path (Join-Path $PSScriptRoot 'Render-SitEdgeV10.cs') -ReferencedAssemblies System.Drawing,System.Web.Extensions
[SitEdgeV10]::Render($projectRoot)
Write-Output 'Rendered local sitting right-edge candidate; all other original pixels retained. No EXE or window created.'
