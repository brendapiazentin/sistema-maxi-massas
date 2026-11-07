import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:provider/provider.dart';

import '../../core/ajuda.dart';
import '../../core/tema.dart';
import '../../models/produto.dart';
import '../../providers/cardapio_provider.dart';
import '../../providers/carrinho_provider.dart';
import '../../widgets/produto_card.dart';

/// Cardápio com busca por nome, filtro por categoria e carregamento em esqueleto.
class CardapioScreen extends StatefulWidget {
  const CardapioScreen({super.key});

  @override
  State<CardapioScreen> createState() => _CardapioScreenState();
}

class _CardapioScreenState extends State<CardapioScreen> {
  final _busca = TextEditingController();
  String _termo = '';
  String? _categoria; // nulo = todas

  @override
  void dispose() {
    _busca.dispose();
    super.dispose();
  }

  Future<void> _atualizar() async {
    final cardapio = context.read<CardapioProvider>();
    final carrinho = context.read<CarrinhoProvider>();
    await cardapio.carregar();
    carrinho.sincronizar(cardapio.produtos);
  }

  /// Compara sem acento e sem diferença de maiúsculas ("lasanha" encontra "Lasanha à Bolonhesa").
  static String _normalizar(String s) {
    const de = 'áàâãäéèêëíìîïóòôõöúùûüçÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇ';
    const para = 'aaaaaeeeeiiiiooooouuuucAAAAAEEEEIIIIOOOOOUUUUC';
    final b = StringBuffer();
    for (final c in s.split('')) {
      final i = de.indexOf(c);
      b.write(i >= 0 ? para[i] : c);
    }
    return b.toString().toLowerCase().trim();
  }

  bool _combina(Produto p) {
    if (_categoria != null && p.categoria != _categoria) return false;
    if (_termo.isEmpty) return true;
    final alvo = _normalizar('${p.nome} ${p.descricao} ${p.categoria}');
    return alvo.contains(_termo);
  }

