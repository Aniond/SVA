param(
    [string]$GamePath = 'C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley',
    [string[]]$SelectedChecks = @()
)
$ErrorActionPreference = 'Stop'
$npcRepo = Split-Path -Parent $PSScriptRoot
$npcSource = Join-Path $npcRepo 'src/AbigailModern'
$npcVersion = (Get-Content (Join-Path $npcSource 'manifest.json') -Raw | ConvertFrom-Json).Version
$npcPackage = Join-Path $npcRepo "dist/NpcModern-$npcVersion.zip"
$npcAuditDll = Join-Path $npcRepo 'src/NpcArtAudit/bin/Release/net6.0/NpcArtAudit.dll'
if (!(Test-Path -LiteralPath $npcPackage) -or !(Test-Path -LiteralPath $npcAuditDll)) { throw 'Build and package the artwork and audit first.' }
$npcSession = Join-Path $npcRepo ('artifacts/npc-modern/runtime-audits/' + $npcVersion + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$npcMods = Join-Path $npcSession 'Mods'
New-Item -ItemType Directory -Path $npcMods | Out-Null
$npcArchive = [IO.Compression.ZipFile]::OpenRead($npcPackage)
try {
    $npcPrefix = [IO.Path]::GetFullPath($npcMods) + [IO.Path]::DirectorySeparatorChar
    foreach ($npcEntry in $npcArchive.Entries) {
        $npcTarget = [IO.Path]::GetFullPath((Join-Path $npcMods $npcEntry.FullName))
        if (!$npcTarget.StartsWith($npcPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe package entry.' }
    }
} finally { $npcArchive.Dispose() }
[IO.Compression.ZipFile]::ExtractToDirectory($npcPackage, $npcMods)
$npcExtracted = Join-Path $npcMods 'AbigailModern'
$npcAudioPreferences = Join-Path $GamePath 'Mods/AbigailModern/audio-preferences.json'
if (Test-Path -LiteralPath $npcAudioPreferences) { Copy-Item -LiteralPath $npcAudioPreferences -Destination (Join-Path $npcExtracted 'audio-preferences.json') }
$npcFiles = @('manifest.json', 'artwork.json', 'README.md') + @((Get-Content (Join-Path $npcSource 'artwork.json') -Raw | ConvertFrom-Json).File | Sort-Object -Unique)
. (Join-Path $PSScriptRoot 'get-player-hd-files.ps1')
$npcFiles += @(Get-PlayerHdFiles $npcSource)
$npcFiles += @(Get-ModernClothingFiles $npcSource)
foreach ($npcFile in $npcFiles) {
    if ((Get-FileHash -LiteralPath (Join-Path $npcSource $npcFile)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $npcExtracted $npcFile)).Hash) { throw "Package is stale: $npcFile" }
}
if ((Get-FileHash -LiteralPath (Join-Path $npcSource 'bin/Release/net6.0/AbigailModern.dll')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $npcExtracted 'AbigailModern.dll')).Hash) { throw 'Packaged DLL is stale.' }
$npcAuditFolder = Join-Path $npcMods 'NpcArtAudit'
New-Item -ItemType Directory -Path $npcAuditFolder | Out-Null
if ($SelectedChecks.Count -gt 0) { ConvertTo-Json -InputObject @($SelectedChecks) | Set-Content -LiteralPath (Join-Path $npcAuditFolder 'selected-checks.json') }
Copy-Item -LiteralPath $npcAuditDll,(Join-Path $npcRepo 'src/NpcArtAudit/manifest.json') -Destination $npcAuditFolder
if ($SelectedChecks -contains 'blue-ui') {
    $npcSolaceFixtures = Join-Path $npcAuditFolder 'solace-fixtures'
    New-Item -ItemType Directory -Path $npcSolaceFixtures | Out-Null
    foreach ($npcFixtureDll in @('SolaceWeather.Core.dll','SolaceWeather.dll')) {
        Copy-Item -LiteralPath (Join-Path $GamePath ('Mods/SolaceWeather/' + $npcFixtureDll)) -Destination $npcSolaceFixtures
    }
}
Copy-Item -LiteralPath (Join-Path $npcRepo 'src/NpcArtAudit/joja-opening-runtime-evidence.json') -Destination $npcAuditFolder
foreach ($npcContract in @('locations-map-names.json','locations-atlas-sizes.json')) {
    Copy-Item -LiteralPath (Join-Path $npcRepo ('src/NpcArtAudit/Contracts/' + $npcContract)) -Destination $npcAuditFolder
}
$npcLogBackup = Join-Path $npcSession 'logs-before'
New-Item -ItemType Directory -Path $npcLogBackup | Out-Null
Get-ChildItem -LiteralPath (Join-Path $env:APPDATA 'StardewValley/ErrorLogs') -Filter 'SMAPI*.txt' -ErrorAction SilentlyContinue | Copy-Item -Destination $npcLogBackup
$npcExistingProcesses = @(Get-Process -Name StardewModdingAPI,StardewValley,'Stardew Valley' -ErrorAction SilentlyContinue | Select-Object Id,StartTime)
$npcStartedAt = Get-Date
$npcProcess = Start-Process -FilePath (Join-Path $GamePath 'StardewModdingAPI.exe') -WorkingDirectory $GamePath -ArgumentList @('--mods-path', ('"' + $npcMods + '"'), '--no-terminal') -WindowStyle Hidden -PassThru
$npcRecord = [ordered]@{ Version=$npcVersion; Pid=$npcProcess.Id; StartedAt=$npcStartedAt.ToString('o'); Session=$npcSession; ModsPath=$npcMods; AuditFolder=$npcAuditFolder; ExistingProcesses=$npcExistingProcesses; InstalledIntoGame=$false; FarmLoaded=$false }
$npcRecord | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $npcSession 'session.json')
$npcRecord | ConvertTo-Json -Depth 4

