$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'SKWReplay.App\SKWReplay.App.csproj'
$ffmpegPath = Join-Path $PSScriptRoot 'SKWReplay.App\ffmpeg\ffmpeg.exe'
$packagePath = Join-Path $PSScriptRoot 'SKWReplay-publish-win-x64'
$publishPath = Join-Path $packagePath 'SKWReplay'

if (-not (Test-Path -LiteralPath $ffmpegPath -PathType Leaf)) {
    throw "FFmpeg fehlt: $ffmpegPath. Lege ffmpeg.exe dort ab und starte den Build erneut."
}

New-Item -ItemType Directory -Path $publishPath -Force | Out-Null
Get-ChildItem -LiteralPath $packagePath -Force |
    Where-Object { $_.Name -notin @('SKWReplay', 'Install-SKWReplay.cmd') } |
    ForEach-Object {
        Move-Item -LiteralPath $_.FullName -Destination $publishPath
    }

$arguments = @(
    'publish'
    $projectPath
    '-c'
    'Release'
    '-r'
    'win-x64'
    '--self-contained'
    'true'
    '-o'
    $publishPath
)

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish ist mit Exitcode $LASTEXITCODE fehlgeschlagen."
}

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-SKWReplay.ps1') -Destination $publishPath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Uninstall-SKWReplay.ps1') -Destination $publishPath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-SKWReplay.cmd') -Destination $packagePath -Force

if (-not (Test-Path -LiteralPath (Join-Path $publishPath 'ffmpeg\ffmpeg.exe') -PathType Leaf)) {
    throw "FFmpeg wurde nicht in den Publish-Ordner übernommen: $publishPath"
}

Write-Host "Fertiges Installationspaket: $packagePath"
