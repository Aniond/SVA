param([Parameter(Mandatory)][string]$Session,[int]$TimeoutSeconds=900)
$ErrorActionPreference='Stop'
$record=Get-Content -LiteralPath (Join-Path $Session 'process.json') -Raw | ConvertFrom-Json
$deadline=(Get-Date).AddSeconds($TimeoutSeconds)
$peak=0L
while((Get-Date) -lt $deadline) {
    $owned=Get-Process -Id $record.Pid -ErrorAction SilentlyContinue
    if(!$owned){break}
    if($owned.ProcessName -ne 'StardewModdingAPI' -or [Math]::Abs(($owned.StartTime-[datetime]$record.StartedAt).TotalSeconds) -gt 10){throw 'Process identity changed.'}
    $peak=[Math]::Max($peak,$owned.PrivateMemorySize64)
    $reason=$null
    if($owned.PrivateMemorySize64 -gt 8GB){$reason='8 GiB process limit'}
    if((Get-CimInstance Win32_OperatingSystem).FreeVirtualMemory -lt 3MB){$reason='3 GiB system headroom'}
    if($reason){Stop-Process -Id $owned.Id; @{Stopped=$true;Reason=$reason;PeakPrivateBytes=$peak}|ConvertTo-Json|Set-Content (Join-Path $Session 'watchdog.json');exit 1}
    Start-Sleep -Seconds 2
}
$owned=Get-Process -Id $record.Pid -ErrorAction SilentlyContinue
if($owned -and $owned.ProcessName -eq 'StardewModdingAPI' -and [Math]::Abs(($owned.StartTime-[datetime]$record.StartedAt).TotalSeconds) -lt 10){Stop-Process -Id $owned.Id; $reason='Time limit'}
@{Stopped=($null -ne $reason);Reason=$reason;PeakPrivateBytes=$peak}|ConvertTo-Json|Set-Content (Join-Path $Session 'watchdog.json')
