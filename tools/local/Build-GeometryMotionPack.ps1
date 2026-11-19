# A22: whole-pose registration and drawn bridges. Source PNGs stay intact; no morphing.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot);$work=Join-Path $root 'build/geometry-motion-v10';New-Item -ItemType Directory -Force $work|Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe';$author=Join-Path $work 'DrawnAssets.exe'
& $compiler /nologo /target:exe /reference:System.Drawing.dll /reference:System.Web.Extensions.dll ('/out:'+$author) (Join-Path $PSScriptRoot 'DrawnMotionAssets.cs')
if($LASTEXITCODE -ne 0){throw 'Asset author failed.'}
$p=Start-Process -FilePath $author -ArgumentList $root,'draft-assets/geometry-motion-v10' -WindowStyle Hidden -Wait -PassThru;if($p.ExitCode -ne 0){throw 'Asset normalization failed.'}
& (Join-Path $root 'local-data/toolchain/python/python.exe') (Join-Path $PSScriptRoot 'Measure-PoseFeatures.py') (Join-Path $root 'draft-assets/geometry-motion-v10/frames')
if($LASTEXITCODE -ne 0){throw 'Pose landmark measurement failed.'}
$features=Get-Content (Join-Path $root 'draft-assets/geometry-motion-v10/features.json') -Raw|ConvertFrom-Json
$registered=Get-Content (Join-Path $root 'draft-assets/geometry-motion-v10/registration.json') -Raw|ConvertFrom-Json
$oldGeometry=Get-Content (Join-Path $root 'build/drawn-motion-v9/head-geometry.json') -Raw|ConvertFrom-Json
$skulls=Get-Content (Join-Path $work 'skull-similarity.json') -Raw|ConvertFrom-Json
$baseline=Join-Path $root 'build/drawn-motion-v9/candidate.zip';$receipt=Get-Content (Join-Path $root 'planning/transition-review-history/v9-rejected.json') -Raw|ConvertFrom-Json
$pin=$receipt.artifacts|Where-Object {$_.path -eq 'build/drawn-motion-v9/candidate.zip'}
if(!$pin -or (Get-FileHash $baseline -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pin.sha256){throw 'V9 baseline changed.'}
$payloads=@{};$s=[IO.File]::OpenRead($baseline);$z=[IO.Compression.ZipArchive]::new($s,[IO.Compression.ZipArchiveMode]::Read)
try{foreach($e in $z.Entries){$input=$e.Open();$copy=[IO.MemoryStream]::new();try{$input.CopyTo($copy);$payloads[$e.FullName]=$copy.ToArray()}finally{$input.Dispose();$copy.Dispose()}}}finally{$z.Dispose();$s.Dispose()}
$manifest=[Text.Encoding]::UTF8.GetString($payloads['manifest.json'])|ConvertFrom-Json
function Old-Key([string]$clip,[int]$frame){
 $e=$manifest.actions|Where-Object {$_.clip -eq $clip -and $_.frame -eq $frame}|Select-Object -First 1|ConvertTo-Json -Depth 25|ConvertFrom-Json
 $g=@($oldGeometry.$clip)[$frame];$e|Add-Member -NotePropertyName measure -NotePropertyValue ([pscustomobject]@{eye=$g.eye;spanNative=($g.displayEyeSpan/$g.scale)}) -Force;return $e
}
function New-Key([string]$name){
 $r=$registered.$name;$key='geometry-v10/'+$name+'.png';$payloads[$key]=[IO.File]::ReadAllBytes($r.file)
 return [pscustomobject]@{key=$key;label=$name;sha256=(Get-FileHash $r.file -Algorithm SHA256).Hash.ToLowerInvariant();id=0;clip='';frame=0;kind='transition';source='draft-assets/geometry-motion-v10/frames/'+$name+'.png';nativeWidth=1024;nativeHeight=1024;bounds=$r.bounds;anchorX=$r.anchorX;supportY=$r.supportY;renderWidthAtSmall=480;customAnchor=$true;airborne=$false;contactColumns=$r.contactColumns;measure=$features.$name}
}
$specs=@(
 @{name='sit_to_loaf';keys=@((Old-Key 'sit_to_loaf' 0),(Old-Key 'sit_to_loaf' 1),(Old-Key 'sit_to_loaf' 2),(New-Key 'loaf-gap-low'),(New-Key 'loaf-gap-a'),(Old-Key 'sit_to_loaf' 3),(Old-Key 'sit_to_loaf' 4),(Old-Key 'sit_to_loaf' 5),(Old-Key 'sit_to_loaf' 6));delays=@(.03,.05,.06,.06,.06,.06,.07,.08,.03)},
 @{name='sit_to_lay';keys=@((Old-Key 'sit_to_lay' 0),(Old-Key 'sit_to_lay' 1),(Old-Key 'sit_to_lay' 2),(Old-Key 'sit_to_lay' 3),(New-Key 'lay-shift-clean'),(New-Key 'lay-shift-b'),(Old-Key 'sit_to_lay' 4),(Old-Key 'sit_to_lay' 5),(Old-Key 'sit_to_lay' 6));delays=@(.03,.05,.06,.06,.06,.06,.07,.08,.03)},
 @{name='loaf_to_lay';keys=@((Old-Key 'loaf_to_lay' 0),(Old-Key 'loaf_to_lay' 1),(New-Key 'direct-gap-b'),(Old-Key 'loaf_to_lay' 2),(Old-Key 'loaf_to_lay' 3),(Old-Key 'loaf_to_lay' 4));delays=@(.03,.07,.08,.08,.11,.03)}
)
$changed=@('sit_to_loaf','loaf_to_sit','sit_to_lay','lay_to_sit','loaf_to_lay','lay_to_loaf');$actions=@($manifest.actions|Where-Object {$_.clip -notin $changed});$clips=@($manifest.actionClips|Where-Object {$_.clip -notin $changed});$plan=@()
foreach($spec in $specs){
 $keys=@($spec.keys);$old=@($oldGeometry.($spec.name));$s0=$skulls.($keys[0].key).physicalScale;$s1=$skulls.($keys[-1].key).physicalScale
 $axis=if($spec.name -eq 'loaf_to_lay'){0}else{1};$p0=$old[0].displayEye[$axis];$p1=$old[-1].displayEye[$axis];$span=$p1-$p0
 for($i=0;$i -lt $keys.Count;$i++){
  $e=$keys[$i];$m=$e.measure;$target=$null
  if($i -gt 0 -and $i -lt $keys.Count-1){
   # Diagnostic only: forehead feature matches, not yaw-dependent eye spacing.
   $position=($m.eye[$axis]-@($e.anchorX,$e.supportY)[$axis])*$e.renderWidthAtSmall/1024
   $t=[Math]::Max(0.0,[Math]::Min(1.0,($position-$p0)/$span));$target=$s0+($s1-$s0)*$t
   $skullKey=if($e.key.StartsWith('geometry-v10/')){[IO.Path]::GetFileNameWithoutExtension($e.key)}else{$e.key}
   $match=$skulls.$skullKey
   if($match.inliers -ge 8){$e.renderWidthAtSmall*=$target/$match.physicalScale}
   $e.anchorX=$e.bounds[0]+269.0/($e.renderWidthAtSmall/480.0)
   $e|Add-Member -NotePropertyName skullMatchInliers -NotePropertyValue $match.inliers -Force
  }
  $e|Add-Member -NotePropertyName skullScaleTarget -NotePropertyValue $target -Force
  $e.PSObject.Properties.Remove('measure')
 }
 $parts=$spec.name -split '_to_';foreach($reverse in @($false,$true)){
  $name=if($reverse){$parts[1]+'_to_'+$parts[0]}else{$spec.name};$ordered=@($keys);$delays=@($spec.delays);if($reverse){[array]::Reverse($ordered);[array]::Reverse($delays)}
  for($i=0;$i -lt $ordered.Count;$i++){$e=$ordered[$i]|ConvertTo-Json -Depth 25|ConvertFrom-Json;$e.clip=$name;$e.frame=$i;$e.id=0;$actions+=$e}
  $seconds=($delays|Measure-Object -Sum).Sum;$clips+=[pscustomobject]@{clip=$name;frames=$ordered.Count;seconds=$seconds;frameSeconds=$delays;loop=$false};$plan+=[pscustomobject]@{clip=$name;keys=$ordered;frameSeconds=$delays;seconds=$seconds}
 }
}
$manifest.actions=$actions;$manifest.actionClips=$clips;$manifest|Add-Member -NotePropertyName registeredRestV10 -NotePropertyValue $true -Force
$payloads['manifest.json']=[Text.Encoding]::UTF8.GetBytes(($manifest|ConvertTo-Json -Depth 60 -Compress))
$s=[IO.File]::Create((Join-Path $work 'candidate.zip'));$z=[IO.Compression.ZipArchive]::new($s,[IO.Compression.ZipArchiveMode]::Create)
try{foreach($key in ($payloads.Keys|Sort-Object)){$o=$z.CreateEntry($key).Open();try{$b=$payloads[$key];$o.Write($b,0,$b.Length)}finally{$o.Dispose()}}}finally{$z.Dispose();$s.Dispose()}
$plan|ConvertTo-Json -Depth 35|Set-Content (Join-Path $work 'playback.json') -Encoding UTF8
