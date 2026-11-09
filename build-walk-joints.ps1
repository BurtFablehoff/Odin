param([Parameter(Mandatory=$true)][string]$ReferencePack)
$ErrorActionPreference = 'Stop'
$jointBuildFolder = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Path $jointBuildFolder -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$jointArguments = @('/nologo','/target:winexe','/platform:x64','/optimize+','/define:ODIN_JOINT_REVIEW',
    ('/out:' + (Join-Path $jointBuildFolder 'Odin-Walk-Joint-Review.exe')),
    ('/resource:' + (Resolve-Path -LiteralPath $ReferencePack).Path + ',Odin.WalkReview'),
    '/reference:System.dll','/reference:System.Core.dll','/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll','/reference:System.Web.Extensions.dll',
    '/reference:System.IO.Compression.dll',
    (Join-Path $PSScriptRoot 'app\WalkJointReview.cs'))
& $compiler @jointArguments
if ($LASTEXITCODE -ne 0) { throw 'Walking joint review compilation failed.' }
