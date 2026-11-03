/// Endereço salvo no perfil do cliente (ou o endereço de um pedido, que não tem id).
class Endereco {
  Endereco({
    this.id,
    required this.logradouro,
    required this.numero,
    this.complemento,
    required this.bairro,
    this.cep,
    required this.cidade,
    required this.estado,
    this.principal = false,
  });

  final int? id;
  final String logradouro;
  final String numero;
  final String? complemento;
  final String bairro;
  final String? cep;
  final String cidade;
  final String estado;
  final bool principal;

  factory Endereco.fromJson(Map<String, dynamic> j) => Endereco(
        id: j['id'] as int?,
        logradouro: j['logradouro'] as String? ?? '',
        numero: j['numero'] as String? ?? '',
        complemento: j['complemento'] as String?,
        bairro: j['bairro'] as String? ?? '',
        cep: j['cep'] as String?,
        cidade: j['cidade'] as String? ?? '',
        estado: j['estado'] as String? ?? '',
        principal: j['principal'] as bool? ?? false,
      );

  Map<String, dynamic> toJson() => {
        'logradouro': logradouro,
        'numero': numero,
        'complemento': complemento,
        'bairro': bairro,
        'cep': cep,
        'cidade': cidade,
        'estado': estado,
        'principal': principal,
      };

  /// Ex.: "Rua das Flores, 120 - Apto 3"
  String get linha1 {
    final comp = (complemento ?? '').trim();
    return '$logradouro, $numero${comp.isEmpty ? '' : ' - $comp'}';
  }

  /// Ex.: "Centro - Itápolis/SP"
  String get linha2 => '$bairro - $cidade/$estado';
}
