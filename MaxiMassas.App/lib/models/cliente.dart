import 'endereco.dart';

class Cliente {
  Cliente({
    this.id,
    required this.nome,
    required this.email,
    required this.telefone,
    this.enderecos = const [],
  });

  final int? id;
  final String nome;
  final String email;
  final String telefone;
  final List<Endereco> enderecos;

  /// Endereço principal; se nenhum estiver marcado, o primeiro salvo.
  Endereco? get enderecoPrincipal {
    if (enderecos.isEmpty) return null;
    return enderecos.firstWhere((e) => e.principal, orElse: () => enderecos.first);
  }

  factory Cliente.fromJson(Map<String, dynamic> j) {
    // Aceita o perfil direto ou dentro de uma chave "cliente".
    final p = j['cliente'] is Map<String, dynamic> ? j['cliente'] as Map<String, dynamic> : j;
    final lista = p['enderecos'];
    return Cliente(
      id: p['id'] as int?,
      nome: (p['nome'] ?? '') as String,
      email: (p['email'] ?? '') as String,
      telefone: (p['telefone'] ?? '') as String,
      enderecos: lista is List
          ? lista.map((e) => Endereco.fromJson(e as Map<String, dynamic>)).toList()
          : const [],
    );
  }
}
