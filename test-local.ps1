$ErrorActionPreference = 'Stop'
$out = Join-Path $PSScriptRoot 'release-artifacts\tests'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sdk = (dotnet --list-sdks | Select-Object -Last 1) -replace ' .*',''
$roslyn = Join-Path (Split-Path (Get-Command dotnet).Source) "sdk\$sdk\Roslyn\bincore\csc.dll"
$framework = Split-Path $compiler
& dotnet $roslyn /nologo /target:exe /nostdlib+ /langversion:9 "/out:$out\StatisticMathTests.exe" "/reference:$framework\mscorlib.dll" "/reference:$framework\System.dll" "/reference:$framework\System.Core.dll" "$PSScriptRoot\analysis\Data\StatisticMath.cs" "$PSScriptRoot\tests\StatisticMathTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& "$out\StatisticMathTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
