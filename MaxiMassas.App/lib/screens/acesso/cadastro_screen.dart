import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/api_client.dart';
import '../../core/mascaras.dart';
import '../../core/tema.dart';
import '../../providers/auth_provider.dart';
import 'acesso_layout.dart';

class CadastroScreen extends StatefulWidget {
  const CadastroScreen({super.key});

  @override
  State<CadastroScreen> createState() => _CadastroScreenState();
}

class _CadastroScreenState extends State<CadastroScreen> {
  final _form = GlobalKey<FormState>();
  final _nome = TextEditingController();
  final _telefone = TextEditingController();
  final _email = TextEditingController();
  final _senha = TextEditingController();
  final _confirmacao = TextEditingController();
  bool _ocultar = true;
  bool _aceitou = false;
  bool _carregando = false;
  String? _erro;

  @override
  void dispose() {
    for (final c in [_nome, _telefone, _email, _senha, _confirmacao]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _cadastrar() async {
    if (!_form.currentState!.validate()) return;
    setState(() {
      _carregando = true;
      _erro = null;
    });
    final navegador = Navigator.of(context);
    try {
      await context.read<AuthProvider>().cadastrar(
            nome: _nome.text,
            email: _email.text,
            telefone: soDigitos(_telefone.text),
            senha: _senha.text,
          );
      navegador.popUntil((r) => r.isFirst);
    } on ApiException catch (e) {
      if (mounted) setState(() => _erro = e.mensagem);
    } finally {
      if (mounted) setState(() => _carregando = false);
    }
  }

  void _verPrivacidade() {
    showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Termos e privacidade'),
        content: const Text(
          'Coletamos seu nome, telefone, e-mail e endereços de entrega apenas para receber, '
          'entregar e acompanhar seus pedidos na Maxi Massas Itápolis.\n\n'
          'Seus dados não são vendidos nem compartilhados com terceiros para outros fins. '
          'Você pode corrigir seus dados a qualquer momento no seu perfil.',
        ),
        actions: [TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Fechar'))],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AcessoLayout(
      comVoltar: true,
      child: Form(
        key: _form,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Criar conta', style: TextStyle(fontSize: 26, fontWeight: FontWeight.w800)),
            const SizedBox(height: 4),
            const Text('Preencha seus dados para pedir', style: TextStyle(color: Colors.black54)),
            const SizedBox(height: 22),
            CampoAcesso(
              controller: _nome,
              rotulo: 'Nome completo',
              autofill: const [AutofillHints.name],
              validator: (v) => (v == null || v.trim().length < 3) ? 'Informe seu nome.' : null,
            ),
            CampoAcesso(
              controller: _telefone,
              rotulo: 'Telefone (WhatsApp)',
              dica: '(16) 99999-9999',
              teclado: TextInputType.phone,
              formatadores: [MascaraTelefone()],
              autofill: const [AutofillHints.telephoneNumber],
              validator: (v) => soDigitos(v ?? '').length < 10 ? 'Informe o telefone com DDD.' : null,
            ),
            CampoAcesso(
              controller: _email,
              rotulo: 'E-mail',
              dica: 'seu@email.com',
              teclado: TextInputType.emailAddress,
              autofill: const [AutofillHints.email],
              validator: (v) => (v == null || !v.contains('@')) ? 'Informe um e-mail válido.' : null,
            ),
            CampoAcesso(
              controller: _senha,
              rotulo: 'Senha',
              ocultar: _ocultar,
              autofill: const [AutofillHints.newPassword],
              validator: (v) => (v == null || v.length < 8 || RegExp(r'[A-Za-z]').allMatches(v).isEmpty) ? 'A senha deve ter ao menos 8 caracteres e uma letra.' : null,
              sufixo: IconButton(
                onPressed: () => setState(() => _ocultar = !_ocultar),
                icon: Icon(_ocultar ? Icons.visibility_outlined : Icons.visibility_off_outlined),
              ),
            ),
            CampoAcesso(
              controller: _confirmacao,
              rotulo: 'Confirme a senha',
              ocultar: _ocultar,
              acao: TextInputAction.done,
              validator: (v) => v != _senha.text ? 'As senhas não conferem.' : null,
            ),
            Row(
              children: [
                Checkbox(
                  value: _aceitou,
                  activeColor: Tema.vinho,
                  onChanged: (v) => setState(() => _aceitou = v ?? false),
                ),
                Expanded(
                  child: GestureDetector(
                    onTap: _verPrivacidade,
                    child: const Text.rich(
                      TextSpan(
                        text: 'Li e aceito os ',
                        children: [
                          TextSpan(
                            text: 'termos e a política de privacidade',
                            style: TextStyle(color: Tema.vinho, decoration: TextDecoration.underline),
                          ),
                        ],
                      ),
                      style: TextStyle(fontSize: 13),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            if (_erro != null) MensagemErro(_erro!),
            BotaoAcesso(
              texto: 'CRIAR CONTA',
              aoTocar: _aceitou ? _cadastrar : null,
              carregando: _carregando,
            ),
          ],
        ),
      ),
    );
  }
}
