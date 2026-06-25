# web.config kalici yedek — deploy klasoru disinda tutulur, git pull silmez
param(
    [string]$SiteRoot = "C:\Plesk Vhosts\ledajans.com\geldim.ledajans.com",
    [string]$PersistPath = "C:\Ledajans\config\web.config"
)

$ErrorActionPreference = "Continue"
New-Item -ItemType Directory -Path (Split-Path $PersistPath -Parent) -Force | Out-Null

$configPath = Join-Path $SiteRoot "web.config"
$backupPath = Join-Path $SiteRoot "web.config.backup"

if (Test-Path $configPath) {
    Copy-Item $configPath $PersistPath -Force
    Copy-Item $configPath $backupPath -Force
    Write-Host "web.config yedeklendi: $PersistPath"
    exit 0
}

if (Test-Path $PersistPath) {
    Copy-Item $PersistPath $configPath -Force
    Copy-Item $PersistPath $backupPath -Force
    Write-Host "web.config geri yuklendi: $PersistPath -> $configPath"
    exit 0
}

if (Test-Path $backupPath) {
    Copy-Item $backupPath $configPath -Force
    Copy-Item $backupPath $PersistPath -Force
    Write-Host "web.config site yedeginden geri yuklendi."
    exit 0
}

Write-Host "web.config bulunamadi. Elle olusturun." -ForegroundColor Yellow
exit 1
