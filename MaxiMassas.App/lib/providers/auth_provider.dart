import 'package:flutter/foundation.dart';

import '../models/cliente.dart';
import '../services/auth_service.dart';

enum EstadoAuth { iniciando, deslogado, logado }

class AuthProvider extends ChangeNotifier {
  AuthProvider(this._servico);

  final AuthService _servico;
  EstadoAuth _estado = EstadoAuth.iniciando;
  Cliente? _cliente;

  EstadoAuth get estado => _estado;
  Cliente? get cliente => _cliente;
  AuthService get servico => _servico;

  Future<void> iniciar() async {
    try {
      _cliente = await _servico.restaurar();
    } catch (_) {
      _cliente = null;
    }
    _estado = _cliente == null ? EstadoAuth.deslogado : EstadoAuth.logado;
    notifyListeners();
  }

  Future<void> entrar(String email, String senha) async {
    _cliente = await _servico.entrar(email, senha);
    _estado = EstadoAuth.logado;
    notifyListeners();
  }

  Future<void> cadastrar({
    required String nome,
    required String email,
    required String telefone,
    required String senha,
  }) async {
    await _servico.cadastrar(nome: nome, email: email, telefone: telefone, senha: senha);
    await entrar(email, senha);
  }

  Future<void> sair() async {
    await _servico.sair();
    _cliente = null;
    _estado = EstadoAuth.deslogado;
    notifyListeners();
  }
}
