#!/usr/bin/env bash
# Roda DENTRO do servidor (chamado pelo publicar-app-web.sh): instala a versão web do app em /app/.
set -euo pipefail
[ "$(id -u)" -eq 0 ] || { echo "Rode com sudo."; exit 1; }
DESTINO=/var/www/maximassas-app
CONF=/etc/nginx/sites-available/maximassas

mkdir -p "$DESTINO"
rm -rf "${DESTINO:?}"/*
tar xzf /home/ubuntu/AppWeb.tgz -C "$DESTINO"
chown -R www-data:www-data "$DESTINO"

# Só na primeira vez: bloco /app/ antes do "location /" (que manda tudo para o painel Web).
if ! grep -q "location /app/" "$CONF"; then
  cp "$CONF" /home/ubuntu/maximassas.nginx.bak
  python3 - "$CONF" <<'PY'
import sys
p = sys.argv[1]
s = open(p).read()
bloco = """    location = /app { return 301 /app/; }
    location /app/ {
        alias /var/www/maximassas-app/;
        index index.html;
    }
"""
s = s.replace("    location / {", bloco + "    location / {")
open(p, "w").write(s)
PY
fi

if nginx -t; then
  systemctl reload nginx
  echo ">> Pronto: https://maximassasitapolis.duckdns.org/app/"
else
  echo ">> Configuracao do nginx invalida; restaurando a anterior."
  cp /home/ubuntu/maximassas.nginx.bak "$CONF"
  nginx -t && systemctl reload nginx
  exit 1
fi
