import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/tema.dart';

/// Moldura das telas de acesso: banner vinho com o logo e folha branca arredondada (como no Sistema Web).
class AcessoLayout extends StatelessWidget {
  const AcessoLayout({super.key, required this.child, this.comVoltar = false});

  final Widget child;
  final bool comVoltar;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Tema.vinho,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            ClipRect(
              child: Stack(
                children: [
                  Positioned(right: -50, top: -60, child: _circulo(170)),
                  Positioned(left: -40, bottom: -50, child: _circulo(110)),
                  SizedBox(
                    width: double.infinity,
                    child: Padding(
                      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
                      child: Column(
                        children: [
                          Image.asset(
                            'assets/logo.png',
                            height: 72,
                            errorBuilder: (c, e, s) => const SizedBox(height: 72),
                          ),
                          const SizedBox(height: 8),
                          const Text('Maxi Massas',
                              style: TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.w800)),
                          const Text('Itápolis', style: TextStyle(color: Colors.white70, fontSize: 13)),
                        ],
                      ),
                    ),
                  ),
                  if (comVoltar)
                    Positioned(
                      left: 4,
                      top: 4,
                      child: IconButton(
                        onPressed: () => Navigator.of(context).maybePop(),
                        icon: const Icon(Icons.arrow_back, color: Colors.white),
                        tooltip: 'Voltar',
                      ),
                    ),
                ],
              ),
            ),
            Expanded(
              child: Container(
                width: double.infinity,
                decoration: const BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.vertical(top: Radius.circular(32)),
                ),
                child: SingleChildScrollView(
                  keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
                  padding: const EdgeInsets.fromLTRB(24, 28, 24, 24),
                  child: child,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _circulo(double tamanho) => Container(
        width: tamanho,
        height: tamanho,
        decoration: BoxDecoration(shape: BoxShape.circle, color: Colors.white.withValues(alpha: 0.07)),
      );
}

/// Campo com rótulo acima, preenchimento azul-claro e sem borda (visual do login web).
class CampoAcesso extends StatelessWidget {
  const CampoAcesso({
    super.key,
    required this.controller,
    required this.rotulo,
    this.dica,
    this.teclado,
    this.ocultar = false,
    this.sufixo,
    this.validator,
    this.formatadores,
    this.autofill,
    this.acao = TextInputAction.next,
    this.aoEnviar,
  });

  final TextEditingController controller;
  final String rotulo;
  final String? dica;
  final TextInputType? teclado;
  final bool ocultar;
  final Widget? sufixo;
  final String? Function(String?)? validator;
  final List<TextInputFormatter>? formatadores;
  final Iterable<String>? autofill;
  final TextInputAction acao;
  final VoidCallback? aoEnviar;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(rotulo, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
          const SizedBox(height: 6),
          TextFormField(
            controller: controller,
            keyboardType: teclado,
            obscureText: ocultar,
            validator: validator,
            inputFormatters: formatadores,
            autofillHints: autofill,
            textInputAction: acao,
            onFieldSubmitted: aoEnviar == null ? null : (_) => aoEnviar!(),
            autocorrect: false,
            decoration: InputDecoration(
              hintText: dica,
              filled: true,
              fillColor: const Color(0xFFE8F0FE),
              suffixIcon: sufixo,
              contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide.none),
            ),
          ),
        ],
      ),
    );
  }
}

/// Botão grande vinho com indicador de carregamento.
class BotaoAcesso extends StatelessWidget {
  const BotaoAcesso({super.key, required this.texto, required this.aoTocar, this.carregando = false});

  final String texto;
  final VoidCallback? aoTocar;
  final bool carregando;

  @override
  Widget build(BuildContext context) {
    return FilledButton(
      onPressed: carregando ? null : aoTocar,
      style: FilledButton.styleFrom(
        minimumSize: const Size.fromHeight(52),
        backgroundColor: Tema.vinho,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
      child: carregando
          ? const SizedBox(
              width: 22,
              height: 22,
              child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white),
            )
          : Text(texto, style: const TextStyle(fontWeight: FontWeight.bold, letterSpacing: 0.5)),
    );
  }
}

/// Faixa de erro padrão das telas de acesso.
class MensagemErro extends StatelessWidget {
  const MensagemErro(this.texto, {super.key});

  final String texto;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: const Color(0xFFFDECEA), borderRadius: BorderRadius.circular(8)),
      child: Text(texto, textAlign: TextAlign.center, style: const TextStyle(color: Color(0xFF9E1B0F), fontSize: 13)),
    );
  }
}
