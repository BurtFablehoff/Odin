param(
    [string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [switch]$SkipRegistryTest
)
$ErrorActionPreference = 'Stop'
& (Join-Path $ProjectRoot 'build.ps1') -Dev
if ($LASTEXITCODE -ne 0) { throw 'Runtime build failed.' }
$runtime = Join-Path $ProjectRoot 'test-exes\Odin-Dev-Local.exe'
foreach ($test in @('self-test','test-static-rest','test-support-anchor','test-screen-edges','test-dev-art')) {
    $report = Join-Path $ProjectRoot ('build\local-' + $test + '.json')
    $testArguments = @('--' + $test, ('"' + $report + '"'))
    if ($test -eq 'self-test' -and $SkipRegistryTest) { $testArguments += '--skip-registry-test' }
    $process = Start-Process -FilePath $runtime -ArgumentList $testArguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Runtime check failed: $test. See $report" }
}
& (Join-Path $PSScriptRoot 'Build-ContactReview.ps1')
$review = Join-Path $ProjectRoot 'test-exes\Odin-ApprovedContact-Local.exe'
$reviewReport = Join-Path $ProjectRoot 'build\local-review-self-test.json'
$process = Start-Process -FilePath $review -ArgumentList @('--self-test',('"' + $reviewReport + '"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Review check failed. See $reviewReport" }
Write-Host 'Verified: runtime build, runtime checks, static resting, screen edges, and current artwork review.'
if ($SkipRegistryTest) { Write-Warning 'Windows startup registry test was explicitly skipped. Run without -SkipRegistryTest from your normal Windows PowerShell to cover it.' }
