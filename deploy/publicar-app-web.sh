#!/usr/bin/env bash
# Uso (no Git Bash, na pasta raiz da solution):
#   bash deploy/publicar-app-web.sh IP_DO_SERVIDOR caminho/maximassas-key.pem
# Compila a versão web do app Flutter (para iPhone e navegador) e publica em
#   https://maximassasitapolis.duckdns.org/app/
# Na primeira vez, também ensina o nginx a servir a pasta /app/ (o resto do site não muda).
set -euo pipefail
IP="${1:?Informe o IP publico do servidor}"
CHAVE="${2:?Informe o caminho do arquivo .pem}"
SAIDA="deploy/saida"
mkdir -p "$SAIDA"

echo ">> Compilando o app para web ..."
# MSYS_NO_PATHCONV: impede o Git Bash de trocar "/app/" por "C:/Program Files/Git/app/".
(cd MaxiMassas.App && MSYS_NO_PATHCONV=1 flutter build web --release --base-href /app/)
tar czf "$SAIDA/AppWeb.tgz" -C MaxiMassas.App/build/web .

echo ">> Enviando para $IP ..."
scp -i "$CHAVE" -o StrictHostKeyChecking=accept-new "$SAIDA/AppWeb.tgz" deploy/servidor-app-web.sh "ubuntu@$IP:~/"
ssh -i "$CHAVE" "ubuntu@$IP" "sudo bash ~/servidor-app-web.sh"
