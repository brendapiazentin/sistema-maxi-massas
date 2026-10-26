import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/formatos.dart';
import '../../core/tema.dart';
import '../../providers/carrinho_provider.dart';
import '../../widgets/seletor_quantidade.dart';

void abrirCarrinho(BuildContext context) {
  Navigator.of(context).push(MaterialPageRoute(builder: (_) => const CarrinhoScreen()));
}

class CarrinhoScreen extends StatelessWidget {
  const CarrinhoScreen({super.key});

  Future<void> _esvaziar(BuildContext context) async {
    final carrinho = context.read<CarrinhoProvider>();
    final confirmou = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Esvaziar carrinho?'),
        content: const Text('Todos os itens serão removidos.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Esvaziar')),
        ],
      ),
    );
    if (confirmou == true) carrinho.limpar();
  }

  @override
  Widget build(BuildContext context) {
    final carrinho = context.watch<CarrinhoProvider>();
    final tema = Theme.of(context);
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Tema.vinho,
        foregroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        title: const Text('Carrinho'),
        actions: [
          if (!carrinho.vazio)
            IconButton(
              onPressed: () => _esvaziar(context),
              icon: const Icon(Icons.delete_outline),
              tooltip: 'Esvaziar carrinho',
            ),
        ],
      ),
      body: carrinho.vazio
          ? const Center(child: Text('Seu carrinho está vazio.'))
          : Column(
              children: [
                Expanded(
                  child: ListView.separated(
                    itemCount: carrinho.itens.length,
                    separatorBuilder: (context, i) => const Divider(height: 1, indent: 16, endIndent: 16),
                    itemBuilder: (context, i) {
                      final item = carrinho.itens[i];
                      return Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                        child: Row(
                          children: [
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(item.produto.nome,
                                      style: tema.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                                  Text('${reais(item.produto.preco)} cada', style: tema.textTheme.bodySmall),
                                  const SizedBox(height: 4),
                                  Text(
                                    reais(item.subtotal),
                                    style: tema.textTheme.titleMedium?.copyWith(
                                      fontWeight: FontWeight.bold,
                                      color: tema.colorScheme.primary,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            SeletorQuantidade(produto: item.produto),
                          ],
                        ),
                      );
                    },
                  ),
                ),
                _Resumo(carrinho: carrinho),
              ],
            ),
    );
  }
}

Widget _linha(String rotulo, String valor) => Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(children: [Text(rotulo), const Spacer(), Text(valor)]),
    );

class _Resumo extends StatelessWidget {
  const _Resumo({required this.carrinho});

  final CarrinhoProvider carrinho;

  @override
  Widget build(BuildContext context) {
    final config = carrinho.config;
    final subtotal = carrinho.subtotal;
    final pequeno = Theme.of(context).textTheme.bodySmall;
    final taxa = config?.calcularTaxa(subtotal);
    final falta = config?.faltaParaEntregaGratis(subtotal);
    return Material(
      elevation: 8,
      color: Colors.white,
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _linha('Subtotal (${carrinho.totalItens} itens)', reais(subtotal)),
              if (config != null) ...[
                _linha('Taxa de entrega', taxa == 0 ? 'Grátis' : reais(taxa!)),
                if (falta != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Text('Faltam ${reais(falta)} para a entrega ficar grátis.', style: pequeno),
                  ),
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Text('Retirada na loja não paga taxa.', style: pequeno),
                ),
              ],
              const SizedBox(height: 12),
              FilledButton(
                onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('Finalização do pedido: próxima etapa.')),
                ),
                style: FilledButton.styleFrom(
                  minimumSize: const Size.fromHeight(52),
                  backgroundColor: Tema.vinho,
                ),
                child: const Text('Continuar'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
