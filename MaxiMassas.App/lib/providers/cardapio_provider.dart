import 'package:flutter/foundation.dart';

import '../core/api_client.dart';
import '../models/produto.dart';
import '../services/produto_service.dart';

class CardapioProvider extends ChangeNotifier {
  CardapioProvider(this._servico);

  final ProdutoService _servico;
  List<Produto> _produtos = [];
  bool _carregando = false;
  String? _erro;

  List<Produto> get produtos => _produtos;
  bool get carregando => _carregando;
  String? get erro => _erro;

  /// Produtos agrupados por categoria, mantendo a ordem em que a API devolveu.
  Map<String, List<Produto>> get porCategoria {
    final mapa = <String, List<Produto>>{};
    for (final p in _produtos) {
      mapa.putIfAbsent(p.categoria, () => []).add(p);
    }
    return mapa;
  }

  Future<void> carregar() async {
    _carregando = true;
    _erro = null;
    notifyListeners();
    try {
      _produtos = await _servico.listar();
    } on ApiException catch (e) {
      _erro = e.mensagem;
    } catch (_) {
      _erro = 'Não foi possível carregar o cardápio.';
    }
    _carregando = false;
    notifyListeners();
  }
}
