# A23: owner explicitly requests live EXE motion review of the existing V11 pack.
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
& (Join-Path $root 'build.ps1') -Dev -CoherentMotionReview -DevExecutableName Odin-Dev-Motion-v11.exe
$exe=Join-Path $root 'test-exes/Odin-Dev-Motion-v11.exe'
$work=Join-Path $root 'build/coherent-motion-v11'
$failed=@()
foreach($test in @('test-transitions','test-tail-flip','test-menus','self-test','test-latest-art','test-body-stability','test-perch-zoomies')) {
 $report=Join-Path $work ($test+'.json');$arguments=@('--'+$test,$report)
 if($test -eq 'test-transitions'){$arguments+=Join-Path $work 'review'}
 if($test -eq 'test-tail-flip'){$arguments+=Join-Path $work 'tail-review'}
 if($test -eq 'self-test'){$arguments+='--skip-registry-test'}
 $p=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){$failed+=$test;Write-Warning "Failed $test; see $report"}else{Write-Host "Passed $test"}
}
Write-Host "Owner motion review pending: $exe"
if($failed.Count){throw ('Checks failed (EXE retained for requested motion review): '+($failed -join ', '))}
