import 'package:flutter/foundation.dart';

import '../core/api_client.dart';
import '../models/pedido.dart';
import '../services/pedido_service.dart';

/// Histórico de pedidos do cliente (aba Pedidos).
class PedidosProvider extends ChangeNotifier {
  PedidosProvider(this.servico);

  final PedidoService servico;
  List<Pedido> _pedidos = [];
  bool _carregando = false;
  String? _erro;
  bool _carregou = false;

  List<Pedido> get pedidos => _pedidos;
  bool get carregando => _carregando;
  String? get erro => _erro;
  bool get carregou => _carregou;

  Future<void> carregar() async {
    _carregando = true;
    _erro = null;
    notifyListeners();
    try {
      _pedidos = await servico.listar();
      _carregou = true;
    } on ApiException catch (e) {
      _erro = e.mensagem;
    } catch (_) {
      _erro = 'Não foi possível carregar seus pedidos.';
    }
    _carregando = false;
    notifyListeners();
  }

  /// Chamado ao sair da conta: o próximo cliente não pode ver estes pedidos.
  void limpar() {
    _pedidos = [];
    _carregou = false;
    _erro = null;
    notifyListeners();
  }
}
