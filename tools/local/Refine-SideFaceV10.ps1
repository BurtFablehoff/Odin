$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$assembly=Join-Path $root 'build/side-face-v10.dll'
& $compiler /nologo /target:library /optimize+ ("/out:"+$assembly) /reference:System.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'Refine-SideFaceV10.cs')
if($LASTEXITCODE -ne 0){throw 'Profile refinement compile failed'}
Add-Type -Path $assembly
$folder=Join-Path $root 'draft-assets/consistency-v10/side'
$report=[SideFaceV10]::Render((Join-Path $root 'draft-assets/style-consistency-v9/side/side-fuller-persian.png'),(Join-Path $root 'art/frames/jump_right/013.png'),(Join-Path $folder 'side.png'))
[IO.File]::WriteAllText((Join-Path $folder 'qa-profile.json'),$report)
$report
