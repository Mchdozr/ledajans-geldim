# web.config uretir -> publish\web.config (Plesk httpdocs'e yukle)
# Kullanim: .\scripts\create-web-config.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root "publish\web.config"

Write-Host "=== Ledajans web.config olusturucu ===" -ForegroundColor Cyan
Write-Host ""

$dbServer = Read-Host "MySQL Host (Plesk'ten: localhost)"
$dbName   = Read-Host "Veritabani adi (orn. LedajansDb)"
$dbUser   = Read-Host "DB kullanici adi"
$dbPass   = Read-Host "DB sifresi" -AsSecureString
$dbPassPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($dbPass))

$adminPass = Read-Host "Canli admin sifresi (Seed__AdminPassword)" -AsSecureString
$adminPassPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminPass))

$jwtKey = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])

$conn = "Server=$dbServer;Port=3306;Database=$dbName;User=$dbUser;Password=$dbPassPlain;"

$xml = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\Ledajans.Server.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="OutOfProcess">
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
"@

New-Item -ItemType Directory -Path (Split-Path $out) -Force | Out-Null
$xml | Out-File -FilePath $out -Encoding UTF8

Write-Host ""
Write-Host "Olusturuldu: $out" -ForegroundColor Green
Write-Host ""
Write-Host "Plesk'e yukleme:" -ForegroundColor Yellow
Write-Host "1. geldim.ledajans.com > Dosyalar > httpdocs klasorune gir"
Write-Host "2. Yukle (Upload) > bu dosyayi sec: publish\web.config"
Write-Host "3. Dosya adi web.config olmali (httpdocs kokunde)"
Write-Host ""
Write-Host "Jwt anahtari kaydedildi (web.config icinde). Admin: admin / sectigin sifre"
