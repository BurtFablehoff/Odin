param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
$history = [IO.Path]::GetFullPath((Join-Path $project 'local-data\vm-history'))
$archive = Join-Path $project 'local-data\archives\vm-history-2026-11-06.7z'
$maintenance = Join-Path $project 'local-data\maintenance'
$sevenzip = Join-Path $project 'local-data\toolchain\7zip\x64\7za.exe'
$inventory = @(Import-Csv -LiteralPath (Join-Path $maintenance 'vm-history-files.csv'))
$omittedPrefix = 'older-art-workspace\v2\tools\'
$expected = @($inventory | Where-Object { -not $_.Path.StartsWith($omittedPrefix,[StringComparison]::OrdinalIgnoreCase) })
if (-not (Test-Path -LiteralPath $history)) { throw 'Expanded history is already absent; nothing removed.' }
if (-not (Test-Path -LiteralPath $archive)) { throw 'Verified archive is required before removing expanded history.' }
if ($history -ne (Join-Path $project 'local-data\vm-history') -or
    -not $history.StartsWith($project.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'History path escaped the checkout.' }
if (@(Get-ChildItem -LiteralPath $history -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'History contains a link; inspect before cleanup.' }
Write-Host 'Testing archive integrity...'
& $sevenzip t -bsp0 $archive
if ($LASTEXITCODE -ne 0) { throw 'Archive integrity test failed; originals retained.' }
$listing = @(& $sevenzip l -slt -ba $archive)
if ($LASTEXITCODE -ne 0) { throw 'Cannot read archive contents.' }
$archived = @{}
$entry = @{}
foreach ($line in ($listing + '')) {
    if ($line -eq '') {
        if ($entry.ContainsKey('Path') -and $entry.ContainsKey('Size') -and $entry['Folder'] -ne '+' -and $entry['Attributes'] -notmatch 'D') {
            $relative = $entry['Path'].Replace('/','\')
            if (-not $relative.StartsWith('vm-history\')) { throw "Unexpected archived path: $relative" }
            $archived[$relative.Substring(11)] = [long]$entry['Size']
        }
        $entry = @{}
    } elseif ($line -match '^([^=]+) = (.*)$') { $entry[$Matches[1].Trim()] = $Matches[2] }
}
if ($archived.Count -ne $expected.Count) { throw "Archive file count mismatch: $($archived.Count) vs $($expected.Count)." }
foreach ($file in $expected) {
    if (-not $archived.ContainsKey($file.Path) -or $archived[$file.Path] -ne [long]$file.Bytes) { throw "Missing/different archived entry: $($file.Path)" }
}
# Recheck the source snapshot's hashes immediately before removal.
$currentFiles = @(Get-ChildItem -LiteralPath $history -Recurse -File -Force)
if ($currentFiles.Count -ne $inventory.Count) { throw 'History changed since inventory; originals retained.' }
foreach ($file in $inventory) {
    $path = [IO.Path]::GetFullPath((Join-Path $history $file.Path))
    if (-not $path.StartsWith($history+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe inventory path.' }
    if ((Get-Item -LiteralPath $path).Length -ne [long]$file.Bytes -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.SHA256) { throw "Source changed: $path" }
}
$expandedBytes = [long](($inventory | Measure-Object Bytes -Sum).Sum)
$archiveBytes = (Get-Item -LiteralPath $archive).Length
$report = [ordered]@{
    date='2026-11-06'; expandedBytes=$expandedBytes; archiveBytes=$archiveBytes
    savedBytes=($expandedBytes-$archiveBytes); archivedFiles=$expected.Count
    discarded='Unused downloaded Blender installation, ZIP, and checksum only'
    archiveSHA256=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
    archiveIntegrityPassed=$true; archivedPathsAndSizesPassed=$true; sourceHashesPassed=$true
    expandedHistoryRemoved=[bool]$Apply
}
if ($Apply) { Remove-Item -LiteralPath $history -Recurse -Force }
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $maintenance 'history-compaction.json') -Encoding UTF8
Write-Host ('Verified {0} archived files. Space recoverable/recovered: {1:N2} GiB.' -f $expected.Count,(($expandedBytes-$archiveBytes)/1GB))
if (-not $Apply) { Write-Host 'No originals removed. Use -Apply to remove the verified expanded copy.' }
