param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$output = Join-Path $projectRoot "src\SolaceWeather\bin\$Configuration\net6.0"
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'src\SolaceWeather\manifest.json') -Raw | ConvertFrom-Json
$destination = Join-Path $projectRoot 'dist\SolaceWeather'
$zip = Join-Path $projectRoot "dist\SolaceWeather-$($manifest.Version).zip"
foreach ($required in @('SolaceWeather.dll','SolaceWeather.Core.dll','System.Speech.dll','manifest.json','i18n','assets')) {
    if (-not (Test-Path -LiteralPath (Join-Path $output $required))) { throw "Missing build output: $required. Build $Configuration first." }
}
if (Test-Path -LiteralPath $destination) {
    $resolvedStage = [IO.Path]::GetFullPath($destination)
    $expectedStage = [IO.Path]::GetFullPath((Join-Path $projectRoot 'dist\SolaceWeather'))
    if ($resolvedStage -ne $expectedStage) { throw 'Unexpected package staging path.' }
    $previousStage = Join-Path $projectRoot ('artifacts/package-staging/SolaceWeather-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory -Path (Split-Path $previousStage -Parent) -Force | Out-Null
    Move-Item -LiteralPath $resolvedStage -Destination $previousStage
}
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach ($file in @('SolaceWeather.dll','SolaceWeather.Core.dll','System.Speech.dll','manifest.json')) {
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
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\phone-texting.md') -Destination (Join-Path $destination 'docs') -Force
foreach ($document in @('town-chatter.md','cemetery-investigation.md','player-portrait-proof.md','testing-haley-fashion.md','testing-emily-tailoring.md','testing-character-profiles.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('docs\' + $document)) -Destination (Join-Path $destination 'docs') -Force
}
$imageDestination = Join-Path $destination 'docs/images'
New-Item -ItemType Directory -Path $imageDestination -Force | Out-Null
foreach ($image in @('player-portrait-reference.png','player-gemini-portrait.png','player-portrait-chat.png','player-portrait-phone.png','haley-tree.png','native-fashion-comment.png','social-portraits.png','emily-tree.png','emily-tailoring-preview.png')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('docs/images/' + $image)) -Destination $imageDestination -Force
}
Compress-Archive -LiteralPath $destination -DestinationPath $zip -Force
Write-Output "Packaged: $zip"
