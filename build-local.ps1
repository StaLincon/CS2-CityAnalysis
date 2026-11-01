param([Parameter(Mandatory=$true)][string]$GameDir)
$ErrorActionPreference = 'Stop'
$managed = Join-Path $GameDir 'Cities2_Data\Managed'
if (!(Test-Path -LiteralPath (Join-Path $managed 'Game.dll'))) { throw 'Game.dll not found in GameDir' }
$sdk = (dotnet --list-sdks | Select-Object -Last 1) -replace ' .*',''
$compiler = Join-Path (Split-Path (Get-Command dotnet).Source) "sdk\$sdk\Roslyn\bincore\csc.dll"
$out = Join-Path $PSScriptRoot 'release-artifacts\CityAnalysis'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$references = Get-ChildItem -LiteralPath $managed -Filter '*.dll' | ForEach-Object { '/reference:' + $_.FullName }
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'analysis') -Filter '*.cs' -Recurse | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } | ForEach-Object FullName
& dotnet $compiler /nologo /target:library /nostdlib+ /langversion:9 /optimize+ "/out:$out\analysis.dll" @references @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Write-Host "Built $out\analysis.dll against $managed"
