import 'endereco.dart';

enum TipoEntrega { entrega, retirada }

/// Mesmos nomes do enum FormaPagamento da API (enviados como texto).
enum FormaPagamento {
  dinheiro('Dinheiro', 'Dinheiro'),
  pix('Pix', 'Pix'),
  cartaoDebito('CartaoDebito', 'Cartão de débito'),
  cartaoCredito('CartaoCredito', 'Cartão de crédito');

  const FormaPagamento(this.api, this.rotulo);

  final String api;
  final String rotulo;

  static String rotuloDe(String api) => switch (api) {
        'Dinheiro' => 'Dinheiro',
        'Pix' => 'Pix',
        'CartaoDebito' => 'Cartão de débito',
        'CartaoCredito' => 'Cartão de crédito',
        'Transferencia' => 'Transferência',
        _ => 'Outro',
      };
}

/// Etapa mostrada ao cliente, traduzida do StatusVenda da API.
enum EtapaPedido { emPreparo, saiu, concluido, cancelado }

class ItemPedido {
  ItemPedido({
    required this.produtoId,
    required this.nome,
    required this.quantidade,
    required this.precoUnitario,
    required this.subtotal,
  });

  final int produtoId;
  final String nome;
  final int quantidade;
  final double precoUnitario;
  final double subtotal;

  factory ItemPedido.fromJson(Map<String, dynamic> j) => ItemPedido(
        produtoId: j['produtoId'] as int,
        nome: j['nome'] as String? ?? '',
        quantidade: j['quantidade'] as int,
        precoUnitario: (j['precoUnitario'] as num).toDouble(),
        subtotal: (j['subtotal'] as num).toDouble(),
      );
}

class Pedido {
  Pedido({
    required this.id,
    required this.numero,
    required this.data,
    required this.status,
    required this.subtotal,
    required this.taxaEntrega,
    required this.total,
    required this.tipoEntrega,
    this.endereco,
    this.horario,
    required this.formaPagamento,
    this.observacao,
    required this.itens,
  });

  final int id;
  final String numero;
  final DateTime data;
  final String status;
  final double subtotal;
  final double taxaEntrega;
  final double total;
  final TipoEntrega tipoEntrega;
  final Endereco? endereco;
  final String? horario;
  final String formaPagamento;
  final String? observacao;
  final List<ItemPedido> itens;

  bool get ehEntrega => tipoEntrega == TipoEntrega.entrega;

  factory Pedido.fromJson(Map<String, dynamic> j) => Pedido(
        id: j['id'] as int,
        numero: j['numero'] as String? ?? '#${j['id']}',
        data: DateTime.parse(j['data'] as String),
        status: j['status'] as String? ?? '',
        subtotal: (j['subtotal'] as num).toDouble(),
        taxaEntrega: (j['taxaEntrega'] as num).toDouble(),
        total: (j['total'] as num).toDouble(),
        tipoEntrega: j['tipoEntrega'] == 'Retirada' ? TipoEntrega.retirada : TipoEntrega.entrega,
        endereco: j['endereco'] is Map<String, dynamic>
            ? Endereco.fromJson(j['endereco'] as Map<String, dynamic>)
            : null,
        horario: j['horario'] as String?,
        formaPagamento: j['formaPagamento'] as String? ?? '',
        observacao: j['observacao'] as String?,
        itens: (j['itens'] as List? ?? [])
            .map((i) => ItemPedido.fromJson(i as Map<String, dynamic>))
            .toList(),
      );

  /// Aberta/EmProcessamento/Paga = em preparo; EmEntrega = saiu (ou pronto); Concluida; Cancelada.
  EtapaPedido get etapa => switch (status) {
        'EmEntrega' => EtapaPedido.saiu,
        'Concluida' => EtapaPedido.concluido,
        'Cancelada' => EtapaPedido.cancelado,
        _ => EtapaPedido.emPreparo,
      };

  /// Rótulos das etapas, diferentes para entrega e retirada.
  List<String> get rotulosEtapas => ehEntrega
      ? const ['Em preparo', 'Saiu para entrega', 'Entregue']
      : const ['Em preparo', 'Pronto para retirada', 'Retirado'];

  String get rotuloStatus =>
      etapa == EtapaPedido.cancelado ? 'Cancelado' : rotulosEtapas[etapa.index];
}
