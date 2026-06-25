# Tek komut: kilidi ac + bekle + pull sonrasi temizlik icin kullanici Plesk'ten ceker
# Tam otomatik pull Plesk UI'dan yapilir; bu script sadece hazirlik yapar.
# Kullanim: .\plesk-git-deploy.ps1

$scriptDir = $PSScriptRoot
& "$scriptDir\plesk-pre-pull.ps1" @args
if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne $null) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "=== ADIM 2 (manuel) ===" -ForegroundColor Yellow
Write-Host "Plesk panel > Git > Simdi cek / Dagit"
Write-Host ""
Write-Host "=== ADIM 3 (pull basarili olunca) ===" -ForegroundColor Yellow
Write-Host "powershell.exe -ExecutionPolicy Bypass -File `"$scriptDir\plesk-after-pull.ps1`""
