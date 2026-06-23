# Ledajans Geldim — Canlı Deploy Raporu

> Son güncelleme: 23 Haziran 2026  
> Repo: https://github.com/Mchdozr/ledajans-geldim  
> Canlı hedef: https://geldim.ledajans.com

---

## Proje Özeti

| Özellik | Detay |
|---------|-------|
| Uygulama | Konum tabanlı yoklama (geofence) + PWA |
| Stack | .NET 8, Blazor WASM Hosted, MySQL, JWT, MudBlazor |
| Admin | `admin` + canlı şifre (`web.config` / `Seed__AdminPassword`) |
| Kullanıcı sayısı | ~25 çalışan |

---

## Mevcut Durum (kaldığımız yer)

### Tamamlanan

- [x] Uygulama geliştirildi (Geldim, admin panel, raporlar, PWA)
- [x] MySQL/MariaDB geçişi (Pomelo EF Core)
- [x] GitHub Actions: `main` push → `deploy` branch otomatik publish
- [x] Eski paylaşımlı hostingte dosyalar doğru yere deploy edildi (Git `deploy` branch)
- [x] `web.config` şablonu hazır (`scripts/plesk-web.config`)
- [x] **Natro xCloud Large — Windows VPS** satın alındı (Plesk dahil)
- [x] VPS kurulum scriptleri: `scripts/vps/windows-kurulum.ps1` (Windows), `scripts/vps/kurulum.sh` (Linux yedek)

### Yapılacak (eve geçince)

- [ ] VPS IP + RDP bilgilerini Natro panelinden al
- [ ] DNS: `geldim.ledajans.com` → **A kaydı** → yeni VPS IP
- [ ] Plesk: `https://VPS_IP:8443` — domain + alt alan adı kur
- [ ] Plesk: MySQL veritabanı `LedajansDb` + kullanıcı `geldimledajans`
- [ ] RDP: `windows-kurulum.ps1` çalıştır
- [ ] Plesk: Let's Encrypt SSL
- [ ] Test: `/health`, `/login`

---

## Neden VPS?

Natro **paylaşımlı Linux** hostingte .NET başlamadı:
- `web.config` (IIS) Linux'ta çalışmaz
- `logs/` boş kaldı — uygulama hiç başlamadı
- `/wwwroot/favicon.png` → 200 (dosyalar vardı) ama `/favicon.png` → 404 (uygulama çalışmıyordu)

**Windows VPS + Plesk + IIS** = mevcut deploy akışı doğrudan çalışır.

---

## Deploy Mimarisi

```
[PC] git push main
        ↓
[GitHub Actions] dotnet publish → deploy branch
        ↓
[Plesk Git veya windows-kurulum.ps1] → httpdocs
        ↓
[IIS + ASP.NET Core Module] → Ledajans.Server.dll
        ↓
https://geldim.ledajans.com (web + PWA mobil)
```

---

## Windows VPS Kurulum (ana yol)

### 1. Natro paneli

- **Müşteri Paneli** → Sunucu Yönetimi → XCloud Sunucular
- Not al: **IP**, **RDP kullanıcı/şifre**, **Plesk URL** (`https://IP:8443`)

### 2. DNS (ledajans.com domain yönetimi)

| Kayıt | Tip | Değer |
|-------|-----|-------|
| `geldim` | A | `VPS_IP_ADRESI` |

> Eski paylaşımlı hosting IP'sini değiştir. Yayılması 5–60 dk sürebilir.

### 3. Plesk ilk ayar

1. `ledajans.com` ekle (yoksa)
2. Alt alan adı: `geldim.ledajans.com`
3. **Veritabanları** → MySQL/MariaDB:
   - Veritabanı: `LedajansDb`
   - Kullanıcı: `geldimledajans` + güçlü şifre
4. **Barındırma Ayarları** → belge kökü yolunu kopyala (script için)

### 4. RDP + kurulum scripti

Windows Uzak Masaüstü → VPS IP → Administrator

