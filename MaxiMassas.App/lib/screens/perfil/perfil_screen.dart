import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/ajuda.dart';
import '../../core/tema.dart';
import '../../providers/auth_provider.dart';
import '../../providers/carrinho_provider.dart';

class PerfilScreen extends StatelessWidget {
  const PerfilScreen({super.key});

  Future<void> _sair(BuildContext context) async {
    final carrinho = context.read<CarrinhoProvider>();
    final auth = context.read<AuthProvider>();
    final confirmou = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Sair da conta?'),
        content: const Text('Você precisará entrar novamente para fazer pedidos.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: FilledButton.styleFrom(backgroundColor: Tema.vinho),
            child: const Text('Sair'),
          ),
        ],
      ),
    );
    if (confirmou == true) {
      carrinho.limpar();
      await auth.sair();
    }
  }

  @override
  Widget build(BuildContext context) {
    final cliente = context.watch<AuthProvider>().cliente;
    final nome = (cliente?.nome ?? '').trim();
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Tema.vinho,
        foregroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        title: const Text('Meu perfil'),
      ),
      body: ListView(
        children: [
          Padding(
            padding: const EdgeInsets.all(24),
            child: Row(
              children: [
                CircleAvatar(
                  radius: 30,
                  backgroundColor: Tema.vinho,
                  child: Text(
                    nome.isEmpty ? '?' : nome[0].toUpperCase(),
                    style: const TextStyle(color: Colors.white, fontSize: 26, fontWeight: FontWeight.bold),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(nome.isEmpty ? 'Cliente' : nome,
                          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                      if ((cliente?.email ?? '').isNotEmpty)
                        Text(cliente!.email, style: const TextStyle(color: Colors.black54)),
                      if ((cliente?.telefone ?? '').isNotEmpty)
                        Text(cliente!.telefone, style: const TextStyle(color: Colors.black54)),
                    ],
                  ),
                ),
              ],
            ),
          ),
          Divider(height: 1, color: Colors.grey.shade300),
          ListTile(
            leading: const Icon(Icons.help_outline),
            title: const Text('Precisa de ajuda?'),
            subtitle: const Text('Falar com a loja pelo WhatsApp'),
            onTap: () => abrirWhatsAppLoja(context),
          ),
          Divider(height: 1, color: Colors.grey.shade300),
          ListTile(
            leading: const Icon(Icons.logout, color: Tema.vinho),
            title: const Text('Sair', style: TextStyle(color: Tema.vinho)),
            onTap: () => _sair(context),
          ),
        ],
      ),
    );
  }
}
