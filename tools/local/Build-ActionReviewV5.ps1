$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
& (Join-Path $root 'local-data/toolchain/python/python.exe') (Join-Path $PSScriptRoot 'Build-StretchTailV5Pack.py')
if($LASTEXITCODE -ne 0){throw 'V5 candidate pack failed.'}
& (Join-Path $root 'build.ps1') -Dev -ReviewPack (Join-Path $root 'build/stretch-tail-v5/candidate.zip') -DevExecutableName Odin-Dev-ActionReview-v5.exe
foreach($test in @('test-stretch-tail','test-stretch-return','test-short-groom','test-groom-return','test-rest-borders','test-latest-art','test-menus')){
 $p=Start-Process -FilePath (Join-Path $root 'test-exes/Odin-Dev-ActionReview-v5.exe') -ArgumentList @('--'+$test,(Join-Path $root ('build/stretch-tail-v5/'+$test+'.json'))) -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){throw "Failed $test"};Write-Host "Passed $test"
}
