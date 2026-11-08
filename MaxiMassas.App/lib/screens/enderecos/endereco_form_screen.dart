import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import '../../core/api_client.dart';
import '../../core/mascaras.dart';
import '../../core/tema.dart';
import '../../models/endereco.dart';
import '../../providers/auth_provider.dart';
import '../../services/perfil_service.dart';

/// Cadastro de endereço: o CEP preenche rua, bairro, cidade e UF.
/// Devolve o endereço salvo (já com id) ao fechar.
class EnderecoFormScreen extends StatefulWidget {
  const EnderecoFormScreen({super.key});

  @override
  State<EnderecoFormScreen> createState() => _EnderecoFormScreenState();
}

class _EnderecoFormScreenState extends State<EnderecoFormScreen> {
  final _form = GlobalKey<FormState>();
  final _cep = TextEditingController();
  final _rua = TextEditingController();
  final _numero = TextEditingController();
  final _complemento = TextEditingController();
  final _bairro = TextEditingController();
  final _cidade = TextEditingController();
  final _uf = TextEditingController();
  final _focoNumero = FocusNode();

  bool _buscandoCep = false;
  bool _salvando = false;
  bool _principal = false;

  late final PerfilService _servico = PerfilService(context.read<ApiClient>());

  @override
  void initState() {
    super.initState();
    // O primeiro endereço salvo já nasce como principal.
    _principal = (context.read<AuthProvider>().cliente?.enderecos ?? const []).isEmpty;
  }

  @override
  void dispose() {
    for (final c in [_cep, _rua, _numero, _complemento, _bairro, _cidade, _uf]) {
      c.dispose();
    }
    _focoNumero.dispose();
    super.dispose();
  }

  Future<void> _buscarCep() async {
    if (soDigitos(_cep.text).length != 8) return;
    setState(() => _buscandoCep = true);
    try {
      final e = await _servico.buscarCep(_cep.text);
      _rua.text = e.logradouro;
      _bairro.text = e.bairro;
      _cidade.text = e.cidade;
      _uf.text = e.estado;
      _focoNumero.requestFocus();
    } on ApiException catch (e) {
      _avisar(e.mensagem);
    } finally {
      if (mounted) setState(() => _buscandoCep = false);
    }
  }

  Future<void> _salvar() async {
    if (!_form.currentState!.validate()) return;
    setState(() => _salvando = true);
    final auth = context.read<AuthProvider>();
    final idsAntes = (auth.cliente?.enderecos ?? const <Endereco>[]).map((e) => e.id).toSet();
    try {
      final cliente = await _servico.adicionarEndereco(Endereco(
        logradouro: _rua.text.trim(),
        numero: _numero.text.trim(),
        complemento: _complemento.text.trim().isEmpty ? null : _complemento.text.trim(),
        bairro: _bairro.text.trim(),
        cep: soDigitos(_cep.text),
        cidade: _cidade.text.trim(),
        estado: _uf.text.trim().toUpperCase(),
        principal: _principal,
      ));
      auth.atualizarCliente(cliente);
      // O endereço novo é o que não existia antes.
      final novo = cliente.enderecos.where((e) => !idsAntes.contains(e.id)).firstOrNull;
      if (mounted) Navigator.pop(context, novo);
    } on ApiException catch (e) {
      _avisar(e.mensagem);
    } finally {
      if (mounted) setState(() => _salvando = false);
    }
  }

  void _avisar(String msg) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));

  String? _obrigatorio(String? v) => (v ?? '').trim().isEmpty ? 'Campo obrigatório' : null;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Tema.vinho,
        foregroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        title: const Text('Novo endereço'),
      ),
      body: Form(
        key: _form,
        child: ListView(
          padding: EdgeInsets.fromLTRB(16, 16, 16, 16 + MediaQuery.of(context).padding.bottom),
          children: [
            const Text('Entregamos somente em Itápolis/SP.', style: TextStyle(color: Colors.black54)),
            const SizedBox(height: 12),
            TextFormField(
              controller: _cep,
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly, LengthLimitingTextInputFormatter(8)],
              decoration: InputDecoration(
                labelText: 'CEP',
                hintText: 'Somente números',
                border: const OutlineInputBorder(),
                suffixIcon: _buscandoCep
                    ? const Padding(
                        padding: EdgeInsets.all(12),
                        child: SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2)),
                      )
                    : IconButton(icon: const Icon(Icons.search), onPressed: _buscarCep),
              ),
              onChanged: (v) {
                if (v.length == 8) _buscarCep();
              },
              validator: (v) => soDigitos(v ?? '').length == 8 ? null : 'Informe os 8 números do CEP',
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _rua,
              textCapitalization: TextCapitalization.words,
              decoration: const InputDecoration(labelText: 'Rua', border: OutlineInputBorder()),
              validator: _obrigatorio,
            ),
            const SizedBox(height: 12),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SizedBox(
                  width: 110,
                  child: TextFormField(
                    controller: _numero,
                    focusNode: _focoNumero,
                    decoration: const InputDecoration(labelText: 'Número', border: OutlineInputBorder()),
                    validator: _obrigatorio,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: _complemento,
                    decoration: const InputDecoration(
                        labelText: 'Complemento (opcional)', border: OutlineInputBorder()),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _bairro,
              textCapitalization: TextCapitalization.words,
              decoration: const InputDecoration(labelText: 'Bairro', border: OutlineInputBorder()),
              validator: _obrigatorio,
            ),
            const SizedBox(height: 12),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: TextFormField(
                    controller: _cidade,
                    textCapitalization: TextCapitalization.words,
                    decoration: const InputDecoration(labelText: 'Cidade', border: OutlineInputBorder()),
                    validator: _obrigatorio,
                  ),
                ),
                const SizedBox(width: 12),
                SizedBox(
                  width: 80,
                  child: TextFormField(
                    controller: _uf,
                    textCapitalization: TextCapitalization.characters,
                    inputFormatters: [LengthLimitingTextInputFormatter(2)],
                    decoration: const InputDecoration(labelText: 'UF', border: OutlineInputBorder()),
                    validator: (v) => (v ?? '').trim().length == 2 ? null : 'UF',
                  ),
                ),
              ],
            ),
            CheckboxListTile(
              value: _principal,
              onChanged: (v) => setState(() => _principal = v ?? false),
              contentPadding: EdgeInsets.zero,
              controlAffinity: ListTileControlAffinity.leading,
              activeColor: Tema.vinho,
              title: const Text('Usar como endereço principal'),
            ),
            const SizedBox(height: 8),
            FilledButton(
              onPressed: _salvando ? null : _salvar,
              style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52), backgroundColor: Tema.vinho),
              child: _salvando
                  ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                  : const Text('Salvar endereço'),
            ),
          ],
        ),
      ),
    );
  }
}
