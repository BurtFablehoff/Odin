# A21: discrete whole-pose drawings only. No V8 flow/tween authoring is invoked.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$work=Join-Path $root 'build/drawn-motion-v9';New-Item -ItemType Directory -Force $work | Out-Null
$source=Join-Path $root 'build/tail-v7/candidate.zip';$receipt=Get-Content (Join-Path $root 'planning/current-tail-review.json') -Raw|ConvertFrom-Json
$pin=$receipt.artifacts|Where-Object {$_.path -eq 'build/tail-v7/candidate.zip'}
if(!$pin -or (Get-FileHash $source -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pin.sha256){throw 'V7 baseline changed.'}
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe';$author=Join-Path $work 'DrawnAssets.exe'
& $compiler /nologo /target:exe /reference:System.Drawing.dll /reference:System.Web.Extensions.dll ('/out:'+$author) (Join-Path $PSScriptRoot 'DrawnMotionAssets.cs')
if($LASTEXITCODE -ne 0){throw 'Drawn asset author compilation failed.'}
$p=Start-Process -FilePath $author -ArgumentList $root -WindowStyle Hidden -Wait -PassThru;if($p.ExitCode -ne 0){throw 'Drawn asset registration failed.'}
$registered=Get-Content (Join-Path $root 'draft-assets/drawn-motion-v9/registration.json') -Raw|ConvertFrom-Json
$payloads=@{};$stream=[IO.File]::OpenRead($source);$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read)
try{foreach($e in $zip.Entries){$input=$e.Open();$copy=[IO.MemoryStream]::new();try{$input.CopyTo($copy);$payloads[$e.FullName]=$copy.ToArray()}finally{$input.Dispose();$copy.Dispose()}}}finally{$zip.Dispose();$stream.Dispose()}
$manifest=[Text.Encoding]::UTF8.GetString($payloads['manifest.json'])|ConvertFrom-Json
function Base([string]$clip,[int]$frame){$manifest.actions|Where-Object {$_.clip -eq $clip -and $_.frame -eq $frame}|Select-Object -First 1}
function New-Key([string]$name){
 $r=$registered.$name;$key='drawn-v9/'+$name+'.png';$payloads[$key]=[IO.File]::ReadAllBytes($r.file)
 return [pscustomobject]@{key=$key;label=$name;sha256=(Get-FileHash $r.file -Algorithm SHA256).Hash.ToLowerInvariant();id=0;clip='';frame=0;kind='transition';source='draft-assets/drawn-motion-v9/frames/'+$name+'.png';nativeWidth=1024;nativeHeight=1024;bounds=$r.bounds;anchorX=$r.anchorX;supportY=$r.supportY;renderWidthAtSmall=480;customAnchor=$true;airborne=$false;contactColumns=$r.contactColumns}
}
$specs=@(
 @{name='sit_to_loaf';keys=@((Base 'sit' 0),(Base 'sit_to_loaf' 1),(Base 'sit_to_loaf' 2),(New-Key 'loaf-a'),(New-Key 'loaf-b'),(Base 'sit_to_loaf' 3),(Base 'loaf' 0));delays=@(.02,.04,.06,.07,.07,.10,.04)},
 @{name='sit_to_lay';keys=@((Base 'sit' 0),(New-Key 'lay-a'),(Base 'sit_to_lay' 1),(New-Key 'lay-mid'),(Base 'sit_to_lay' 3),(New-Key 'lay-b'),(Base 'lay' 0));delays=@(.02,.05,.07,.07,.08,.10,.03)},
 @{name='loaf_to_lay';keys=@((Base 'loaf' 0),(New-Key 'direct-a'),(New-Key 'direct-b'),(New-Key 'direct-c'),(Base 'lay' 0));delays=@(.02,.10,.11,.13,.04)}
)
$changed=@('sit_to_loaf','loaf_to_sit','sit_to_lay','lay_to_sit','loaf_to_lay','lay_to_loaf')
$actions=@($manifest.actions|Where-Object {$_.clip -notin $changed});$clips=@($manifest.actionClips|Where-Object {$_.clip -notin $changed});$playback=@()
foreach($spec in $specs){$parts=$spec.name -split '_to_';foreach($reverse in @($false,$true)){
 $name=if($reverse){$parts[1]+'_to_'+$parts[0]}else{$spec.name};$keys=@($spec.keys);$delays=@($spec.delays)
 if($reverse){[array]::Reverse($keys);[array]::Reverse($delays)}
 for($i=0;$i -lt $keys.Count;$i++){$entry=$keys[$i]|ConvertTo-Json -Depth 25|ConvertFrom-Json;$entry.clip=$name;$entry.frame=$i;$entry.id=0;$actions+=$entry}
 $seconds=($delays|Measure-Object -Sum).Sum;$clips+=[pscustomobject]@{clip=$name;frames=$keys.Count;seconds=$seconds;frameSeconds=$delays;loop=$false}
 $playback+=[pscustomobject]@{clip=$name;keys=$keys;frameSeconds=$delays;seconds=$seconds}
}}
$manifest.actions=$actions;$manifest.actionClips=$clips;$manifest|Add-Member -NotePropertyName drawnRestV9 -NotePropertyValue $true -Force
$payloads['manifest.json']=[Text.Encoding]::UTF8.GetBytes(($manifest|ConvertTo-Json -Depth 60 -Compress))
$stream=[IO.File]::Create((Join-Path $work 'candidate.zip'));$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
try{foreach($key in ($payloads.Keys|Sort-Object)){$s=$zip.CreateEntry($key).Open();try{$bytes=$payloads[$key];$s.Write($bytes,0,$bytes.Length)}finally{$s.Dispose()}}}finally{$zip.Dispose();$stream.Dispose()}
$playback|ConvertTo-Json -Depth 35|Set-Content (Join-Path $work 'playback.json') -Encoding UTF8
