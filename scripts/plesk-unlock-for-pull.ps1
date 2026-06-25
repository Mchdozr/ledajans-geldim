# Pull ONCESI calistirin — IIS DLL kilidini acar
# Yonetici PowerShell:
#   powershell.exe -ExecutionPolicy Bypass -File "C:\Plesk Vhosts\ledajans.com\geldim.ledajans.com\scripts\plesk-unlock-for-pull.ps1"

$ErrorActionPreference = "Continue"

function Resolve-SiteRoot {
    if ($PSScriptRoot) {
        $parent = Split-Path -Parent $PSScriptRoot
        if (Test-Path (Join-Path $parent "Ledajans.Server.dll")) {
            return $parent
        }
    }
    return "C:\Plesk Vhosts\ledajans.com\geldim.ledajans.com"
}

$SiteRoot = Resolve-SiteRoot
$PoolName = "geldim.ledajans.com(domain)(4.0)(pool)"
$AppCmd = "$env:windir\system32\inetsrv\appcmd.exe"

if (-not (Test-Path $SiteRoot)) {
    Write-Host "HATA: Site klasoru yok: $SiteRoot" -ForegroundColor Red
    exit 1
}

Write-Host "Site: $SiteRoot" -ForegroundColor Cyan

$persistScript = Join-Path $PSScriptRoot "plesk-persist-webconfig.ps1"
if (Test-Path $persistScript) {
    & $persistScript -SiteRoot $SiteRoot
}

$offlinePath = Join-Path $SiteRoot "app_offline.htm"
@"<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>Guncelleniyor</title></head>
<body><p>Ledajans guncelleniyor, lutfen bekleyin...</p></body></html>
"@ | Set-Content -Path $offlinePath -Encoding UTF8
Write-Host "app_offline.htm olusturuldu." -ForegroundColor Green

if (Test-Path $AppCmd) {
    & $AppCmd stop apppool "/apppool.name:$PoolName" 2>$null
    Write-Host "App pool durduruldu: $PoolName" -ForegroundColor Green
    Start-Sleep -Seconds 10
} else {
    Write-Host "appcmd bulunamadi — sadece app_offline kullanildi." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
}

Write-Host ""
Write-Host "Simdi Plesk > Git > Pull Now yapin." -ForegroundColor Cyan
Write-Host "Pull basarili olunca Deploy Now (veya otomatik deploy eylemi)." -ForegroundColor Cyan
