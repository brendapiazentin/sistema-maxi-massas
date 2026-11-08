#!/usr/bin/env bash
# Uso (no Git Bash, na pasta raiz da solution):
#   bash deploy/publicar-apk.sh IP_DO_SERVIDOR caminho/maximassas-key.pem
# Publica o APK já compilado em
#   https://maximassasitapolis.duckdns.org/downloads/maximassas.apk
set -euo pipefail
IP="${1:?Informe o IP publico do servidor}"
CHAVE="${2:?Informe o caminho do arquivo .pem}"
APK="MaxiMassas.App/build/app/outputs/flutter-apk/app-release.apk"
[ -f "$APK" ] || { echo "APK nao encontrado. Rode antes: flutter build apk --release"; exit 1; }

echo ">> Enviando para $IP ..."
scp -i "$CHAVE" -o StrictHostKeyChecking=accept-new "$APK" "ubuntu@$IP:~/maximassas.apk"
scp -i "$CHAVE" deploy/servidor-downloads.sh "ubuntu@$IP:~/"
ssh -i "$CHAVE" "ubuntu@$IP" "sudo bash ~/servidor-downloads.sh"
