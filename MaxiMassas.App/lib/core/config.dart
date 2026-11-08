/// Endereço do servidor (ambiente de demonstração na AWS).
class Config {
  static const String host = 'https://maximassasitapolis.duckdns.org';
  static const String baseUrl = '$host/api/app';

  /// As fotos vêm da API como caminho relativo; esta função monta o endereço completo.
  static String urlAbsoluta(String caminho) => '$host$caminho';

  /// Versão exibida no rodapé do perfil (manter igual à do pubspec.yaml).
  static const String versaoApp = '1.0.1';

  /// Link do APK Android (publicado pelo deploy/publicar-apk.sh).
  static const String urlApk = '$host/downloads/maximassas.apk';
}
