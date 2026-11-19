# A16: reuse the frozen v3 pack; original PNG bytes and transition registrations stay intact.
param([ValidateSet(6,7)][int]$Revision=7)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$source=Join-Path $root 'draft-assets/approved-transition-preview-v3/build/transitions-v3/candidate.zip'
if((Get-FileHash $source -Algorithm SHA256).Hash.ToLowerInvariant() -ne '1ebfa705ca9476998a59a226a4ca7659a5c9273e6096508e8a13e0ecad8e6bc5'){throw 'Frozen transition pack changed.'}
$output=Join-Path $root ('build/tail-v'+$Revision+'/candidate.zip')
New-Item -ItemType Directory -Force (Split-Path $output) | Out-Null
$inputStream=[IO.File]::OpenRead($source)
$inputZip=[IO.Compression.ZipArchive]::new($inputStream,[IO.Compression.ZipArchiveMode]::Read)
try {
 $reader=[IO.StreamReader]::new($inputZip.GetEntry('manifest.json').Open())
 try {$manifest=$reader.ReadToEnd() | ConvertFrom-Json}finally{$reader.Dispose()}
 $manifest | Add-Member -NotePropertyName subtleTailTipV6 -NotePropertyValue $true -Force
 if($Revision -ge 7){$manifest | Add-Member -NotePropertyName tailTipRevision -NotePropertyValue $Revision -Force}
 $actions=@($manifest.actions | Where-Object {$_.clip -notin @('sit_tail','lay_tail','sleep_tail')})
 foreach($family in @('sit','lay','sleep')){
  $base=$manifest.actions | Where-Object {$_.clip -eq $family -and $_.frame -eq 0} | Select-Object -First 1
  for($frame=0;$frame -lt 17;$frame++){
   $tail=($base | ConvertTo-Json -Depth 20 -Compress) | ConvertFrom-Json
   $tail.clip=$family+'_tail';$tail.frame=$frame
   $actions+=$tail
  }
  ($manifest.actionClips | Where-Object {$_.clip -eq ($family+'_tail')}).frames=17
 }
 $manifest.actions=$actions;$manifest.restoredRestFrameCount=109
 $stream=[IO.File]::Create($output);$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
 try {
  foreach($entry in $inputZip.Entries){
   $target=$zip.CreateEntry($entry.FullName);$destination=$target.Open()
   try {
    if($entry.FullName -eq 'manifest.json'){$bytes=[Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 60 -Compress));$destination.Write($bytes,0,$bytes.Length)}
    else{$original=$entry.Open();try{$original.CopyTo($destination)}finally{$original.Dispose()}}
   }finally{$destination.Dispose()}
  }
 }finally{$zip.Dispose();$stream.Dispose()}
}finally{$inputZip.Dispose();$inputStream.Dispose()}
Write-Host "Tail candidate: $output"
