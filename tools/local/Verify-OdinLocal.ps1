param(
    [string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [switch]$SkipRegistryTest
)
$ErrorActionPreference = 'Stop'
& (Join-Path $ProjectRoot 'build.ps1') -Dev
if ($LASTEXITCODE -ne 0) { throw 'Runtime build failed.' }
$runtime = Join-Path $ProjectRoot 'test-exes\Odin-Dev-Local.exe'
foreach ($test in @('self-test','test-static-rest','test-support-anchor','test-screen-edges','test-dev-art','test-body-stability','test-follow-mouse','test-jump-tilt','test-run-motion','test-perch-zoomies','test-menus')) {
    $report = Join-Path $ProjectRoot ('build\local-' + $test + '.json')
    $testArguments = @('--' + $test, ('"' + $report + '"'))
    if ($test -eq 'self-test' -and $SkipRegistryTest) { $testArguments += '--skip-registry-test' }
    $process = Start-Process -FilePath $runtime -ArgumentList $testArguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Runtime check failed: $test. See $report" }
}
# Part 5c: the production build draws the promoted pack (art/latest); run its headless checks too.
& (Join-Path $ProjectRoot 'build.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Production build failed.' }
$production = Join-Path $ProjectRoot 'build\Odin.exe'
foreach ($test in @('self-test','check-assets','test-behavior','test-static-rest','test-support-anchor','test-screen-edges','test-run-motion','test-jump-tilt','test-body-stability','test-latest-art','test-updater','test-menus')) {
    $report = Join-Path $ProjectRoot ('build\production-' + $test + '.json')
    $testArguments = @('--' + $test, ('"' + $report + '"'))
    if ($test -eq 'self-test' -and $SkipRegistryTest) { $testArguments += '--skip-registry-test' }
    $process = Start-Process -FilePath $production -ArgumentList $testArguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Production check failed: $test. See $report" }
}
& (Join-Path $PSScriptRoot 'Build-ContactReview.ps1')
$review = Join-Path $ProjectRoot 'test-exes\Odin-ApprovedContact-Local.exe'
$reviewReport = Join-Path $ProjectRoot 'build\local-review-self-test.json'
$process = Start-Process -FilePath $review -ArgumentList @('--self-test',('"' + $reviewReport + '"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Review check failed. See $reviewReport" }
Write-Host 'Verified: runtime build, runtime checks, static resting, screen edges, body stability, production build and checks, and current artwork review.'
if ($SkipRegistryTest) { Write-Warning 'Windows startup registry test was explicitly skipped. Run without -SkipRegistryTest from your normal Windows PowerShell to cover it.' }
