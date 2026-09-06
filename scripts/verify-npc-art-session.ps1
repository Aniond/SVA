param(
    [Parameter(Mandatory)][string]$Session,
    [Parameter(Mandatory)][ValidateSet('Isolated','Installed')][string]$Mode,
    [string[]]$RequiredChecks = @(),
    [string]$LogPath = "$env:APPDATA/StardewValley/ErrorLogs/SMAPI-latest.txt",
    [switch]$StopOwnedProcess
)
$ErrorActionPreference = 'Stop'
$npcRepo = Split-Path -Parent $PSScriptRoot
$npcSessionPath = [IO.Path]::GetFullPath((Join-Path $npcRepo $Session))
$npcAuditRoot = [IO.Path]::GetFullPath((Join-Path $npcRepo 'artifacts/npc-modern')) + [IO.Path]::DirectorySeparatorChar
if (!$npcSessionPath.StartsWith($npcAuditRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Session must be inside the NPC artwork workspace.' }
$npcRecord = Get-Content -LiteralPath (Join-Path $npcSessionPath 'session.json') -Raw | ConvertFrom-Json
$npcSource = Join-Path $npcRepo 'src/AbigailModern'
$npcVersion = (Get-Content -LiteralPath (Join-Path $npcSource 'manifest.json') -Raw | ConvertFrom-Json).Version
if ($npcRecord.Version -ne $npcVersion) { throw 'Session and source versions differ.' }
if ([bool]$npcRecord.InstalledIntoGame -ne ($Mode -eq 'Installed')) { throw 'Session mode differs.' }
$npcRegistry = Get-Content -LiteralPath (Join-Path $npcSource 'artwork.json') -Raw | ConvertFrom-Json
$npcCount = @($npcRegistry).Count
$npcLog = Get-Content -LiteralPath $LogPath -Raw
$npcExpectedMods = [IO.Path]::GetFullPath($npcRecord.ModsPath).Replace($env:USERPROFILE, '~')
if ((Get-Item -LiteralPath $LogPath).LastWriteTime -lt [datetime]$npcRecord.StartedAt) { throw 'Runtime log predates session.' }
if (!$npcLog.Contains("Mods go here: $npcExpectedMods") -or !$npcLog.Contains("NPC Modern $npcVersion") -or !$npcLog.Contains("Registered asset verification: $npcCount/$npcCount passed.") -or $npcLog -match 'ERROR\s+(NPC Modern|NPC Art Audit)') { throw 'Runtime log did not pass.' }
$npcEvidence = Join-Path $npcRepo 'artifacts/npc-modern/evidence'
foreach ($npcCheckName in $RequiredChecks) {
    if ($Mode -ne 'Isolated' -or $npcCheckName -notmatch '^[a-z0-9-]+$') { throw 'Invalid isolated check name.' }
    $npcCheck = Get-Content -LiteralPath (Join-Path $npcRecord.AuditFolder "$npcCheckName-checks.json") -Raw | ConvertFrom-Json
    if (!$npcCheck.Passed) { throw "Failed audit: $npcCheckName" }
    Get-ChildItem -LiteralPath $npcRecord.AuditFolder -Filter "$npcCheckName-*" -File | Copy-Item -Destination $npcEvidence -Force
}
$npcFiles = @('manifest.json','artwork.json','README.md') + @($npcRegistry.File | Sort-Object -Unique)
$npcTarget = Join-Path $npcRecord.ModsPath 'AbigailModern'
$npcHashes = [ordered]@{}
foreach ($npcFile in $npcFiles) {
    $npcHash = (Get-FileHash -LiteralPath (Join-Path $npcSource $npcFile)).Hash.ToLowerInvariant()
    if ($npcHash -ne (Get-FileHash -LiteralPath (Join-Path $npcTarget $npcFile)).Hash.ToLowerInvariant()) { throw "Source/loaded package differs: $npcFile" }
    $npcHashes[$npcFile] = $npcHash
}
$npcDllHash = (Get-FileHash -LiteralPath (Join-Path $npcSource 'bin/Release/net6.0/AbigailModern.dll')).Hash.ToLowerInvariant()
if ($npcDllHash -ne (Get-FileHash -LiteralPath (Join-Path $npcTarget 'AbigailModern.dll')).Hash.ToLowerInvariant()) { throw 'Loaded DLL differs from source build.' }
$npcHashes['AbigailModern.dll'] = $npcDllHash
$npcLowerMode = $Mode.ToLowerInvariant()
$npcEvidenceName = "loader-$npcLowerMode-$npcVersion.txt"
Copy-Item -LiteralPath $LogPath -Destination (Join-Path $npcSessionPath 'loader.txt')
Copy-Item -LiteralPath $LogPath -Destination (Join-Path $npcEvidence $npcEvidenceName)
$npcProof = [ordered]@{ Version=$npcVersion; VerifiedAt=[DateTime]::UtcNow.ToString('o'); Passed=$true; RegisteredTextures=$npcCount; InstalledIntoGame=($Mode -eq 'Installed'); RuntimeScope="$npcLowerMode-title-screen"; FarmLoaded=$false; FullScenePlayback=$false; Evidence="artifacts/npc-modern/evidence/$npcEvidenceName"; Session=[IO.Path]::GetRelativePath($npcRepo,$npcSessionPath).Replace('\','/'); AssetFileHashes=$npcHashes }
if ($Mode -eq 'Isolated') {
    $npcProof.PackageFiles=$npcHashes.Count; $npcProof.PackageChecksumsVerified=$true
    $npcProof.PackageHash=(Get-FileHash -LiteralPath (Join-Path $npcRepo "dist/NpcModern-$npcVersion.zip")).Hash.ToLowerInvariant()
    $npcProof.RegistryHash=$npcHashes['artwork.json']
} else {
    $npcProof.InstalledFilesVerified=$npcHashes.Count
    Copy-Item -LiteralPath $LogPath -Destination (Join-Path $npcEvidence 'registered-loader-check.txt')
}
$npcProof | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $npcEvidence "$npcLowerMode-$npcVersion.json")
if ($Mode -eq 'Isolated') { $npcProof | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $npcEvidence 'isolated-latest.json') }
if ($StopOwnedProcess) {
    $npcOwned=Get-Process -Id $npcRecord.Pid -ErrorAction Stop
    if ($npcOwned.ProcessName -notin @('StardewModdingAPI','StardewValley','Stardew Valley') -or [Math]::Abs(($npcOwned.StartTime-[datetime]$npcRecord.StartedAt).TotalSeconds) -gt 10) { throw 'Process identity changed; not stopping it.' }
    Stop-Process -Id $npcOwned.Id
    if (!$npcOwned.WaitForExit(5000)) { throw 'Owned verification process did not exit yet.' }
}
Write-Output "Verified $Mode $npcVersion`: $npcCount/$npcCount textures and $($npcHashes.Count) package files."
