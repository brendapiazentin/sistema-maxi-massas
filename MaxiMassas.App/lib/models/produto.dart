class Produto {
  Produto({
    required this.id,
    required this.nome,
    required this.descricao,
    required this.preco,
    required this.categoria,
    required this.disponivel,
    required this.estoque,
    this.fotoUrl,
  });

  final int id;
  final String nome;
  final String descricao;
  final double preco;
  final String categoria;
  final bool disponivel;
  final int estoque;
  final String? fotoUrl;

  factory Produto.fromJson(Map<String, dynamic> j) => Produto(
        id: j['id'] as int,
        nome: j['nome'] as String? ?? '',
        descricao: j['descricao'] as String? ?? '',
        preco: (j['preco'] as num).toDouble(),
        categoria: j['categoria'] as String? ?? 'Outros',
        disponivel: j['disponivel'] as bool? ?? false,
        estoque: j['estoque'] as int? ?? 0,
        fotoUrl: j['fotoUrl'] as String?,
      );
}
