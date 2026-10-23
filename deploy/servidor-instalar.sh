#!/usr/bin/env bash
# Roda DENTRO do servidor EC2 (Ubuntu 24.04):  sudo bash servidor-instalar.sh
# Instala nginx, configura Web (porta 80) e Api (porta 8080) como servicos.
set -euo pipefail
[ "$(id -u)" -eq 0 ] || { echo "Rode com sudo."; exit 1; }

ENDPOINT_PADRAO="database-maximassas.c6bg6ewuqhzt.us-east-1.rds.amazonaws.com"
read -r -p "Endpoint do RDS [$ENDPOINT_PADRAO]: " ENDPOINT
ENDPOINT="${ENDPOINT:-$ENDPOINT_PADRAO}"
read -r -s -p "Senha do banco (usuario admin): " SENHA; echo
IP_PUBLICO="$(curl -s http://checkip.amazonaws.com | tr -d '\n')"
echo "IP publico detectado: $IP_PUBLICO"

apt-get update -y
apt-get install -y nginx libfontconfig1 libicu74 || apt-get install -y nginx libfontconfig1

mkdir -p /opt/maximassas/web /opt/maximassas/api /etc/maximassas
HOME_UBUNTU="/home/ubuntu"
tar xzf "$HOME_UBUNTU/Web.tgz" -C /opt/maximassas/web
tar xzf "$HOME_UBUNTU/Api.tgz" -C /opt/maximassas/api
chmod +x /opt/maximassas/web/MaxiMassas.Web /opt/maximassas/api/MaxiMassas.Api
chown -R ubuntu:ubuntu /opt/maximassas

CONN="Server=$ENDPOINT,1433;Database=MaxiMassas;User Id=admin;Password=$SENHA;TrustServerCertificate=True"
for p in web:5000 api:5001; do
  nome="${p%%:*}"; porta="${p##*:}"
  cat > "/etc/maximassas/$nome.env" <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:$porta
ConnectionStrings__Padrao="$CONN"
EmailSettings__UrlBase=http://$IP_PUBLICO
Banco__AplicarMigrationsAoIniciar=false
EOF
  chmod 600 "/etc/maximassas/$nome.env"
done

criar_servico() { # nome exe
  cat > "/etc/systemd/system/maximassas-$1.service" <<EOF
[Unit]
Description=Maxi Massas $1
After=network.target

[Service]
User=ubuntu
WorkingDirectory=/opt/maximassas/$1
ExecStart=/opt/maximassas/$1/$2
EnvironmentFile=/etc/maximassas/$1.env
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF
}
criar_servico web MaxiMassas.Web
criar_servico api MaxiMassas.Api

cat > /etc/nginx/sites-available/maximassas <<'EOF'
server {
    listen 80 default_server;
    client_max_body_size 10m;
    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
server {
    listen 8080;
    client_max_body_size 10m;
    location / {
        proxy_pass http://127.0.0.1:5001;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF
rm -f /etc/nginx/sites-enabled/default
ln -sf /etc/nginx/sites-available/maximassas /etc/nginx/sites-enabled/maximassas
nginx -t

systemctl daemon-reload
systemctl enable --now maximassas-web maximassas-api
systemctl restart maximassas-web maximassas-api nginx
sleep 5
systemctl --no-pager --lines=3 status maximassas-web maximassas-api | grep -E "Active|●" || true
echo
echo "Web: http://$IP_PUBLICO   |   Api: http://$IP_PUBLICO:8080"
