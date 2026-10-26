import 'package:flutter/material.dart';

import '../../core/tema.dart';
import '../../widgets/barra_carrinho.dart';
import '../cardapio/cardapio_screen.dart';
import '../pedidos/pedidos_screen.dart';
import '../perfil/perfil_screen.dart';

/// Estrutura principal depois do login: três abas na barra inferior.
class HomeShell extends StatefulWidget {
  const HomeShell({super.key});

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  int _aba = 0;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _aba,
        children: const [CardapioScreen(), PedidosScreen(), PerfilScreen()],
      ),
      bottomNavigationBar: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const BarraCarrinho(),
          NavigationBar(
            selectedIndex: _aba,
            onDestinationSelected: (i) => setState(() => _aba = i),
            backgroundColor: Colors.white,
            surfaceTintColor: Colors.transparent,
            indicatorColor: Tema.vinho.withValues(alpha: 0.12),
            destinations: const [
              NavigationDestination(
                icon: Icon(Icons.storefront_outlined),
                selectedIcon: Icon(Icons.storefront, color: Tema.vinho),
                label: 'Início',
              ),
              NavigationDestination(
                icon: Icon(Icons.receipt_long_outlined),
                selectedIcon: Icon(Icons.receipt_long, color: Tema.vinho),
                label: 'Pedidos',
              ),
              NavigationDestination(
                icon: Icon(Icons.person_outline),
                selectedIcon: Icon(Icons.person, color: Tema.vinho),
                label: 'Perfil',
              ),
            ],
          ),
        ],
      ),
    );
  }
}
