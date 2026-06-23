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

## GitHub + Plesk (Natro) Canlı Güncelleme

### 1. GitHub
Repo: public `ledajans-geldim` — push sonrası GitHub Actions build doğrular.

### 2. Plesk Git bağlantısı
1. **Web Siteleri** → domain (ör. `geldim.ledajans.com`) → **Git**
2. **Depo klonla** → `https://github.com/Mchdozr/ledajans-geldim.git`
3. **Dağıtım yolu:** `source` (httpdocs altında)
4. **Ek deploy eylemi:**
   ```
   powershell.exe -ExecutionPolicy Bypass -File source\scripts\plesk-after-pull.ps1
   ```
5. Ortam değişkeni (Plesk veya web.config): `PLESK_HTTPDOCS` = `httpdocs` tam yolu
6. İlk kurulumda `scripts\plesk-web.config` → `httpdocs\web.config` (SQL, Jwt, admin şifresi)
7. **.NET Core 8.0**, startup: `Ledajans.Server.dll`, **HTTPS** açık

### 3. Güncelleme akışı
```
git push → Plesk'te "Güncellemeleri çek" veya otomatik deploy → publish → canlı
```

## Notlar
- Geolocation API yalnızca HTTPS veya `localhost` üzerinde çalışır.
- Tarayıcı konumu teorik olarak sahtelenebilir; iç kullanım için kabul edilebilir kabul edilmiştir.
