import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/api_client.dart';
import '../../core/formatos.dart';
import '../../core/tema.dart';
import '../../models/endereco.dart';
import '../../models/horario.dart';
import '../../models/pedido.dart';
import '../../providers/auth_provider.dart';
import '../../providers/cardapio_provider.dart';
import '../../providers/carrinho_provider.dart';
import '../../providers/pedidos_provider.dart';
import '../enderecos/endereco_form_screen.dart';
import '../pedidos/pedido_detalhe_screen.dart';

/// Última etapa: entrega ou retirada, endereço, horário, pagamento e observação.
class FinalizarPedidoScreen extends StatefulWidget {
  const FinalizarPedidoScreen({super.key});

  @override
  State<FinalizarPedidoScreen> createState() => _FinalizarPedidoScreenState();
}

class _FinalizarPedidoScreenState extends State<FinalizarPedidoScreen> {
  TipoEntrega _tipo = TipoEntrega.entrega;
  Endereco? _endereco;
  List<Horario> _horarios = [];
  Horario? _horario;
  bool _carregandoHorarios = false;
  String? _erroHorarios;
  FormaPagamento? _pagamento;
  final _observacao = TextEditingController();
  bool _enviando = false;

  @override
  void initState() {
    super.initState();
    _endereco = context.read<AuthProvider>().cliente?.enderecoPrincipal;
    _carregarHorarios();
  }

  @override
  void dispose() {
    _observacao.dispose();
    super.dispose();
  }

