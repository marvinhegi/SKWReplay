$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'SKWReplay.App\SKWReplay.App.csproj'
$ffmpegPath = Join-Path $PSScriptRoot 'SKWReplay.App\ffmpeg\ffmpeg.exe'
$publishPath = Join-Path $PSScriptRoot 'publish-win-x64'

if (-not (Test-Path -LiteralPath $ffmpegPath -PathType Leaf)) {
    throw "FFmpeg fehlt: $ffmpegPath. Lege ffmpeg.exe dort ab und starte den Build erneut."
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

foreach ($file in @('Install-SKWReplay.cmd', 'Install-SKWReplay.ps1')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $publishPath -Force
}

if (-not (Test-Path -LiteralPath (Join-Path $publishPath 'ffmpeg\ffmpeg.exe') -PathType Leaf)) {
    throw "FFmpeg wurde nicht in den Publish-Ordner übernommen: $publishPath"
}

Write-Host "Fertiges Installationspaket: $publishPath"
