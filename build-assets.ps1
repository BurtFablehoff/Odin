param([string]$OutputPath = (Join-Path $PSScriptRoot 'build\Odin.assets'))
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'art\manifest.json') -Raw | ConvertFrom-Json
$payload = New-Object System.IO.MemoryStream
$stored = @{}
$sha = [System.Security.Cryptography.SHA256]::Create()
try {
    foreach ($entry in $manifest.clips.PSObject.Properties) {
        $clip = $entry.Value
        for ($i = 0; $i -lt $clip.images.Count; $i++) {
            $relative = 'art\frames\{0}\{1:D3}.png' -f $entry.Name, $i
            $bytes = [System.IO.File]::ReadAllBytes((Join-Path $PSScriptRoot $relative))
            $hash = [System.BitConverter]::ToString($sha.ComputeHash($bytes))
            if ($stored.ContainsKey($hash)) {
                $clip.images[$i].offset = [int]$stored[$hash]
            } else {
                $clip.images[$i].offset = [int]$payload.Position
                $stored[$hash] = [int]$payload.Position
                $payload.Write($bytes, 0, $bytes.Length)
            }
            $clip.images[$i].length = $bytes.Length
        }
    }
    $json = $manifest | ConvertTo-Json -Depth 100 -Compress
    $index = [System.Text.Encoding]::UTF8.GetBytes($json)
    $directory = Split-Path -Parent $OutputPath
    if ($directory) { New-Item -ItemType Directory -Force -Path $directory | Out-Null }
    $file = [System.IO.File]::Create($OutputPath)
    $writer = New-Object System.IO.BinaryWriter($file)
    try {
        $writer.Write([int]0x324E444F)
        $writer.Write([int]$index.Length)
        $writer.Write($index)
        $payload.Position = 0
        $payload.CopyTo($file)
    } finally { $writer.Dispose() }
} finally { $sha.Dispose(); $payload.Dispose() }
Write-Host "Artwork pack: $OutputPath"
