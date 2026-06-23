# Ilk canli kurulum: geldim.ledajans.com + Plesk Git

$jwtKey = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
$guide = @"
=== PLESK ADIM ADIM (geldim.ledajans.com) ===

1) ALT ALAN ADI
   Plesk > Web Siteleri > Alt Alan Adi Ekle
   - Ad: geldim
   - Kok: geldim.ledajans.com
   - Belge kok dizini: httpdocs (varsayilan)

2) MS SQL VERITABANI (veriler Plesk icinde)
   Plesk > Veritabanlari > Veritabani Ekle
   - Tur: MS SQL Server
   - Ad: LedajansDb (veya panelin verdigi ad)
   - Kullanici + sifre olustur
   - NOT: Plesk'in gosterdigi baglanti bilgisini kopyala

   Ornek baglanti (Natro'ya gore degisir):
   Server=localhost;Database=LedajansDb;User Id=DB_KULLANICI;Password=DB_SIFRE;TrustServerCertificate=True;MultipleActiveResultSets=true

3) GIT BAGLANTISI
   geldim.ledajans.com > Git > Depoyu Klonla
   - URL: https://github.com/Mchdozr/ledajans-geldim.git
   - Dal: main
   - Dizin: source   (httpdocs\source olacak)

4) WEB.CONFIG (httpdocs kokune - SIRLAR BURADA)
   Plesk Dosya Yoneticisi > httpdocs > web.config olustur
   scripts\plesk-web.config sablonunu kopyala ve doldur:
   - ConnectionStrings__DefaultConnection = adim 2'deki baglanti
   - Jwt__Key = $jwtKey
   - Seed__AdminPassword = canli admin sifren (guclu)

5) DEPLOY EYLEMI
   Git sayfasinda "Ek deployment eylemleri":
   powershell.exe -ExecutionPolicy Bypass -File source\scripts\plesk-after-pull.ps1

   Ortam degiskeni ekle (Plesk .NET veya web.config disinda):
   PLESK_HTTPDOCS = httpdocs tam yolu
   (Ornek: C:\Inetpub\vhosts\ledajans.com\geldim.ledajans.com\httpdocs)

6) .NET CORE
   Web Sitesi > ASP.NET Ayarlari / .NET Core
   - Surum: 8.0
   - Startup dosyasi: Ledajans.Server.dll
   - Uygulama havuzunu yeniden baslat

7) SSL
   SSL/TLS > Let's Encrypt > geldim.ledajans.com > Kur

8) ILK CALISTIRMA
   - Git > Guncellemeleri cek / Deploy
   - https://geldim.ledajans.com/health  -> {"status":"ok"}
   - https://geldim.ledajans.com/login   -> admin giris
   - Konum ayari (admin) + calisan test

9) SONRAKI GUNCELLEMELER
   Bilgisayarda: git push
   Plesk Git: Guncellemeleri cek (veya otomatik)

HATA: httpdocs\logs\stdout_*.log
"@

$out = Join-Path $PSScriptRoot "plesk-canli-kurulum.txt"
$guide | Out-File -FilePath $out -Encoding UTF8
Write-Host "Kurulum kilavuzu: $out" -ForegroundColor Green
Write-Host "Jwt__Key: $jwtKey" -ForegroundColor Yellow
