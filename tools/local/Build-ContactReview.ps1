param([string]$ExecutableName='Odin-ApprovedContact-Local.exe')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([IO.Path]::GetFileName($ExecutableName) -ne $ExecutableName -or -not $ExecutableName.EndsWith('.exe')) { throw 'ExecutableName must be an EXE filename without a directory.' }
& (Join-Path $root 'local-data\toolchain\python\python.exe') (Join-Path $PSScriptRoot 'Build-ContactReviewPack.py')
if ($LASTEXITCODE -ne 0) { throw 'Contact review pack failed.' }
New-Item -ItemType Directory -Path (Join-Path $root 'test-exes') -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$arguments = @('/nologo','/target:winexe','/platform:x64','/optimize+','/unsafe','/define:ODIN_CONTACT_REVIEW',
    ('/out:' + (Join-Path (Join-Path $root 'test-exes') $ExecutableName)),
    ('/resource:' + (Join-Path $root 'local-data\reference-review\approved-contact-v1\contact-review.zip') + ',Odin.ContactReview'),
    '/reference:System.dll','/reference:System.Core.dll','/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll','/reference:System.Web.Extensions.dll','/reference:System.IO.Compression.dll',
    (Join-Path $root 'app\ContactReview.cs'),(Join-Path $root 'app\SharedSpriteSupport.cs'),
    (Join-Path $root 'app\SpriteSupportTest.cs'),(Join-Path $root 'app\BlinkEyeRenderer.cs'),
    (Join-Path $root 'app\PoseVariantRenderer.cs'))
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Contact review compilation failed.' }
Write-Host 'Built approved-contact review with native sprites and support anchors.'
