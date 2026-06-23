# Tek komut: build + web.config + ZIP
# Kullanim: .\scripts\kolay-deploy.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$publishDir = Join-Path $root "publish"
$zipPath = Join-Path $root "geldim-yukle.zip"

Write-Host ""
Write-Host "=== LEDAJANS Kolay Deploy ===" -ForegroundColor Cyan
Write-Host "Sadece sifreleri gir, gerisini script halleder." -ForegroundColor DarkGray
Write-Host ""

$dbPass = Read-Host "MySQL sifresi (Plesk - geldimledajans)" -AsSecureString
$dbPassPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($dbPass))
$adminPass = Read-Host "Canli admin sifresi (admin kullanicisi)" -AsSecureString
$adminPassPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminPass))

$jwtKey = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
$conn = "Server=localhost;Port=3306;Database=LedajansDb;User=geldimledajans;Password=$dbPassPlain;"

Write-Host ""
Write-Host "Derleniyor..." -ForegroundColor Yellow
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish "$root\src\Ledajans.Server\Ledajans.Server.csproj" -c Release -o $publishDir /p:EnvironmentName=Production
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$devSettings = Join-Path $publishDir "appsettings.Development.json"
if (Test-Path $devSettings) { Remove-Item $devSettings -Force }

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\Ledajans.Server.dll"
                  stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="OutOfProcess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="ConnectionStrings__DefaultConnection" value="$conn" />
          <environmentVariable name="Jwt__Key" value="$jwtKey" />
          <environmentVariable name="Seed__AdminPassword" value="$adminPassPlain" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@ | Out-File -FilePath (Join-Path $publishDir "web.config") -Encoding UTF8

New-Item -ItemType Directory -Path (Join-Path $publishDir "logs") -Force | Out-Null

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath

Write-Host ""
Write-Host "Hazir: $zipPath" -ForegroundColor Green
Write-Host ""
Write-Host "=== PLESK'E YUKLEME ===" -ForegroundColor Cyan
Write-Host "ZIP cikarma Plesk'te cok dosyada hata verir. FTP kullan:" -ForegroundColor Yellow
Write-Host "  .\scripts\ftp-yukle.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Veya FileZilla: publish\ klasorunun ICINDEKILERI -> geldim.ledajans.com httpdocs"
Write-Host "Sonra: .NET 8 ac + SSL (Let's Encrypt) - bir kez"
Write-Host ""
Write-Host "Ac: https://geldim.ledajans.com/login" -ForegroundColor Yellow
Write-Host "Kullanici: admin" -ForegroundColor Yellow
