import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/api_client.dart';
import 'core/tema.dart';
import 'providers/auth_provider.dart';
import 'providers/cardapio_provider.dart';
import 'providers/carrinho_provider.dart';
import 'screens/acesso/login_screen.dart';
import 'screens/cardapio/cardapio_screen.dart';
import 'services/auth_service.dart';
import 'services/entrega_service.dart';
import 'services/produto_service.dart';

void main() => runApp(const MaxiMassasApp());

class MaxiMassasApp extends StatelessWidget {
  const MaxiMassasApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        Provider<ApiClient>(create: (_) => ApiClient()),
        ChangeNotifierProvider<AuthProvider>(
          create: (ctx) => AuthProvider(AuthService(ctx.read<ApiClient>()))..iniciar(),
        ),
        ChangeNotifierProvider<CardapioProvider>(
          create: (ctx) => CardapioProvider(ProdutoService(ctx.read<ApiClient>()))..carregar(),
        ),
        ChangeNotifierProvider<CarrinhoProvider>(
          create: (ctx) => CarrinhoProvider(EntregaService(ctx.read<ApiClient>()))..carregarConfigEntrega(),
        ),
      ],
      child: MaterialApp(
        title: 'Maxi Massas',
        debugShowCheckedModeBanner: false,
        theme: Tema.claro(),
        home: const _Porteiro(),
      ),
    );
  }
}

/// Decide a primeira tela: abertura, login ou cardápio.
class _Porteiro extends StatelessWidget {
  const _Porteiro();

  @override
  Widget build(BuildContext context) {
    switch (context.watch<AuthProvider>().estado) {
      case EstadoAuth.iniciando:
        return Scaffold(
          backgroundColor: Tema.vinho,
          body: Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Image.asset('assets/logo.png', height: 96, errorBuilder: (c, e, s) => const SizedBox(height: 96)),
                const SizedBox(height: 24),
                const CircularProgressIndicator(color: Colors.white),
              ],
            ),
          ),
        );
      case EstadoAuth.deslogado:
        return const LoginScreen();
      case EstadoAuth.logado:
        return const CardapioScreen();
    }
  }
}
