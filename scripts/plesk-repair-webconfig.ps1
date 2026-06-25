# web.config IIS handler duzeltmesi — POST 405 hatasi icin
param(
    [string]$SiteRoot = "C:\Plesk Vhosts\ledajans.com\geldim.ledajans.com"
)

$ErrorActionPreference = "Stop"
$configPath = Join-Path $SiteRoot "web.config"

if (-not (Test-Path $configPath)) {
    Write-Host "web.config bulunamadi: $configPath" -ForegroundColor Red
    exit 1
}

[xml]$xml = Get-Content $configPath
$webServer = $xml.configuration.location.system.webServer
if (-not $webServer) {
    Write-Host "system.webServer bulunamadi" -ForegroundColor Red
    exit 1
}

# WebDAV kaldir
$modules = $webServer.modules
if (-not $modules) {
    $modules = $xml.CreateElement("modules")
    [void]$webServer.AppendChild($modules)
}
$removeMod = $xml.CreateElement("remove")
$removeMod.SetAttribute("name", "WebDAVModule")
[void]$modules.AppendChild($removeMod)

$handlers = $webServer.handlers
if (-not $handlers) {
    $handlers = $xml.CreateElement("handlers")
    [void]$webServer.AppendChild($handlers)
}

# Eski handlerlari temizle
$toRemove = @($handlers.ChildNodes | Where-Object { $_.Name -in @("add", "remove", "clear") })
foreach ($node in $toRemove) { [void]$handlers.RemoveChild($node) }

$removeDav = $xml.CreateElement("remove")
$removeDav.SetAttribute("name", "WebDAV")
[void]$handlers.AppendChild($removeDav)

$add = $xml.CreateElement("add")
$add.SetAttribute("name", "aspNetCore")
$add.SetAttribute("path", "*")
$add.SetAttribute("verb", "*")
$add.SetAttribute("modules", "AspNetCoreModuleV2")
$add.SetAttribute("resourceType", "Unspecified")
[void]$handlers.AppendChild($add)

$xml.Save($configPath)
Write-Host "web.config duzeltildi: path=* verb=* WebDAV kaldirildi" -ForegroundColor Green

$pool = "geldim.ledajans.com(domain)(4.0)(pool)"
$appcmd = "$env:windir\system32\inetsrv\appcmd.exe"
if (Test-Path $appcmd) {
    & $appcmd recycle apppool "/apppool.name:$pool"
    Write-Host "App pool yenilendi: $pool" -ForegroundColor Green
}
