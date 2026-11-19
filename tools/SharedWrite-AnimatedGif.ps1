param(
    [Parameter(Mandatory=$true)][string]$FramesDirectory,
    [Parameter(Mandatory=$true)][string]$OutputPath,
    [Parameter(Mandatory=$true)][int[]]$Delays
)
$ErrorActionPreference = 'Stop'
# Delays are GIF centiseconds. Frames are numbered 00.png, 01.png, ... .
# Extracted from the verified pet review exporter; compressed image blocks are
# preserved while explicit timing and infinite-loop controls are written.
if ($Delays.Length -eq 0) { throw 'At least one frame is required.' }
foreach ($delay in $Delays) { if ($delay -lt 1 -or $delay -gt 65535) { throw 'Invalid GIF delay.' } }
Add-Type -AssemblyName PresentationCore,WindowsBase
$encoder = New-Object System.Windows.Media.Imaging.GifBitmapEncoder
for ($index=0; $index -lt $delays.Length; $index++) {
    $file = Join-Path $FramesDirectory ($index.ToString('00')+'.png')
    $uri = New-Object Uri($file)
    $frame = [System.Windows.Media.Imaging.BitmapFrame]::Create($uri)
    $metadata = New-Object System.Windows.Media.Imaging.BitmapMetadata('gif')
    $metadata.SetQuery('/grctlext/Delay',[UInt16]$delays[$index])
    $metadata.SetQuery('/grctlext/Disposal',[Byte]1)
    if ($index -eq 0) {
        $metadata.SetQuery('/appext/Application',[Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0'))
        $metadata.SetQuery('/appext/Data',[Byte[]]@(3,1,0,0,0))
    }
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($frame,$null,$metadata,$null))
}
$gifPath = $OutputPath
$stream = [IO.File]::Create($gifPath)
try { $encoder.Save($stream) } finally { $stream.Dispose() }
# WPF's encoder can drop GIF timing metadata. Write and verify this review's
# frame control blocks explicitly; compressed image data stays unchanged.
$bytes = [IO.File]::ReadAllBytes($gifPath)
$output = New-Object IO.MemoryStream
$headerLength = 13
if ($bytes[10] -band 128) { $headerLength += 3 * [Math]::Pow(2,($bytes[10] -band 7)+1) }
$output.Write($bytes,0,$headerLength)
$loopBlock = [Byte[]]@(33,255,11,78,69,84,83,67,65,80,69,50,46,48,3,1,0,0,0)
$output.Write($loopBlock,0,$loopBlock.Length)
$offset = $headerLength
$frameIndex = 0
while ($offset -lt $bytes.Length) {
    $start = $offset
    $marker = $bytes[$offset]
    if ($marker -eq 59) { $output.WriteByte(59); break }
    if ($marker -eq 33) {
        $label = $bytes[$offset+1]
        $offset += 2
        while ($true) {
            $count = $bytes[$offset]; $offset++
            if ($count -eq 0) { break }
            $offset += $count
        }
        if ($label -ne 249) { $output.Write($bytes,$start,$offset-$start) }
        continue
    }
    if ($marker -ne 44) { throw 'Unexpected GIF block in review export.' }
    $delay = $delays[$frameIndex]
    $control = [Byte[]]@(33,249,4,4,($delay -band 255),(($delay -shr 8) -band 255),0,0)
    $output.Write($control,0,$control.Length)
    $packed = $bytes[$offset+9]
    $offset += 10
    if ($packed -band 128) { $offset += 3 * [Math]::Pow(2,($packed -band 7)+1) }
    $offset++ # LZW minimum code size
    while ($true) {
        $count = $bytes[$offset]; $offset++
        if ($count -eq 0) { break }
        $offset += $count
    }
    $output.Write($bytes,$start,$offset-$start)
    $frameIndex++
}
if ($frameIndex -ne $delays.Length) { throw 'Review GIF frame count mismatch.' }
[IO.File]::WriteAllBytes($gifPath,$output.ToArray())
$output.Dispose()
