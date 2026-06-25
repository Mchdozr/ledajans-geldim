# Plesk Git pull SONRASI — GitHub Actions deploy branch icin
# Plesk > Git > Ek deployment eylemleri:
#   powershell.exe -ExecutionPolicy Bypass -File "C:\Inetpub\vhosts\...\git\ledajans-geldim\scripts\plesk-after-pull.ps1"
#
# ONEMLI: Pull oncesi mutlaka plesk-pre-pull.ps1 calistirin (DLL kilidi)

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
    $site = Get-Website | Where-Object { $_.physicalPath -eq $SiteRoot -or $SiteRoot -like "$($_.physicalPath)*" } | Select-Object -First 1
    if ($site) { return $site.applicationPool }
    return $null
}

$siteRoot = $env:PLESK_HTTPDOCS
if ([string]::IsNullOrWhiteSpace($siteRoot)) {
    $siteRoot = Find-SiteRoot -DomainName $Domain
}
if (-not $siteRoot -or -not (Test-Path $siteRoot)) {
    Write-Host "HATA: Site klasoru bulunamadi." -ForegroundColor Red
    exit 1
}

Write-Host "Site: $siteRoot" -ForegroundColor Cyan

# web.config geri yukle (deploy branch icermez)
$configPath = Join-Path $siteRoot "web.config"
$backupPath = Join-Path $siteRoot "web.config.backup"
$templatePath = Join-Path $PSScriptRoot "plesk-web.config"

if (-not (Test-Path $configPath)) {
    if (Test-Path $backupPath) {
        Copy-Item $backupPath $configPath -Force
        Write-Host "web.config yedekten geri yuklendi." -ForegroundColor Green
    } elseif (Test-Path $templatePath) {
        Copy-Item $templatePath $configPath -Force
        Write-Host "web.config sablondan olusturuldu — sifreleri duzenleyin!" -ForegroundColor Yellow
    }
} elseif (Test-Path $configPath) {
    Copy-Item $configPath $backupPath -Force
}

# Bakim modunu kaldir
$offlinePath = Join-Path $siteRoot "app_offline.htm"
if (Test-Path $offlinePath) {
    Remove-Item $offlinePath -Force
    Write-Host "app_offline.htm kaldirildi." -ForegroundColor Green
}

# logs klasoru
New-Item -ItemType Directory -Path (Join-Path $siteRoot "logs") -Force | Out-Null

# App pool yeniden baslat
Import-Module WebAdministration -ErrorAction SilentlyContinue
$poolName = Get-AppPoolName -SiteRoot $siteRoot
if ($poolName) {
    try {
        Start-WebAppPool -Name $poolName -ErrorAction SilentlyContinue
        Restart-WebAppPool -Name $poolName
        Write-Host "App pool yenilendi: $poolName" -ForegroundColor Green
    } catch {
        Write-Host "App pool yenilenemedi: $_" -ForegroundColor Yellow
        Write-Host "Plesk > IIS Application Pool > Geri Donustur yapin." -ForegroundColor Yellow
    }
}

$versionPath = Join-Path $siteRoot "wwwroot\version.txt"
if (-not (Test-Path $versionPath)) {
    $versionPath = Join-Path $siteRoot "version.txt"
}
if (Test-Path $versionPath) {
    $ver = Get-Content $versionPath -Raw
    Write-Host "Deploy surumu: $ver" -ForegroundColor Green
}

Write-Host "Tamam." -ForegroundColor Green
