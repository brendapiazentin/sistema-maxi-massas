#!/usr/bin/env bash
# Roda DENTRO do servidor (chamado pelo publicar-apk.sh): publica o APK Android em /downloads/.
set -euo pipefail
[ "$(id -u)" -eq 0 ] || { echo "Rode com sudo."; exit 1; }
DESTINO=/var/www/maximassas-downloads
CONF=/etc/nginx/sites-available/maximassas

mkdir -p "$DESTINO"
install -o www-data -g www-data -m 644 /home/ubuntu/maximassas.apk "$DESTINO/maximassas.apk"

cp "$CONF" /home/ubuntu/maximassas.nginx.downloads.bak

# Só na primeira vez: bloco /downloads/ antes do "location /" (que manda tudo para o painel Web).
if ! grep -q "location /downloads/" "$CONF"; then
  python3 - "$CONF" <<'PY'
import sys
p = sys.argv[1]
s = open(p).read()
bloco = """    location /downloads/ {
        alias /var/www/maximassas-downloads/;
        default_type application/vnd.android.package-archive;
    }
"""
s = s.replace("    location / {", bloco + "    location / {")
open(p, "w").write(s)
PY
fi

if nginx -t; then
  systemctl reload nginx
  echo ">> Pronto: https://maximassasitapolis.duckdns.org/downloads/maximassas.apk"
else
  echo ">> Configuracao do nginx invalida; restaurando a anterior."
  cp /home/ubuntu/maximassas.nginx.downloads.bak "$CONF"
  nginx -t && systemctl reload nginx
  exit 1
fi
