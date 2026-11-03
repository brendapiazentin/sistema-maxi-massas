/// Horário livre para entrega ou retirada, calculado pela API conforme o funcionamento da loja.
class Horario {
  Horario({required this.valor, required this.dia, required this.hora});

  /// Enviado de volta no pedido, ex.: "03/11/2026 18:30".
  final String valor;

  /// Ex.: "Hoje", "Amanhã", "Qua, 04/11".
  final String dia;

  /// Ex.: "18:30".
  final String hora;

  factory Horario.fromJson(Map<String, dynamic> j) => Horario(
        valor: j['valor'] as String,
        dia: j['dia'] as String,
        hora: j['hora'] as String,
      );
}
