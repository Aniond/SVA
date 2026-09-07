param([Parameter(Mandatory)][string]$Session,[string]$GamePath='C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley')
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$sessionPath=[IO.Path]::GetFullPath($Session)
$allowed=[IO.Path]::GetFullPath((Join-Path $repo 'artifacts/new-game-persistence'))+[IO.Path]::DirectorySeparatorChar
if(!$sessionPath.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Disposable session must be inside the persistence audit folder.'}
$mods=Join-Path $sessionPath 'Mods'
foreach($modName in @('AbigailModern','SolaceWeather')) {if(!(Test-Path -LiteralPath (Join-Path $mods "$modName/manifest.json"))){throw "Missing copied installation: $modName"}}
$audit=Join-Path $mods 'SvaPersistenceAudit'
New-Item -ItemType Directory -Force $audit | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'tests/SvaPersistenceAudit/bin/Release/net6.0/SvaPersistenceAudit.dll'),(Join-Path $repo 'tests/SvaPersistenceAudit/manifest.json') -Destination $audit
(Join-Path $sessionPath 'Profile') | Set-Content -LiteralPath (Join-Path $audit 'profile-root.txt')
$started=Get-Date
$process=Start-Process -FilePath (Join-Path $GamePath 'StardewModdingAPI.exe') -WorkingDirectory $GamePath -ArgumentList @('--mods-path',('"'+$mods+'"'),'--no-terminal') -WindowStyle Hidden -PassThru
@{Pid=$process.Id;StartedAt=$started.ToString('o');Session=$sessionPath;AuditFolder=$audit}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $sessionPath 'process.json')
Start-Process -FilePath 'pwsh' -ArgumentList @('-NoProfile','-File',('"'+(Join-Path $PSScriptRoot 'watch-sva-persistence.ps1')+'"'),'-Session',('"'+$sessionPath+'"')) -WindowStyle Hidden | Out-Null
Get-Content -LiteralPath (Join-Path $sessionPath 'process.json')
