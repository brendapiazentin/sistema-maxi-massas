import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/ajuda.dart';
import '../../core/tema.dart';
import '../../providers/cardapio_provider.dart';
import "../../providers/auth_provider.dart";
import '../../providers/carrinho_provider.dart';
import '../../widgets/barra_carrinho.dart';
import '../../widgets/produto_card.dart';

class CardapioScreen extends StatelessWidget {
  const CardapioScreen({super.key});

  Future<void> _atualizar(BuildContext context) async {
    final cardapio = context.read<CardapioProvider>();
    final carrinho = context.read<CarrinhoProvider>();
    await cardapio.carregar();
    carrinho.sincronizar(cardapio.produtos);
  }

  @override
  Widget build(BuildContext context) {
    final cardapio = context.watch<CardapioProvider>();
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Tema.vinho,
        foregroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        centerTitle: false,
        title: Image.asset(
          'assets/logo.png',
          height: 36,
          errorBuilder: (c, e, s) => const Text('Maxi Massas Itápolis', style: TextStyle(color: Colors.white)),
        ),
        actions: [
          IconButton(
            onPressed: () {
              context.read<CarrinhoProvider>().limpar();
              context.read<AuthProvider>().sair();
            },
            tooltip: "Sair",
            icon: const Icon(Icons.logout, color: Colors.white),
          ),
          IconButton(
            onPressed: () => abrirWhatsAppLoja(context),
            tooltip: 'Precisa de ajuda?',
            icon: const Icon(Icons.help_outline, color: Colors.white),
          ),
        ],
      ),
      body: _corpo(context, cardapio),
      bottomNavigationBar: const BarraCarrinho(),
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
              FilledButton(onPressed: () => _atualizar(context), child: const Text('Tentar de novo')),
            ],
          ),
        ),
      );
    }
    final grupos = c.porCategoria.entries.toList();
    return RefreshIndicator(
      onRefresh: () => _atualizar(context),
      child: ListView(
        padding: const EdgeInsets.only(bottom: 24),
        children: [
          for (var i = 0; i < grupos.length; i++) ...[
            if (i > 0) Container(height: 8, color: Colors.grey.shade200),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 14, 16, 2),
              child: Text(
                grupos[i].key,
                style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
              ),
            ),
            for (var j = 0; j < grupos[i].value.length; j++)
              ProdutoCard(
                produto: grupos[i].value[j],
                mostrarDivisor: j < grupos[i].value.length - 1,
              ),
          ],
        ],
      ),
    );
  }
}
