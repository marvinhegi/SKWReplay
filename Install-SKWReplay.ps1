$ErrorActionPreference = 'Stop'

$applicationName = 'SKWReplay'
$applicationPath = Join-Path $env:LOCALAPPDATA "Programs\$applicationName"
$executablePath = Join-Path $applicationPath 'SKWReplay.App.exe'
$sourceExecutable = Join-Path $PSScriptRoot 'SKWReplay.App.exe'
$sourceFfmpeg = Join-Path $PSScriptRoot 'ffmpeg\ffmpeg.exe'
$startMenuPath = Join-Path ([Environment]::GetFolderPath('Programs')) "$applicationName.lnk"
$desktopPath = Join-Path ([Environment]::GetFolderPath('Desktop')) "$applicationName.lnk"
$uninstallKeyPath = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\SKWReplay'

if (-not (Test-Path -LiteralPath $sourceExecutable -PathType Leaf)) {
    throw "SKWReplay.App.exe fehlt im Paketordner: $PSScriptRoot"
}
if (-not (Test-Path -LiteralPath $sourceFfmpeg -PathType Leaf)) {
    throw "ffmpeg\ffmpeg.exe fehlt im Paketordner: $PSScriptRoot"
}
if (Get-Process -Name 'SKWReplay.App' -ErrorAction SilentlyContinue) {
    throw 'SKWReplay läuft bereits. Schliesse die App und starte die Installation erneut.'
}

New-Item -ItemType Directory -Path $applicationPath -Force | Out-Null
Get-ChildItem -LiteralPath $PSScriptRoot -Force |
    Where-Object { $_.Name -notin @('Install-SKWReplay.cmd', 'Install-SKWReplay.ps1') } |
    ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $applicationPath -Recurse -Force
    }

$shell = $null
$startMenuShortcut = $null
$desktopShortcut = $null
try {
    $shell = New-Object -ComObject WScript.Shell
    foreach ($shortcutPath in @($startMenuPath, $desktopPath)) {
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $executablePath
        $shortcut.WorkingDirectory = $applicationPath
        $shortcut.IconLocation = "$executablePath,0"
        $shortcut.Description = 'SKWReplay'
        $shortcut.Save()
        if ($shortcutPath -eq $startMenuPath) {
            $startMenuShortcut = $shortcut
        } else {
            $desktopShortcut = $shortcut
        }
    }
} finally {
    if ($null -ne $desktopShortcut) {
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($desktopShortcut)
    }
    if ($null -ne $startMenuShortcut) {
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($startMenuShortcut)
    }
    if ($null -ne $shell) {
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell)
    }
}

    $powershellPath = Join-Path $PSHOME 'powershell.exe'
    $uninstallerPath = Join-Path $applicationPath 'Uninstall-SKWReplay.ps1'
    $uninstallString = '"{0}" -NoProfile -ExecutionPolicy Bypass -File "{1}"' -f $powershellPath, $uninstallerPath
    $uninstallKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($uninstallKeyPath)
    if ($null -eq $uninstallKey) {
        throw 'Der Deinstallationseintrag konnte nicht erstellt werden.'
    }
    try {
        $uninstallKey.SetValue('DisplayName', $applicationName, [Microsoft.Win32.RegistryValueKind]::String)
        $uninstallKey.SetValue('Publisher', $applicationName, [Microsoft.Win32.RegistryValueKind]::String)
        $uninstallKey.SetValue('InstallLocation', $applicationPath, [Microsoft.Win32.RegistryValueKind]::String)
        $uninstallKey.SetValue('DisplayIcon', "$executablePath,0", [Microsoft.Win32.RegistryValueKind]::String)
        $uninstallKey.SetValue('UninstallString', $uninstallString, [Microsoft.Win32.RegistryValueKind]::String)
        $uninstallKey.SetValue('InstallDate', (Get-Date -Format 'yyyyMMdd'), [Microsoft.Win32.RegistryValueKind]::String)
        $uninstallKey.SetValue('NoModify', 1, [Microsoft.Win32.RegistryValueKind]::DWord)
        $uninstallKey.SetValue('NoRepair', 1, [Microsoft.Win32.RegistryValueKind]::DWord)
    } finally {
        $uninstallKey.Dispose()
    }

Write-Host "SKWReplay wurde installiert nach: $applicationPath"
Write-Host 'Die App ist jetzt im Startmenue und auf dem Desktop verfuegbar.'
