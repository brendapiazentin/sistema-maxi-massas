import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/ajuda.dart';
import '../../core/api_client.dart';
import '../../core/tema.dart';
import '../../models/cliente.dart';
import '../../providers/auth_provider.dart';
import '../../providers/carrinho_provider.dart';
import '../../providers/pedidos_provider.dart';
import '../../services/perfil_service.dart';
import '../enderecos/endereco_form_screen.dart';

class PerfilScreen extends StatelessWidget {
  const PerfilScreen({super.key});

  Future<void> _sair(BuildContext context) async {
    final carrinho = context.read<CarrinhoProvider>();
    final auth = context.read<AuthProvider>();
    final pedidos = context.read<PedidosProvider>();
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
      pedidos.limpar();
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
          const _Enderecos(),
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

/// Endereços salvos: adicionar, tornar principal e remover.
class _Enderecos extends StatelessWidget {
  const _Enderecos();

  Future<void> _executar(BuildContext context, Future<Cliente> Function(PerfilService s) acao) async {
    final auth = context.read<AuthProvider>();
    final mensageiro = ScaffoldMessenger.of(context);
    try {
      final cliente = await acao(PerfilService(context.read<ApiClient>()));
      auth.atualizarCliente(cliente);
    } on ApiException catch (e) {
      mensageiro.showSnackBar(SnackBar(content: Text(e.mensagem)));
    }
  }

  Future<void> _remover(BuildContext context, int id) async {
    final confirmou = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Remover endereço?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: FilledButton.styleFrom(backgroundColor: Tema.vinho),
            child: const Text('Remover'),
          ),
        ],
      ),
    );
    if (confirmou == true && context.mounted) await _executar(context, (s) => s.removerEndereco(id));
  }

  @override
  Widget build(BuildContext context) {
    final enderecos = context.watch<AuthProvider>().cliente?.enderecos ?? const [];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Padding(
          padding: EdgeInsets.fromLTRB(16, 16, 16, 4),
          child: Text('Meus endereços', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
        ),
        for (final e in enderecos)
          ListTile(
            leading: Icon(e.principal ? Icons.home : Icons.location_on_outlined,
                color: e.principal ? Tema.vinho : null),
            title: Text(e.linha1),
            subtitle: Text(e.principal ? '${e.linha2}\nPrincipal' : e.linha2),
            isThreeLine: e.principal,
            trailing: PopupMenuButton<String>(
              onSelected: (op) {
                if (op == 'principal') _executar(context, (s) => s.definirPrincipal(e.id!));
                if (op == 'remover') _remover(context, e.id!);
              },
              itemBuilder: (_) => [
                if (!e.principal) const PopupMenuItem(value: 'principal', child: Text('Tornar principal')),
                const PopupMenuItem(value: 'remover', child: Text('Remover')),
              ],
            ),
          ),
        ListTile(
          leading: const Icon(Icons.add, color: Tema.vinho),
          title: const Text('Adicionar endereço', style: TextStyle(color: Tema.vinho)),
          onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const EnderecoFormScreen())),
        ),
      ],
    );
  }
}
