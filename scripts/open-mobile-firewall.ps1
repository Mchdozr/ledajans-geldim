# Yönetici PowerShell'de çalıştırın: sağ tık -> Yönetici olarak çalıştır
$rules = @(
    @{ Name = "Ledajans Dev HTTPS 7259"; Port = 7259 },
    @{ Name = "Ledajans Dev HTTP 5132"; Port = 5132 }
)

foreach ($rule in $rules) {
    $existing = Get-NetFirewallRule -DisplayName $rule.Name -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host "Zaten var: $($rule.Name)"
        continue
    }

    New-NetFirewallRule -DisplayName $rule.Name -Direction Inbound -Protocol TCP -LocalPort $rule.Port -Action Allow | Out-Null
    Write-Host "Eklendi: $($rule.Name)"
}

Write-Host ""
Write-Host "Telefonda acin: https://192.168.1.245:7259"
Write-Host "Sertifika uyarisi -> Gelismis -> Devam"
