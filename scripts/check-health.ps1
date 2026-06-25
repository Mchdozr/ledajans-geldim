# Sunucuda migration kontrolu — site acilmiyorsa Plesk'ten app pool restart sonrasi tekrar deneyin
$healthUrl = "https://geldim.ledajans.com/health"
try {
    $r = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 30
    Write-Host "status: $($r.status)"
    Write-Host "migrationsPending: $($r.migrationsPending)"
    if ($r.migrationsPending -gt 0) {
        Write-Host "Bekleyen migration var. App pool yeniden baslatin." -ForegroundColor Yellow
        exit 1
    }
    exit 0
} catch {
    Write-Host "Health kontrolu basarisiz: $_" -ForegroundColor Red
    exit 1
}
