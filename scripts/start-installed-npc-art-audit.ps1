param([string]$GamePath = 'C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley')
$ErrorActionPreference = 'Stop'
$npcRepo = Split-Path -Parent $PSScriptRoot
if (@(Get-Process -Name StardewModdingAPI,StardewValley,'Stardew Valley' -ErrorAction SilentlyContinue).Count) { throw 'Game already running; leave it open and defer the ordinary launch.' }
$npcVersion = (Get-Content -LiteralPath (Join-Path $npcRepo 'src/AbigailModern/manifest.json') -Raw | ConvertFrom-Json).Version
$npcInstalledVersion = (Get-Content -LiteralPath (Join-Path $GamePath 'Mods/AbigailModern/manifest.json') -Raw | ConvertFrom-Json).Version
if ($npcInstalledVersion -ne $npcVersion) { throw 'Install the current source version before launching.' }
$npcSession = Join-Path $npcRepo ('artifacts/npc-modern/installed-audits/' + $npcVersion + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path (Join-Path $npcSession 'logs-before') | Out-Null
Get-ChildItem -LiteralPath (Join-Path $env:APPDATA 'StardewValley/ErrorLogs') -Filter 'SMAPI*.txt' -ErrorAction SilentlyContinue | Copy-Item -Destination (Join-Path $npcSession 'logs-before')
$npcStarted = Get-Date
$npcProcess = Start-Process -FilePath (Join-Path $GamePath 'StardewModdingAPI.exe') -WorkingDirectory $GamePath -ArgumentList '--no-terminal' -WindowStyle Hidden -PassThru
$npcRecord = [ordered]@{ Version=$npcVersion; Pid=$npcProcess.Id; StartedAt=$npcStarted.ToString('o'); Session=$npcSession; ModsPath=(Join-Path $GamePath 'Mods'); InstalledIntoGame=$true; FarmLoaded=$false }
$npcRecord | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $npcSession 'session.json')
$npcRecord | ConvertTo-Json