  @override
  Widget build(BuildContext context) {
    final cardapio = context.watch<CardapioProvider>();
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Tema.vinho,
        foregroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        centerTitle: false,
        title: Row(
          children: [
            Image.asset(
              'assets/logo.png',
              height: 36,
              errorBuilder: (c, e, s) => const Text('Maxi Massas Itápolis', style: TextStyle(color: Colors.white)),
            ),
            const SizedBox(width: 10),
            const Icon(Icons.location_on, color: Colors.white, size: 16),
            const SizedBox(width: 2),
            const Expanded(
              child: Text(
                'Rua Joaquim Nabuco, 47\nJardim Karina',
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: Colors.white, fontSize: 12, height: 1.2),
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            onPressed: () => mostrarHorariosLoja(context),
            tooltip: 'Horários da loja',
            icon: const Icon(Icons.info_outline, color: Colors.white),
          ),
          IconButton(
            onPressed: () => abrirWhatsAppLoja(context),
            tooltip: 'Falar com a loja no WhatsApp',
            icon: const FaIcon(FontAwesomeIcons.whatsapp, color: Colors.white),
          ),
        ],
      ),
      body: Column(
        children: [
          _campoBusca(),
          _chipsCategorias(cardapio),
          Expanded(child: _corpo(cardapio)),
        ],
      ),
    );
  }

  Widget _campoBusca() => Padding(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
        child: TextField(
          controller: _busca,
          textInputAction: TextInputAction.search,
          onChanged: (v) => setState(() => _termo = _normalizar(v)),
          decoration: InputDecoration(
            hintText: 'Buscar no cardápio',
            prefixIcon: const Icon(Icons.search),
            suffixIcon: _termo.isEmpty
                ? null
                : IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: () => setState(() {
                      _busca.clear();
                      _termo = '';
                    }),
                  ),
            filled: true,
            fillColor: Colors.grey.shade100,
            isDense: true,
            contentPadding: const EdgeInsets.symmetric(vertical: 12),
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: BorderSide.none),
          ),
        ),
      );

  Widget _chipsCategorias(CardapioProvider c) {
    final categorias = c.porCategoria.keys.toList();
    if (categorias.length < 2) return const SizedBox(height: 4);
    Widget chip(String rotulo, String? valor) {
      final ativo = _categoria == valor;
      return Padding(
        padding: const EdgeInsets.only(right: 8),
        child: ChoiceChip(
          label: Text(rotulo),
          selected: ativo,
          showCheckmark: false,
          selectedColor: Tema.vinho,
          backgroundColor: Colors.white,
          labelStyle: TextStyle(color: ativo ? Colors.white : Colors.black87, fontWeight: FontWeight.w600),
          side: BorderSide(color: ativo ? Tema.vinho : Colors.grey.shade300),
          onSelected: (_) => setState(() => _categoria = valor),
        ),
      );
    }

    return SizedBox(
      height: 52,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.fromLTRB(16, 8, 8, 4),
        children: [
          chip('Todos', null),
          for (final cat in categorias) chip(cat, cat),
        ],
      ),
    );
  }

  Widget _corpo(CardapioProvider c) {
    if (c.carregando && c.produtos.isEmpty) return const _Esqueleto();
    if (c.erro != null && c.produtos.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(c.erro!, textAlign: TextAlign.center),
              const SizedBox(height: 12),
              FilledButton(onPressed: _atualizar, child: const Text('Tentar de novo')),
            ],
          ),
        ),
      );
    }

    final grupos = <String, List<Produto>>{};
    c.porCategoria.forEach((cat, lista) {
      final filtrados = lista.where(_combina).toList();
      if (filtrados.isNotEmpty) grupos[cat] = filtrados;
    });
    final entradas = grupos.entries.toList();

    return RefreshIndicator(
      onRefresh: _atualizar,
      child: entradas.isEmpty
          ? ListView(
              children: const [
                SizedBox(height: 80),
                Icon(Icons.search_off, size: 48, color: Colors.black38),
                SizedBox(height: 8),
                Center(child: Text('Nenhum produto encontrado.', style: TextStyle(color: Colors.black54))),
              ],
            )
          : ListView(
              padding: const EdgeInsets.only(bottom: 24),
              children: [
                for (var i = 0; i < entradas.length; i++) ...[
                  if (i > 0) Container(height: 8, color: Colors.grey.shade200),
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 14, 16, 2),
                    child: Text(
                      entradas[i].key,
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                    ),
                  ),
                  for (var j = 0; j < entradas[i].value.length; j++)
                    ProdutoCard(
                      produto: entradas[i].value[j],
                      mostrarDivisor: j < entradas[i].value.length - 1,
                    ),
                ],
              ],
            ),
    );
  }
}

/// Linhas cinza pulsando no formato do cardápio enquanto ele carrega.
class _Esqueleto extends StatefulWidget {
  const _Esqueleto();

  @override
  State<_Esqueleto> createState() => _EsqueletoState();
}

class _EsqueletoState extends State<_Esqueleto> with SingleTickerProviderStateMixin {
  late final AnimationController _anim =
      AnimationController(vsync: this, duration: const Duration(milliseconds: 900))..repeat(reverse: true);

  @override
  void dispose() {
    _anim.dispose();
    super.dispose();
  }

  Widget _bloco(double largura, double altura) => Container(
        width: largura,
        height: altura,
        decoration: BoxDecoration(color: Colors.grey.shade300, borderRadius: BorderRadius.circular(6)),
      );

  @override
  Widget build(BuildContext context) {
    return FadeTransition(
      opacity: Tween(begin: 0.4, end: 1.0).animate(_anim),
      child: ListView.builder(
        physics: const NeverScrollableScrollPhysics(),
        itemCount: 6,
        itemBuilder: (context, i) => Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          child: Row(
            children: [
              _bloco(72, 72),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _bloco(160, 14),
                    const SizedBox(height: 8),
                    _bloco(220, 12),
                    const SizedBox(height: 20),
                    _bloco(70, 14),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
