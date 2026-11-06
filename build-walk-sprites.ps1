param([Parameter(Mandatory=$true)][string]$ReferencePack)
$ErrorActionPreference = 'Stop'
$spriteBuildFolder = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Path $spriteBuildFolder -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$spriteArguments = @('/nologo','/target:winexe','/platform:x64','/optimize+','/define:ODIN_SPRITE_REVIEW',
    ('/out:' + (Join-Path $spriteBuildFolder 'Odin-Walk-Sprite-Review.exe')),
    ('/resource:' + (Resolve-Path -LiteralPath $ReferencePack).Path + ',Odin.SpriteReview'),
    '/reference:System.dll','/reference:System.Core.dll','/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll','/reference:System.Web.Extensions.dll',
    '/reference:System.IO.Compression.dll',
    (Join-Path $PSScriptRoot 'app\WalkSpriteReview.cs'))
& $compiler @spriteArguments
if ($LASTEXITCODE -ne 0) { throw 'Walking sprite review compilation failed.' }
