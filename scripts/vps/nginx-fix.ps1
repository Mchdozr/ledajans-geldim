# nginx -> IIS proxy duzeltmesi
$Domain = "geldim.ledajans.com"
$Plesk = "C:\Plesk Dir\bin\plesk.exe"

Write-Host "=== nginx fix: $Domain ===" -ForegroundColor Cyan

# Plesk standart yolu olustur
$standardConf = "C:\Plesk Dir\var\www\vhosts\system\$Domain\conf"
New-Item -ItemType Directory -Path $standardConf -Force | Out-Null

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

$customFile = Join-Path $standardConf "vhost_nginx.conf"
Set-Content $customFile $proxyBlock -Encoding ASCII
Write-Host "Yazildi: $customFile" -ForegroundColor Green

# Mevcut nginx conf dosyalarini listele
Write-Host ""
Write-Host "geldim iceren conf dosyalari:" -ForegroundColor Yellow
Get-ChildItem "C:\Plesk Dir" -Recurse -Include "*.conf" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match "geldim|nginx" } |
    Select-Object -First 15 FullName

& $Plesk repair web -domain-name $Domain -y

Write-Host ""
Write-Host "Test (sunucu):" -ForegroundColor Cyan
curl.exe -sk https://$Domain/health
