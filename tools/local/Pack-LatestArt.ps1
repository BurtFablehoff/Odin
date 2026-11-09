# Part 5b: pack the promoted artwork (art/latest, rule A9) into the zip both builds embed as the resource Odin.LatestArt.
# Entries come from art/latest/index.json in its order, each read from the tracked file the index names and checked against
# its sha256, with one fixed timestamp, so the same tracked files give the same zip. PowerShell and .NET only: a GitHub clone
# builds production without the Python toolchain (the Python chain is the authoring tool; tools/local/Promote-LatestArtV12.py promotes).
param([string]$OutputPath = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'build\Odin.latest.zip'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$latest = Join-Path $root 'art\latest'
$index = Get-Content -LiteralPath (Join-Path $latest 'index.json') -Raw | ConvertFrom-Json
$sha = [System.Security.Cryptography.SHA256]::Create()
function Get-Hex([byte[]]$bytes) { ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant() }
$stamp = New-Object System.DateTimeOffset(2026, 11, 8, 0, 0, 0, [TimeSpan]::Zero)
$directory = Split-Path -Parent $OutputPath
if ($directory) { New-Item -ItemType Directory -Force -Path $directory | Out-Null }
if (Test-Path -LiteralPath $OutputPath) { Remove-Item -LiteralPath $OutputPath }
$file = [System.IO.File]::Create($OutputPath)
$zip = New-Object System.IO.Compression.ZipArchive($file, [System.IO.Compression.ZipArchiveMode]::Create)
$count = 0
try {
    $manifestBytes = [System.IO.File]::ReadAllBytes((Join-Path $latest 'manifest.json'))
    if ((Get-Hex $manifestBytes) -ne $index.manifestSha256) { throw 'art/latest/manifest.json does not match index.json.' }
    $entry = $zip.CreateEntry('manifest.json', [System.IO.Compression.CompressionLevel]::Optimal); $entry.LastWriteTime = $stamp
    $stream = $entry.Open(); try { $stream.Write($manifestBytes, 0, $manifestBytes.Length) } finally { $stream.Dispose() }
    foreach ($row in $index.entries) {
        $path = Join-Path $root ($row.from -replace '/', '\')
        $bytes = [System.IO.File]::ReadAllBytes($path)
        if ((Get-Hex $bytes) -ne $row.sha256) { throw "Promoted artwork changed since promotion: $($row.key) ($($row.from))" }
        $entry = $zip.CreateEntry($row.key, [System.IO.Compression.CompressionLevel]::Optimal); $entry.LastWriteTime = $stamp
        $stream = $entry.Open(); try { $stream.Write($bytes, 0, $bytes.Length) } finally { $stream.Dispose() }
        $count++
    }
} finally { $zip.Dispose(); $file.Dispose(); $sha.Dispose() }
$digest = (Get-FileHash -LiteralPath $OutputPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Latest artwork pack: $OutputPath ($count entries + manifest, sha256 $digest)"
