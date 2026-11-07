param(
    [ValidateSet('Help','Build','Verify','Review','Package','Python','History','Clean')]
    [string]$Task = 'Help',
    [ValidateSet('RestSizing','Sitting','Sleeping','SleepingContact','ApprovedContact')]
    [string]$Review = 'ApprovedContact',
    [switch]$SkipRegistryTest,
    [switch]$Open,
    [string]$DevExecutableName='Odin-Dev-Local.exe',
    [Parameter(ValueFromRemainingArguments=$true)][string[]]$PythonArgs
)
$ErrorActionPreference = 'Stop'
$localTools = Join-Path $PSScriptRoot 'tools\local'
switch ($Task) {
    'Build' { & (Join-Path $PSScriptRoot 'build.ps1') -Dev -DevExecutableName $DevExecutableName }
    'Verify' { & (Join-Path $localTools 'Verify-OdinLocal.ps1') -SkipRegistryTest:$SkipRegistryTest }
    'Review' {
        if ($Review -eq 'ApprovedContact') {
            & (Join-Path $localTools 'Build-ContactReview.ps1')
            if ($Open) { Start-Process -FilePath (Join-Path $PSScriptRoot 'test-exes\Odin-ApprovedContact-Local.exe') }
            break
        }
        if ($Review -eq 'SleepingContact') {
            $python = Join-Path $PSScriptRoot 'local-data\toolchain\python\python.exe'
            if (-not (Test-Path -LiteralPath $python)) { throw 'Local Python toolchain is missing; see tools\local\LOCAL-DEVELOPMENT.md.' }
            & $python (Join-Path $localTools 'Build-SleepContactReview.py')
            if ($LASTEXITCODE -ne 0) { throw 'Sleeping contact review rendering failed.' }
            if ($Open) {
                Start-Process -FilePath (Join-Path $PSScriptRoot 'local-data\reference-review\sleeping-floor-review-v1\sleeping-floor-review.png')
            }
            break
        }
        & (Join-Path $localTools 'Build-Review.ps1') -Review $Review
        if ($Open) { Start-Process -FilePath (Join-Path $PSScriptRoot ('test-exes\Odin-' + $Review + '-Local.exe')) }
    }
    'Package' { & (Join-Path $localTools 'Build-Installer.ps1') }
    'Clean' {
        $setupCleanup = Join-Path $PSScriptRoot 'local-data\maintenance\Finish-SetupCleanup.ps1'
        if (Test-Path -LiteralPath $setupCleanup) { & $setupCleanup }
        else { & (Join-Path $localTools 'Compact-History.ps1') -Apply }
    }
    'Python' {
        $python = Join-Path $PSScriptRoot 'local-data\toolchain\python\python.exe'
        if (-not (Test-Path -LiteralPath $python)) { throw 'Run tools\local\Setup-Toolchain.py first; see tools\local\LOCAL-DEVELOPMENT.md.' }
        & $python @PythonArgs
        if ($LASTEXITCODE -ne 0) { throw "Python exited with code $LASTEXITCODE" }
    }
    'History' {
        $archive = Join-Path $PSScriptRoot 'local-data\archives\vm-history-2026-11-06.7z'
        Write-Host "Historical files: $archive"
        Write-Host 'Restore only what you need; see tools\local\LOCAL-DEVELOPMENT.md.'
    }
    default {
        Write-Host @'
Odin local development (run from this folder)
  .\odin.ps1 Build                     Build the development companion
  .\odin.ps1 Verify                    Build and run project checks
  .\odin.ps1 Verify -SkipRegistryTest  Checks in a registry-restricted sandbox
  .\odin.ps1 Review -Open              Build and open current artwork review
  .\odin.ps1 Review -Review SleepingContact -Open  Compare sleeping size and floor contact
  .\odin.ps1 Review -Review ApprovedContact -Open  Interactive approved-pose contact review
  .\odin.ps1 Package                   Build the production EXE and installer
  .\odin.ps1 Python -PythonArgs script.py  Run with local image-processing tools
  .\odin.ps1 History                   Locate the compressed historical archive
  .\odin.ps1 Clean                     Verify archive and remove expanded legacy history
See tools\local\LOCAL-DEVELOPMENT.md for details.
'@
    }
}
