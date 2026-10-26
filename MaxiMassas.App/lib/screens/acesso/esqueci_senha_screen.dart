import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/api_client.dart';
import '../../providers/auth_provider.dart';
import 'acesso_layout.dart';

class EsqueciSenhaScreen extends StatefulWidget {
  const EsqueciSenhaScreen({super.key});

  @override
  State<EsqueciSenhaScreen> createState() => _EsqueciSenhaScreenState();
}

class _EsqueciSenhaScreenState extends State<EsqueciSenhaScreen> {
  final _form = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _codigo = TextEditingController();
  final _novaSenha = TextEditingController();
  bool _codigoEnviado = false;
  bool _ocultar = true;
  bool _carregando = false;
  String? _erro;

  @override
  void dispose() {
    _email.dispose();
    _codigo.dispose();
    _novaSenha.dispose();
    super.dispose();
  }

  Future<void> _executar(Future<void> Function() acao) async {
    if (!_form.currentState!.validate()) return;
    setState(() {
      _carregando = true;
      _erro = null;
    });
    try {
      await acao();
    } on ApiException catch (e) {
      if (mounted) setState(() => _erro = e.mensagem);
    } finally {
      if (mounted) setState(() => _carregando = false);
    }
  }

  Future<void> _enviarCodigo() => _executar(() async {
        await context.read<AuthProvider>().servico.esqueciSenha(_email.text);
        if (mounted) setState(() => _codigoEnviado = true);
      });

  Future<void> _redefinir() => _executar(() async {
        final navegador = Navigator.of(context);
        final mensageiro = ScaffoldMessenger.of(context);
        await context.read<AuthProvider>().servico.redefinirSenha(
              email: _email.text,
              codigo: _codigo.text,
              novaSenha: _novaSenha.text,
            );
        mensageiro.showSnackBar(const SnackBar(content: Text('Senha alterada. Entre com a nova senha.')));
        navegador.pop();
      });

  @override
  Widget build(BuildContext context) {
    return AcessoLayout(
      comVoltar: true,
      child: Form(
        key: _form,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Recuperar senha', style: TextStyle(fontSize: 26, fontWeight: FontWeight.w800)),
            const SizedBox(height: 4),
            Text(
              _codigoEnviado
                  ? 'Enviamos um código de 6 dígitos para o seu e-mail. Ele vale por 15 minutos. '
                      'Se não encontrar, verifique a caixa de spam.'
                  : 'Informe o e-mail da sua conta para receber um código.',
              style: const TextStyle(color: Colors.black54),
            ),
            const SizedBox(height: 22),
            CampoAcesso(
              controller: _email,
              rotulo: 'E-mail',
              teclado: TextInputType.emailAddress,
              acao: _codigoEnviado ? TextInputAction.next : TextInputAction.done,
              aoEnviar: _codigoEnviado ? null : _enviarCodigo,
              validator: (v) => (v == null || !v.contains('@')) ? 'Informe um e-mail válido.' : null,
            ),
            if (_codigoEnviado) ...[
              CampoAcesso(
                controller: _codigo,
                rotulo: 'Código recebido',
                dica: '123456',
                teclado: TextInputType.number,
                validator: (v) => (v == null || v.trim().length != 6) ? 'O código tem 6 dígitos.' : null,
              ),
              CampoAcesso(
                controller: _novaSenha,
                rotulo: 'Nova senha',
                ocultar: _ocultar,
                acao: TextInputAction.done,
                aoEnviar: _redefinir,
                validator: (v) => (v == null || v.length < 8 || RegExp(r'[A-Za-z]').allMatches(v).isEmpty) ? 'A senha deve ter ao menos 8 caracteres e uma letra.' : null,
                sufixo: IconButton(
                  onPressed: () => setState(() => _ocultar = !_ocultar),
                  icon: Icon(_ocultar ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                ),
              ),
            ],
            if (_erro != null) MensagemErro(_erro!),
            BotaoAcesso(
              texto: _codigoEnviado ? 'REDEFINIR SENHA' : 'ENVIAR CÓDIGO',
              aoTocar: _codigoEnviado ? _redefinir : _enviarCodigo,
              carregando: _carregando,
            ),
            if (_codigoEnviado)
              TextButton(
                onPressed: _carregando ? null : _enviarCodigo,
                child: const Text('Reenviar código'),
              ),
          ],
        ),
      ),
    );
  }
}
