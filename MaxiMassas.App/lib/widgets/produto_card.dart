import 'package:flutter/material.dart';

import '../core/config.dart';
import '../core/formatos.dart';
import '../models/produto.dart';
import 'seletor_quantidade.dart';

class ProdutoCard extends StatelessWidget {
  const ProdutoCard({super.key, required this.produto, this.mostrarDivisor = true});

  final Produto produto;
  final bool mostrarDivisor;

  @override
  Widget build(BuildContext context) {
    final p = produto;
    final tema = Theme.of(context);
    return Opacity(
      opacity: p.disponivel ? 1 : 0.5,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _Foto(url: p.fotoUrl),
                const SizedBox(width: 12),
                Expanded(
                  child: SizedBox(
                    height: _Foto.tamanho,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(p.nome,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: tema.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                        if (p.descricao.isNotEmpty)
                          Text(p.descricao,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: tema.textTheme.bodySmall?.copyWith(color: Colors.black54)),
                        const Spacer(),
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.center,
                          children: [
                            Text(
                              reais(p.preco),
                              style: tema.textTheme.titleSmall?.copyWith(
                                fontWeight: FontWeight.bold,
                                color: tema.colorScheme.primary,
                              ),
                            ),
                            const Spacer(),
                            if (!p.disponivel)
                              const Text('Em falta',
                                  style: TextStyle(color: Colors.red, fontWeight: FontWeight.bold, fontSize: 13))
                            else
                              SeletorQuantidade(produto: p),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
          if (mostrarDivisor) Divider(height: 1, indent: 16, endIndent: 16, color: Colors.grey.shade300),
        ],
      ),
    );
  }
}

class _Foto extends StatelessWidget {
  const _Foto({this.url});

  final String? url;

  static const double tamanho = 72;

  @override
  Widget build(BuildContext context) {
    final vazio = Container(
      width: tamanho,
      height: tamanho,
      decoration: BoxDecoration(color: Colors.grey.shade200, borderRadius: BorderRadius.circular(8)),
      child: Icon(Icons.restaurant, color: Colors.grey.shade500, size: 28),
    );
    if (url == null) return vazio;
    return ClipRRect(
      borderRadius: BorderRadius.circular(8),
      child: Image.network(
        Config.urlAbsoluta(url!),
        width: tamanho,
        height: tamanho,
        fit: BoxFit.cover,
        errorBuilder: (context, erro, pilha) => vazio,
        loadingBuilder: (context, filho, progresso) => progresso == null
            ? filho
            : const SizedBox(
                width: tamanho,
                height: tamanho,
                child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
              ),
      ),
    );
  }
}
