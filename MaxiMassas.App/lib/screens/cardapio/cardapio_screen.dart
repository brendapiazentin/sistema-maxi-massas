import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/cardapio_provider.dart';
import '../../widgets/produto_card.dart';

class CardapioScreen extends StatelessWidget {
  const CardapioScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final cardapio = context.watch<CardapioProvider>();
    return Scaffold(
      appBar: AppBar(title: const Text('Maxi Massas Itápolis')),
      body: _corpo(context, cardapio),
    );
  }

  Widget _corpo(BuildContext context, CardapioProvider c) {
    if (c.carregando && c.produtos.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }
    if (c.erro != null && c.produtos.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(c.erro!, textAlign: TextAlign.center),
              const SizedBox(height: 12),
              FilledButton(onPressed: c.carregar, child: const Text('Tentar de novo')),
            ],
          ),
        ),
      );
    }
    final grupos = c.porCategoria.entries.toList();
    return RefreshIndicator(
      onRefresh: c.carregar,
      child: ListView(
        padding: const EdgeInsets.only(bottom: 24),
        children: [
          for (final g in grupos) ...[
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
              child: Text(
                g.key,
                style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
              ),
            ),
            for (final p in g.value) ProdutoCard(produto: p),
          ],
        ],
      ),
    );
  }
}
