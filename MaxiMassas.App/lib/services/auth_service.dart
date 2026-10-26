import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../core/api_client.dart';
import '../models/cliente.dart';

class AuthService {
  AuthService(this._api, [FlutterSecureStorage? storage])
      : _storage = storage ?? const FlutterSecureStorage();

  final ApiClient _api;
  final FlutterSecureStorage _storage;

  static const _chaveEmail = 'email';
  static const _chaveSenha = 'senha';

  /// Retoma a sessão salva no aparelho; devolve nulo se não houver ou se as credenciais não valem mais.
  Future<Cliente?> restaurar() async {
    final email = await _storage.read(key: _chaveEmail);
    final senha = await _storage.read(key: _chaveSenha);
    if (email == null || senha == null) return null;
    try {
      return await entrar(email, senha, guardar: false);
    } on ApiException catch (e) {
      if (e.status == 401) await _limpar();
      return null;
    }
  }

  /// Valida as credenciais com GET /me (a API não tem token).
  Future<Cliente> entrar(String email, String senha, {bool guardar = true}) async {
    _api.definirCredenciais(email.trim(), senha);
    try {
      final dados = await _api.get('/me') as Map<String, dynamic>;
      if (guardar) {
        await _storage.write(key: _chaveEmail, value: email.trim());
        await _storage.write(key: _chaveSenha, value: senha);
      }
      return Cliente.fromJson(dados);
    } catch (_) {
      _api.definirCredenciais(null, null);
      rethrow;
    }
  }

  Future<void> cadastrar({
    required String nome,
    required String email,
    required String telefone,
    required String senha,
  }) async {
    await _api.post('/cadastro', {
      'nome': nome.trim(),
      'email': email.trim(),
      'telefone': telefone,
      'senha': senha,
    });
  }

  Future<void> esqueciSenha(String email) async {
    await _api.post('/senha/esqueci', {'email': email.trim()});
  }

  Future<void> redefinirSenha({required String email, required String codigo, required String novaSenha}) async {
    await _api.post('/senha/redefinir', {
      'email': email.trim(),
      'codigo': codigo.trim(),
      'novaSenha': novaSenha,
    });
  }

  Future<void> sair() async {
    _api.definirCredenciais(null, null);
    await _limpar();
  }

  Future<void> _limpar() async {
    await _storage.delete(key: _chaveEmail);
    await _storage.delete(key: _chaveSenha);
  }
}
