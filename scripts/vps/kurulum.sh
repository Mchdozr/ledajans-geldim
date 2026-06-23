#!/bin/bash
# Natro Linux VPS - Ledajans Geldim kurulum
# Kullanim: sudo bash kurulum.sh

set -e

DOMAIN="geldim.ledajans.com"
APP_DIR="/var/www/geldim"
REPO="https://github.com/Mchdozr/ledajans-geldim.git"
BRANCH="deploy"

echo "=== Ledajans Geldim VPS Kurulum ==="

read -p "MySQL root sifresi (yeni kurulumda belirleyeceksin): " MYSQL_ROOT_PASS
read -p "DB kullanici sifresi (geldimledajans): " DB_PASS
read -p "Admin sifresi (admin kullanicisi): " ADMIN_PASS
JWT_KEY=$(openssl rand -base64 48)

echo ""
echo "Paketler kuruluyor..."
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq wget curl nginx git mariadb-server

if ! command -v dotnet &>/dev/null; then
  wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  apt-get update -qq
  apt-get install -y -qq aspnetcore-runtime-8.0
fi

echo "MySQL ayarlaniyor..."
mysql -u root <<EOF
ALTER USER 'root'@'localhost' IDENTIFIED BY '${MYSQL_ROOT_PASS}';
CREATE DATABASE IF NOT EXISTS LedajansDb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'geldimledajans'@'localhost' IDENTIFIED BY '${DB_PASS}';
GRANT ALL ON LedajansDb.* TO 'geldimledajans'@'localhost';
FLUSH PRIVILEGES;
EOF

CONN="Server=localhost;Port=3306;Database=LedajansDb;User=geldimledajans;Password=${DB_PASS};"

echo "Uygulama indiriliyor (deploy branch)..."
rm -rf "$APP_DIR"
git clone -b "$BRANCH" --depth 1 "$REPO" "$APP_DIR"
chown -R www-data:www-data "$APP_DIR"

echo "Systemd servisi..."
sed -e "s|REPLACE_CONN|${CONN}|" \
    -e "s|REPLACE_JWT|${JWT_KEY}|" \
    -e "s|REPLACE_ADMIN|${ADMIN_PASS}|" \
    "$(dirname "$0")/geldim.service" > /etc/systemd/system/geldim.service

systemctl daemon-reload
systemctl enable geldim
systemctl restart geldim

echo "Nginx..."
cp "$(dirname "$0")/nginx-geldim.conf" /etc/nginx/sites-available/geldim
ln -sf /etc/nginx/sites-available/geldim /etc/nginx/sites-enabled/geldim
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl reload nginx

echo ""
echo "=== Kurulum tamam ==="
echo "1) DNS: $DOMAIN -> bu sunucunun IP (A kaydi)"
echo "2) SSL: certbot --nginx -d $DOMAIN"
echo "3) Test: http://SUNUCU_IP/health (DNS oncesi)"
echo "4) Giris: https://$DOMAIN/login  kullanici: admin"
echo ""
echo "Log: journalctl -u geldim -f"
