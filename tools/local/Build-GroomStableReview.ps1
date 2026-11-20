$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
& (Join-Path $root 'local-data/toolchain/python/python.exe') (Join-Path $PSScriptRoot 'Build-GroomStableLoopPack.py')
if($LASTEXITCODE -ne 0){throw 'Stable groom candidate failed.'}
& (Join-Path $root 'build.ps1') -Dev -ReviewPack (Join-Path $root 'build/groom-stable-v3/candidate.zip') -DevExecutableName Odin-Dev-GroomReturn-v3.exe
foreach($test in @('test-groom-return','test-stretch-return','test-latest-art','test-menus')){
 $p=Start-Process -FilePath (Join-Path $root 'test-exes/Odin-Dev-GroomReturn-v3.exe') -ArgumentList @('--'+$test,(Join-Path $root ('build/groom-stable-v3/'+$test+'.json'))) -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){throw "Failed $test"};Write-Host "Passed $test"
}
