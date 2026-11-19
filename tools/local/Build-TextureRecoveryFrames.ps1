$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$work=Join-Path $root 'build/transitions-v3';New-Item -ItemType Directory -Force $work | Out-Null
$exe=Join-Path $work 'AuthorTextureFrames.exe'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:exe /platform:x64 /reference:System.Drawing.dll /reference:System.Web.Extensions.dll ('/out:'+$exe) (Join-Path $PSScriptRoot 'TextureRecoveryFrames.cs')
if($LASTEXITCODE -ne 0){throw 'Texture key author compilation failed.'}
$process=Start-Process -FilePath $exe -ArgumentList ('"'+$root+'"') -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw 'Texture key export failed. See draft-assets/texture-recovery-v3/authoring-error.txt'}
