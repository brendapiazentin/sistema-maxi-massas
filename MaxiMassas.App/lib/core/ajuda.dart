import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

const _whatsappLoja = '5516997185628';

Future<void> abrirWhatsAppLoja(BuildContext context) async {
  final uri = Uri.parse('https://wa.me/$_whatsappLoja?text=${Uri.encodeComponent('Olá! Preciso de ajuda com meu pedido.')}');
  final ok = await launchUrl(uri, mode: LaunchMode.externalApplication);
  if (!ok && context.mounted) {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Não foi possível abrir o WhatsApp.')),
    );
  }
}

Future<void> mostrarHorariosLoja(BuildContext context) {
  const dias = [
    ('Segunda a sexta', '10h às 20h', '18h às 20h'),
    ('Sábado', '10h às 20h', '13h às 20h'),
    ('Domingo', '10h às 13h', '10h às 13h'),
  ];
  return showDialog<void>(
    context: context,
    builder: (ctx) {
      final tema = Theme.of(ctx);
      return AlertDialog(
        title: const Text('Horários de funcionamento'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (final (dia, loja, entrega) in dias) ...[
                Text(dia, style: tema.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                const SizedBox(height: 2),
                Text('Loja aberta e retirada: $loja'),
                Text('Entrega: $entrega'),
                const SizedBox(height: 12),
              ],
              Text(
                'O horário de cada pedido é escolhido ao finalizar a compra.',
                style: tema.textTheme.bodySmall,
              ),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Fechar')),
        ],
      );
    },
  );
}
