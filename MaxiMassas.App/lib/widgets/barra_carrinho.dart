import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/formatos.dart';
import '../core/tema.dart';
import '../providers/carrinho_provider.dart';
import '../screens/carrinho/carrinho_screen.dart';

class BarraCarrinho extends StatelessWidget {
  const BarraCarrinho({super.key});

  @override
  Widget build(BuildContext context) {
    final carrinho = context.watch<CarrinhoProvider>();
    if (carrinho.vazio) return const SizedBox.shrink();
    final falta = carrinho.config?.faltaParaEntregaGratis(carrinho.subtotal);
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
        child: Material(
          color: Tema.vinho,
          borderRadius: BorderRadius.circular(12),
          elevation: 6,
          child: InkWell(
            borderRadius: BorderRadius.circular(12),
            onTap: () => abrirCarrinho(context),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.shopping_cart_outlined, color: Colors.white),
                      const SizedBox(width: 10),
                      Text('Ver carrinho (${carrinho.totalItens})',
                          style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16)),
                      const Spacer(),
                      Text(reais(carrinho.subtotal),
                          style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16)),
                    ],
                  ),
                  if (falta != null)
                    Padding(
                      padding: const EdgeInsets.only(top: 4),
                      child: Text('Faltam ${reais(falta)} para a entrega ficar grátis',
                          style: const TextStyle(color: Colors.white70, fontSize: 12)),
                    ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
