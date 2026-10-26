/// Regra da taxa de entrega definida pelos gestores no Sistema Web.
class ConfigEntrega {
  const ConfigEntrega({required this.taxaEntrega, required this.pedidoMinimoEntregaGratis});

  final double taxaEntrega;
  final double pedidoMinimoEntregaGratis;

  factory ConfigEntrega.fromJson(Map<String, dynamic> j) => ConfigEntrega(
        taxaEntrega: (j['taxaEntrega'] as num).toDouble(),
        pedidoMinimoEntregaGratis: (j['pedidoMinimoEntregaGratis'] as num).toDouble(),
      );

  /// Mesma regra do servidor: a partir do pedido mínimo a entrega sai grátis (zero = sempre cobra).
  double calcularTaxa(double subtotal) =>
      pedidoMinimoEntregaGratis > 0 && subtotal >= pedidoMinimoEntregaGratis ? 0 : taxaEntrega;

  /// Quanto falta para a entrega ficar grátis; nulo se já é grátis ou se não há entrega grátis.
  double? faltaParaEntregaGratis(double subtotal) =>
      pedidoMinimoEntregaGratis > 0 && subtotal < pedidoMinimoEntregaGratis
          ? pedidoMinimoEntregaGratis - subtotal
          : null;
}
