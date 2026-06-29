# Canlıya Alma (Standart Deploy)

Bu proje **her zaman** aşağıdaki sırayla canlıya alınır. Plesk Git pull kullanılmaz (DLL kilidi ve web.config sorunları nedeniyle).

## Akış

```
Cursor/VS → git push (main) → GitHub Actions (deploy branch) → sunucuda canliya-al.ps1 → canlı
```

## 1. Kodu gönder

```bash
git add .
git commit -m "..."
git push origin main
```

GitHub Actions **Plesk Deploy** workflow'unun bitmesini bekle (~2 dk).  
Kontrol: https://github.com/Mchdozr/ledajans-geldim/actions — yeşil tik.

## 2. Sunucuda tek komut

**Yönetici PowerShell** — `C:\Ledajans` klasöründe:

```powershell
powershell.exe -ExecutionPolicy Bypass -File "C:\Ledajans\canliya-al.ps1"
```

Başarılı çıktı örneği:

```
Surum: 6ec2361
Tamam -> https://geldim.ledajans.com
```

## 3. Tarayıcı

Telefon/PC'de **Ctrl+Shift+R** (hard refresh).

## İlk kurulum (bir kez)

Script sunucuda yoksa:

```powershell
New-Item -ItemType Directory -Path "C:\Ledajans" -Force
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/Mchdozr/ledajans-geldim/main/scripts/canliya-al.ps1" -OutFile "C:\Ledajans\canliya-al.ps1" -UseBasicParsing -Headers @{ "User-Agent" = "Ledajans-Deploy/1.0" }
```

`web.config` yedek yolu: `C:\Ledajans\config\web.config` (script otomatik yedekler/geri yükler).

## Doğrulama

| Kontrol | Adres |
|---------|--------|
| Sürüm | https://geldim.ledajans.com/version.txt |
| Sağlık | https://geldim.ledajans.com/health |
| Uygulama | https://geldim.ledajans.com |

## Script ne yapar?

1. App pool durdurur + `app_offline.htm`
2. `web.config` yedekler
3. GitHub `deploy` branch zip indirir
4. Site dosyalarını günceller (`web.config` ve `logs` hariç)
5. `web.config` geri yükler, app pool başlatır

Kaynak: `scripts/canliya-al.ps1`
