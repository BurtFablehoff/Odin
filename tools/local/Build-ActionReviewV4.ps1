$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
& (Join-Path $root 'local-data/toolchain/python/python.exe') (Join-Path $PSScriptRoot 'Build-ActionReviewV4Pack.py')
if($LASTEXITCODE -ne 0){throw 'V4 candidate pack failed.'}
& (Join-Path $root 'build.ps1') -Dev -ReviewPack (Join-Path $root 'build/action-review-v4/candidate.zip') -DevExecutableName Odin-Dev-ActionReview-v4.exe
foreach($test in @('test-short-groom','test-rest-borders','test-groom-return','test-stretch-return','test-latest-art','test-menus','self-test')){
 $arguments=@('--'+$test,(Join-Path $root ('build/action-review-v4/'+$test+'.json')))
 if($test -eq 'self-test'){$arguments+='--skip-registry-test'}
 $p=Start-Process -FilePath (Join-Path $root 'test-exes/Odin-Dev-ActionReview-v4.exe') -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){throw "Failed $test"};Write-Host "Passed $test"
}
