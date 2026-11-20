$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
& (Join-Path $root 'local-data/toolchain/python/python.exe') (Join-Path $PSScriptRoot 'Build-GroomScalePack.py')
if($LASTEXITCODE -ne 0){throw 'Groom scale candidate failed.'}
& (Join-Path $root 'build.ps1') -Dev -ReviewPack (Join-Path $root 'build/groom-scale-v2/candidate.zip') -DevExecutableName Odin-Dev-GroomReturn-v2.exe
foreach($test in @('test-groom-scale','test-groom-return','test-stretch-return','test-latest-art','test-menus')){
 $arguments=@('--'+$test,(Join-Path $root ('build/groom-scale-v2/'+$test+'.json')))
 if($test -eq 'test-groom-scale'){$arguments+=Join-Path $root 'build/groom-return-v1/candidate.zip'}
 $p=Start-Process -FilePath (Join-Path $root 'test-exes/Odin-Dev-GroomReturn-v2.exe') -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){throw "Failed $test"};Write-Host "Passed $test"
}
