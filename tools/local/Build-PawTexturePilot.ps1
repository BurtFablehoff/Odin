# A13: build and export a single texture pilot without opening a visible window.
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$source=Join-Path $root 'draft-assets/paw-texture-pilot-v1/raw-edit.png'
$output=Join-Path $root 'draft-assets/paw-texture-pilot-v1'
$exe=Join-Path $root 'test-exes/Odin-PawTexture-Pilot-v1.exe'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:winexe /platform:x64 /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll ('/out:'+$exe) ('/resource:'+(Join-Path $root 'art/frames/sit/000.png')+',Odin.PilotBase') ('/resource:'+$source+',Odin.PilotSource') (Join-Path $PSScriptRoot 'PawTexturePilot.cs')
if($LASTEXITCODE -ne 0){throw 'Paw texture pilot compilation failed.'}
$process=Start-Process -FilePath $exe -ArgumentList @('--export',('"'+$output+'"')) -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw 'Paw texture export failed.'}
Get-Content (Join-Path $output 'checks.json')
