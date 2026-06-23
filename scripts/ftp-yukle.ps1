# publish klasorunu FTP ile Plesk'e yukler
# Once: .\scripts\kolay-deploy.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$localDir = Join-Path $root "publish"

if (-not (Test-Path (Join-Path $localDir "Ledajans.Server.dll"))) {
    Write-Host "Once kolay-deploy.ps1 calistir!" -ForegroundColor Red
    exit 1
}

function New-FtpRequest($uri, $method, $cred, $usePassive, $useSsl) {
    $r = [System.Net.FtpWebRequest]::Create($uri)
    $r.Method = $method
    $r.Credentials = $cred
    $r.UseBinary = $true
    $r.UsePassive = $usePassive
    $r.EnableSsl = $useSsl
    $r.KeepAlive = $false
    return $r
}

function Get-FtpList($hostName, $path, $cred, $usePassive, $useSsl) {
    $uri = "ftp://$hostName$path"
    $r = New-FtpRequest $uri ([System.Net.WebRequestMethods+Ftp]::ListDirectoryDetails) $cred $usePassive $useSsl
    $resp = $r.GetResponse()
    $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
    $text = $reader.ReadToEnd()
    $reader.Close()
    $resp.Close()
    return $text
}

function Upload-FtpFile($hostName, $remoteFile, $localFile, $cred, $usePassive, $useSsl) {
    $uri = "ftp://$hostName$remoteFile"
    $r = New-FtpRequest $uri ([System.Net.WebRequestMethods+Ftp]::UploadFile) $cred $usePassive $useSsl
    $bytes = [System.IO.File]::ReadAllBytes($localFile)
    $r.ContentLength = $bytes.Length
    $stream = $r.GetRequestStream()
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Close()
    $resp = $r.GetResponse()
    $resp.Close()
}

function Ensure-FtpDir($hostName, $remoteDir, $cred, $usePassive, $useSsl) {
    if ([string]::IsNullOrWhiteSpace($remoteDir) -or $remoteDir -eq "/") { return }
    $parts = $remoteDir.Trim("/").Split("/")
    $path = ""
    foreach ($p in $parts) {
        $path += "/$p"
        try {
            $r = New-FtpRequest "ftp://$hostName$path" ([System.Net.WebRequestMethods+Ftp]::MakeDirectory) $cred $usePassive $useSsl
            $resp = $r.GetResponse()
            $resp.Close()
        } catch { }
    }
}

Write-Host "=== FTP Yukleme ===" -ForegroundColor Cyan
Write-Host ""

$ftpHost = Read-Host "FTP sunucu (194.36.84.221)"
$ftpUser = Read-Host "FTP kullanici (l3daj2ns)"
$ftpPass = Read-Host "FTP sifre" -AsSecureString
$ftpPassPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($ftpPass))
$cred = New-Object System.Net.NetworkCredential($ftpUser, $ftpPassPlain)

$useSsl = (Read-Host "FTPS kullan? (e/h) [h]").ToLower() -eq "e"
$usePassive = (Read-Host "Pasif mod? (e/h) [e]").ToLower() -ne "h"

Write-Host ""
Write-Host "Baglanti test ediliyor..." -ForegroundColor Yellow

try {
    $listing = Get-FtpList $ftpHost "/" $cred $usePassive $useSsl
    Write-Host "FTP kok klasorleri:" -ForegroundColor Green
    $listing -split "`n" | ForEach-Object { if ($_.Trim()) { Write-Host "  $_" } }
} catch {
    Write-Host "Baglanti BASARISIZ: $($_.Exception.Message)" -ForegroundColor Red
    if ($usePassive) {
        Write-Host "Tekrar dene: Pasif mod = h" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host ""
Write-Host "Uzak klasor ornekleri:" -ForegroundColor DarkGray
Write-Host "  /geldim.ledajans.com"
Write-Host "  /httpdocs"
Write-Host "  /  (kok - FileZilla'da geldim klasorune girince bos birak)"
Write-Host ""
$remotePath = Read-Host "Uzak klasor"
if ([string]::IsNullOrWhiteSpace($remotePath)) { $remotePath = "" }
$remotePath = $remotePath.TrimEnd("/")

# Test dosyasi
$testLocal = Join-Path $env:TEMP "ledajans-ftp-test.txt"
"ok $(Get-Date)" | Out-File $testLocal -Encoding ascii
$testRemote = if ($remotePath) { "$remotePath/ledajans-ftp-test.txt" } else { "/ledajans-ftp-test.txt" }

Write-Host ""
Write-Host "Test dosyasi yukleniyor: $testRemote" -ForegroundColor Yellow
try {
    if ($remotePath) { Ensure-FtpDir $ftpHost $remotePath $cred $usePassive $useSsl }
    Upload-FtpFile $ftpHost $testRemote $testLocal $cred $usePassive $useSsl
    Write-Host "Test BASARILI - yukleme basliyor" -ForegroundColor Green
} catch {
    Write-Host "Test BASARISIZ: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "FileZilla ile dene:" -ForegroundColor Yellow
    Write-Host "  1. Host: $ftpHost  Kullanici: $ftpUser"
    Write-Host "  2. Sag tarafta geldim.ledajans.com klasorune gir"
    Write-Host "  3. Sol: $localDir  ->  Sag: httpdocs veya subdomain kok"
    Write-Host "  4. Plesk > geldim.ledajans.com > Barindirma > Belge kokunu kontrol et"
    Remove-Item $testLocal -Force -ErrorAction SilentlyContinue
    exit 1
}
Remove-Item $testLocal -Force -ErrorAction SilentlyContinue

$total = (Get-ChildItem $localDir -Recurse -File).Count
$done = 0
$failed = 0
$firstError = $null

Write-Host ""
Write-Host "$total dosya yukleniyor..." -ForegroundColor Yellow

Get-ChildItem $localDir -Recurse -File | ForEach-Object {
    $relative = $_.FullName.Substring($localDir.Length).TrimStart("\", "/").Replace("\", "/")
    $remoteFile = if ($remotePath) { "$remotePath/$relative" } else { "/$relative" }
    $remoteDir = Split-Path $remoteFile -Parent
    if ($remoteDir) { $remoteDir = $remoteDir.Replace("\", "/") }

    try {
        if ($remoteDir) { Ensure-FtpDir $ftpHost $remoteDir $cred $usePassive $useSsl }
        Upload-FtpFile $ftpHost $remoteFile $_.FullName $cred $usePassive $useSsl
        $script:done++
        if ($done % 100 -eq 0) { Write-Host "  $done / $total" -ForegroundColor DarkGray }
    } catch {
        $script:failed++
        if (-not $firstError) { $script:firstError = $_.Exception.Message }
        if ($failed -le 5) {
            Write-Host "  HATA: $relative -> $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Tamam: $done basarili, $failed hata" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Yellow" })
if ($firstError -and $failed -gt 5) { Write-Host "Ornek hata: $firstError" -ForegroundColor DarkGray }
Write-Host "Test: https://geldim.ledajans.com/health"
