class Cliente {
  Cliente({required this.nome, required this.email, required this.telefone});

  final String nome;
  final String email;
  final String telefone;

  factory Cliente.fromJson(Map<String, dynamic> j) {
    // Aceita o perfil direto ou dentro de uma chave "cliente".
    final p = j['cliente'] is Map<String, dynamic> ? j['cliente'] as Map<String, dynamic> : j;
    return Cliente(
      nome: (p['nome'] ?? '') as String,
      email: (p['email'] ?? '') as String,
      telefone: (p['telefone'] ?? '') as String,
    );
  }
}
