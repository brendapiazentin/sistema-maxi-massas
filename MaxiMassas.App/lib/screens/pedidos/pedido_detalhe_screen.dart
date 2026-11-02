import 'dart:async';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/ajuda.dart';
import '../../core/api_client.dart';
import '../../core/formatos.dart';
import '../../core/tema.dart';
import '../../models/pedido.dart';
import '../../providers/cardapio_provider.dart';
import '../../providers/carrinho_provider.dart';
import '../../providers/pedidos_provider.dart';
import '../carrinho/carrinho_screen.dart';

/// Detalhe de um pedido: etapas, itens, entrega e "pedir novamente".
class PedidoDetalheScreen extends StatefulWidget {
  const PedidoDetalheScreen({super.key, required this.pedido, this.recemCriado = false});

  final Pedido pedido;
  final bool recemCriado;

  @override
  State<PedidoDetalheScreen> createState() => _PedidoDetalheScreenState();
}

class _PedidoDetalheScreenState extends State<PedidoDetalheScreen> {
  late Pedido _pedido = widget.pedido;
  late final PedidosProvider _pedidos = context.read<PedidosProvider>();
  Timer? _timer;

  bool get _finalizado =>
      _pedido.etapa == EtapaPedido.concluido || _pedido.etapa == EtapaPedido.cancelado;

  @override
  void initState() {
    super.initState();
    // Status mudado pela loja no Sistema Web aparece aqui sem o cliente precisar atualizar.
    _timer = Timer.periodic(const Duration(seconds: 20), (_) {
      if (!_finalizado) _atualizar(silencioso: true);
    });
    if (!widget.recemCriado) _atualizar(silencioso: true);
  }

  @override
  void dispose() {
    _timer?.cancel();
    // A lista da aba Pedidos fica com o mesmo status do detalhe (depois que a tela sai).
    Future.microtask(_pedidos.carregar);
    super.dispose();
  }

