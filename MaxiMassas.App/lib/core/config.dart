/// Endereço do servidor (ambiente de demonstração na AWS).
class Config {
  static const String host = 'https://maximassasitapolis.duckdns.org';
  static const String baseUrl = '$host/api/app';

  /// As fotos vêm da API como caminho relativo; esta função monta o endereço completo.
  static String urlAbsoluta(String caminho) => '$host$caminho';
}
