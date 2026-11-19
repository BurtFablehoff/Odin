# A16/A17: drawn tail keys, preserving every v3 payload and all non-tail metadata.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$work=Join-Path $root 'build/tail-v5';New-Item -ItemType Directory -Force $work | Out-Null
$author=Join-Path $work 'AuthorDrawnTail.exe'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:exe /reference:System.Drawing.dll ('/out:'+$author) (Join-Path $PSScriptRoot 'TailDrawnFrames.cs')
if($LASTEXITCODE -ne 0){throw 'Tail author compilation failed.'}
foreach($family in @('sit','lay','sleep')){foreach($name in @('low','mid','peak')){
 $p=Start-Process -FilePath $author -ArgumentList $root,$family,$name -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){throw 'Tail authoring failed.'}
}}
$source=Join-Path $root 'draft-assets/approved-transition-preview-v3/build/transitions-v3/candidate.zip'
if((Get-FileHash $source -Algorithm SHA256).Hash.ToLowerInvariant() -ne '1ebfa705ca9476998a59a226a4ca7659a5c9273e6096508e8a13e0ecad8e6bc5'){throw 'Frozen transition pack changed.'}
$inputStream=[IO.File]::OpenRead($source);$inputZip=[IO.Compression.ZipArchive]::new($inputStream,[IO.Compression.ZipArchiveMode]::Read)
try {
 $reader=[IO.StreamReader]::new($inputZip.GetEntry('manifest.json').Open());try{$manifest=$reader.ReadToEnd() | ConvertFrom-Json}finally{$reader.Dispose()}
 $manifest | Add-Member -NotePropertyName drawnTailFlipV5 -NotePropertyValue $true -Force
 $actions=@($manifest.actions | Where-Object {$_.clip -notin @('sit_tail','lay_tail','sleep_tail')});$payloads=@{}
 foreach($family in @('sit','lay','sleep')){
  $base=$manifest.actions | Where-Object {$_.clip -eq $family -and $_.frame -eq 0} | Select-Object -First 1
  $names=@('neutral','low','mid','peak','mid','low','neutral')
  for($f=0;$f -lt $names.Count;$f++){
   $entry=($base | ConvertTo-Json -Depth 20 -Compress) | ConvertFrom-Json;$entry.clip=$family+'_tail';$entry.frame=$f
   if($names[$f] -ne 'neutral'){
    $file=Join-Path $root ('draft-assets/tail-v5/frames/'+$family+'-'+$names[$f]+'.png')
    $entry.key='tail-v5/'+$family+'-'+$names[$f]+'.png';$entry.sha256=(Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant()
    $entry.source='draft-assets/tail-v5/frames/'+$family+'-'+$names[$f]+'.png';$payloads[$entry.key]=[IO.File]::ReadAllBytes($file)
   }
   $actions+=$entry
  }
  ($manifest.actionClips | Where-Object {$_.clip -eq ($family+'_tail')}).frames=7
 }
 $manifest.actions=$actions;$manifest.restoredRestFrameCount=79
 $stream=[IO.File]::Create((Join-Path $work 'candidate.zip'));$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
 try {
  foreach($entry in $inputZip.Entries){
   $target=$zip.CreateEntry($entry.FullName);$destination=$target.Open()
   try{if($entry.FullName -eq 'manifest.json'){$bytes=[Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 60 -Compress));$destination.Write($bytes,0,$bytes.Length)}else{$original=$entry.Open();try{$original.CopyTo($destination)}finally{$original.Dispose()}}}finally{$destination.Dispose()}
  }
  foreach($key in $payloads.Keys){$destination=$zip.CreateEntry($key).Open();try{$bytes=$payloads[$key];$destination.Write($bytes,0,$bytes.Length)}finally{$destination.Dispose()}}
 }finally{$zip.Dispose();$stream.Dispose()}
}finally{$inputZip.Dispose();$inputStream.Dispose()}