  Future<void> _carregarHorarios() async {
    setState(() {
      _carregandoHorarios = true;
      _erroHorarios = null;
      _horario = null;
    });
    try {
      final lista = await context.read<PedidosProvider>().servico.horarios(_tipo);
      if (!mounted) return;
      setState(() {
        _horarios = lista;
        _horario = lista.isEmpty ? null : lista.first;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _erroHorarios = e.mensagem);
    } finally {
      if (mounted) setState(() => _carregandoHorarios = false);
    }
  }

  void _trocarTipo(TipoEntrega tipo) {
    if (tipo == _tipo) return;
    setState(() => _tipo = tipo);
    _carregarHorarios();
  }

  Future<void> _escolherEndereco() async {
    final enderecos = context.read<AuthProvider>().cliente?.enderecos ?? const <Endereco>[];
    if (enderecos.isEmpty) return _novoEndereco();
    final escolhido = await showModalBottomSheet<Object>(
      context: context,
      showDragHandle: true,
      backgroundColor: Colors.white,
      builder: (ctx) => SafeArea(
        child: ListView(
          shrinkWrap: true,
          children: [
            const Padding(
              padding: EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: Text('Entregar em', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            ),
            for (final e in enderecos)
              ListTile(
                leading: Icon(e.id == _endereco?.id ? Icons.radio_button_checked : Icons.radio_button_off,
                    color: Tema.vinho),
                title: Text(e.linha1),
                subtitle: Text(e.linha2),
                onTap: () => Navigator.pop(ctx, e),
              ),
            ListTile(
              leading: const Icon(Icons.add, color: Tema.vinho),
              title: const Text('Adicionar novo endereço', style: TextStyle(color: Tema.vinho)),
              onTap: () => Navigator.pop(ctx, 'novo'),
            ),
          ],
        ),
      ),
    );
    if (escolhido is Endereco) {
      setState(() => _endereco = escolhido);
    } else if (escolhido == 'novo') {
      await _novoEndereco();
    }
  }

  Future<void> _novoEndereco() async {
    final novo = await Navigator.of(context)
        .push<Endereco>(MaterialPageRoute(builder: (_) => const EnderecoFormScreen()));
    if (novo != null && mounted) setState(() => _endereco = novo);
  }

  String? _pendencia() {
    if (_tipo == TipoEntrega.entrega && _endereco == null) return 'Informe o endereço de entrega.';
    if (_horario == null) return 'Escolha o horário.';
    if (_pagamento == null) return 'Escolha a forma de pagamento.';
    return null;
  }

  Future<void> _enviar() async {
    final falta = _pendencia();
    if (falta != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(falta)));
      return;
    }
    final carrinho = context.read<CarrinhoProvider>();
    final pedidos = context.read<PedidosProvider>();
    final cardapio = context.read<CardapioProvider>();
    final navegador = Navigator.of(context);
    setState(() => _enviando = true);
    try {
      final pedido = await pedidos.servico.criar(
        itens: carrinho.itens,
        tipo: _tipo,
        endereco: _endereco,
        horario: _horario!.valor,
        pagamento: _pagamento!,
        observacao: _observacao.text,
      );
      carrinho.limpar();
      pedidos.carregar();
      cardapio.carregar(); // o estoque mudou
      navegador.popUntil((r) => r.isFirst);
      navegador.push(MaterialPageRoute(
        builder: (_) => PedidoDetalheScreen(pedido: pedido, recemCriado: true),
      ));
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() => _enviando = false);
      await showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('Não foi possível enviar o pedido'),
          content: Text(e.mensagem),
          actions: [FilledButton(onPressed: () => Navigator.pop(ctx), child: const Text('Entendi'))],
        ),
      );
      // Horário pode ter expirado ou o estoque mudou: atualiza as opções.
      _carregarHorarios();
    }
  }

  @override
  Widget build(BuildContext context) {
    final carrinho = context.watch<CarrinhoProvider>();
    final subtotal = carrinho.subtotal;
    final taxa = _tipo == TipoEntrega.entrega ? (carrinho.config?.calcularTaxa(subtotal) ?? 0.0) : 0.0;
    final total = subtotal + taxa;

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Tema.vinho,
        foregroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        title: const Text('Finalizar pedido'),
      ),
      body: ListView(
        padding: const EdgeInsets.only(bottom: 24),
        children: [
          const _Titulo('Como você quer receber?'),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: SegmentedButton<TipoEntrega>(
              segments: const [
                ButtonSegment(value: TipoEntrega.entrega, label: Text('Entrega'), icon: Icon(Icons.delivery_dining)),
                ButtonSegment(value: TipoEntrega.retirada, label: Text('Retirada'), icon: Icon(Icons.storefront)),
              ],
              selected: {_tipo},
              onSelectionChanged: (s) => _trocarTipo(s.first),
              showSelectedIcon: false,
            ),
          ),
          if (_tipo == TipoEntrega.entrega) ...[
            const _Titulo('Endereço de entrega'),
            _endereco == null
                ? Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    child: OutlinedButton.icon(
                      onPressed: _novoEndereco,
                      icon: const Icon(Icons.add_location_alt_outlined),
                      label: const Text('Adicionar endereço'),
                    ),
                  )
                : ListTile(
                    leading: const Icon(Icons.location_on_outlined, color: Tema.vinho),
                    title: Text(_endereco!.linha1),
                    subtitle: Text(_endereco!.linha2),
                    trailing: TextButton(onPressed: _escolherEndereco, child: const Text('Trocar')),
                  ),
          ] else
            const Padding(
              padding: EdgeInsets.fromLTRB(16, 12, 16, 0),
              child: Text('Retire seu pedido na loja Maxi Massas Itápolis. Retirada não paga taxa de entrega.',
                  style: TextStyle(color: Colors.black54)),
            ),
          _Titulo(_tipo == TipoEntrega.entrega ? 'Horário da entrega' : 'Horário da retirada'),
          _seletorHorario(),
          const _Titulo('Pagamento'),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 4),
            child: Text(
              _tipo == TipoEntrega.entrega ? 'Você paga ao entregador.' : 'Você paga na retirada.',
              style: const TextStyle(color: Colors.black54),
            ),
          ),
          RadioGroup<FormaPagamento>(
            groupValue: _pagamento,
            onChanged: (v) => setState(() => _pagamento = v),
            child: Column(
              children: [
                for (final f in FormaPagamento.values)
                  RadioListTile<FormaPagamento>(
                    value: f,
                    activeColor: Tema.vinho,
                    dense: true,
                    title: Text(f.rotulo),
                  ),
              ],
            ),
          ),
          const _Titulo('Observação (opcional)'),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: TextField(
              controller: _observacao,
              maxLength: 500,
              maxLines: 3,
              minLines: 2,
              decoration: const InputDecoration(
                hintText: 'Ex.: troco para R\$ 100, ponto de referência...',
                border: OutlineInputBorder(),
              ),
            ),
          ),
          const _Titulo('Resumo'),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Column(
              children: [
                _linha('Subtotal (${carrinho.totalItens} itens)', reais(subtotal)),
                if (_tipo == TipoEntrega.entrega) _linha('Taxa de entrega', taxa == 0 ? 'Grátis' : reais(taxa)),
                const Divider(),
                _linha('Total', reais(total), negrito: true),
              ],
            ),
          ),
        ],
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: FilledButton(
            onPressed: _enviando || carrinho.vazio ? null : _enviar,
            style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52), backgroundColor: Tema.vinho),
            child: _enviando
                ? const SizedBox(
                    width: 22, height: 22, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                : Text('Fazer pedido • ${reais(total)}'),
          ),
        ),
      ),
    );
  }

  Widget _seletorHorario() {
    if (_carregandoHorarios) {
      return const Padding(padding: EdgeInsets.all(16), child: Center(child: CircularProgressIndicator()));
    }
    if (_erroHorarios != null) {
      return ListTile(
        title: Text(_erroHorarios!),
        trailing: TextButton(onPressed: _carregarHorarios, child: const Text('Tentar de novo')),
      );
    }
    if (_horarios.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(horizontal: 16),
        child: Text('Não há horários disponíveis nos próximos dias.'),
      );
    }
    final h = _horario;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16),
      child: InkWell(
        borderRadius: BorderRadius.circular(4),
        onTap: _abrirSeletorHorario,
        child: InputDecorator(
          decoration: const InputDecoration(
            border: OutlineInputBorder(),
            prefixIcon: Icon(Icons.schedule),
            suffixIcon: Icon(Icons.keyboard_arrow_down),
          ),
          child: Text(h == null ? 'Escolher horário' : '${h.dia}, ${h.hora}',
              style: const TextStyle(fontSize: 16)),
        ),
      ),
    );
  }

  /// Rolagem em duas colunas (dia e hora), no estilo dos seletores de horário do celular.
  Future<void> _abrirSeletorHorario() async {
    final porDia = <String, List<Horario>>{};
    for (final h in _horarios) {
      porDia.putIfAbsent(h.dia, () => []).add(h);
    }
    final dias = porDia.keys.toList();
    var iDia = _horario == null ? 0 : dias.indexOf(_horario!.dia);
    if (iDia < 0) iDia = 0;
    var iHora = _horario == null ? 0 : porDia[dias[iDia]]!.indexWhere((h) => h.valor == _horario!.valor);
    if (iHora < 0) iHora = 0;

    final escolhido = await showModalBottomSheet<Horario>(
      context: context,
      backgroundColor: Colors.white,
      showDragHandle: true,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setSheet) {
          final horas = porDia[dias[iDia]]!;
          return SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(_tipo == TipoEntrega.entrega ? 'Horário da entrega' : 'Horário da retirada',
                      textAlign: TextAlign.center,
                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                  SizedBox(
                    height: 200,
                    child: Row(
                      children: [
                        Expanded(
                          child: CupertinoPicker(
                            itemExtent: 40,
                            scrollController: FixedExtentScrollController(initialItem: iDia),
                            onSelectedItemChanged: (i) => setSheet(() {
                              iDia = i;
                              iHora = 0;
                            }),
                            children: [for (final d in dias) Center(child: Text(d))],
                          ),
                        ),
                        Expanded(
                          child: CupertinoPicker(
                            // A chave nova recria a coluna de horas quando o dia muda.
                            key: ValueKey(dias[iDia]),
                            itemExtent: 40,
                            scrollController: FixedExtentScrollController(initialItem: iHora),
                            onSelectedItemChanged: (i) => iHora = i,
                            children: [for (final h in horas) Center(child: Text(h.hora))],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 8),
                  FilledButton(
                    onPressed: () => Navigator.pop(ctx, horas[iHora < horas.length ? iHora : 0]),
                    style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48), backgroundColor: Tema.vinho),
                    child: const Text('Confirmar'),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
    if (escolhido != null && mounted) setState(() => _horario = escolhido);
  }
}

class _Titulo extends StatelessWidget {
  const _Titulo(this.texto);

  final String texto;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.fromLTRB(16, 20, 16, 8),
        child: Text(texto, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
      );
}

Widget _linha(String rotulo, String valor, {bool negrito = false}) {
  final estilo = negrito ? const TextStyle(fontWeight: FontWeight.bold, fontSize: 16) : null;
  return Padding(
    padding: const EdgeInsets.symmetric(vertical: 2),
    child: Row(children: [Text(rotulo, style: estilo), const Spacer(), Text(valor, style: estilo)]),
  );
}
