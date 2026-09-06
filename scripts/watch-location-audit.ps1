param([Parameter(Mandatory)][string]$Session,[int]$TimeoutSeconds=300,[int]$MemoryLimitMiB=8192,[ValidatePattern('^[a-z0-9-]+$')][string]$CheckName='locations')
$ErrorActionPreference='Stop'
$npcRepo=Split-Path -Parent $PSScriptRoot
$npcRecord=Get-Content -LiteralPath (Join-Path $npcRepo "$Session/session.json") -Raw | ConvertFrom-Json
$npcEnd=(Get-Date).AddSeconds($TimeoutSeconds)
$npcPeak=0L
while((Get-Date) -lt $npcEnd) {
    $npcProcess=Get-Process -Id $npcRecord.Pid -ErrorAction SilentlyContinue
    if(!$npcProcess){throw 'Owned audit process exited before completion.'}
    if($npcProcess.ProcessName -notin @('StardewModdingAPI','StardewValley','Stardew Valley') -or [Math]::Abs(($npcProcess.StartTime-[datetime]$npcRecord.StartedAt).TotalSeconds) -gt 10){throw 'Owned process identity changed.'}
    $npcPeak=[Math]::Max($npcPeak,$npcProcess.PrivateMemorySize64)
    if($npcProcess.PrivateMemorySize64 -gt $MemoryLimitMiB*1MB){Stop-Process -Id $npcProcess.Id;throw "Owned audit stopped at memory limit; peak $npcPeak bytes."}
    $npcMemory=Get-CimInstance Win32_OperatingSystem
    if($npcMemory.FreeVirtualMemory -lt 3MB){Stop-Process -Id $npcProcess.Id;throw 'Owned audit stopped to retain 3 GiB of system virtual-memory headroom.'}
    $npcResult=Join-Path $npcRecord.AuditFolder "$CheckName-checks.json"
    if((Test-Path -LiteralPath $npcResult) -and (Get-Item -LiteralPath $npcResult).LastWriteTime -ge [datetime]$npcRecord.StartedAt){
        try{$npcCheck=Get-Content -LiteralPath $npcResult -Raw | ConvertFrom-Json}catch{Start-Sleep -Milliseconds 500;continue}
        if(!$npcCheck.Running){[pscustomobject]@{Passed=$npcCheck.Passed;Error=$npcCheck.Error;PeakPrivateBytes=$npcPeak;NativeMapsChecked=$npcCheck.NativeMapsChecked}|ConvertTo-Json;exit}
    }
    Start-Sleep -Milliseconds 500
}
throw 'Audit watchdog timeout; inspect owned process and progress before continuing.'
