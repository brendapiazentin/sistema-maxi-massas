import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../models/produto.dart';
import '../providers/carrinho_provider.dart';

/// Botão "+" quando o produto não está no carrinho; depois, "−", quantidade e "+".
class SeletorQuantidade extends StatelessWidget {
  const SeletorQuantidade({super.key, required this.produto});

  final Produto produto;

  static final _compacto = IconButton.styleFrom(
    fixedSize: const Size(32, 32),
    minimumSize: const Size(32, 32),
    padding: EdgeInsets.zero,
    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
  );

  @override
  Widget build(BuildContext context) {
    if (!produto.disponivel) return const SizedBox.shrink();
    final carrinho = context.watch<CarrinhoProvider>();
    final qtd = carrinho.quantidadeDe(produto.id);

    if (qtd == 0) {
      return IconButton.filled(
        onPressed: () => carrinho.adicionar(produto),
        icon: const Icon(Icons.add, size: 18),
        tooltip: 'Adicionar',
        style: _compacto,
      );
    }

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        IconButton.outlined(
          onPressed: () => carrinho.remover(produto),
          icon: const Icon(Icons.remove, size: 18),
          tooltip: 'Diminuir',
          style: _compacto,
        ),
        SizedBox(
          width: 28,
          child: Text('$qtd', textAlign: TextAlign.center, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
        ),
        IconButton.filled(
          onPressed: carrinho.podeAdicionar(produto) ? () => carrinho.adicionar(produto) : null,
          icon: const Icon(Icons.add, size: 18),
          tooltip: carrinho.podeAdicionar(produto) ? 'Aumentar' : 'Estoque máximo',
          style: _compacto,
        ),
      ],
    );
  }
}
