# Candidate authoring only: RULES A12/A1/A2/A9. Never writes art/.
param([string]$OutputPath,[switch]$TextureRecovery)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$version=if($TextureRecovery){'transitions-v3'}else{'transitions-v2'}
if (!$OutputPath) { $OutputPath=Join-Path $root ('build/'+$version+'/candidate.zip') }
$work=Join-Path $root ('build/'+$version)
if($TextureRecovery){& (Join-Path $PSScriptRoot 'Build-TextureRecoveryFrames.ps1')}
New-Item -ItemType Directory -Force $work | Out-Null
& (Join-Path $PSScriptRoot 'Pack-LatestArt.ps1') -OutputPath (Join-Path $work 'baseline.zip')
Add-Type -AssemblyName System.Drawing,System.IO.Compression
Add-Type -ReferencedAssemblies @([Drawing.Bitmap].Assembly.Location,[Drawing.Rectangle].Assembly.Location) -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
public static class OdinTransitionCells {
 public static int[] Single(string path,string output) {
  System.IO.File.Copy(path,output,true);using(Bitmap image=new Bitmap(path)) {
   int minX=image.Width,minY=image.Height,maxX=0,maxY=0;
   for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A>32){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
   return new[]{minX,minY,maxX-minX+1,maxY-minY+1,image.Width,image.Height};
  }
 }
 public static double[] Measure(byte[] bytes,int bodyLeft,int bodyRight) {
  using(var stream=new System.IO.MemoryStream(bytes))using(Bitmap image=new Bitmap(stream)) {
   int left=image.Width,right=0;var bottoms=new System.Collections.Generic.List<int>();
   for(int x=0;x<image.Width;x++) {
    int bottom=-1;
    for(int y=0;y<image.Height;y++)if(image.GetPixel(x,y).A>128){left=Math.Min(left,x);right=Math.Max(right,x);bottom=y;}
    if(x>=bodyLeft&&x<bodyRight&&bottom>=0)bottoms.Add(bottom);
   }
   bottoms.Sort();if(bottoms.Count==0)throw new Exception("No body contact pixels in the measured columns.");
   double median=(bottoms[(bottoms.Count-1)/2]+bottoms[bottoms.Count/2])/2.0;
   return new[]{(double)left,(double)right,median};
  }
 }
 public static int[] Extract(string path,int slot,string output) {
  using(Bitmap strip=new Bitmap(path)) {
   int left=slot*strip.Width/3,right=(slot+1)*strip.Width/3;
   using(Bitmap frame=new Bitmap(right-left+64,strip.Height+64,PixelFormat.Format32bppArgb)) {
    using(Graphics g=Graphics.FromImage(frame)) {g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;g.DrawImage(strip,new Rectangle(32,32,right-left,strip.Height),new Rectangle(left,0,right-left,strip.Height),GraphicsUnit.Pixel);}
    int minX=frame.Width,minY=frame.Height,maxX=0,maxY=0;
    for(int y=0;y<frame.Height;y++)for(int x=0;x<frame.Width;x++)if(frame.GetPixel(x,y).A>32){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
    frame.Save(output,ImageFormat.Png);return new[]{minX,minY,maxX-minX+1,maxY-minY+1,frame.Width,frame.Height};
   }
  }
 }
}
'@
$payloads=@{}
$inputFile=[IO.File]::OpenRead((Join-Path $work 'baseline.zip'))
$inputZip=[IO.Compression.ZipArchive]::new($inputFile,[IO.Compression.ZipArchiveMode]::Read)
try {
 foreach($entry in $inputZip.Entries){$stream=$entry.Open();$copy=[IO.MemoryStream]::new();try{$stream.CopyTo($copy);$payloads[$entry.FullName]=$copy.ToArray()}finally{$stream.Dispose();$copy.Dispose()}}
}finally{$inputZip.Dispose();$inputFile.Dispose()}
$manifest=[Text.Encoding]::UTF8.GetString($payloads['manifest.json']) | ConvertFrom-Json
$actions=[Collections.Generic.List[object]]::new();foreach($entry in $manifest.actions){$actions.Add($entry)}
$clips=[Collections.Generic.List[object]]::new();foreach($entry in $manifest.actionClips){$clips.Add($entry)}
# Fixed strip scales use the initial skull/face measurement, never body bounds:
# sit/loaf 232*.378=87.70px, sit/lay 210*.418=87.78px, sit/groom 226*.388=87.69px
# versus sit's 87.66px facial core; lay/sleep 192*.378=72.58px between pinnae
# versus awake laying's 72.48px. Keep one scale throughout each strip.
# These remain candidate display estimates, not owner-approved anatomy.
$families=@(
 @{name='sit_loaf';from='sit';to='loaf';file='sit_loaf.png';scale=.378;body=@(250,400);seconds=1.4},
 @{name='sit_lay';from='sit';to='lay';file='sit_lay-alpha.png';scale=.418;body=@(300,400);seconds=1.6},
 @{name='lay_sleep';from='lay';to='sleep';file='lay_sleep-alpha.png';scale=.378;body=@(300,420);seconds=1.25},
 @{name='sit_groom';from='sit';to='groom';file='sit_groom-alpha.png';scale=.388;body=@(250,400);seconds=1.0}
)
if($TextureRecovery){
 $families=@(
  @{name='sit_loaf';from='sit';to='loaf';scale=(480.0/1024);body=@(420,480);seconds=1.4},
  @{name='sit_lay';from='sit';to='lay';scale=(480.0/1024);body=@(450,520);seconds=1.6},
  @{name='lay_sleep';from='lay';to='sleep';scale=(444.432/1024);body=@(450,520);seconds=1.25},
  # Measure the unchanged haunch, not columns occupied by the lifting foreleg.
  @{name='sit_groom';from='sit';to='groom';scale=(480.0/1024);body=@(420,440);seconds=1.0}
 )
}
function Copy-Record($record){$record | ConvertTo-Json -Depth 20 | ConvertFrom-Json}
function Get-Endpoint([string]$clip,[int]$frame){@($manifest.actions | Where-Object {$_.clip -eq $clip -and $_.frame -eq $frame})[0]}
# A12: one uniform scale per strip, selected from its first head/skull measurement
# and carried through the clip. Rear silhouette is a translation landmark only.
# Unlike v1, no independent per-frame eye-span resizing or face-driven lateral motion.
$rearOffset=(243-512)*480/1024.0
$restColumns=@{sit=@(420,480);loaf=@(470,545);lay=@(450,520);sleep=@(490,570)}
$restChanges=@{}
foreach($pose in $manifest.poses | Where-Object {$_.id -in @(1,2,3,4)}){
 $family=@{1='sit';2='loaf';3='lay';4='sleep'}[[int]$pose.id]
 $columns=$restColumns[$family];$metrics=[OdinTransitionCells]::Measure($payloads[$pose.key],$columns[0],$columns[1])
 $oldSupport=$pose.supportY
 if($family -eq 'sleep'){$pose.renderWidthAtSmall=(Get-Endpoint 'lay' 0).renderWidthAtSmall}
 $scale=$pose.renderWidthAtSmall/$pose.nativeWidth
 if($family -ne 'sit'){$pose.supportY=$metrics[2]+1;$pose.anchorX=$metrics[0]-$rearOffset/$scale}
 $pose | Add-Member -NotePropertyName contactColumns -NotePropertyValue $columns -Force
 $restChanges[$family]=@{supportY=$pose.supportY;anchorX=$pose.anchorX;width=$pose.renderWidthAtSmall;oldSupport=$oldSupport;contact=$columns}
}
foreach($entry in $manifest.actions){
 $family=$entry.clip -replace '^look_','' -replace '_(eyes|tail)$',''
 if($restChanges.ContainsKey($family)){
  $change=$restChanges[$family];$entry.supportY=$change.supportY+($entry.supportY-$change.oldSupport);$entry.anchorX=$change.anchorX;$entry.renderWidthAtSmall=$change.width
  $entry | Add-Member -NotePropertyName contactColumns -NotePropertyValue $change.contact -Force
 }elseif($entry.clip -eq 'groom'){
  $metrics=[OdinTransitionCells]::Measure($payloads[$entry.key],390,450)
  # 465.1 / 1024 matches the original sitting facial-core width; fixed for all 16 frames.
  $entry.renderWidthAtSmall=465.1;$entry.anchorX=$metrics[0]-$rearOffset/(465.1/1024);$entry.supportY=$metrics[2]+1
  $entry | Add-Member -NotePropertyName contactColumns -NotePropertyValue @(390,450) -Force
 }
}
$actions.Clear();foreach($entry in $manifest.actions){$actions.Add($entry)}
$registration=@()
foreach($family in $families){
 $source=if($TextureRecovery){$null}else{Join-Path $root ('draft-assets/transitions-v1/'+$family.file)}
 $keys=@()
 for($slot=0;$slot -lt 3;$slot++){
  $key='transition-'+$family.name+'-'+$slot+'.png';$target=Join-Path $work $key
  if($TextureRecovery){$source=Join-Path $root ('draft-assets/texture-recovery-v3/frames/'+$family.name+'-'+$slot+'.png');$bounds=[OdinTransitionCells]::Single($source,$target)}else{$bounds=[OdinTransitionCells]::Extract($source,$slot,$target)}
  $scale=$family.scale
  $metrics=[OdinTransitionCells]::Measure([IO.File]::ReadAllBytes($target),$family.body[0],$family.body[1])
  $support=$metrics[2]+1;if($TextureRecovery -and $family.name -eq 'sit_groom'){$support=944}
  $sourceRelative=if($TextureRecovery){'draft-assets/texture-recovery-v3/frames/'+$family.name+'-'+$slot+'.png'}else{'draft-assets/transitions-v1/'+$family.file}
  $entry=[pscustomobject]@{key=$key;label=$family.name;sha256=(Get-FileHash $target -Algorithm SHA256).Hash.ToLowerInvariant();id=0;clip='';frame=0;kind='transition';source=$sourceRelative;nativeWidth=$bounds[4];nativeHeight=$bounds[5];bounds=@($bounds[0],$bounds[1],$bounds[2],$bounds[3]);supportY=$support;anchorX=($metrics[0]-$rearOffset/$scale);customAnchor=$true;airborne=$false;renderWidthAtSmall=($scale*$bounds[4]);contactColumns=$family.body}
  $payloads[$key]=[IO.File]::ReadAllBytes($target);$keys+=,$entry
 }
 foreach($reverse in @($false,$true)){
  $from=if($reverse){$family.to}else{$family.from};$to=if($reverse){$family.from}else{$family.to}
  $name=$from+'_to_'+$to
  $first=Get-Endpoint $from $(if($from -eq 'groom'){15}else{0});$last=Get-Endpoint $to 0
  if(!$first -or !$last){throw "Missing endpoint for $name"}
  $sequence=@($first)+$(if($reverse){@($keys[2],$keys[1],$keys[0])}else{$keys})+@($last)
  for($frame=0;$frame -lt $sequence.Count;$frame++){$entry=Copy-Record $sequence[$frame];$entry.clip=$name;$entry.frame=$frame;$entry.id=0;$actions.Add($entry)}
  $clips.Add([pscustomobject]@{clip=$name;frames=5;seconds=$family.seconds;loop=$false})
  $registration+=@{clip=$name;frames=$sequence;seconds=$family.seconds}
 }
}
$manifest.actions=$actions.ToArray();$manifest.actionClips=$clips.ToArray()
$payloads['manifest.json']=[Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 60 -Compress))
$file=[IO.File]::Create($OutputPath);$zip=[IO.Compression.ZipArchive]::new($file,[IO.Compression.ZipArchiveMode]::Create)
try {foreach($key in ($payloads.Keys | Sort-Object)){$entry=$zip.CreateEntry($key);$entry.LastWriteTime=[DateTimeOffset]::new(2026,11,15,0,0,0,[TimeSpan]::Zero);$stream=$entry.Open();try{$stream.Write($payloads[$key],0,$payloads[$key].Length)}finally{$stream.Dispose()}}}finally{$zip.Dispose();$file.Dispose()}
@{status=('candidate '+$version+'; complete motion pending owner visual pass');rule=$(if($TextureRecovery){'A14'}else{'A12'});registration=$registration;restChanges=$restChanges;rearOffset=$rearOffset;landmarks='fixed clip scales; rear landmark translation; median underside contact columns; approved source pixels unchanged';baselineSha256=(Get-FileHash (Join-Path $work 'baseline.zip')).Hash;packSha256=(Get-FileHash $OutputPath).Hash} | ConvertTo-Json -Depth 50 | Set-Content (Join-Path $work 'registration.json') -Encoding utf8
Write-Host "Candidate transition pack: $OutputPath"
