# Ledajans Geldim

Konum tabanlı (geofence) çalışan yoklama PWA'sı. Çalışan, yalnızca ofis konum sınırı içindeyken günde bir kez "Geldim" işaretleyebilir. Konum doğrulaması sunucu tarafında (Haversine) yapılır.

## Teknoloji
- .NET 8, Blazor WebAssembly (PWA) + ASP.NET Core API (tek host)
- Entity Framework Core + MySQL/MariaDB (Plesk)
- ASP.NET Core Identity + JWT (roller: `Admin`, `Employee`)
- MudBlazor (UI), Leaflet (konum haritası)

## Proje Yapısı
```
src/
  Ledajans.Server   -> API + WASM host + EF Core + Identity/JWT + seed
  Ledajans.Client   -> Blazor WASM PWA (sayfalar, servisler)
  Ledajans.Shared   -> DTO'lar
```

## Özellikler
- Çalışan: tek büyük "Geldim" butonu, günlük durum, konum izni/uzaklık geri bildirimi
- Admin: kullanıcı oluşturma/düzenleme/şifre verme, konum + yarıçap ayarı (harita), tarih/kullanıcı bazlı raporlar, CSV dışa aktarma

## Çalıştırma (Visual Studio Enterprise)
1. `Ledajans.Server` projesini başlangıç projesi yapın.
2. `appsettings.Development.json` içindeki MySQL bağlantısını kontrol edin (local MariaDB veya Plesk).
3. F5 ile çalıştırın. Veritabanı ilk çalıştırmada otomatik oluşturulur (migration + seed).

### CLI ile
```bash
dotnet run --project src/Ledajans.Server
```
- Uygulama: https://localhost:7259
- Swagger: https://localhost:7259/swagger

## Varsayılan Yönetici
`appsettings.json > Seed` bölümünden ayarlanır:
- Kullanıcı: `admin`
- Şifre: `Admin123!`

> Üretimde `Jwt:Key` ve admin şifresini ortam değişkenleri veya Plesk `web.config` ile ayarlayın. Geliştirme ayarları `appsettings.Development.json` içindedir (git'e dahil, yalnızca local).

## GitHub + Plesk (önerilen — FTP/ZIP yok)

### Akış
```
git push (main) → GitHub Actions derler → deploy branch → Plesk Git çeker → canlı
```

### İlk kurulum (bir kez)

1. **GitHub** — `main` branch'e push et (workflow `deploy` branch oluşturur).

2. **Plesk** → `geldim.ledajans.com` → **Git**:
   - Depo: `https://github.com/Mchdozr/ledajans-geldim.git`
   - **Dal:** `deploy` (main değil!)
   - **Dağıtım yolu:** boş bırak veya `/` (doğrudan httpdocs)
   - Ek deploy eylemi: `powershell.exe -ExecutionPolicy Bypass -File scripts\plesk-after-pull.ps1`

3. **web.config** — Plesk Dosya Yöneticisi → `httpdocs\web.config` (bir kez oluştur):
   - Şablon: `scripts\plesk-web.config` (MySQL, Jwt, admin şifresi doldur)
   - `deploy` branch bu dosyayı içermez; sunucudaki kopya korunur.

4. **Plesk** → `.NET Core 8.0` aç, startup: `Ledajans.Server.dll`, SSL (Let's Encrypt).

5. Eski yanlış dosyalar varsa (`src/`, `.git` httpdocs kökünde) sil — sadece publish çıktısı + `web.config` kalsın.

### Güncelleme
```bash
git add . && git commit -m "..." && git push
```

**DLL kilidi hatası** (`unable to unlink Ledajans.Server.dll`): IIS dosyayı kilitliyor. Pull **öncesi**:

```powershell
powershell.exe -ExecutionPolicy Bypass -File scripts\plesk-pre-pull.ps1
```

Sonra Plesk Git → **Güncellemeleri çek**. Başarılı olunca `plesk-after-pull.ps1` çalışır (ek deploy eylemi veya manuel).

Doğrulama: `https://geldim.ledajans.com/version.txt`

Plesk Git → **Güncellemeleri çek** (veya otomatik deploy).

Test: `https://geldim.ledajans.com/health`

### Yedek: sunucuda build (dotnet SDK varsa)
Dağıtım yolu `source`, ek eylem:
```
powershell.exe -ExecutionPolicy Bypass -File scripts\plesk-after-pull.ps1
```

## Notlar
- Geolocation API yalnızca HTTPS veya `localhost` üzerinde çalışır.
- Tarayıcı konumu teorik olarak sahtelenebilir; iç kullanım için kabul edilebilir kabul edilmiştir.
