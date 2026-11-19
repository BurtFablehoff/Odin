# A22: diagnose registration using anatomical face landmarks, keeping raster bytes intact.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing,System.IO.Compression
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$work=Join-Path $root 'build/geometry-motion-v10';New-Item -ItemType Directory -Force $work | Out-Null
$guides=Join-Path $root 'draft-assets/geometry-motion-v10/guides';New-Item -ItemType Directory -Force $guides | Out-Null
$geometry=Get-Content (Join-Path $root 'build/drawn-motion-v9/head-geometry.json') -Raw|ConvertFrom-Json
$stream=[IO.File]::OpenRead((Join-Path $root 'build/drawn-motion-v9/candidate.zip'));$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read)
try{
 $reader=[IO.StreamReader]::new($zip.GetEntry('manifest.json').Open());try{$manifest=$reader.ReadToEnd()|ConvertFrom-Json}finally{$reader.Dispose()}
 $all=@()
 foreach($clip in @('sit_to_loaf','sit_to_lay','loaf_to_lay')){
  $measure=@($geometry.$clip);$entries=@($manifest.actions|Where-Object {$_.clip -eq $clip}|Sort-Object frame)
  $span0=$measure[0].displayEyeSpan;$span1=$measure[-1].displayEyeSpan
  for($i=0;$i -lt $entries.Count;$i++){
   $e=$entries[$i]|ConvertTo-Json -Depth 20|ConvertFrom-Json;$m=$measure[$i]
   if($i -gt 0 -and $i -lt $entries.Count-1){
    $axis=if($clip -eq 'loaf_to_lay'){0}else{1};$t=($m.displayEye[$axis]-$measure[0].displayEye[$axis])/($measure[-1].displayEye[$axis]-$measure[0].displayEye[$axis]);$t=[Math]::Max(0.0,[Math]::Min(1.0,$t))
    $target=$span0+($span1-$span0)*$t;$factor=$target/$m.displayEyeSpan;$e.renderWidthAtSmall*=$factor
    # Keep the old rear placement while correcting each drawing's actual anatomical units.
    $e.anchorX=$e.bounds[0]+269.0/($e.renderWidthAtSmall/480.0)
   }
   $e|Add-Member -NotePropertyName previousFrame -NotePropertyValue $i -Force
   $all+=$e
   $input=$zip.GetEntry($e.key).Open();try{$image=[Drawing.Bitmap]::new($input);try{
    $canvas=[Drawing.Bitmap]::new(1024,1024,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($canvas)
    try{$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy;$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$s=$e.renderWidthAtSmall/480.0;$rect=[Drawing.RectangleF]::new([single](512-$e.anchorX*$s),[single](944-$e.supportY*$s),[single]($image.Width*$s),[single]($image.Height*$s));$g.DrawImage($image,$rect)}finally{$g.Dispose()}
    $canvas.Save((Join-Path $guides ($clip+'-'+$i+'.png')),[Drawing.Imaging.ImageFormat]::Png);$canvas.Dispose()
   }finally{$image.Dispose()}}finally{$input.Dispose()}
  }
 }
 $all|ConvertTo-Json -Depth 25|Set-Content (Join-Path $work 'registered-keys.json') -Encoding UTF8
}finally{$zip.Dispose();$stream.Dispose()}
