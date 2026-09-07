param(
    [string]$GamePath = 'C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (Get-Process -Name StardewModdingAPI,StardewValley,'Stardew Valley' -ErrorAction SilentlyContinue) {
    throw 'Save and quit Stardew Valley before installing this update. No installed files were changed.'
}
$source = Join-Path $repo 'src/AbigailModern'
$binary = Join-Path $source 'bin/Release/net6.0/AbigailModern.dll'
if (!(Test-Path -LiteralPath $binary)) { throw 'Build the visual mod before installing.' }
$files = @(
    @{ Source = $binary; Relative = 'AbigailModern.dll' },
    @{ Source = (Join-Path $source 'manifest.json'); Relative = 'manifest.json' },
    @{ Source = (Join-Path $source 'README.md'); Relative = 'README.md' },
    @{ Source = (Join-Path $source 'artwork.json'); Relative = 'artwork.json' }
)
. (Join-Path $PSScriptRoot 'get-player-hd-files.ps1')
$artFiles = @((Get-Content (Join-Path $source 'artwork.json') -Raw | ConvertFrom-Json).File | Sort-Object -Unique) + @(Get-PlayerHdFiles $source)
$artFiles += @(Get-ModernClothingFiles $source)
foreach ($relative in $artFiles) {
        if ([IO.Path]::IsPathRooted($relative) -or ($relative -split '[/\\]') -contains '..') { throw "Unsafe asset path: $relative" }
        $assetSource = Join-Path $source $relative
        if (!(Test-Path -LiteralPath $assetSource)) { throw "Missing asset: $relative" }
        $files += @{ Source = $assetSource; Relative = $relative }
}
$install = Join-Path $GamePath 'Mods/AbigailModern'
if (Test-Path -LiteralPath $install) {
    $backup = Join-Path $repo ('artifacts/mod-backups/AbigailModern-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
    Copy-Item -LiteralPath $install -Destination $backup -Recurse
    Write-Output "Previous version backed up: $backup"
}
foreach ($file in $files) {
    $target = Join-Path $install $file.Relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath $file.Source -Destination $target -Force
    if ((Get-FileHash -LiteralPath $file.Source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) {
        throw "Installed file differs: $target"
    }
}
Write-Output "Installed and hash-verified $($files.Count) files listed in artwork.json: $install"
