import '../core/api_client.dart';
import '../core/mascaras.dart';
import '../models/cliente.dart';
import '../models/endereco.dart';

/// Endereços salvos. Toda operação devolve o perfil completo já atualizado pelo servidor.
class PerfilService {
  PerfilService(this._api);

  final ApiClient _api;

  Future<Cliente> obter() async => Cliente.fromJson(await _api.get('/me') as Map<String, dynamic>);

  Future<Endereco> buscarCep(String cep) async =>
      Endereco.fromJson(await _api.get('/cep/${soDigitos(cep)}') as Map<String, dynamic>);

  Future<Cliente> adicionarEndereco(Endereco e) async =>
      Cliente.fromJson(await _api.post('/enderecos', e.toJson()) as Map<String, dynamic>);

  Future<Cliente> removerEndereco(int id) async =>
      Cliente.fromJson(await _api.delete('/enderecos/$id') as Map<String, dynamic>);

  Future<Cliente> definirPrincipal(int id) async =>
      Cliente.fromJson(await _api.put('/enderecos/$id/principal') as Map<String, dynamic>);
}
