# Tek komut canli deploy — GitHub deploy branch zip (Plesk Git pull gerekmez)
# Kullanim (Yonetici PowerShell):
#   powershell.exe -ExecutionPolicy Bypass -File "C:\Ledajans\canliya-al.ps1"

$ErrorActionPreference = "Stop"

$SiteRoot = "C:\Plesk Vhosts\ledajans.com\geldim.ledajans.com"
$PersistConfig = "C:\Ledajans\config\web.config"
$PoolName = "geldim.ledajans.com(domain)(4.0)(pool)"
$AppCmd = "$env:windir\system32\inetsrv\appcmd.exe"
$ZipUrl = "https://github.com/Mchdozr/ledajans-geldim/archive/refs/heads/deploy.zip"
$TempZip = "$env:TEMP\geldim-deploy.zip"
$TempExtract = "$env:TEMP\geldim-deploy-extract"

Write-Host "=== Ledajans canliya al ===" -ForegroundColor Cyan

# 1) Siteyi durdur
$html = "<!DOCTYPE html><html><body><p>Guncelleniyor...</p></body></html>"
Set-Content -Path (Join-Path $SiteRoot "app_offline.htm") -Value $html -Encoding UTF8
if (Test-Path $AppCmd) {
    & $AppCmd stop apppool "/apppool.name:$PoolName" 2>$null
}
Start-Sleep -Seconds 8
Write-Host "[1/5] App pool durduruldu" -ForegroundColor Green

# 2) web.config yedekle
$siteConfig = Join-Path $SiteRoot "web.config"
if (Test-Path $siteConfig) {
    New-Item -ItemType Directory -Path (Split-Path $PersistConfig -Parent) -Force | Out-Null
    Copy-Item $siteConfig $PersistConfig -Force
}

# 3) GitHub'dan deploy indir
Write-Host "[2/5] Deploy indiriliyor..." -ForegroundColor Yellow
Invoke-WebRequest -Uri $ZipUrl -OutFile $TempZip -UseBasicParsing
if (Test-Path $TempExtract) { Remove-Item $TempExtract -Recurse -Force }
Expand-Archive -Path $TempZip -DestinationPath $TempExtract -Force
$sourceDir = Get-ChildItem $TempExtract -Directory | Select-Object -First 1
if (-not $sourceDir) { throw "Zip bos" }

# 4) Dosyalari kopyala (web.config haric)
Write-Host "[3/5] Dosyalar kopyalaniyor..." -ForegroundColor Yellow
Get-ChildItem $SiteRoot -Force | Where-Object { $_.Name -notin @("web.config", "logs") } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -Path "$($sourceDir.FullName)\*" -Destination $SiteRoot -Recurse -Force
New-Item -ItemType Directory -Path (Join-Path $SiteRoot "logs") -Force | Out-Null

# 5) web.config geri yukle
if (Test-Path $PersistConfig) {
    Copy-Item $PersistConfig $siteConfig -Force
    Write-Host "[4/5] web.config geri yuklendi" -ForegroundColor Green
} else {
    Write-Host "[4/5] UYARI: web.config yedegi yok!" -ForegroundColor Red
}

Remove-Item $TempZip -Force -ErrorAction SilentlyContinue
Remove-Item $TempExtract -Recurse -Force -ErrorAction SilentlyContinue

# 6) Siteyi ac
Remove-Item (Join-Path $SiteRoot "app_offline.htm") -ErrorAction SilentlyContinue
if (Test-Path $AppCmd) {
    & $AppCmd start apppool "/apppool.name:$PoolName" 2>$null
    Start-Sleep -Seconds 2
    & $AppCmd recycle apppool "/apppool.name:$PoolName" 2>$null
}
Write-Host "[5/5] App pool baslatildi" -ForegroundColor Green

$versionFile = Join-Path $SiteRoot "wwwroot\version.txt"
if (Test-Path $versionFile) {
    $ver = Get-Content $versionFile -Raw
    Write-Host "Surum: $ver" -ForegroundColor Cyan
}
Write-Host "Tamam -> https://geldim.ledajans.com" -ForegroundColor Green
