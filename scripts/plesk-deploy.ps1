# Ledajans Geldim - Natro / Plesk deploy paketi
# Cikti: .\publish\ ve .\ledajans-geldim-plesk.zip

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$publishDir = Join-Path $root "publish"
$zipPath = Join-Path $root "ledajans-geldim-plesk.zip"

Write-Host "Plesk icin publish (framework-dependent)..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish "$root\src\Ledajans.Server\Ledajans.Server.csproj" `
    -c Release `
    -o $publishDir `
    /p:EnvironmentName=Production

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Gelistirme ayarlarini pakete dahil etme
$devSettings = Join-Path $publishDir "appsettings.Development.json"
if (Test-Path $devSettings) { Remove-Item $devSettings -Force }

# Plesk web.config (ortam degiskenleri burada)
Copy-Item "$PSScriptRoot\plesk-web.config" (Join-Path $publishDir "web.config") -Force

# stdout log klasoru
$logsDir = Join-Path $publishDir "logs"
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath

Write-Host ""
Write-Host "Hazir:" -ForegroundColor Green
Write-Host "  Klasor: $publishDir"
Write-Host "  ZIP:    $zipPath"
Write-Host ""
Write-Host "=== NATRO / PLESK ADIMLARI ===" -ForegroundColor Yellow
Write-Host "1. Plesk > Veritabanlari > MS SQL veritabani olustur (orn. LedajansDb)"
Write-Host "2. scripts\plesk-web.config icindeki baglanti, Jwt__Key ve admin sifresini duzenle"
Write-Host "3. ZIP'i Plesk Dosya Yoneticisi ile httpdocs'e yukle ve cikar"
Write-Host "   (veya alt alan adi: geldim.ledajans.com -> httpdocs)"
Write-Host "4. Plesk > Web Siteleri > .NET Core > Surum 8.0, Startup: Ledajans.Server.dll"
Write-Host "5. SSL: Let's Encrypt ile HTTPS ac (Geldim icin zorunlu)"
Write-Host "6. Uygulama havuzunu yeniden baslat, https://DOMAIN adresini ac"
Write-Host "7. Hata olursa: httpdocs\logs\stdout*.log dosyasina bak"
Write-Host ""
Write-Host "Saglik kontrolu: https://DOMAIN/health"
