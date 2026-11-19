# REJECTED V8 (A21). Retained as provenance; use Build-DrawnMotionReview.ps1.
param([switch]$ExportOnly,[switch]$Reauthor)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$work=Join-Path $root 'build/motion-v8';$review=Join-Path $work 'review'
if(!$ExportOnly){
 if($Reauthor -or !(Test-Path (Join-Path $work 'frames.json'))){& (Join-Path $PSScriptRoot 'Build-SmoothMotionPack.ps1')}
 & (Join-Path $root 'build.ps1') -Dev -MotionReview -DevExecutableName Odin-Dev-Motion-v8.exe
 & (Join-Path $root 'local-data/toolchain/python/python.exe') (Join-Path $PSScriptRoot 'Audit-SmoothMotion.py')
 if($LASTEXITCODE -ne 0){throw 'Motion/preservation audit failed.'}
 foreach($test in @('test-transitions','test-tail-flip','test-menus','self-test','test-latest-art','test-body-stability','test-perch-zoomies')){
  $report=Join-Path $work ($test+'.json');$arguments=@('--'+$test,$report)
  if($test -eq 'test-transitions'){$arguments+=$review}
  if($test -eq 'test-tail-flip'){$arguments+=Join-Path $work 'tail-review'}
  if($test -eq 'self-test'){$arguments+='--skip-registry-test'}
  $p=Start-Process -FilePath (Join-Path $root 'test-exes/Odin-Dev-Motion-v8.exe') -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
  if($p.ExitCode -ne 0){throw "Failed $test; see $report"};Write-Host "Passed $test"
 }
}
$exporter=Join-Path $root 'tools/SharedWrite-AnimatedGif.ps1'
foreach($folder in Get-ChildItem $review -Directory){
 $delays=[int[]]((Get-Content (Join-Path $folder.FullName 'delays.txt') -Raw).Trim() -split ',')
 & $exporter -FramesDirectory $folder.FullName -OutputPath ($folder.FullName+'.gif') -Delays $delays
}
$frames=Get-Content (Join-Path $work 'frames.json') -Raw | ConvertFrom-Json
foreach($background in @('dark','light')){
 $destination=Join-Path $work ('cycle-'+$background);New-Item -ItemType Directory -Force $destination | Out-Null
 $delays=[Collections.Generic.List[int]]::new();$index=0
 $parts=@(@{clip='sit_to_loaf';first=0;count=@($frames.sit_to_loaf).Count},@{clip='loaf_to_lay';first=0;count=@($frames.loaf_to_lay).Count},@{clip='sit_to_lay';first=(@($frames.sit_to_lay).Count+1);count=@($frames.sit_to_lay).Count})
 foreach($part in $parts){
  $source=Join-Path $review ($part.clip+'-'+$background);$timing=[int[]]((Get-Content (Join-Path $source 'delays.txt') -Raw).Trim() -split ',')
  for($i=0;$i -lt $part.count;$i++){
   $frame=$part.first+$i;Copy-Item -LiteralPath (Join-Path $source ($frame.ToString('00')+'.png')) -Destination (Join-Path $destination ($index.ToString('00')+'.png'))
   $delays.Add($timing[$frame]);$index++
  }
  $delays[$delays.Count-1]=55
 }
 & $exporter -FramesDirectory $destination -OutputPath ($destination+'.gif') -Delays $delays.ToArray()
}