  Future<void> _atualizar({bool silencioso = false}) async {
    try {
      final p = await _pedidos.servico.obter(_pedido.id);
      if (mounted) setState(() => _pedido = p);
    } on ApiException catch (e) {
      if (mounted && !silencioso) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.mensagem)));
      }
    }
  }

  void _pedirNovamente() {
    final cardapio = context.read<CardapioProvider>();
    final carrinho = context.read<CarrinhoProvider>();
    var faltaram = 0;
    for (final item in _pedido.itens) {
      final produto = cardapio.produtos.where((p) => p.id == item.produtoId).firstOrNull;
      var adicionados = 0;
      while (produto != null && adicionados < item.quantidade && carrinho.podeAdicionar(produto)) {
        carrinho.adicionar(produto);
        adicionados++;
      }
      faltaram += item.quantidade - adicionados;
    }
    if (carrinho.vazio) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Os itens deste pedido não estão disponíveis no momento.')),
      );
      return;
    }
    if (faltaram > 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Alguns itens estão em falta e não foram adicionados.')),
      );
    }
    abrirCarrinho(context);
  }

  @override
  Widget build(BuildContext context) {
    final p = _pedido;
    final data = DateFormat("dd/MM/yyyy 'às' HH:mm").format(p.data);
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Tema.vinho,
        foregroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        title: Text('Pedido ${p.numero}'),
      ),
      body: RefreshIndicator(
        onRefresh: _atualizar,
        child: ListView(
          padding: const EdgeInsets.only(bottom: 24),
          children: [
            if (widget.recemCriado)
              Container(
                margin: const EdgeInsets.all(16),
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.green.shade50,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Row(
                  children: [
                    Icon(Icons.check_circle, color: Colors.green, size: 32),
                    SizedBox(width: 12),
                    Expanded(
                      child: Text('Pedido recebido! A loja já foi avisada.',
                          style: TextStyle(fontWeight: FontWeight.w600)),
                    ),
                  ],
                ),
              ),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
              child: Text('Feito em $data', style: const TextStyle(color: Colors.black54)),
            ),
            _Etapas(pedido: p),
            const Divider(height: 32),
            _secao(context, p.ehEntrega ? 'Entrega' : 'Retirada na loja'),
            if (p.ehEntrega && p.endereco != null)
              ListTile(
                leading: const Icon(Icons.location_on_outlined),
                title: Text(p.endereco!.linha1),
                subtitle: Text(p.endereco!.linha2),
              ),
            if ((p.horario ?? '').isNotEmpty)
              ListTile(
                leading: const Icon(Icons.schedule),
                title: Text('Horário: ${p.horario}'),
              ),
            ListTile(
              leading: const Icon(Icons.payments_outlined),
              title: Text(FormaPagamento.rotuloDe(p.formaPagamento)),
              subtitle: Text(p.ehEntrega ? 'Pagamento ao entregador' : 'Pagamento na retirada'),
            ),
            if ((p.observacao ?? '').isNotEmpty)
              ListTile(leading: const Icon(Icons.notes), title: Text(p.observacao!)),
            const Divider(height: 32),
            _secao(context, 'Itens'),
            for (final i in p.itens)
              ListTile(
                dense: true,
                title: Text('${i.quantidade}x ${i.nome}'),
                trailing: Text(reais(i.subtotal)),
              ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Column(
                children: [
                  const Divider(),
                  _linha('Subtotal', reais(p.subtotal)),
                  if (p.ehEntrega) _linha('Taxa de entrega', p.taxaEntrega == 0 ? 'Grátis' : reais(p.taxaEntrega)),
                  _linha('Total', reais(p.total), negrito: true),
                ],
              ),
            ),
            const SizedBox(height: 16),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  FilledButton.icon(
                    onPressed: _pedirNovamente,
                    style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48), backgroundColor: Tema.vinho),
                    icon: const Icon(Icons.replay),
                    label: const Text('Pedir novamente'),
                  ),
                  const SizedBox(height: 8),
                  OutlinedButton.icon(
                    onPressed: () => abrirWhatsAppLoja(context),
                    style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(48)),
                    icon: const Icon(Icons.help_outline),
                    label: const Text('Precisa de ajuda?'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _secao(BuildContext context, String texto) => Padding(
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 4),
        child: Text(texto, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
      );
}

Widget _linha(String rotulo, String valor, {bool negrito = false}) {
  final estilo = negrito ? const TextStyle(fontWeight: FontWeight.bold, fontSize: 16) : null;
  return Padding(
    padding: const EdgeInsets.symmetric(vertical: 2),
    child: Row(children: [Text(rotulo, style: estilo), const Spacer(), Text(valor, style: estilo)]),
  );
}

/// Linha do tempo com as três etapas do pedido (ou o aviso de cancelado).
class _Etapas extends StatelessWidget {
  const _Etapas({required this.pedido});

  final Pedido pedido;

  @override
  Widget build(BuildContext context) {
    if (pedido.etapa == EtapaPedido.cancelado) {
      return const ListTile(
        leading: Icon(Icons.cancel, color: Colors.red),
        title: Text('Pedido cancelado', style: TextStyle(fontWeight: FontWeight.bold)),
        subtitle: Text('Em caso de dúvida, fale com a loja.'),
      );
    }
    final atual = pedido.indiceEtapa;
    final rotulos = pedido.rotulosEtapas;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Column(
        children: [
          for (var i = 0; i < rotulos.length; i++)
            Row(
              children: [
                Icon(
                  i < atual || (i == atual && i == rotulos.length - 1)
                      ? Icons.check_circle
                      : i == atual
                          ? Icons.radio_button_checked
                          : Icons.radio_button_unchecked,
                  color: i <= atual ? Tema.vinho : Colors.black26,
                ),
                const SizedBox(width: 12),
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  child: Text(
                    rotulos[i],
                    style: TextStyle(
                      fontWeight: i == atual ? FontWeight.bold : FontWeight.normal,
                      color: i <= atual ? Colors.black87 : Colors.black38,
                    ),
                  ),
                ),
              ],
            ),
        ],
      ),
    );
  }
}
