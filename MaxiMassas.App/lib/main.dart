import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/api_client.dart';
import 'core/tema.dart';
import 'providers/cardapio_provider.dart';
import 'screens/cardapio/cardapio_screen.dart';
import 'services/produto_service.dart';

void main() => runApp(const MaxiMassasApp());

class MaxiMassasApp extends StatelessWidget {
  const MaxiMassasApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        Provider<ApiClient>(create: (_) => ApiClient()),
        ChangeNotifierProvider<CardapioProvider>(
          create: (ctx) => CardapioProvider(ProdutoService(ctx.read<ApiClient>()))..carregar(),
        ),
      ],
      child: MaterialApp(
        title: 'Maxi Massas',
        debugShowCheckedModeBanner: false,
        theme: Tema.claro(),
        home: const CardapioScreen(),
      ),
    );
  }
}
