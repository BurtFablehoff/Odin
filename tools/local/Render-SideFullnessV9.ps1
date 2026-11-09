$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$assembly=Join-Path $root 'build/side-fullness-v9.dll'
& $compiler /nologo /target:library /optimize+ ("/out:"+$assembly) /reference:System.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'Render-SideFullnessV9.cs')
if($LASTEXITCODE -ne 0){throw 'Side body renderer compilation failed'}
Add-Type -Path $assembly
$folder=Join-Path $root 'draft-assets/style-consistency-v9/side'
$registration=Get-Content (Join-Path $folder 'face-registration.json') -Raw | ConvertFrom-Json
$report=[SideFullnessV9]::Render((Join-Path $root 'draft-assets/style-consistency-v8/side/side-attempt1.png'),(Join-Path $folder 'generated-source.png'),(Join-Path $folder 'side-fuller.png'),$registration.scale,$registration.x,$registration.y)
[IO.File]::WriteAllText((Join-Path $folder 'qa.json'),$report)
$report
