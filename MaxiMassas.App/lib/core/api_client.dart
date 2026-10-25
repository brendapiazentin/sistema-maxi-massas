import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import 'config.dart';

/// Erro já com mensagem pronta para mostrar ao cliente.
class ApiException implements Exception {
  ApiException(this.mensagem, {this.status});

  final String mensagem;
  final int? status;

  @override
  String toString() => mensagem;
}

/// Fala com a API. A autenticação é HTTP Basic (e-mail e senha do cliente), reenviada a cada chamada.
class ApiClient {
  ApiClient({http.Client? cliente}) : _cliente = cliente ?? http.Client();

  final http.Client _cliente;
  String? _email;
  String? _senha;

  bool get temCredenciais => _email != null && _senha != null;

  void definirCredenciais(String? email, String? senha) {
    _email = email;
    _senha = senha;
  }

  Map<String, String> get _cabecalhos {
    final h = <String, String>{
      'Accept': 'application/json',
      'Content-Type': 'application/json',
    };
    if (temCredenciais) {
      h['Authorization'] = 'Basic ${base64Encode(utf8.encode('$_email:$_senha'))}';
    }
    return h;
  }

  Uri _uri(String caminho) => Uri.parse('${Config.baseUrl}$caminho');

  Future<dynamic> get(String caminho) =>
      _enviar(() => _cliente.get(_uri(caminho), headers: _cabecalhos));

  Future<dynamic> post(String caminho, [Object? corpo]) => _enviar(() =>
      _cliente.post(_uri(caminho), headers: _cabecalhos, body: corpo == null ? null : jsonEncode(corpo)));

  Future<dynamic> put(String caminho, [Object? corpo]) => _enviar(() =>
      _cliente.put(_uri(caminho), headers: _cabecalhos, body: corpo == null ? null : jsonEncode(corpo)));

  Future<dynamic> delete(String caminho) =>
      _enviar(() => _cliente.delete(_uri(caminho), headers: _cabecalhos));

  /// Executa a chamada e devolve o campo "data" do envelope { success, message, data, errors }.
  Future<dynamic> _enviar(Future<http.Response> Function() chamada) async {
    http.Response resposta;
    try {
      resposta = await chamada().timeout(const Duration(seconds: 20));
    } on TimeoutException {
      throw ApiException('O servidor demorou para responder. Tente novamente.');
    } catch (_) {
      throw ApiException('Sem conexão com o servidor. Verifique sua internet.');
    }

    dynamic corpo;
    if (resposta.bodyBytes.isNotEmpty) {
      try {
        corpo = jsonDecode(utf8.decode(resposta.bodyBytes));
      } catch (_) {
        corpo = null;
      }
    }

    final ok = resposta.statusCode >= 200 && resposta.statusCode < 300;
    if (ok && corpo is Map<String, dynamic> && corpo['success'] == true) {
      return corpo['data'];
    }
    if (ok && corpo == null) return null;

    if (resposta.statusCode == 401) {
      throw ApiException('E-mail ou senha incorretos.', status: 401);
    }
    var mensagem = 'Não foi possível concluir a operação.';
    if (corpo is Map<String, dynamic>) {
      final erros = corpo['errors'];
      if (erros is List && erros.isNotEmpty) {
        mensagem = erros.join('\n');
      } else if (corpo['message'] is String && (corpo['message'] as String).isNotEmpty) {
        mensagem = corpo['message'] as String;
      }
    }
    throw ApiException(mensagem, status: resposta.statusCode);
  }
}
