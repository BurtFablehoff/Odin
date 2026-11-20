$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
& (Join-Path $root 'local-data/toolchain/python/python.exe') (Join-Path $PSScriptRoot 'Build-GroomReturnPack.py')
if($LASTEXITCODE -ne 0){throw 'Grooming candidate pack failed.'}
& (Join-Path $root 'build.ps1') -Dev -ReviewPack (Join-Path $root 'build/groom-return-v1/candidate.zip') -DevExecutableName Odin-Dev-GroomReturn-v1.exe
foreach($test in @('test-groom-return','test-stretch-return','test-latest-art','test-menus')){
 $p=Start-Process -FilePath (Join-Path $root 'test-exes/Odin-Dev-GroomReturn-v1.exe') -ArgumentList @('--'+$test,(Join-Path $root ('build/groom-return-v1/'+$test+'.json'))) -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){throw "Failed $test"};Write-Host "Passed $test"
}
Write-Host 'Review Animations > Grooming, including the full return; retained Stretch is also available.'
