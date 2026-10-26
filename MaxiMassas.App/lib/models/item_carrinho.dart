import 'produto.dart';

class ItemCarrinho {
  ItemCarrinho({required this.produto, required this.quantidade});

  final Produto produto;
  final int quantidade;

  double get subtotal => produto.preco * quantidade;
}
