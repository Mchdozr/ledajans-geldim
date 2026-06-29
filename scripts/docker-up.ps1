# Ledajans Geldim — Docker yeniden derleme (Windows)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot\..

Write-Host "=== Ledajans Docker yeniden kurulum ===" -ForegroundColor Cyan

docker compose down --remove-orphans
docker compose build --no-cache app
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker build BASARISIZ — eski imaj calismaya devam eder, once hatayi duzeltin." -ForegroundColor Red
    exit 1
}
docker compose up -d

Start-Sleep -Seconds 5

Write-Host ""
Write-Host "Surum:" -ForegroundColor Yellow
Invoke-RestMethod -Uri "http://localhost:8080/version.txt" -TimeoutSec 10

Write-Host ""
Write-Host "Health:" -ForegroundColor Yellow
(Invoke-RestMethod -Uri "http://localhost:8080/health" -TimeoutSec 10) | ConvertTo-Json -Compress

Write-Host ""
Write-Host "Beklenen: version.txt = multi-location-v2, health icinde deviceBindingEnabled = true" -ForegroundColor Green
Write-Host "Uygulama: http://localhost:8080" -ForegroundColor Green
