import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/formatos.dart';
import '../../core/tema.dart';
import '../../models/pedido.dart';
import '../../providers/pedidos_provider.dart';
import 'pedido_detalhe_screen.dart';

/// Aba Pedidos: histórico do cliente, mais recentes primeiro.
class PedidosScreen extends StatefulWidget {
  const PedidosScreen({super.key});

  @override
  State<PedidosScreen> createState() => _PedidosScreenState();
}

class _PedidosScreenState extends State<PedidosScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final pedidos = context.read<PedidosProvider>();
      if (!pedidos.carregou && !pedidos.carregando) pedidos.carregar();
    });
  }

  @override
  Widget build(BuildContext context) {
    final pedidos = context.watch<PedidosProvider>();
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Tema.vinho,
        foregroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        title: const Text('Meus pedidos'),
      ),
      body: RefreshIndicator(
        onRefresh: pedidos.carregar,
        child: _corpo(pedidos),
      ),
    );
  }

  Widget _corpo(PedidosProvider c) {
    if (c.carregando && c.pedidos.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }
    if (c.erro != null && c.pedidos.isEmpty) {
      return ListView(
        children: [
          const SizedBox(height: 120),
          Center(child: Text(c.erro!, textAlign: TextAlign.center)),
          const SizedBox(height: 12),
          Center(child: FilledButton(onPressed: c.carregar, child: const Text('Tentar de novo'))),
        ],
      );
    }
    if (c.pedidos.isEmpty) {
      // ListView para o "puxar para atualizar" funcionar também com a lista vazia.
      return ListView(
        children: const [
          SizedBox(height: 120),
          Icon(Icons.receipt_long_outlined, size: 56, color: Colors.black38),
          SizedBox(height: 12),
          Center(child: Text('Nenhum pedido ainda', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600))),
          SizedBox(height: 4),
          Center(
            child: Text('Quando você fizer um pedido, ele aparece aqui.', style: TextStyle(color: Colors.black54)),
          ),
        ],
      );
    }
    final formato = DateFormat('dd/MM/yyyy HH:mm');
    return ListView.separated(
      itemCount: c.pedidos.length,
      separatorBuilder: (_, i) => Divider(height: 1, color: Colors.grey.shade300),
      itemBuilder: (context, i) {
        final p = c.pedidos[i];
        final resumo = p.itens.map((it) => '${it.quantidade}x ${it.nome}').join(', ');
        return ListTile(
          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
          title: Row(
            children: [
              Text(p.numero, style: const TextStyle(fontWeight: FontWeight.bold)),
              const Spacer(),
              _Selo(pedido: p),
            ],
          ),
          subtitle: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: 4),
              Text(resumo, maxLines: 1, overflow: TextOverflow.ellipsis),
              Text('${formato.format(p.data)}  •  ${p.ehEntrega ? 'Entrega' : 'Retirada'}  •  ${reais(p.total)}'),
            ],
          ),
          onTap: () => Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => PedidoDetalheScreen(pedido: p)),
          ),
        );
      },
    );
  }
}

class _Selo extends StatelessWidget {
  const _Selo({required this.pedido});

  final Pedido pedido;

  @override
  Widget build(BuildContext context) {
    final cor = switch (pedido.etapa) {
      EtapaPedido.cancelado => Colors.red,
      EtapaPedido.concluido => Colors.green,
      _ => Tema.vinho,
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(color: cor.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(12)),
      child: Text(pedido.rotuloStatus, style: TextStyle(color: cor, fontSize: 12, fontWeight: FontWeight.w600)),
    );
  }
}
