import 'package:flutter/foundation.dart';

import '../models/config_entrega.dart';
import '../models/item_carrinho.dart';
import '../models/produto.dart';
import '../services/entrega_service.dart';

class CarrinhoProvider extends ChangeNotifier {
  CarrinhoProvider(this._entrega);

  final EntregaService _entrega;
  final Map<int, ItemCarrinho> _itens = {};
  ConfigEntrega? _config;

  List<ItemCarrinho> get itens => _itens.values.toList();
  bool get vazio => _itens.isEmpty;
  ConfigEntrega? get config => _config;
  int get totalItens => _itens.values.fold(0, (soma, i) => soma + i.quantidade);
  double get subtotal => _itens.values.fold(0.0, (soma, i) => soma + i.subtotal);

  int quantidadeDe(int produtoId) => _itens[produtoId]?.quantidade ?? 0;

  /// Só deixa adicionar se o produto está disponível e ainda há estoque.
  bool podeAdicionar(Produto p) => p.disponivel && quantidadeDe(p.id) < p.estoque;

  void adicionar(Produto p) {
    if (!podeAdicionar(p)) return;
    _itens[p.id] = ItemCarrinho(produto: p, quantidade: quantidadeDe(p.id) + 1);
    notifyListeners();
  }

  void remover(Produto p) {
    final atual = quantidadeDe(p.id);
    if (atual == 0) return;
    if (atual == 1) {
      _itens.remove(p.id);
    } else {
      _itens[p.id] = ItemCarrinho(produto: p, quantidade: atual - 1);
    }
    notifyListeners();
  }

  void limpar() {
    if (_itens.isEmpty) return;
    _itens.clear();
    notifyListeners();
  }

  /// Depois de recarregar o cardápio: tira produtos que saíram do ar e ajusta preço e estoque.
  void sincronizar(List<Produto> cardapio) {
    final porId = {for (final p in cardapio) p.id: p};
    for (final id in _itens.keys.toList()) {
      final novo = porId[id];
      final item = _itens[id]!;
      if (novo == null || !novo.disponivel) {
        _itens.remove(id);
        continue;
      }
      final qtd = item.quantidade > novo.estoque ? novo.estoque : item.quantidade;
      _itens[id] = ItemCarrinho(produto: novo, quantidade: qtd);
    }
    notifyListeners();
  }

  Future<void> carregarConfigEntrega() async {
    try {
      _config = await _entrega.obter();
      notifyListeners();
    } catch (_) {
      // Sem a taxa o carrinho continua funcionando; ela só não aparece no resumo.
    }
  }
}
