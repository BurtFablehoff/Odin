param([string]$ExecutablePath)
$ErrorActionPreference='Stop'
$qaRoot=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$assembly=Join-Path $qaRoot 'build\qa-original-rest-v10.dll'
& $compiler /nologo /target:library /platform:x64 /optimize+ /define:ODIN_DEV ("/out:"+$assembly) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $qaRoot 'app\OriginalRestRenderer.cs') (Join-Path $PSScriptRoot 'QA-OriginalRestV10.cs')
if($LASTEXITCODE -ne 0){throw 'Original-rest headless QA compile failed.'}
Add-Type -Path $assembly
if($ExecutablePath){
    $report=[OriginalRestV10QA]::RunRuntime($qaRoot,(Resolve-Path -LiteralPath $ExecutablePath).Path)
    [IO.File]::WriteAllText((Join-Path $qaRoot 'build\qa-original-rest-runtime-v10.json'),$report)
}else{
    $report=[OriginalRestV10QA]::Run($qaRoot)
    [IO.File]::WriteAllText((Join-Path $qaRoot 'build\qa-original-rest-renderer-v10.json'),$report)
}
$report
