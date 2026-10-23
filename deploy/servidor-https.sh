#!/usr/bin/env bash
# Roda DENTRO do servidor EC2:  sudo bash servidor-https.sh DOMINIO EMAIL
# Ex.: sudo bash servidor-https.sh maximassasitapolis.duckdns.org alguem@gmail.com
# Configura HTTPS (certificado gratuito Let's Encrypt) para a Web e para a Api no mesmo dominio:
#   https://DOMINIO/            -> Web (painel)
#   https://DOMINIO/api/app/... -> Api (aplicativo dos clientes)
set -euo pipefail
[ "$(id -u)" -eq 0 ] || { echo "Rode com sudo."; exit 1; }
DOMINIO="${1:?Informe o dominio}"
EMAIL="${2:?Informe um e-mail para avisos do certificado}"

apt-get update -y
apt-get install -y certbot python3-certbot-nginx

cat > /etc/nginx/sites-available/maximassas <<EOF
server {
    listen 80 default_server;
    server_name $DOMINIO;
    client_max_body_size 10m;

    location /api/app/ {
        proxy_pass http://127.0.0.1:5001;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOF
nginx -t
systemctl reload nginx

certbot --nginx -d "$DOMINIO" --non-interactive --agree-tos -m "$EMAIL" --redirect

for f in /etc/maximassas/web.env /etc/maximassas/api.env; do
  sed -i "s#^EmailSettings__UrlBase=.*#EmailSettings__UrlBase=https://$DOMINIO#" "$f"
done
systemctl restart maximassas-web maximassas-api
nginx -t && systemctl reload nginx

echo
echo "Pronto. Web: https://$DOMINIO   |   Api do app: https://$DOMINIO/api/app/produtos"
echo "Renovacao do certificado: automatica (certbot.timer). Teste: sudo certbot renew --dry-run"
