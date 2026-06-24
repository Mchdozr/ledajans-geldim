# nginx -> IIS proxy duzeltmesi (disaridan 404 icin)
# Sunucuda Yonetici PowerShell: .\nginx-fix.ps1

$Domain = "geldim.ledajans.com"
$Plesk = "C:\Plesk Dir\bin\plesk.exe"

$confDirs = @(
    "C:\Plesk Dir\var\www\vhosts\system\$Domain\conf",
    "C:\Plesk Dir\etc\nginx\conf.d\vhosts"
)

Write-Host "=== nginx fix: $Domain ===" -ForegroundColor Cyan

$targetDir = $null
foreach ($d in $confDirs) {
    if (Test-Path $d) { $targetDir = $d; break }
}

if (-not $targetDir) {
    Write-Host "conf klasoru araniyor..." -ForegroundColor Yellow
    $found = Get-ChildItem "C:\Plesk Dir" -Recurse -Directory -Filter "conf" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*$Domain*" } | Select-Object -First 1
    if ($found) { $targetDir = $found.FullName }
}

if (-not $targetDir) {
    Write-Host "HATA: nginx conf klasoru bulunamadi." -ForegroundColor Red
    Get-ChildItem "C:\Plesk Dir\var\www\vhosts\system" -ErrorAction SilentlyContinue | Select-Object Name
    exit 1
}

Write-Host "Conf: $targetDir" -ForegroundColor Green

$customFile = Join-Path $targetDir "vhost_nginx.conf"
$proxyBlock = @"
location / {
    proxy_pass http://127.0.0.1:7080;
    proxy_set_header Host `$host;
    proxy_set_header X-Real-IP `$remote_addr;
    proxy_set_header X-Forwarded-For `$proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto `$scheme;
    proxy_read_timeout 300;
}
"@

if (Test-Path $customFile) {
    $existing = Get-Content $customFile -Raw
    if ($existing -match "proxy_pass") {
        Write-Host "proxy_pass zaten var." -ForegroundColor Yellow
    } else {
        Add-Content $customFile "`n$proxyBlock"
        Write-Host "proxy_pass eklendi." -ForegroundColor Green
    }
} else {
    Set-Content $customFile $proxyBlock -Encoding ASCII
    Write-Host "vhost_nginx.conf olusturuldu." -ForegroundColor Green
}

& $Plesk repair web -domain-name $Domain -y
Write-Host ""
Write-Host "Test: curl.exe -k https://$Domain/health"