PowerShell **Yönetici**:

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
git clone https://github.com/Mchdozr/ledajans-geldim.git C:\ledajans
cd C:\ledajans\scripts\vps
.\windows-kurulum.ps1
```

Sorular:
- **httpdocs yolu** — örnek: `C:\Inetpub\vhosts\ledajans.com\geldim.ledajans.com\httpdocs`
- **MySQL şifre** — `geldimledajans`
- **Admin şifre** — canlı admin giriş şifresi

Script otomatik yapar:
- .NET 8 Hosting Bundle kurulumu
- GitHub `deploy` branch indirme → httpdocs
- `web.config` oluşturma (JWT key otomatik üretilir)
- IIS yeniden başlatma

### 5. SSL

Plesk → `geldim.ledajans.com` → **SSL/TLS** → Let's Encrypt  
Sadece: **Alan adını koru** (`geldim.ledajans.com`)

### 6. Plesk Git (isteğe bağlı — güncellemeler için)

| Ayar | Değer |
|------|-------|
| Depo | `https://github.com/Mchdozr/ledajans-geldim.git` |
| Dal | `deploy` |
| Dağıtım yolu | boş veya `/geldim.ledajans.com/httpdocs` |
| Otomatik dağıtım | `deploy` dalı |

> `web.config` deploy branch'te yok — sunucudaki kopya korunur.

### 7. Test checklist

- [ ] `https://geldim.ledajans.com/health` → `{"status":"ok","app":"Ledajans Geldim"}`
- [ ] `https://geldim.ledajans.com/login` → admin giriş
- [ ] Telefon: site açılıyor, konum izni, Geldim butonu
- [ ] PWA: tarayıcı menüsü → Ana ekrana ekle

---

## Güncelleme (canlıya aldıktan sonra)

```bash
# PC'de kod değişikliği
git add .
git commit -m "..."
git push
```

GitHub Actions `deploy` branch'i günceller → Plesk **Şimdi çek** + **Şimdi dağıt**

---

## Sorun giderme

| Belirti | Çözüm |
|---------|-------|
| `logs/` boş | IIS .NET modülü yok → Hosting Bundle tekrar kur, `iisreset` |
| 404 | Belge kökü ≠ `Ledajans.Server.dll` konumu |
| 500 | `httpdocs\logs\stdout_*.log` oku; MySQL bağlantısı / Jwt eksik |
| SSL hatası | Let's Encrypt kur, DNS'in VPS'e işaret ettiğini doğrula |
| DB hatası | `LedajansDb`, kullanıcı `geldimledajans`, şifre `web.config` ile uyumlu mu |

### web.config ortam değişkenleri

```xml
ConnectionStrings__DefaultConnection = Server=localhost;Port=3306;Database=LedajansDb;User=geldimledajans;Password=...;
Jwt__Key = en az 32 karakter (rastgele)
Seed__AdminPassword = admin canlı şifresi
```

Şablon: `scripts/plesk-web.config`

---

## Dosya rehberi

| Dosya | Açıklama |
|-------|----------|
| `scripts/kolay-deploy.ps1` | PC'de build + zip (yerel test) |
| `scripts/ftp-yukle.ps1` | FTP yükleme (VPS öncesi denendi, gerekmez) |
| `scripts/vps/windows-kurulum.ps1` | **VPS ana kurulum scripti** |
| `scripts/vps/kurulum.sh` | Linux VPS yedek |
| `scripts/plesk-web.config` | web.config şablonu |
| `.github/workflows/deploy.yml` | CI → deploy branch |
| `docs/DEPLOY-RAPOR.md` | Bu dosya |

---

## Natro destek geçmişi (özet)

1. ZIP çıkartma Plesk'te failed → FTP de failed (paylaşımlı Linux)
2. Destek: "Windows öneririz" / "hosting Linux görünüyor"
3. Sonuç: **xCloud Large Windows VPS** alındı

---

## Yerel geliştirme (Visual Studio)

```bash
dotnet run --project src/Ledajans.Server
```

- MySQL local gerekli (`appsettings.Development.json`)
- https://localhost:7259
- Dev admin: `admin` / `Admin123!` (Development ayarları)

---

## İletişim / hesaplar

- GitHub: `Mchdozr/ledajans-geldim`
- Domain: `ledajans.com` / `geldim.ledajans.com`
- Natro: xCloud Large Windows VPS
- Eski paylaşımlı FTP: `194.36.84.221`, kullanıcı `l3daj2ns` (artık kullanılmayacak)
