import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../core/config.dart';
import '../core/tema.dart';

/// Só faz sentido na versão web aberta num navegador Android (o app instalado e o iPhone não veem o aviso).
bool get deveMostrarBannerAndroid => kIsWeb && defaultTargetPlatform == TargetPlatform.android;

class BannerAndroid extends StatelessWidget {
  const BannerAndroid({super.key, this.aoFechar, this.margem = EdgeInsets.zero});

  final VoidCallback? aoFechar;
  final EdgeInsetsGeometry margem;

  Future<void> _baixar(BuildContext context) async {
    final mensageiro = ScaffoldMessenger.of(context);
    final ok = await launchUrl(Uri.parse(Config.urlApk), webOnlyWindowName: '_self');
    if (!ok) {
      mensageiro.showSnackBar(const SnackBar(content: Text('Não foi possível iniciar o download.')));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: margem,
      child: Container(
        padding: EdgeInsets.fromLTRB(14, 10, aoFechar == null ? 12 : 4, 10),
        decoration: BoxDecoration(color: Tema.vinho, borderRadius: BorderRadius.circular(12)),
        child: Row(
          children: [
            const Icon(Icons.android, color: Colors.white),
            const SizedBox(width: 10),
            const Expanded(
              child: Text(
                'Possui Android? Baixe o nosso aplicativo!',
                style: TextStyle(color: Colors.white, fontWeight: FontWeight.w600, fontSize: 13),
              ),
            ),
            const SizedBox(width: 8),
            FilledButton(
              onPressed: () => _baixar(context),
              style: FilledButton.styleFrom(
                backgroundColor: Colors.white,
                foregroundColor: Tema.vinho,
                visualDensity: VisualDensity.compact,
              ),
              child: const Text('Baixar', style: TextStyle(fontWeight: FontWeight.bold)),
            ),
            if (aoFechar != null)
              IconButton(
                onPressed: aoFechar,
                icon: const Icon(Icons.close, color: Colors.white, size: 20),
                tooltip: 'Fechar',
              ),
          ],
        ),
      ),
    );
  }
}