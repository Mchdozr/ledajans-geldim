# Sunucuda git pull sonrasi (Plesk > Git > Ek deploy eylemleri)
# Plesk ortam degiskeni: PLESK_HTTPDOCS = httpdocs tam yolu (orn. C:\Inetpub\vhosts\domain\httpdocs)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$publishDir = $env:PLESK_HTTPDOCS
if ([string]::IsNullOrWhiteSpace($publishDir)) {
    $publishDir = $repoRoot
}

Write-Host "Publish: $repoRoot -> $publishDir" -ForegroundColor Cyan

dotnet publish "$repoRoot\src\Ledajans.Server\Ledajans.Server.csproj" `
    -c Release `
    -o $publishDir `
    /p:EnvironmentName=Production

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$devSettings = Join-Path $publishDir "appsettings.Development.json"
if (Test-Path $devSettings) { Remove-Item $devSettings -Force }

$pleskConfig = Join-Path $publishDir "web.config"
if (-not (Test-Path $pleskConfig)) {
    Copy-Item (Join-Path $PSScriptRoot "plesk-web.config") $pleskConfig -Force
}

New-Item -ItemType Directory -Path (Join-Path $publishDir "logs") -Force | Out-Null
Write-Host "Tamam." -ForegroundColor Green
