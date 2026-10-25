import 'package:flutter/material.dart';

import '../core/config.dart';
import '../core/formatos.dart';
import '../models/produto.dart';

class ProdutoCard extends StatelessWidget {
  const ProdutoCard({super.key, required this.produto});

  final Produto produto;

  @override
  Widget build(BuildContext context) {
    final p = produto;
    final tema = Theme.of(context);
    return Opacity(
      opacity: p.disponivel ? 1 : 0.5,
      child: Card(
        margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
        clipBehavior: Clip.antiAlias,
        child: Row(
          children: [
            _Foto(url: p.fotoUrl),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(p.nome, style: tema.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                    if (p.descricao.isNotEmpty)
                      Padding(
                        padding: const EdgeInsets.only(top: 2),
                        child: Text(p.descricao, maxLines: 2, overflow: TextOverflow.ellipsis, style: tema.textTheme.bodySmall),
                      ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        Text(
                          reais(p.preco),
                          style: tema.textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.bold,
                            color: tema.colorScheme.primary,
                          ),
                        ),
                        const Spacer(),
                        if (!p.disponivel)
                          const Text('Em falta', style: TextStyle(color: Colors.red, fontWeight: FontWeight.bold)),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Foto extends StatelessWidget {
  const _Foto({this.url});

  final String? url;

  static const double _tamanho = 96;

  @override
  Widget build(BuildContext context) {
    final vazio = Container(
      width: _tamanho,
      height: _tamanho,
      color: Colors.black12,
      child: const Icon(Icons.restaurant, color: Colors.black38, size: 36),
    );
    if (url == null) return vazio;
    return Image.network(
      Config.urlAbsoluta(url!),
      width: _tamanho,
      height: _tamanho,
      fit: BoxFit.cover,
      errorBuilder: (context, erro, pilha) => vazio,
      loadingBuilder: (context, filho, progresso) => progresso == null
          ? filho
          : const SizedBox(
              width: _tamanho,
              height: _tamanho,
              child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
            ),
    );
  }
}
