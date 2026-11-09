param([string]$Executable='test-exes\Odin-Dev-StyleRecovery-v7.exe',[string]$ReportPath='build/qa-geometry-style-v7.json')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location -LiteralPath $projectRoot
# Run in Windows PowerShell/.NET Framework. Add-Type keeps the helper in memory;
# this geometry audit never invokes the companion entry point or native UI.
$source=Get-Content -LiteralPath 'tools/local/QA-Dev-ArtGeometry.cs' -Raw
$source=$source.Replace('class DevArtGeometryQA','public class DevArtGeometryQA')
$source=$source.Replace('static int Main(', 'public static int Main(')
$qaTypes=Add-Type -TypeDefinition $source -ReferencedAssemblies System.Drawing,System.Web.Extensions -PassThru
$qaType=$qaTypes | Where-Object { $_.FullName -eq 'DevArtGeometryQA' }
$entry=$qaType.GetMethod('Main',[Reflection.BindingFlags]'Static,Public,NonPublic')
$arguments=[string[]]@((Join-Path $projectRoot $Executable),(Join-Path $projectRoot $ReportPath))
$result=$entry.Invoke($null,[object[]]@(,$arguments))
if($result -ne 0){throw (Get-Content $ReportPath -Raw)}
Get-Content $ReportPath
