param([ValidateSet('CurledTextureSize','RestSizing','QuietBreathing','Sitting','Sleeping')][string]$Review='CurledTextureSize')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$packs = @{
    CurledTextureSize='curled-texture-size-v1'; RestSizing='rest-sizing-v1';
    QuietBreathing='sleep-breathing-v2'; Sitting='sitting-poses-v1'; Sleeping='sleeping-poses-v2'
}
$pack = Join-Path $projectRoot ('local-data\reference-review\' + $packs[$Review] + '\rest-review.zip')
if (-not (Test-Path -LiteralPath $pack)) { throw "Local review data is missing: $pack" }
& (Join-Path $projectRoot 'build-rest-poses.ps1') -ReferencePack $pack
if ($LASTEXITCODE -ne 0) { throw 'Review build failed.' }
$destination = Join-Path $projectRoot ('build\Odin-' + $Review + '-Local.exe')
Copy-Item -LiteralPath (Join-Path $projectRoot 'build\Odin-Sitting-Pose-Review.exe') -Destination $destination -Force
Write-Host "Review app: $destination"
