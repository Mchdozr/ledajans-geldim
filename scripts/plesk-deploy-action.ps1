# Site kokundeki scripts/ klasorunden calisir (deploy branch ile gelir)
$SiteRoot = Split-Path -Parent $PSScriptRoot
$PersistScript = Join-Path $PSScriptRoot "plesk-persist-webconfig.ps1"

if (Test-Path $PersistScript) {
    & $PersistScript -SiteRoot $SiteRoot
}

Remove-Item (Join-Path $SiteRoot "app_offline.htm") -ErrorAction SilentlyContinue

$appcmd = "$env:windir\system32\inetsrv\appcmd.exe"
$pool = "geldim.ledajans.com(domain)(4.0)(pool)"
if (Test-Path $appcmd) {
    & $appcmd start apppool $pool 2>$null
    & $appcmd recycle apppool /apppool.name:$pool 2>$null
}
