param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repo 'src/AbigailModern'
$manifest = Get-Content (Join-Path $source 'manifest.json') -Raw | ConvertFrom-Json
$registry = Get-Content (Join-Path $source 'artwork.json') -Raw | ConvertFrom-Json
$stage = Join-Path $repo ('artifacts/package-staging/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '/AbigailModern')
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$files = @('manifest.json', 'artwork.json', 'README.md') + @($registry.File | Sort-Object -Unique)
foreach ($relative in $files) {
    if ([IO.Path]::IsPathRooted($relative) -or ($relative -split '[/\\]') -contains '..') { throw "Unsafe asset path: $relative" }
    $destination = Join-Path $stage $relative
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $source $relative) -Destination $destination
}
Copy-Item -LiteralPath (Join-Path $source "bin/$Configuration/net6.0/AbigailModern.dll") -Destination $stage
$files += 'AbigailModern.dll'
$zipPath = Join-Path $repo "dist/NpcModern-$($manifest.Version).zip"
New-Item -ItemType Directory -Path (Split-Path $zipPath -Parent) -Force | Out-Null
Compress-Archive -LiteralPath $stage -DestinationPath $zipPath -Force
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($archive.Entries | Where-Object { $_.Name })
    if ($entries.Count -ne $files.Count) { throw 'Archive file count differs from staged files.' }
    foreach ($relative in $files) {
        $entry = $archive.GetEntry('AbigailModern/' + $relative.Replace('\', '/'))
        if (!$entry) { throw "Archive missing $relative" }
        $stream = $entry.Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = [Convert]::ToHexString($sha.ComputeHash($stream)) }
        finally { $stream.Dispose(); $sha.Dispose() }
        if ($hash -ne (Get-FileHash -LiteralPath (Join-Path $stage $relative)).Hash) { throw "Archive checksum mismatch: $relative" }
    }
} finally { $archive.Dispose() }
Write-Output "Packaged and verified $($files.Count) files: $zipPath"
