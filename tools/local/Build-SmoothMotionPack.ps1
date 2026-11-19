param([switch]$PrepareOnly,[switch]$SkipAuthor)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$work=Join-Path $root 'build/motion-v8';New-Item -ItemType Directory -Force $work | Out-Null
$source=Join-Path $root 'build/tail-v7/candidate.zip'
$approval=Get-Content (Join-Path $root 'planning/current-tail-review.json') -Raw | ConvertFrom-Json
$pin=$approval.artifacts | Where-Object {$_.path -eq 'build/tail-v7/candidate.zip'}
if(!$pin -or (Get-FileHash $source -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pin.sha256){throw 'V7 baseline differs from its review record.'}
$payloads=@{};$stream=[IO.File]::OpenRead($source);$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read)
try{foreach($e in $zip.Entries){$input=$e.Open();$copy=[IO.MemoryStream]::new();try{$input.CopyTo($copy);$payloads[$e.FullName]=$copy.ToArray()}finally{$input.Dispose();$copy.Dispose()}}}finally{$zip.Dispose();$stream.Dispose()}
$manifest=[Text.Encoding]::UTF8.GetString($payloads['manifest.json']) | ConvertFrom-Json
New-Item -ItemType Directory -Force (Join-Path $work 'input') | Out-Null
function Key($entry){
 $file=Join-Path $work ('input/'+$entry.key.Replace('/','_'));[IO.File]::WriteAllBytes($file,$payloads[$entry.key])
 return [pscustomobject]@{file=$file;key=$entry.key;anchorX=$entry.anchorX;supportY=$entry.supportY;renderWidthAtSmall=$entry.renderWidthAtSmall}
}
function Entry([string]$clip,[int]$frame){$manifest.actions | Where-Object {$_.clip -eq $clip -and $_.frame -eq $frame} | Select-Object -First 1}
$sequences=@()
foreach($spec in @(@{name='sit_to_loaf';order=@(0,1,2,3,4);seconds=1.4},@{name='sit_to_lay';order=@(0,1,3,4);seconds=1.6})){
 $keys=@();foreach($f in $spec.order){$keys+=Key (Entry $spec.name $f)}
 $sequences+=[pscustomobject]@{name=$spec.name;keys=$keys;seconds=$spec.seconds}
}
$direct=@((Key (Entry 'loaf' 0)))
foreach($name in @('early')){
 $raw=Join-Path $root ('draft-assets/motion-v8/raw/loaf-lay-'+$name+'.png')
 if(Test-Path $raw){$direct+=[pscustomobject]@{file=$raw;key='loaf-lay-'+$name;anchorX=512;supportY=944;renderWidthAtSmall=480}}
}
$direct+=Key (Entry 'lay' 0)
$sequences+=[pscustomobject]@{name='loaf_to_lay';keys=$direct;seconds=1.2}
@{sequences=$sequences} | ConvertTo-Json -Depth 15 | Set-Content (Join-Path $work 'keys.json') -Encoding UTF8
$author=Join-Path $work 'AuthorMotion.exe';$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:exe /reference:System.Drawing.dll /reference:System.Web.Extensions.dll ('/out:'+$author) (Join-Path $PSScriptRoot 'TransitionMotionFrames.cs')
if($LASTEXITCODE -ne 0){throw 'Motion author compilation failed.'}
if(!$SkipAuthor){
 $p=Start-Process -FilePath $author -ArgumentList $work,'prepare' -WindowStyle Hidden -Wait -PassThru;if($p.ExitCode -ne 0){throw 'Prepare motion failed.'}
 if($PrepareOnly){return}
 if($direct.Count -ne 3){throw 'Missing direct loaf/lay drawn key.'}
 & (Join-Path $root 'local-data/toolchain/python/python.exe') (Join-Path $PSScriptRoot 'Measure-TransitionEyes.py') $work
 if($LASTEXITCODE -ne 0){throw 'Iris landmark measurement failed.'}
 & (Join-Path $root 'local-data/toolchain/python/python.exe') (Join-Path $PSScriptRoot 'Estimate-TransitionFlow.py') $work
 if($LASTEXITCODE -ne 0){throw 'Motion estimation failed.'}
 $p=Start-Process -FilePath $author -ArgumentList $work,'render' -WindowStyle Hidden -Wait -PassThru;if($p.ExitCode -ne 0){throw 'Render motion failed.'}
}
$authored=Get-Content (Join-Path $work 'frames.json') -Raw | ConvertFrom-Json
$changed=@('sit_to_loaf','loaf_to_sit','sit_to_lay','lay_to_sit','loaf_to_lay','lay_to_loaf')
$actions=@($manifest.actions | Where-Object {$_.clip -notin $changed});$clips=@($manifest.actionClips | Where-Object {$_.clip -notin $changed})
foreach($seq in $sequences){
 $parts=$seq.name -split '_to_';$new=@();$rows=@($authored.($seq.name))
 for($i=0;$i -lt $rows.Count;$i++){
  if($i -eq 0){$entry=Entry $parts[0] 0 | ConvertTo-Json -Depth 20 | ConvertFrom-Json}
  elseif($i -eq $rows.Count-1){$entry=Entry $parts[1] 0 | ConvertTo-Json -Depth 20 | ConvertFrom-Json}
  else{
   $row=$rows[$i];$key='motion-v8/'+$seq.name+'-'+$i.ToString('000')+'.png';$payloads[$key]=[IO.File]::ReadAllBytes($row.file)
   $entry=[pscustomobject]@{key=$key;label=$seq.name;sha256=(Get-FileHash $row.file -Algorithm SHA256).Hash.ToLowerInvariant();id=0;clip='';frame=0;kind='transition';source='build/motion-v8/frames/'+[IO.Path]::GetFileName($row.file);nativeWidth=1024;nativeHeight=1024;bounds=$row.bounds;supportY=$row.supportY;anchorX=$row.anchorX;customAnchor=$true;airborne=$false;renderWidthAtSmall=480;contactColumns=$row.contactColumns}
  }
  $new+=$entry
 }
 foreach($reverse in @($false,$true)){
  $clip=if($reverse){$parts[1]+'_to_'+$parts[0]}else{$seq.name};$ordered=@($new);if($reverse){[array]::Reverse($ordered)}
  for($i=0;$i -lt $ordered.Count;$i++){$entry=$ordered[$i] | ConvertTo-Json -Depth 20 | ConvertFrom-Json;$entry.clip=$clip;$entry.frame=$i;$entry.id=0;$actions+=$entry}
  $clips+=[pscustomobject]@{clip=$clip;frames=$new.Count;seconds=$seq.seconds;loop=$false}
 }
}
$manifest.actions=$actions;$manifest.actionClips=$clips;$manifest | Add-Member -NotePropertyName smoothRestV8 -NotePropertyValue $true -Force
$payloads['manifest.json']=[Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 60 -Compress))
$stream=[IO.File]::Create((Join-Path $work 'candidate.zip'));$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
try{foreach($key in ($payloads.Keys | Sort-Object)){$s=$zip.CreateEntry($key).Open();try{$bytes=$payloads[$key];$s.Write($bytes,0,$bytes.Length)}finally{$s.Dispose()}}}finally{$zip.Dispose();$stream.Dispose()}
