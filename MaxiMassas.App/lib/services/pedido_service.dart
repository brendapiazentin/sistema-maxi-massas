import '../core/api_client.dart';
import '../models/endereco.dart';
import '../models/horario.dart';
import '../models/item_carrinho.dart';
import '../models/pedido.dart';

class PedidoService {
  PedidoService(this._api);

  final ApiClient _api;

  static String _tipo(TipoEntrega t) => t == TipoEntrega.entrega ? 'Entrega' : 'Retirada';

  Future<List<Horario>> horarios(TipoEntrega tipo) async {
    final dados = await _api.get('/horarios?tipo=${_tipo(tipo)}') as List;
    return dados.map((h) => Horario.fromJson(h as Map<String, dynamic>)).toList();
  }

  Future<Pedido> criar({
    required List<ItemCarrinho> itens,
    required TipoEntrega tipo,
    Endereco? endereco,
    required String horario,
    required FormaPagamento pagamento,
    String? observacao,
  }) async {
    final dados = await _api.post('/pedidos', {
      'itens': [
        for (final i in itens) {'produtoId': i.produto.id, 'quantidade': i.quantidade},
      ],
      'tipoEntrega': _tipo(tipo),
      'endereco': tipo == TipoEntrega.entrega ? endereco?.toJson() : null,
      'horario': horario,
      'formaPagamento': pagamento.api,
      'observacao': (observacao ?? '').trim().isEmpty ? null : observacao!.trim(),
    }) as Map<String, dynamic>;
    return Pedido.fromJson(dados);
  }

  Future<List<Pedido>> listar() async {
    final dados = await _api.get('/pedidos') as List;
    return dados.map((p) => Pedido.fromJson(p as Map<String, dynamic>)).toList();
  }

  Future<Pedido> obter(int id) async {
    final dados = await _api.get('/pedidos/$id') as Map<String, dynamic>;
    return Pedido.fromJson(dados);
  }
}
