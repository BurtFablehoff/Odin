param([switch]$SkipRegistryTest,[string]$ApprovedPack)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
& (Join-Path $root 'build.ps1')
$exe=Join-Path $root 'build/Odin.exe';$work=Join-Path $root 'build/release-3.3.1'
New-Item -ItemType Directory -Force $work|Out-Null
foreach($test in @('self-test','check-assets','test-behavior','test-static-rest','test-support-anchor','test-screen-edges','test-run-motion','test-jump-tilt','test-body-stability','test-latest-art','test-updater','test-menus')) {
 $arguments=@('--'+$test,(Join-Path $work ($test+'.json')))
 if($test -eq 'self-test' -and $SkipRegistryTest){$arguments+='--skip-registry-test'}
 $p=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){throw "Failed $test"};Write-Host "Passed $test"
}
if($ApprovedPack){
 $p=Start-Process -FilePath $exe -ArgumentList @('--test-approved-actions',(Join-Path $work 'approved-actions.json'),(Resolve-Path -LiteralPath $ApprovedPack).Path) -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){throw 'Approved motion comparison failed.'}
}
if($SkipRegistryTest){Write-Host 'Windows startup registry test explicitly skipped.'}
