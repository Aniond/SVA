function Get-PlayerHdFiles([string]$Source) {
    $manifestPath = Join-Path $Source 'player-hd.json'
    if (!(Test-Path -LiteralPath $manifestPath)) { return @() }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $files = @('player-hd.json')
    foreach ($entry in $manifest.Assets) {
        foreach ($relative in @($entry.File, $entry.ShadeFile)) {
            if (!$relative) { continue }
            if ([IO.Path]::IsPathRooted($relative) -or ($relative -split '[/\\]') -contains '..') { throw 'Unsafe HD asset path.' }
            $files += $relative
        }
    }
    return @($files | Sort-Object -Unique)
}

function Get-ModernClothingFiles([string]$Source) {
    $catalogPath = Join-Path $Source 'assets/modern-clothing.json'
    if (!(Test-Path -LiteralPath $catalogPath)) { return @() }
    $catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
    $files = @('assets/modern-clothing.json')
    foreach ($texture in $catalog.Textures) {
        $relative = $texture.File
        if (!$relative -or [IO.Path]::IsPathRooted($relative) -or ($relative -split '[/\\]') -contains '..') { throw 'Unsafe clothing asset path.' }
        if (!(Test-Path -LiteralPath (Join-Path $Source $relative))) { throw "Missing clothing texture: $relative" }
        $files += $relative
    }
    return @($files | Sort-Object -Unique)
}
