# Plesk yolunu bulur + deploy branch yukler + web.config yazar
# Sunucuda Yonetici PowerShell: 
#   git clone https://github.com/Mchdozr/ledajans-geldim.git C:\ledajans
#   cd C:\ledajans\scripts\vps
#   .\bul-ve-kur.ps1

$ErrorActionPreference = "Stop"
$Domain = "geldim.ledajans.com"

function Find-PleskExe {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Plesk\admin\bin\plesk.exe",
        "${env:ProgramFiles(x86)}\Parallels\Plesk\admin\bin\plesk.exe",
        "C:\Plesk Dir\admin\bin\plesk.exe",
        "$env:ProgramFiles\Plesk\admin\bin\plesk.exe"
    )
    foreach ($p in $candidates) { if (Test-Path $p) { return $p } }
    Get-ChildItem "${env:ProgramFiles(x86)}" -Recurse -Filter "plesk.exe" -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
}

function Find-SiteRoot($pleskExe) {
    if ($pleskExe) {
        $out = & $pleskExe bin site --info $Domain 2>&1 | Out-String
        if ($out -match "WWW-Root:\s*(.+)") {
            $p = $Matches[1].Trim()
            if ($p -notmatch "preview") { return $p }
        }
        if ($out -match "Document root:\s*(.+)") {
            $p = $Matches[1].Trim()
            if ($p -notmatch "preview") { return $p }
        }
    }
    $searchRoots = @(
        "C:\Inetpub\vhosts",
        "D:\Inetpub\vhosts",
        "C:\Plesk Dir\vhosts",
        "D:\Plesk Dir\vhosts"
    )
    foreach ($vhosts in $searchRoots) {
        if (-not (Test-Path $vhosts)) { continue }
        Get-ChildItem $vhosts -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            $candidates = @(
                (Join-Path $_.FullName $Domain),
                (Join-Path $_.FullName "httpdocs\$Domain"),
                (Join-Path $_.FullName "subdomains\geldim"),
                (Join-Path $_.FullName "subdomains\geldim\httpdocs")
            )
            foreach ($c in $candidates) {
                if ((Test-Path $c) -and $c -notmatch "preview") { return $c }
            }
        }
    }
    foreach ($d in @("C", "D")) {
        $found = Get-ChildItem "$d`:\" -Directory -Filter $Domain -Recurse -Depth 8 -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match "vhosts|Inetpub|Plesk" -and $_.FullName -notmatch "preview|error_docs|tmp" } |
            Select-Object -First 1
        if ($found) { return $found.FullName }
    }
    $null
}

Write-Host "=== Bul ve Kur: $Domain ===" -ForegroundColor Cyan

$plesk = Find-PleskExe
if ($plesk) { Write-Host "Plesk: $plesk" -ForegroundColor DarkGray }

$siteRoot = Find-SiteRoot $plesk
if (-not $siteRoot) {
    Write-Host "HATA: Site klasoru bulunamadi." -ForegroundColor Red
    Write-Host "Plesk File Manager > geldim.ledajans.com yolunu manuel kontrol et."
    exit 1
}
Write-Host "Site klasoru: $siteRoot" -ForegroundColor Green

$dbPass = Read-Host "MySQL sifre (geldimledajans)" -AsSecureString
$dbPassPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($dbPass))
$adminPass = Read-Host "Admin sifresi" -AsSecureString
$adminPassPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminPass))
$jwtKey = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
$conn = "Server=localhost;Port=3306;Database=LedajansDb;User=geldimledajans;Password=$dbPassPlain;"

$bundleUrl = "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/8.0.16/dotnet-hosting-8.0.16-win.exe"
$bundlePath = "$env:TEMP\dotnet-hosting-8.0.exe"
if (-not (Test-Path "$env:ProgramFiles\dotnet\shared\Microsoft.AspNetCore.App")) {
    Write-Host ".NET Hosting Bundle kuruluyor..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $bundleUrl -OutFile $bundlePath -UseBasicParsing
    Start-Process -FilePath $bundlePath -Args "/quiet /norestart" -Wait
}

Write-Host "Deploy indiriliyor..." -ForegroundColor Yellow
$zipPath = "$env:TEMP\geldim-deploy.zip"
Invoke-WebRequest -Uri "https://github.com/Mchdozr/ledajans-geldim/archive/refs/heads/deploy.zip" -OutFile $zipPath -UseBasicParsing
$extract = "$env:TEMP\geldim-extract"
if (Test-Path $extract) { Remove-Item $extract -Recurse -Force }
Expand-Archive -Path $zipPath -DestinationPath $extract -Force
$src = (Get-ChildItem $extract -Directory | Select-Object -First 1).FullName

Get-ChildItem $siteRoot -Exclude "web.config" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item "$src\*" $siteRoot -Recurse -Force

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
"@ | Out-File -FilePath (Join-Path $siteRoot "web.config") -Encoding UTF8

New-Item -ItemType Directory -Path (Join-Path $siteRoot "logs") -Force | Out-Null

if (Test-Path "$env:SystemRoot\System32\inetsrv\appcmd.exe") {
    & "$env:SystemRoot\System32\inetsrv\appcmd.exe" recycle apppool "/apppool.name:DefaultAppPool" 2>$null
}

Write-Host ""
Write-Host "Tamam: $siteRoot" -ForegroundColor Green
Write-Host "Test: http://$Domain/health"
if (Test-Path (Join-Path $siteRoot "Ledajans.Server.dll")) {
    Write-Host "Ledajans.Server.dll OK" -ForegroundColor Green
} else {
    Write-Host "HATA: Ledajans.Server.dll yok!" -ForegroundColor Red
}
