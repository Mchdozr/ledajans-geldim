# Ledajans Geldim - Production publish
# Çıktı: .\publish\

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

Write-Host "Release publish basliyor..." -ForegroundColor Cyan
dotnet publish "$root\src\Ledajans.Server\Ledajans.Server.csproj" `
    -c Release `
    -o "$root\publish" `
    --runtime win-x64 `
    /p:EnvironmentName=Production

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Publish tamam: $root\publish" -ForegroundColor Green
Write-Host ""
Write-Host "Sunucuda ayarlanacak ortam degiskenleri:" -ForegroundColor Yellow
Write-Host '  ASPNETCORE_ENVIRONMENT=Production'
Write-Host '  ConnectionStrings__DefaultConnection=<SQL Server baglanti>'
Write-Host '  Jwt__Key=<en az 32 karakter guclu anahtar>'
Write-Host '  Seed__AdminPassword=<admin sifresi>'
Write-Host ""
Write-Host "IIS: .NET 8 Hosting Bundle kurulu olmali, site Application Pool = No Managed Code"
