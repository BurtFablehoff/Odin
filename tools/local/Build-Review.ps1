param([ValidateSet('RestSizing','Sitting','Sleeping')][string]$Review='RestSizing')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$packs = @{
    RestSizing='rest-sizing-v1';
    Sitting='sitting-poses-v1'; Sleeping='sleeping-poses-v2'
}
$pack = Join-Path $projectRoot ('local-data\reference-review\' + $packs[$Review] + '\rest-review.zip')
if (-not (Test-Path -LiteralPath $pack)) { throw "Local review data is missing: $pack" }
$activePack = Join-Path $projectRoot ('local-data\reference-review\active-' + $packs[$Review] + '\rest-review.zip')
& (Join-Path $projectRoot 'local-data\toolchain\python\python.exe') (Join-Path $PSScriptRoot 'Prepare-ActiveReviewPack.py') $pack $activePack
if ($LASTEXITCODE -ne 0) { throw 'Could not prepare active review pack.' }
& (Join-Path $projectRoot 'build-rest-poses.ps1') -ReferencePack $activePack
if ($LASTEXITCODE -ne 0) { throw 'Review build failed.' }
$destination = Join-Path $projectRoot ('test-exes\Odin-' + $Review + '-Local.exe')
Copy-Item -LiteralPath (Join-Path $projectRoot 'test-exes\Odin-Sitting-Pose-Review.exe') -Destination $destination -Force
Write-Host "Review app: $destination"
