# A16/P2/P5: complete candidate and headless motion review, no install or promotion.
param([switch]$ExportOnly,[switch]$Drawn,[switch]$Tip=$true,[ValidateSet(6,7)][int]$TipRevision=7)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if($Drawn){$Tip=$false}
$version=if($Tip){'tail-v'+$TipRevision}elseif($Drawn){'tail-v5'}else{'tail-v4'}
$name=if($Tip){'Odin-Dev-Tail-v'+$TipRevision+'.exe'}elseif($Drawn){'Odin-Dev-Tail-v5.exe'}else{'Odin-Dev-Tail-v4.exe'}
$review=Join-Path $root ('build/'+$version+'/review')
if(!$ExportOnly){
 if($Tip){& (Join-Path $root 'build.ps1') -Dev -TailTipReview -TailTipRevision $TipRevision -DevExecutableName $name}
 elseif($Drawn){& (Join-Path $root 'build.ps1') -Dev -DrawnTailReview -DevExecutableName $name}
 else{& (Join-Path $root 'build.ps1') -Dev -TailReview -DevExecutableName $name}
 $auditArguments=@((Join-Path $PSScriptRoot 'Audit-TailPack.py'));if($Tip){$auditArguments+='--tip';$auditArguments+=$TipRevision.ToString()}elseif($Drawn){$auditArguments+='--drawn'}
 & (Join-Path $root 'local-data/toolchain/python/python.exe') @auditArguments
 if($LASTEXITCODE -ne 0){throw 'Tail pack preservation audit failed.'}
 foreach($test in @('test-tail-flip','test-transitions','test-menus','self-test','test-latest-art','test-body-stability','test-perch-zoomies')){
  $report=Join-Path $root ('build/'+$version+'/'+$test+'.json')
  $arguments=@('--'+$test,$report)
  if($test -eq 'test-tail-flip'){$arguments+=$review}
  if($test -eq 'test-transitions'){$arguments+=Join-Path $root ('build/'+$version+'/transitions')}
  if($test -eq 'self-test'){$arguments+='--skip-registry-test'}
  $p=Start-Process -FilePath (Join-Path $root ('test-exes/'+$name)) -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
  if($p.ExitCode -ne 0){throw "Failed $test; see $report"}
  Write-Host "Passed $test"
 }
}
foreach($folder in Get-ChildItem $review -Directory){
 $delays=[int[]]((Get-Content (Join-Path $folder.FullName 'delays.txt') -Raw).Trim() -split ',')
 & (Join-Path $root 'tools/SharedWrite-AnimatedGif.ps1') -FramesDirectory $folder.FullName -OutputPath ($folder.FullName+'.gif') -Delays $delays
}
foreach($background in @('dark','light')){
 $cycle=Join-Path $root ('build/'+$version+'/cycle-'+$background);New-Item -ItemType Directory -Force $cycle | Out-Null
 $delays=[Collections.Generic.List[int]]::new();$index=0
 foreach($family in @('sit','lay','sleep')){
  $source=Join-Path $review ($family+'-'+$background)
  $timing=[int[]]((Get-Content (Join-Path $source 'delays.txt') -Raw).Trim() -split ',')
  for($frame=0;$frame -lt $timing.Length;$frame++){
   Copy-Item -LiteralPath (Join-Path $source ($frame.ToString('00')+'.png')) -Destination (Join-Path $cycle ($index.ToString('00')+'.png'))
   $delays.Add($timing[$frame]);$index++
  }
 }
 & (Join-Path $root 'tools/SharedWrite-AnimatedGif.ps1') -FramesDirectory $cycle -OutputPath ($cycle+'.gif') -Delays $delays.ToArray()
}
