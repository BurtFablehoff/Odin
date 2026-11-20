$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
& (Join-Path $PSScriptRoot 'Pack-LatestArt.ps1')
$python=Join-Path $root 'local-data/toolchain/python/python.exe'
& $python (Join-Path $PSScriptRoot 'Inspect-StretchContact.py')
if($LASTEXITCODE -ne 0){throw 'Stretch contact measurement failed.'}
& $python (Join-Path $PSScriptRoot 'Build-StretchReturnPack.py')
if($LASTEXITCODE -ne 0){throw 'Stretch candidate pack failed.'}
& (Join-Path $root 'build.ps1') -Dev -ReviewPack (Join-Path $root 'build/stretch-return-v1/candidate.zip') -DevExecutableName Odin-Dev-StretchReturn-v1.exe
foreach($test in @('test-stretch-return','test-latest-art','test-menus')){
 $p=Start-Process -FilePath (Join-Path $root 'test-exes/Odin-Dev-StretchReturn-v1.exe') -ArgumentList @('--'+$test,(Join-Path $root ('build/stretch-return-v1/'+$test+'.json'))) -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){throw "Failed $test"}
}
Write-Host 'Review Animations > Stretch in Odin-Dev-StretchReturn-v1.exe; live motion pass pending.'
