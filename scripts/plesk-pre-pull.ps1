# Plesk Git pull ONCESI calistirin — DLL kilidini acar
# Kullanim (sunucuda Yonetici PowerShell):
#   powershell.exe -ExecutionPolicy Bypass -File C:\path\to\repo\scripts\plesk-pre-pull.ps1
#
# Sonra Plesk > Git > Guncellemeleri cek / Simdi dagit

param(
    [string]$Domain = "geldim.ledajans.com"
)

$ErrorActionPreference = "Continue"

function Find-SiteRoot {
    param([string]$DomainName)
    $pleskPaths = @(
        "${env:ProgramFiles(x86)}\Plesk\admin\bin\plesk.exe",
        "${env:ProgramFiles(x86)}\Parallels\Plesk\admin\bin\plesk.exe",
        "C:\Plesk Dir\admin\bin\plesk.exe"
    )
    foreach ($plesk in $pleskPaths) {
        if (-not (Test-Path $plesk)) { continue }
        $out = & $plesk bin site --info $DomainName 2>&1 | Out-String
        if ($out -match "WWW-Root:\s*(.+)") { return $Matches[1].Trim() }
        if ($out -match "Document root:\s*(.+)") { return $Matches[1].Trim() }
    }
  foreach ($vhosts in @("C:\Inetpub\vhosts", "D:\Inetpub\vhosts", "C:\Plesk Dir\vhosts")) {
        if (-not (Test-Path $vhosts)) { continue }
        Get-ChildItem $vhosts -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            $candidates = @(
                (Join-Path $_.FullName $DomainName),
                (Join-Path $_.FullName "httpdocs\$DomainName"),
                (Join-Path $_.FullName "subdomains\geldim\httpdocs")
            )
            foreach ($c in $candidates) {
                if (Test-Path $c) { return $c }
            }
        }
    }
    return $null
}

function Get-AppPoolName {
    param([string]$SiteRoot)
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    if (-not (Get-Module WebAdministration)) { return $null }
    $siteName = Get-Website | Where-Object { $_.physicalPath -eq $SiteRoot -or $SiteRoot -like "$($_.physicalPath)*" } | Select-Object -First 1
    if ($siteName) { return $siteName.applicationPool }
    return $null
}

$siteRoot = $env:PLESK_HTTPDOCS
if ([string]::IsNullOrWhiteSpace($siteRoot)) {
    $siteRoot = Find-SiteRoot -DomainName $Domain
}
if (-not $siteRoot -or -not (Test-Path $siteRoot)) {
    Write-Host "HATA: Site klasoru bulunamadi. -Domain parametresi veya PLESK_HTTPDOCS ayarlayin." -ForegroundColor Red
    exit 1
}

Write-Host "Site: $siteRoot" -ForegroundColor Cyan

# web.config yedegi (deploy branch web.config icermez)
$configPath = Join-Path $siteRoot "web.config"
$backupPath = Join-Path $siteRoot "web.config.backup"
if ((Test-Path $configPath) -and -not (Test-Path $backupPath)) {
    Copy-Item $configPath $backupPath -Force
    Write-Host "web.config -> web.config.backup yedeklendi." -ForegroundColor Green
}

# IIS uygulamayi bosaltir, DLL kilitleri acilir
$offlinePath = Join-Path $siteRoot "app_offline.htm"
@"
<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>Guncelleniyor</title></head>
<body><p>Ledajans guncelleniyor, lutfen bekleyin...</p></body></html>
"@ | Set-Content -Path $offlinePath -Encoding UTF8
Write-Host "app_offline.htm olusturuldu." -ForegroundColor Green

Import-Module WebAdministration -ErrorAction SilentlyContinue
$poolName = Get-AppPoolName -SiteRoot $siteRoot
if ($poolName) {
    try {
        Stop-WebAppPool -Name $poolName -ErrorAction Stop
        Write-Host "App pool durduruldu: $poolName" -ForegroundColor Green
    } catch {
        Write-Host "App pool durdurulamadi (devam ediliyor): $_" -ForegroundColor Yellow
    }
} else {
    Write-Host "App pool bulunamadi — app_offline.htm yeterli olabilir." -ForegroundColor Yellow
}

Start-Sleep -Seconds 5
Write-Host ""
Write-Host "Simdi Plesk > Git > Guncellemeleri cek yapin." -ForegroundColor Cyan
Write-Host "Basarili olunca plesk-after-pull.ps1 otomatik veya manuel calistirin." -ForegroundColor Cyan
