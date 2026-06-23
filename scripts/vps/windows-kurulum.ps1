# Natro xCloud Windows VPS - Ledajans Geldim kurulum (RDP/Plesk icinde Administrator olarak calistir)
# Once Plesk'te geldim.ledajans.com alt alan adi + MySQL veritabani olustur

$ErrorActionPreference = "Stop"

Write-Host "=== Ledajans Geldim - Windows VPS Kurulum ===" -ForegroundColor Cyan

$httpdocs = Read-Host "httpdocs tam yolu (orn. C:\Inetpub\vhosts\ledajans.com\geldim.ledajans.com\httpdocs)"
$dbPass = Read-Host "MySQL sifre (geldimledajans)" -AsSecureString
$dbPassPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($dbPass))
$adminPass = Read-Host "Admin sifresi" -AsSecureString
$adminPassPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminPass))

$jwtKey = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
$conn = "Server=localhost;Port=3306;Database=LedajansDb;User=geldimledajans;Password=$dbPassPlain;"

# .NET 8 Hosting Bundle
$bundleUrl = "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/8.0.16/dotnet-hosting-8.0.16-win.exe"
$bundlePath = "$env:TEMP\dotnet-hosting-8.0.exe"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host ".NET 8 Hosting Bundle indiriliyor..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $bundleUrl -OutFile $bundlePath -UseBasicParsing
    Start-Process -FilePath $bundlePath -Args "/quiet /norestart" -Wait
    Write-Host "Kurulum tamam - IIS yeniden baslatiliyor..." -ForegroundColor Green
    iisreset
} else {
    Write-Host ".NET zaten kurulu: $(dotnet --version)" -ForegroundColor Green
}

# deploy branch indir
Write-Host "Uygulama indiriliyor..." -ForegroundColor Yellow
$zipUrl = "https://github.com/Mchdozr/ledajans-geldim/archive/refs/heads/deploy.zip"
$zipPath = "$env:TEMP\geldim-deploy.zip"
Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing

if (Test-Path $httpdocs) {
    Get-ChildItem $httpdocs -Exclude "web.config" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $httpdocs -Force | Out-Null
Expand-Archive -Path $zipPath -DestinationPath "$env:TEMP\geldim-extract" -Force
$extracted = Get-ChildItem "$env:TEMP\geldim-extract" -Directory | Select-Object -First 1
Copy-Item "$($extracted.FullName)\*" $httpdocs -Recurse -Force

# web.config
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
"@ | Out-File -FilePath (Join-Path $httpdocs "web.config") -Encoding UTF8

New-Item -ItemType Directory -Path (Join-Path $httpdocs "logs") -Force | Out-Null
iisreset | Out-Null

Write-Host ""
Write-Host "=== Tamam ===" -ForegroundColor Green
Write-Host "1) DNS: geldim.ledajans.com -> bu VPS IP"
Write-Host "2) Plesk SSL: Let's Encrypt"
Write-Host "3) Test: https://geldim.ledajans.com/health"
Write-Host "4) Giris: /login  admin"
