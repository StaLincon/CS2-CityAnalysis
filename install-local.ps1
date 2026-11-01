param(
    [string]$ModDirectory = (Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\.cache\Mods\pdx_mods\144817_1'),
    [switch]$Restore,
    [string]$BackupDirectory
)
$ErrorActionPreference = 'Stop'
if (Get-Process Cities2 -ErrorAction SilentlyContinue) { throw 'Please exit Cities: Skylines II before installing or restoring.' }
$target = Join-Path $ModDirectory 'analysis.dll'
if (!(Test-Path -LiteralPath $target)) { throw "Subscribed City Data Analysis DLL not found: $target. Supply -ModDirectory for another installed version." }
$backupRoot = Join-Path $PSScriptRoot 'release-artifacts\backups'
if ($Restore) {
    if (!$BackupDirectory) {
        $BackupDirectory = Get-ChildItem -LiteralPath $backupRoot -Directory | Sort-Object Name -Descending | Select-Object -First 1 -ExpandProperty FullName
    }
    $original = Join-Path $BackupDirectory 'analysis.dll'
    if (!(Test-Path -LiteralPath $original)) { throw 'Original DLL backup not found.' }
    Copy-Item -LiteralPath $original -Destination $target -Force
    Write-Host "Original mod restored from $BackupDirectory"
    exit
}
$source = Join-Path $PSScriptRoot 'release-artifacts\CityAnalysis\analysis.dll'
if (!(Test-Path -LiteralPath $source)) { throw 'Build the mod first using build-local.ps1.' }
if ((Get-FileHash -LiteralPath $source).Hash -eq (Get-FileHash -LiteralPath $target).Hash) { Write-Host 'This build is already installed.'; exit }
$backup = Join-Path $backupRoot (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $backup -Force | Out-Null
Copy-Item -LiteralPath $target -Destination (Join-Path $backup 'analysis.dll')
$exported = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Cities Skylines II\analysis'
if (Test-Path -LiteralPath $exported) { Copy-Item -LiteralPath $exported -Destination (Join-Path $backup 'exported-data') -Recurse }
Copy-Item -LiteralPath $source -Destination $target -Force
if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw 'Installed DLL hash does not match.' }
Write-Host "Installed compatibility build. Backup: $backup"
Write-Host 'Start the game, load your city, then Options > City Analysis > Export City Data.'
