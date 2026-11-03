import '../core/api_client.dart';
import '../models/produto.dart';

class ProdutoService {
  ProdutoService(this._api);

  final ApiClient _api;

  /// Cardápio: só produtos ativos; os sem estoque vêm como indisponíveis ("Em falta").
  Future<List<Produto>> listar() async {
    final dados = await _api.get('/produtos') as List<dynamic>;
    return dados.map((e) => Produto.fromJson(e as Map<String, dynamic>)).toList();
  }
}
