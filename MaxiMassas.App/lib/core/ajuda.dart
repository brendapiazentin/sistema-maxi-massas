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
