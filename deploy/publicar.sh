#!/usr/bin/env bash
# Uso (no Git Bash, na pasta raiz da solution):
#   bash deploy/publicar.sh IP_DO_SERVIDOR caminho/maximassas-key.pem
# Publica Web e Api para Linux (auto-contido, sem precisar instalar .NET no servidor)
# e envia os arquivos para o servidor EC2.
set -euo pipefail
IP="${1:?Informe o IP publico do servidor}"
CHAVE="${2:?Informe o caminho do arquivo .pem}"
chmod 400 "$CHAVE" 2>/dev/null || true
SAIDA="deploy/saida"
rm -rf "$SAIDA" && mkdir -p "$SAIDA"

for proj in Web Api; do
  echo ">> Publicando MaxiMassas.$proj ..."
  dotnet publish "MaxiMassas.$proj" -c Release -r linux-x64 --self-contained true -o "$SAIDA/$proj"
  tar czf "$SAIDA/$proj.tgz" -C "$SAIDA/$proj" .
done

echo ">> Enviando para $IP ..."
scp -i "$CHAVE" -o StrictHostKeyChecking=accept-new \
  "$SAIDA/Web.tgz" "$SAIDA/Api.tgz" deploy/servidor-instalar.sh "ubuntu@$IP:~/"
echo ">> Pronto. Agora entre no servidor:  ssh -i $CHAVE ubuntu@$IP"
echo "   e rode:  sudo bash servidor-instalar.sh"
