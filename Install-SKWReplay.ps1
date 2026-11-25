$ErrorActionPreference = 'Stop'

$applicationName = 'SKWReplay'
$applicationPath = Join-Path $env:LOCALAPPDATA "Programs\$applicationName"
$executablePath = Join-Path $applicationPath 'SKWReplay.App.exe'
$sourceExecutable = Join-Path $PSScriptRoot 'SKWReplay.App.exe'
$sourceFfmpeg = Join-Path $PSScriptRoot 'ffmpeg\ffmpeg.exe'
$startMenuPath = Join-Path ([Environment]::GetFolderPath('Programs')) "$applicationName.lnk"
$desktopPath = Join-Path ([Environment]::GetFolderPath('Desktop')) "$applicationName.lnk"

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

Write-Host "SKWReplay wurde installiert nach: $applicationPath"
Write-Host 'Die App ist jetzt im Startmenue und auf dem Desktop verfuegbar.'
