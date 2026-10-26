import '../core/api_client.dart';
import '../models/config_entrega.dart';

class EntregaService {
  EntregaService(this._api);

  final ApiClient _api;

  Future<ConfigEntrega> obter() async {
    final dados = await _api.get('/entrega') as Map<String, dynamic>;
    return ConfigEntrega.fromJson(dados);
  }
}
