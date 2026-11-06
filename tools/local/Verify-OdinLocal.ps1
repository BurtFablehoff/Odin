param([string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
$ErrorActionPreference = 'Stop'
& (Join-Path $ProjectRoot 'build.ps1') -Dev
if ($LASTEXITCODE -ne 0) { throw 'Runtime build failed.' }
$runtime = Join-Path $ProjectRoot 'build\Odin.exe'
foreach ($test in @('self-test','test-quiet-rest','test-screen-edges')) {
    $report = Join-Path $ProjectRoot ('build\local-' + $test + '.json')
    $process = Start-Process -FilePath $runtime -ArgumentList @('--' + $test, ('"' + $report + '"')) -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Runtime check failed: $test. See $report" }
}
& (Join-Path $PSScriptRoot 'Build-Review.ps1') -Review CurledTextureSize
$review = Join-Path $ProjectRoot 'build\Odin-CurledTextureSize-Local.exe'
$reviewReport = Join-Path $ProjectRoot 'build\local-review-self-test.json'
$process = Start-Process -FilePath $review -ArgumentList @('--self-test',('"' + $reviewReport + '"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Review check failed. See $reviewReport" }
Write-Host 'Verified: runtime build, full self-test, quiet behavior, screen edges, and current artwork review.'
