$ErrorActionPreference='Stop'
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path (Join-Path $PSScriptRoot 'Render-RearSilhouetteV10.cs') -ReferencedAssemblies System.Drawing,System.Web.Extensions
[RearSilhouetteV10]::Render($projectRoot)
