param([Parameter(Mandatory=$true)][string]$ReferencePack)
$ErrorActionPreference = 'Stop'
$restBuildFolder = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Path $restBuildFolder -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$restArguments = @('/nologo','/target:winexe','/platform:x64','/optimize+','/define:ODIN_REST_REVIEW',
    ('/out:' + (Join-Path $restBuildFolder 'Odin-Sitting-Pose-Review.exe')),
    ('/resource:' + (Resolve-Path -LiteralPath $ReferencePack).Path + ',Odin.RestReview'),
    '/reference:System.dll','/reference:System.Core.dll','/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll','/reference:System.Web.Extensions.dll',
    '/reference:System.IO.Compression.dll',
    (Join-Path $PSScriptRoot 'app\RestPoseReview.cs'))
& $compiler @restArguments
if ($LASTEXITCODE -ne 0) { throw 'Sitting pose review compilation failed.' }
