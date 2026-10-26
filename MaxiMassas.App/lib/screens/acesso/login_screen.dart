import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/api_client.dart';
import '../../core/tema.dart';
import '../../providers/auth_provider.dart';
import 'acesso_layout.dart';
import 'cadastro_screen.dart';
import 'esqueci_senha_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _form = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _senha = TextEditingController();
  bool _ocultar = true;
  bool _carregando = false;
  String? _erro;

  @override
  void dispose() {
    _email.dispose();
    _senha.dispose();
    super.dispose();
  }

  Future<void> _entrar() async {
    if (!_form.currentState!.validate()) return;
    setState(() {
      _carregando = true;
      _erro = null;
    });
    try {
      await context.read<AuthProvider>().entrar(_email.text, _senha.text);
    } on ApiException catch (e) {
      if (mounted) setState(() => _erro = e.mensagem);
    } finally {
      if (mounted) setState(() => _carregando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AcessoLayout(
      child: Form(
        key: _form,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Bem-vindo(a)!', style: TextStyle(fontSize: 26, fontWeight: FontWeight.w800)),
            const SizedBox(height: 4),
            const Text('Entre para fazer seu pedido', style: TextStyle(color: Colors.black54)),
            const SizedBox(height: 22),
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
              dica: '••••••••',
              ocultar: _ocultar,
              autofill: const [AutofillHints.password],
              acao: TextInputAction.done,
              aoEnviar: _entrar,
              validator: (v) => (v == null || v.isEmpty) ? 'Informe sua senha.' : null,
              sufixo: IconButton(
                onPressed: () => setState(() => _ocultar = !_ocultar),
                icon: Icon(_ocultar ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                tooltip: _ocultar ? 'Mostrar senha' : 'Ocultar senha',
              ),
            ),
            Align(
              alignment: Alignment.centerRight,
              child: TextButton(
                onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const EsqueciSenhaScreen())),
                style: TextButton.styleFrom(foregroundColor: Tema.vinho),
                child: const Text('Esqueci minha senha'),
              ),
            ),
            if (_erro != null) MensagemErro(_erro!),
            BotaoAcesso(texto: 'ENTRAR', aoTocar: _entrar, carregando: _carregando),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Text('Ainda não tem conta?'),
                TextButton(
                  onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const CadastroScreen())),
                  style: TextButton.styleFrom(foregroundColor: Tema.vinho),
                  child: const Text('Criar conta', style: TextStyle(fontWeight: FontWeight.bold)),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
