param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$output = Join-Path $projectRoot "src\SolaceWeather\bin\$Configuration\net6.0"
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'src\SolaceWeather\manifest.json') -Raw | ConvertFrom-Json
$destination = Join-Path $projectRoot 'dist\SolaceWeather'
$zip = Join-Path $projectRoot "dist\SolaceWeather-$($manifest.Version).zip"
foreach ($required in @('SolaceWeather.dll','SolaceWeather.Core.dll','manifest.json','i18n','assets')) {
    if (-not (Test-Path -LiteralPath (Join-Path $output $required))) { throw "Missing build output: $required. Build $Configuration first." }
}
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach ($file in @('SolaceWeather.dll','SolaceWeather.Core.dll','manifest.json')) {
    Copy-Item -LiteralPath (Join-Path $output $file) -Destination $destination -Force
}
foreach ($directory in @('i18n','assets')) {
    Copy-Item -LiteralPath (Join-Path $output $directory) -Destination $destination -Recurse -Force
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $destination -Force
New-Item -ItemType Directory -Path (Join-Path $destination 'docs') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\testing.md') -Destination (Join-Path $destination 'docs') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\abigail-delivery-quest.md') -Destination (Join-Path $destination 'docs') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\abigail-relationship-tree.md') -Destination (Join-Path $destination 'docs') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\global-romance.md') -Destination (Join-Path $destination 'docs') -Force
Compress-Archive -LiteralPath $destination -DestinationPath $zip -Force
Write-Output "Packaged: $zip"
