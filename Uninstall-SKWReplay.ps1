$ErrorActionPreference = 'Stop'

$applicationName = 'SKWReplay'
$applicationPath = Join-Path $env:LOCALAPPDATA "Programs\$applicationName"
$startMenuPath = Join-Path ([Environment]::GetFolderPath('Programs')) "$applicationName.lnk"
$desktopPath = Join-Path ([Environment]::GetFolderPath('Desktop')) "$applicationName.lnk"
$uninstallKeyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\SKWReplay'

if (Get-Process -Name 'SKWReplay.App' -ErrorAction SilentlyContinue) {
    throw 'SKWReplay läuft noch. Schliesse die App und starte die Deinstallation erneut.'
}

if (Test-Path -LiteralPath $applicationPath) {
    Remove-Item -LiteralPath $applicationPath -Recurse -Force
}
foreach ($shortcutPath in @($startMenuPath, $desktopPath)) {
    if (Test-Path -LiteralPath $shortcutPath) {
        Remove-Item -LiteralPath $shortcutPath -Force
    }
}
if (Test-Path -LiteralPath $uninstallKeyPath) {
    Remove-Item -LiteralPath $uninstallKeyPath -Recurse -Force
}

Write-Host 'SKWReplay wurde deinstalliert.'
